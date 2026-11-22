using UnityEngine;
using UnityEngine.InputSystem;

public class ItemHold : MonoBehaviour
{
    public Transform holdPoint; // The point where the item will be held
    public float turnSpeed = 0.4f; // The speed at which the item will rotate to face the camera

    public Clue Held { get; private set; } // The currently held item
    public bool IsHolding => Held != null; // Whether the player is currently holding an item

    PlayerInteractor eyes;
    Journal journal;
    Accusation charge;
    Transform home;
    Vector3 homePosition;
    Quaternion homeRotation;
    Collider[] shapes;

    void Awake()
    {
        // Get the PlayerInteractor component from the player
        eyes = GetComponent<PlayerInteractor>();
        journal = GetComponent<Journal>();
        charge = GetComponent<Accusation>();
    }

    // Update is called once per frame
    void Update()
    {
        // Get the current mouse input
        var mouse = Mouse.current;
        if (mouse == null || (journal != null && journal.IsOpen) || (charge != null && charge.IsOpen) || (GameScreens.Instance != null && GameScreens.Instance.BlocksPlay))
            return;
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Take();
            return;
        }
        if (mouse.rightButton.wasPressedThisFrame)
        {
            PutBack();
            return;
        }
        if (IsHolding)
            Turn(mouse.delta.ReadValue());
    }

    // Method to take the currently selected item
    void Take()
    {
        // If the player is already holding an item or there is no item selected, return
        if (IsHolding || eyes == null || holdPoint == null)
            return;

        var seen = eyes.Refresh();
        if (seen == null)
            return;

        var clue = seen.GetComponentInParent<Clue>();
        if (clue == null)
            return;
        Held = clue;
        home = clue.transform.parent;
        homePosition = clue.transform.localPosition;
        homeRotation = clue.transform.localRotation;
        foreach (var body in clue.GetComponentsInChildren<Rigidbody>())
            KeepPropsStill.Freeze(body);
        shapes = clue.GetComponentsInChildren<Collider>();
        foreach (var shape in shapes)
            shape.enabled = false;
        clue.transform.SetParent(holdPoint, false);
        clue.transform.localRotation = Quaternion.identity;
        clue.transform.localPosition = Vector3.zero;
        CenterOnHoldPoint(clue);
    }

    void CenterOnHoldPoint(Clue clue)
    {
        var hasBounds = false;
        var bounds = new Bounds(clue.transform.position, Vector3.zero);
        foreach (var renderer in clue.GetComponentsInChildren<Renderer>())
        {
            if (renderer.gameObject.name.Contains("Label"))
                continue;
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
            return;

        var shift = holdPoint.InverseTransformPoint(bounds.center);
        clue.transform.localPosition -= shift;
    }

    // Method to rotate the held item based on mouse input
    void Turn(Vector2 look)
    {
        // Rotate the held item around the hold point based on mouse movement
        Held.transform.Rotate(holdPoint.up, -look.x * turnSpeed, Space.World);
        Held.transform.Rotate(holdPoint.right, look.y * turnSpeed, Space.World);
    }

    // Method to put back the currently held item
    void PutBack()
    {
        // If the player is not holding an item, return
        if (!IsHolding)
            return;
        // Return the held item to its original parent and position
        Held.transform.SetParent(home, false);
        Held.transform.localPosition = homePosition;
        Held.transform.localRotation = homeRotation;
        foreach (var body in Held.GetComponentsInChildren<Rigidbody>())
            KeepPropsStill.Freeze(body);
        foreach (var shape in shapes)
            shape.enabled = true;
        Held = null;
        shapes = null;
    }
}
