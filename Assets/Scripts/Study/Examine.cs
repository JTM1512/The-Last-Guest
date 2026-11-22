using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Examine : MonoBehaviour
{
    public Text readout;
    public Text holdPrompt;
    public List<Clue> found = new List<Clue>();
    public List<Suspect> interviewed = new List<Suspect>();

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

    void Update()
    {
        if (holdPrompt != null)
        {
            if (hands != null && hands.IsHolding && (GameScreens.Instance == null || !GameScreens.Instance.BlocksPlay))
                holdPrompt.text = "E analyse     Mouse turn     Right click put back";
            else
                holdPrompt.text = "";
        }

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

        var target = eyes.Current;
        if (target == null)
        {
            readout.text = "";
            return;
        }

        var suspect = target.GetComponentInParent<Suspect>();
        if (suspect != null)
        {
            if (!interviewed.Contains(suspect))
                interviewed.Add(suspect);
            readout.text = suspect.suspectName + "\n" + suspect.claim;
            return;
        }

        readout.text = "";
    }
}
