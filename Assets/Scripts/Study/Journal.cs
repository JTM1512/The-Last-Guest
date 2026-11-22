using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Journal : MonoBehaviour
{
    public Examine examine;
    public GameObject panel;
    public Text list;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (examine == null)
            examine = GetComponent<Examine>();
        if (panel != null)
            panel.SetActive(false);

        if (list != null)
        {
            list.fontSize = 18;
            list.rectTransform.sizeDelta = new Vector2(680f, 540f);
            var card = list.transform.parent as RectTransform;
            if (card != null)
                card.sizeDelta = new Vector2(760f, 700f);
        }
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.jKey.wasPressedThisFrame && panel != null)
            panel.SetActive(!panel.activeSelf);

        if (IsOpen)
            Show();
    }

    void Show()
    {
        if (examine == null || examine.found.Count == 0)
        {
            list.text = "You have not examined a clue yet.";
            return;
        }

        var lines = "";
        foreach (var clue in examine.found)
        {
            if (clue == null)
                continue;
            if (lines.Length > 0)
                lines += "\n\n";
            lines += "<b>" + clue.clueName + "</b>";
            if (!string.IsNullOrWhiteSpace(clue.description))
                lines += "\n" + clue.description.Trim();
            if (!string.IsNullOrWhiteSpace(clue.comment))
                lines += "\n" + clue.comment.Trim();
        }

        list.text = lines.Length > 0 ? lines : "You have not examined a clue yet.";
    }
}
