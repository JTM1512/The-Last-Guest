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
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.eKey.wasPressedThisFrame)
            return;

        var target = eyes.Current;
        if (target == null)
        {
            readout.text = "";
            return;
        }

        var clue = target.GetComponentInParent<Clue>();
        if (clue != null)
        {
            if (!found.Contains(clue))
            {
                found.Add(clue);
            }
            readout.text = clue.clueName + "\n" + clue.description;
            return;
        }

        var suspect = target.GetComponentInParent<Suspect>();
        if (suspect != null)
        {
            readout.text = suspect.suspectName + "\n" + suspect.claim;
            return;
        }

        readout.text = "";

    }
}
