using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCamera : MonoBehaviour{

	public float movementSpeed = 5.0f;

	void FixedUpdate()
	{
		var keyboard = Keyboard.current;
		var mouse = Mouse.current;
		if (keyboard == null || mouse == null)
			return;

		float horizontalAxis = 0f;
		float verticalAxis = 0f;
		if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontalAxis -= 1f;
		if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontalAxis += 1f;
		if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) verticalAxis -= 1f;
		if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) verticalAxis += 1f;

		horizontalAxis *= Time.fixedDeltaTime * movementSpeed;
		verticalAxis *= Time.fixedDeltaTime * movementSpeed;
		var look = mouse.delta.ReadValue() * 0.05f;

		transform.Translate(horizontalAxis, 0, 0);
		transform.Translate(0, 0, verticalAxis);
		transform.eulerAngles += new Vector3(-look.y, look.x, 0);
	}
}