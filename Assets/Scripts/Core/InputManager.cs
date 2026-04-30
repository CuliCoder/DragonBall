using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public Vector2 MoveInput { get; private set; }
    public bool number1 { get; private set; }
    public bool number2 { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        MoveInput = GetMoveInput();
        number1 = Input.GetKeyDown(KeyCode.Alpha1);
        number2 = Input.GetKeyDown(KeyCode.Alpha2);
    }
    private Vector2 GetMoveInput()
    {
        int horizontal = (int)Input.GetAxisRaw("Horizontal");
        int vertical = (int)Input.GetAxisRaw("Vertical");

        return new Vector2(horizontal, vertical);
    }
}   
