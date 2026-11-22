using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Examine : MonoBehaviour
{
    public Text readout;
    public List<Clue> found = new List<Clue>();

    PlayerInteractor eyes; 
   
    void Awake()
    {
        eyes = GetComponent<PlayerInteractor>();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if the 'E' key was pressed this frame
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.eKey.wasPressedThisFrame)
            return;

        // Get the currently selected object from the PlayerInteractor
        var target = eyes.Current;
        if (target == null)
        {
            readout.text = "";
            return;
        }

        // Check if the target has a Clue component in its parent hierarchy
        var clue = target.GetComponentInParent<Clue>();
        if (clue != null)
        {
            // If the clue is not already in the found list, add it
            if (!found.Contains(clue))
            {
                found.Add(clue);
            }
           readout.text = clue.clueName + "\n" + clue.comment;
            return;
        }

        // Check if the target has a Suspect component in its parent hierarchy
        var suspect = target.GetComponentInParent<Suspect>();
        if (suspect != null)
        {
            readout.text = suspect.suspectName + "\n" + suspect.claim;
            return;
        }

        readout.text = "";

    }
}
