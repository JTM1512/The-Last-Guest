using UnityEngine;
using UnityEngine.InputSystem;

public class StudyWalker : MonoBehaviour
{
    public float moveSpeed = 3.1f;
    public float lookSensitivity = 0.1f;

    CharacterController body;
    Transform view;
    float pitch;
    ItemHold hands;
    Journal journal;

    void Awake()
    {
        // Get the CharacterController component attached to this GameObject
        body = GetComponent<CharacterController>();
        view = GetComponentInChildren<Camera>().transform;
        hands = GetComponent<ItemHold>();
        journal = GetComponent<Journal>();
    }

    void Update()
    {
        // Get the current keyboard and mouse input devices
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (journal != null && journal.IsOpen)
            return;

        if ((hands == null || !hands.IsHolding) && mouse.rightButton.isPressed)
        {
            var look = mouse.delta.ReadValue();
            transform.Rotate(0f, look.x * lookSensitivity, 0f);
            pitch = Mathf.Clamp(pitch - look.y * lookSensitivity, -75f, 75f);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        var move = Vector2.zero;
        if (keyboard.wKey.isPressed) move.y += 1f;
        if (keyboard.sKey.isPressed) move.y -= 1f;
        if (keyboard.dKey.isPressed) move.x += 1f;
        if (keyboard.aKey.isPressed) move.x -= 1f;

        var world = transform.right * move.x + transform.forward * move.y;
        if (world.sqrMagnitude > 1f)
            world.Normalize();

        world *= moveSpeed;
        world.y = body.isGrounded ? -1.5f : -9f;
        body.Move(world * Time.deltaTime);
    }
}
