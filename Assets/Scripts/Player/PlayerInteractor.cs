using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public float reach = 4.5f;
    public Camera view;

    public GameObject Current { get; private set; }

    void Awake()
    {
        if (view == null)
            view = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        Refresh();
    }

    public GameObject Refresh()
    {
        Current = Look();
        return Current;
    }

    GameObject Look()
    {
        var mouse = Mouse.current;
        var ray = mouse == null
            ? new Ray(view.transform.position, view.transform.forward)
            : view.ScreenPointToRay(mouse.position.ReadValue());

        var clue = ClosestAlongRay<Clue>(ray);
        if (clue != null)
            return clue.gameObject;

        var hits = Physics.RaycastAll(ray, reach);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.GetComponentInParent<Suspect>() != null)
                return hit.collider.gameObject;
        }

        if (hits.Length > 0)
            return hits[0].collider.gameObject;

        return null;
    }

    T ClosestAlongRay<T>(Ray ray) where T : Component
    {
        var found = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        T best = null;
        var bestDistance = float.MaxValue;
        foreach (var item in found)
        {
            if (!AimingAt(item, ray, out float distance) || distance >= bestDistance)
                continue;
            best = item;
            bestDistance = distance;
        }

        return best;
    }

    bool AimingAt(Component item, Ray ray, out float distance)
    {
        distance = 0f;
        var renderers = item.GetComponentsInChildren<Renderer>();
        var hasBounds = false;
        var bounds = new Bounds(item.transform.position, Vector3.zero);
        foreach (var renderer in renderers)
        {
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
            return false;

        bounds.Expand(0.35f);
        return bounds.IntersectRay(ray, out distance) && distance > 0.05f && distance <= reach;
    }
}
