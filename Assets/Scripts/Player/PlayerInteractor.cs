using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public float reach = 3f; // The maximum distance the player can reach to interact with objects
    public Camera view;

    public GameObject Current { get; private set; } // The currently selected interactable object

    void Awake()
    {
        // If no camera is assigned, try to find one in the children of this GameObject
        if (view == null)
        {
            view = GetComponentInChildren<Camera>();
        }
    }

    void Update() 
    {
        Current = Look();

        // Check for interaction input
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame && Current != null)
            Debug.Log(Current.name);
    }

    GameObject Look()
    {
        // Create a ray from the camera's position in the direction it is facing
        Ray ray = new Ray(view.transform.position, view.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, reach))
            return hit.collider.gameObject;

        return null;
    }
}
