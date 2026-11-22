using UnityEngine;

public class Suspect : MonoBehaviour
{
    public string suspectName;

    [TextArea(2, 4)]
    public string claim;

    [TextArea(2, 4)]
    public string motive;
}
