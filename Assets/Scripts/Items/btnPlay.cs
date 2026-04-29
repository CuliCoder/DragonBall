using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
public class btnPlay : MonoBehaviour, Button
{
    public void Use()
    {
        Debug.Log("Play button clicked!");
        SceneManager.LoadScene("GameScene"); // Thay "GameScene" bằng tên scene bạn muốn tải khi nhấn nút Play
    }
}