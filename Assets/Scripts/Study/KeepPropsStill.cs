using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class KeepPropsStill : MonoBehaviour
{
    void Awake()
    {
        var bodies = FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var body in bodies)
            Freeze(body);
    }

    public static void Freeze(Rigidbody body)
    {
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
    }
}
