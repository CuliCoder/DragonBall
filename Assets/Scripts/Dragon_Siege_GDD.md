# DRAGON SIEGE — Game Design Document (GDD)
**Phiên bản:** 1.1  
**Ngày:** Tháng 4/2026  
**Tác giả:** Dev team  

---

## 1. TỔNG QUAN

### Mô tả game
Dragon Siege là game multiplayer action RPG thể loại PvE Boss Raid chơi theo phòng. Nhiều người chơi cùng hợp sức đánh hạ các boss khổng lồ trong các map chiến đấu khác nhau. Boss càng mạnh thì loot càng xịn. Cảm giác gameplay gần với Ngọc Rồng Online — săn boss, nhặt loot, cày cuốc tiến hóa nhân vật.

### Thông số cơ bản
| Hạng mục | Chi tiết |
|---|---|
| Thể loại | Multiplayer Action RPG — PvE Boss Raid |
| Số người / phòng | 2–6 người |
| Thời gian 1 trận | 3–7 phút |
| Platform giai đoạn 1 | Web browser (HTML + JS) |
| Platform giai đoạn 2 | Unity WebGL — host trên itch.io |
| Server | ASP.NET Core (C#) + WebSocket + MySQL + Redis |

### Vibe & cảm xúc mục tiêu
- Hào hứng khi boss phase change — tim đập nhanh hơn
- Hả hê khi boss chết sau trận đánh căng thẳng
- Tham lam khi thấy loot S hiếm rớt ra
- Cạnh tranh lành mạnh — luôn nhìn bảng damage xem ai đang dẫn đầu
- Muốn chơi thêm 1 trận nữa ngay sau khi kết thúc

---

## 2. CORE GAMEPLAY LOOP

```
Đăng ký → Chọn class 1 lần duy nhất → Tạo tài khoản
        ↓
Đăng nhập
        ↓
Vào Lobby — tạo phòng hoặc join bằng mã phòng
        ↓
Chọn Map (hoặc Random)
        ↓
Tất cả nhấn Ready → Boss spawn
        ↓
[VÒNG LẶP CHIẾN ĐẤU]
  Di chuyển trên map để tránh đòn / áp sát boss
  Chọn skill tấn công khi cooldown hết
  Server tính damage → trừ HP boss → broadcast cho tất cả
  Boss tấn công theo timer → chọn mục tiêu theo phase
  Người bị đánh có thể dùng skill phòng thủ / uống thuốc
  Người chết → chờ đồng đội nhặt Linh hồn thạch để hồi sinh
        ↓
Boss chết HOẶC toàn team chết
        ↓
Màn hình kết quả: bảng damage, loot drop
        ↓
Nhận loot → cất vào inventory
        ↓
Chơi lại (đổi boss / đổi map) HOẶC thoát phòng
```

---

## 3. CLASS NHÂN VẬT

Class được chọn **1 lần duy nhất khi đăng ký tài khoản** — không thể đổi sau đó. Mỗi tài khoản gắn cứng với 1 class, tạo cảm giác gắn bó và bản sắc riêng cho từng người chơi.

**Lưu ý đội hình:** Phòng chơi hiển thị cảnh báo nhẹ nếu thiếu Warrior hoặc Healer — không cấm vào nhưng nhắc nhở team cân nhắc chiến thuật.

### ⚔️ Warrior — Chiến binh
| Chỉ số | Giá trị |
|---|---|
| HP | 1500 (cao nhất) |
| Base damage | 80 |
| Defense | 30 |

**Skill đặc biệt — Provoke (Khiêu khích):**
Warrior hét vào mặt boss, ép boss tập trung tấn công mình thay vì đồng đội trong 6 giây. Trong thời gian đó Warrior nhận thêm 30% damage nhưng bảo vệ toàn team.

**Playstyle:** Tank — lao vào gần boss, hứng đòn, dùng Provoke khi boss sắp xử đồng đội yếu hơn.

---

### 🔮 Mage — Pháp sư
| Chỉ số | Giá trị |
|---|---|
| HP | 700 (thấp nhất) |
| Base damage | 180 |
| Defense | 10 |

**Skill đặc biệt — Burst (Bùng nổ):**
Mage charge năng lượng trong 2 lượt liên tiếp không được tấn công. Lượt thứ 3 giải phóng toàn bộ, gây damage x3.5. Rủi ro cao — nếu boss chọn Mage trong lúc đang charge thì cực kỳ nguy hiểm vì HP thấp.

**Playstyle:** Glass cannon — đứng xa boss, tối đa hóa damage, phụ thuộc Warrior tank và Healer đỡ đòn.

---

### 🌿 Healer — Thầy thuốc
| Chỉ số | Giá trị |
|---|---|
| HP | 1000 |
| Base damage | 50 |
| Defense | 20 |

**Skill đặc biệt — Shield (Khiên hộ mệnh):**
Tạo khiên bảo vệ cho 1 đồng đội được chỉ định, hấp thụ hoàn toàn 1 đòn tấn công tiếp theo của boss. Cooldown 12 giây.

**Playstyle:** Hỗ trợ — đứng sau, theo dõi HP đồng đội, quyết định ai cần khiên, hồi sinh người chết.

**Tính điểm Healer:** Heal lượng được quy đổi thành damage tương đương khi tính loot — Healer không bị thiệt thòi dù damage thấp.

---

## 4. HỆ THỐNG CHIẾN ĐẤU

### Di chuyển
Người chơi di chuyển tự do trong map bằng WASD (web) hoặc joystick (Unity). Vị trí ảnh hưởng trực tiếp đến gameplay:

- Warrior cần đứng gần boss để tank
- Mage đứng xa để an toàn khi charge
- Healer đứng sau để nhìn bao quát team
- Một số skill boss chỉ đánh theo vùng — tránh được nếu di chuyển kịp

**Server xử lý movement:**
- Client gửi input di chuyển lên server
- Server tính vị trí mới, check collision với tường và boss hitbox
- Broadcast vị trí tất cả người chơi xuống client 20 lần/giây (50ms)

### Skill cơ bản (tất cả class đều có)
| Skill | Damage | Cooldown | Ghi chú |
|---|---|---|---|
| Tấn công thường | Base × 1.0 | Không cooldown | Dùng được liên tục |
| Tấn công mạnh | Base × 2.2 | 5 giây | Knockback nhỏ |
| Uống thuốc | Hồi 200 HP | 15 giây | Tối đa 3 lần/trận |
| Skill đặc biệt | Khác nhau theo class | 10–15 giây | Xem mục Class |

### Công thức tính damage
```
Final Damage = (Base damage của class × Hệ số skill × Random 0.85–1.15) − Defense boss
```

Nếu kết quả < 0 → tính là 1 damage (luôn gây được tối thiểu 1 damage).

### Critical Hit
Mỗi đòn đánh có 10% cơ hội critical — gây damage x1.8, số damage hiện màu vàng to hơn bình thường, kèm âm thanh "CRACK" đặc biệt.

### Hồi sinh
Khi người chơi HP về 0:
- Màn hình tối lại, chuyển sang chế độ xem trận (không điều khiển được)
- Boss có % nhỏ drop **Linh hồn thạch** khi bị đánh đủ damage mốc
- Đồng đội nhặt Linh hồn thạch → chạy đến xác → giữ nút E trong 2 giây để hồi sinh
- Người được hồi sinh sống lại với 30% HP tối đa — cần uống thuốc ngay

---

## 5. HỆ THỐNG MAP

### Chọn map
Khi tạo phòng, host chọn 1 trong các map hoặc bật Random. Map ảnh hưởng đến cả boss lẫn người chơi.

### Danh sách map giai đoạn 1

**Hang động tối**
- Tầm nhìn bị giới hạn bởi bóng tối — chỉ thấy rõ xung quanh nhân vật
- Boss ẩn náu được ở góc tối, xuất hiện bất ngờ
- Boss nhận thêm 15% dodge (có thể tránh được đòn)
- Màu sắc: xanh đen, mờ ảo

**Đồng bằng lửa**
- Map rộng, không có chướng ngại vật — không có chỗ ẩn nấp
- Nền đất có các vết nứt phun lửa ngẫu nhiên, đứng vào mất HP nhỏ
- Tất cả sát thương +20% (cả người chơi lẫn boss)
- Màu sắc: đỏ cam, nóng bức

**Rừng cổ thụ**
- Nhiều cây lớn làm chướng ngại vật — có thể ẩn sau cây tránh đòn boss
- Healer được buff +30% lượng heal
- Boss di chuyển chậm hơn 15% vì vướng cây
- Màu sắc: xanh lá đậm, huyền bí

**Đỉnh núi băng**
- Nền băng trơn — người chơi có quán tính khi di chuyển, dừng chậm hơn
- Boss có thể dùng skill đóng băng người chơi 2 giây
- Tất cả di chuyển chậm hơn 20%
- Màu sắc: trắng xanh, lạnh lẽo

---

## 6. BOSS DESIGN

### Cơ chế chung
- Boss có 3 phase dựa theo % HP còn lại
- Mỗi phase boss đổi màu, đổi skill, đổi AI target
- Khi chuyển phase: màn hình rung nhẹ + âm thanh đặc biệt + thông báo nổi lên

### Boss 1 — Rồng Đỏ (Boss cơ bản)

**Phase 1 — HP 100% → 60% (Bình thường)**
- AI: tấn công ngẫu nhiên vào bất kỳ người chơi nào
- Skill *Claw Strike*: Đánh 1 người, damage trung bình, timer 4 giây
- Skill *Roar*: Gầm vang, giảm damage tất cả người chơi 20% trong 5 giây, timer 15 giây
- Màu sắc boss: đỏ bình thường

**Phase 2 — HP 60% → 30% (Cuồng nộ)**
- AI: ưu tiên tấn công người đang dẫn đầu bảng damage
- Skill *Fury Strike*: Đánh người damage cao nhất, damage x1.5 so với Phase 1, timer 3 giây
- Skill *Tail Sweep*: Quét đuôi đánh TẤT CẢ người đứng trong vòng bán kính 3m, damage thấp, timer 8 giây
- Màu sắc boss: đỏ đậm hơn, mắt phát sáng
- Tốc độ di chuyển boss tăng 20%

**Phase 3 — HP 30% → 0% (Cuồng loạn)**
- AI: tấn công 2 người cùng lúc, ưu tiên người HP thấp nhất
- Skill *Death Breath*: Phun lửa theo đường thẳng, đánh tất cả người đứng trên đường đó, damage rất cao, timer 6 giây
- Skill *Enrage*: Tự buff thêm 30% damage trong 8 giây, timer 20 giây
- **Berserk Mode:** Nếu không giết boss trong 60 giây ở Phase 3 → boss Berserk, tốc độ tấn công x2, damage x2, gần như 1-hit kill tất cả
- Màu sắc boss: đỏ đen, toàn thân bốc khói

### Boss 2 — Rồng Băng (Boss khó hơn)
*(Mở khóa sau khi đánh thắng Rồng Đỏ 3 lần)*
- Phase 1: Đánh ngẫu nhiên + skill đóng băng 1 người 3 giây
- Phase 2: Tấn công người HP thấp nhất + gọi thêm 2 tiểu quái cản đường
- Phase 3: Đóng băng toàn bộ map 4 giây — ai không uống thuốc kháng băng thì không di chuyển được

### Boss đặc biệt — Rồng Huyền Thoại (Săn được Đệ tử)
*(Phát triển sau — giai đoạn 2)*
- Xuất hiện ngẫu nhiên khi tất cả người chơi trong phòng đã thắng Rồng Băng ít nhất 5 lần
- Cực kỳ khó, cần phối hợp chặt chẽ
- Khi chết: 30% cơ hội drop **Trứng Rồng Huyền Thoại** — ấp xong có đệ tử rồng đi theo chiến đấu

---

## 7. HỆ THỐNG LOOT

### Phân loại loot theo hạng
| Hạng | Màu sắc | Tỉ lệ drop | Điều kiện |
|---|---|---|---|
| S | Vàng ánh kim | 5% | Người damage cao nhất |
| A | Tím | 20% | Top 2–3 damage |
| B | Xanh | 50% | Tất cả người chơi |
| C | Trắng | Luôn có | Tất cả người chơi |

### Loại vật phẩm
**Vũ khí** — tăng base damage theo % hoặc giá trị cố định

**Giáp** — tăng HP tối đa và defense

**Bùa hộ mệnh** — giảm cooldown tất cả skill

**Linh hồn thạch** — dùng để hồi sinh đồng đội, không stack quá 3 cái

**Thuốc hồi HP** — hồi 300 HP ngay lập tức, dùng trong trận (thay thế slot thuốc hiện tại)

**Mảnh ghép** — thu thập 3 mảnh cùng loại để tổng hợp 1 item hạng A hoặc S

**Trứng Đệ tử** *(phát triển sau)* — ấp sau 5 trận thắng, nhận đệ tử tương ứng với boss đã săn

### Inventory
- Mỗi nhân vật giữ tối đa 20 item
- Nếu đầy khi nhận loot → chọn bỏ item cũ hoặc bỏ item mới
- Có thể trang bị tối đa: 1 vũ khí + 1 giáp + 1 bùa hộ mệnh
- Item không trang bị thì nằm trong túi, có thể hoán đổi bất cứ lúc nào ngoài trận

---

## 8. HỆ THỐNG ÂM THANH

Âm thanh là yếu tố quan trọng nhất tạo cảm giác cuốn. Mỗi hành động phải có âm thanh phản hồi rõ ràng.

### Âm thanh chiến đấu
| Sự kiện | Âm thanh |
|---|---|
| Tấn công thường | Tiếng kiếm chém crispy, gọn, sắc |
| Tấn công mạnh | Tiếng nổ to hơn, có echo nhỏ |
| Critical hit | Tiếng "CRACK" sắc bén + số damage vàng to hơn |
| Boss bị đánh | Tiếng gầm đau đớn ngắn — khác nhau theo phase |
| Boss tấn công | Tiếng gió rít trước đòn 0.3 giây — cảnh báo sắp bị ăn đòn |
| Nhận damage | Tiếng "thịch" + viền đỏ chớp quanh màn hình |
| Nhân vật chết | Tiếng rơi xuống + nhạc nền nhỏ lại đột ngột |
| Dodge thành công | Tiếng swoosh + nhân vật lướt nhanh |
| Uống thuốc | Tiếng "glug glug" + hiệu ứng xanh lá nổi lên |

### Âm thanh boss phase change
**Chuyển sang Phase 2:**
- Tiếng gầm dài, vang vọng
- Nhạc nền đổi sang bản nhanh hơn, căng hơn ngay lập tức
- Thông báo "PHASE 2 — RỒNG CUỒNG NỘ" nổi lên giữa màn hình

**Chuyển sang Phase 3:**
- Tiếng gầm cực to, như sấm
- Màn hình tối lại 0.5 giây
- Nhạc boss final phase bắt đầu — dồn dập, cảm giác khẩn cấp
- Thông báo "PHASE 3 — CUỒNG LOẠN" + màn hình rung 1 giây

**Berserk Mode:**
- Tiếng nhạc tắt đột ngột 0.5 giây
- Tiếng gầm khác hoàn toàn — trầm, nguy hiểm
- Nhạc Berserk bắt đầu — tông tối, nhanh, áp lực cao

### Âm thanh loot
| Hạng loot | Âm thanh | Hiệu ứng visual |
|---|---|---|
| C | Tiếng ding nhỏ | Ánh sáng trắng nhỏ |
| B | Tiếng ding to hơn | Ánh sáng xanh nhỏ |
| A | Tiếng "TING" vang | Particle tím bay ra |
| S | Tiếng "TING" đặc biệt + reverb | Toàn màn hình flash vàng 0.2 giây + particle vàng dày đặc |
| Rare drop | Âm thanh hoàn toàn khác — thiêng liêng | Màn hình flash trắng toàn bộ + nhạc jingle ngắn |

### Nhạc nền theo trạng thái
| Trạng thái | Nhạc |
|---|---|
| Lobby | Nhạc chill, nhẹ nhàng, có tiếng đàn tranh |
| Phase 1 | Action vừa phải, hào hứng, nhịp đều |
| Phase 2 | Nhanh hơn, căng hơn, thêm trống |
| Phase 3 | Dồn dập, epic, cảm giác phải kết thúc ngay |
| Boss chết | Fanfare thắng trận ngắn → nhạc victory nhẹ nhàng |
| Toàn team chết | Nhạc tắt đột ngột → tiếng boss gầm chiến thắng → nhạc thua buồn |
| Berserk Mode | Nhạc tối, áp lực, cảm giác tuyệt vọng |

---

## 9. UI / UX

### Màn hình chiến đấu — layout
```
[Thanh HP Boss — to, rõ, ở trên cùng, giảm realtime cho tất cả]
[Tên boss + Phase hiện tại]

[Khu vực map — nhân vật di chuyển, boss di chuyển]

[Bảng damage realtime]     [HP từng người chơi]
[Ai đang dẫn đầu]         [Warrior: ████░░ 800/1500]
[1. Player A — 1250]       [Mage:    ████░░ 400/700]
[2. Player B — 980]        [Healer:  ███░░░ 600/1000]

[Skill buttons với cooldown timer]
[Tấn công] [Tấn công mạnh] [Skill đặc biệt] [Uống thuốc]
```

### Chi tiết UI elements
**Thanh HP boss:**
- Chiều rộng 80% màn hình, đặt ở trên cùng
- Đổi màu theo phase: xanh lá (Phase 1) → vàng (Phase 2) → đỏ (Phase 3)
- Có animation smooth khi giảm — không nhảy số đột ngột

**Damage number:**
- Bay lên từ vị trí boss bị đánh, fade out sau 1 giây
- Màu trắng: damage thường
- Màu vàng + to hơn: critical hit
- Màu đỏ: damage nhận vào (player bị đánh)
- Màu xanh lá: heal

**Skill buttons:**
- Khi đang cooldown: nút tối đi + số đếm ngược hiển thị
- Khi ready: nút sáng lên, có pulse nhỏ
- Khi dùng: animation nhấn + âm thanh

**Chat nhanh (Quick Chat):**
Không cần gõ — 4 nút preset:
- "Heal me!" — Healer biết ai cần heal
- "Focus boss!" — nhắc team tập trung
- "Đợi tôi!" — đang charge Burst
- "GG" — sau trận

---

## 10. FLOW MÀN HÌNH

```
Màn hình đăng ký
├── Nhập username + password
└── Chọn class (Warrior / Mage / Healer) — 1 lần duy nhất, không đổi được
        ↓
Màn hình đăng nhập
        ↓
Lobby chính — hiển thị class và tên nhân vật của mình
├── Tạo phòng mới → chọn map → chờ người vào
└── Join phòng → nhập mã phòng 6 ký tự
        ↓
Màn hình phòng chờ
├── Danh sách người chơi + class của từng người (hiện sẵn, không cần chọn lại)
├── Cảnh báo đội hình nếu thiếu Warrior hoặc Healer
└── Host bấm Start khi tất cả Ready
        ↓
Màn hình loading map (2–3 giây)
        ↓
Màn hình chiến đấu
        ↓
Boss chết → Màn hình kết quả thắng
Boss giết hết team → Màn hình kết quả thua
        ↓
Màn hình loot — item rớt ra lần lượt với animation
        ↓
Màn hình inventory — xem item vừa nhận
        ↓
Chơi lại HOẶC về Lobby
```

---

## 11. TECHNICAL STACK

### Server
| Thành phần | Công nghệ |
|---|---|
| Backend framework | ASP.NET Core (C#) |
| Realtime | WebSocket (built-in ASP.NET Core) |
| REST API | ASP.NET Core Web API |
| Database chính | MySQL + Entity Framework Core (Pomelo) |
| Cache realtime | Redis (StackExchange.Redis) |
| Authentication | JWT Bearer Token |
| Deploy | Docker → Railway.app |

### Client Giai đoạn 1
| Thành phần | Công nghệ |
|---|---|
| UI | HTML + CSS |
| Logic | JavaScript thuần |
| Realtime | WebSocket API có sẵn của browser |
| Deploy | Vercel hoặc Netlify (free) |

### Client Giai đoạn 2
| Thành phần | Công nghệ |
|---|---|
| Engine | Unity |
| Kết nối server | NativeWebSocket package |
| Build target | WebGL → host trên itch.io |

### WebSocket Events chính
| Event | Hướng | Mô tả |
|---|---|---|
| `player_join` | Client → Server | Vào phòng |
| `player_ready` | Client → Server | Sẵn sàng bắt đầu |
| `use_skill` | Client → Server | Dùng skill |
| `move` | Client → Server | Input di chuyển |
| `boss_update` | Server → All | HP boss, phase, vị trí |
| `player_update` | Server → All | HP, vị trí tất cả người chơi |
| `boss_attack` | Server → All | Boss đánh ai, damage bao nhiêu |
| `player_dead` | Server → All | Người chơi nào vừa chết |
| `loot_drop` | Server → All | Item gì vừa rớt, hạng mấy |
| `match_end` | Server → All | Trận kết thúc, kết quả |

---

## 12. DATABASE SCHEMA (MySQL)

### Bảng Users
```sql
users (id, username, email, password_hash, class, created_at)
```
Class lưu thẳng trong bảng users vì 1 tài khoản chỉ có đúng 1 class cố định.

### Bảng Characters (chỉ số nhân vật theo cấp độ)
```sql
characters (id, user_id, level, base_damage, hp_max, defense, total_matches, total_wins)
```
Quan hệ 1-1 với users — mỗi user có đúng 1 character, chỉ số tăng theo level.

### Bảng Items
```sql
items (id, name, type, rarity, stat_type, stat_value)
```

### Bảng Inventory
```sql
inventory (id, character_id, item_id, is_equipped, slot)
```

### Bảng Matches
```sql
matches (id, map_id, boss_id, started_at, ended_at, result)
match_players (match_id, user_id, class, total_damage, loot_tier)
```

### Redis Keys (realtime)
```
room:{room_id}:boss_hp         → HP boss hiện tại
room:{room_id}:boss_phase      → Phase hiện tại (1/2/3)
room:{room_id}:player_positions → JSON vị trí tất cả người chơi
room:{room_id}:player_hp        → HP từng người chơi
room:{room_id}:skill_cooldowns  → Cooldown từng skill theo player
```

---

## 13. KẾ HOẠCH PHÁT TRIỂN

### Giai đoạn 1 — Web Client (3–4 tuần)
Mục tiêu: game chạy được trên browser, bạn bè join qua link và chơi được 1 trận hoàn chỉnh.

- Tuần 1: Setup server, database, auth API, room API
- Tuần 2: WebSocket + boss AI + movement system
- Tuần 3: Web client HTML/JS — UI chiến đấu đầy đủ
- Tuần 4: Deploy Docker → Railway, quay video demo, lên GitHub

### Giai đoạn 2 — Unity Client (sau khi có intern)
- Port client sang Unity, thêm animation và hiệu ứng visual
- Build WebGL, host itch.io
- Thêm boss thứ 2 (Rồng Băng)
- Hoàn thiện hệ thống hồi sinh bằng Healer

### Giai đoạn 3 — Mở rộng
- Hệ thống đệ tử — săn boss đặc biệt để có trợ thủ
- Thêm boss Rồng Huyền Thoại
- PvP mode — team đấu team
- Leaderboard toàn server
- Hệ thống guild

---

## 14. ĐIỀU CẦN TRÁNH KHI PHÁT TRIỂN

- Đừng làm đồ họa phức tạp ở giai đoạn 1 — gameplay trước, đẹp sau
- Đừng ôm đồm tính năng — xong core loop rồi mới thêm
- Đừng để 3 ngày code mà không test — mỗi ngày phải có thứ gì chạy được
- Đừng lo movement quá — nếu stuck thì tạm bỏ, làm combat trước
- Đừng quên quay video demo — nhà tuyển dụng thấy video chạy được quan trọng hơn đọc code
