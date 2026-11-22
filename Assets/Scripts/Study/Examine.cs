using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Examine : MonoBehaviour
{
    public Text readout;
    public List<Clue> found = new List<Clue>();

    PlayerInteractor eyes;
    ItemHold hands;
    Journal journal;
    Accusation charge;

    void Awake()
    {
        eyes = GetComponent<PlayerInteractor>();
        hands = GetComponent<ItemHold>();
        journal = GetComponent<Journal>();
        charge = GetComponent<Accusation>();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if the 'E' key was pressed this frame
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.eKey.wasPressedThisFrame || (journal != null && journal.IsOpen) || (charge != null && charge.IsOpen) || (GameScreens.Instance != null && GameScreens.Instance.BlocksPlay))
            return;

        if (hands != null && hands.Held != null)
        {
            var clue = hands.Held;
            if (!found.Contains(clue))
                found.Add(clue);
            readout.text = clue.clueName + "\n" + clue.comment;
            return;
        }

        // Get the currently selected object from the PlayerInteractor
        var target = eyes.Current;
        if (target == null)
        {
            readout.text = "";
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
