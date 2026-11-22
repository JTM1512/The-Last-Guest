using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        var away = transform.position - cam.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(away);
    }
}
