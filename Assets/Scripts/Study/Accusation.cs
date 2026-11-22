using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Accusation : MonoBehaviour
{
    public enum Verdict { None, Solved, Partial, Escaped }
    public Examine examine;
    public Suspect killer;
    public Clue proof;
    public GameObject panel;
    public Transform whoChoices;
    public Transform whyChoices;
    public Transform evidenceChoices;
    public Text verdict;
    public Button accuseButton;
    public bool IsOpen => panel != null && panel.activeSelf;
    public Verdict Result { get; private set; }
    Suspect chosenWho;
    string chosenWhy;
    Clue chosenProof;
    Sprite box;
    static readonly Color Normal = new Color(0.16f, 0.12f, 0.1f, 1f);
    static readonly Color Picked = new Color(0.45f, 0.28f, 0.14f, 1f);
    void Awake()
    {
        if (examine == null)
            examine = GetComponent<Examine>();
        var dot = new Texture2D(1, 1);
        dot.SetPixel(0, 0, Color.white);
        dot.Apply();
        box = Sprite.Create(dot, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        var card = panel.GetComponentInChildren<Image>();
        if (card != null && card.sprite == null)
            card.sprite = box;
        if (panel != null)
            panel.SetActive(false);
        if (accuseButton != null)
            accuseButton.onClick.AddListener(Submit);
    }
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || panel == null || (GameScreens.Instance != null && GameScreens.Instance.BlocksPlay))
            return;
        if (!keyboard.enterKey.wasPressedThisFrame && !keyboard.numpadEnterKey.wasPressedThisFrame)
            return;
        panel.SetActive(!panel.activeSelf);
        if (IsOpen)
            Build();
    }
    void Build()
    {
        Clear(whoChoices);
        Clear(whyChoices);
        Clear(evidenceChoices);
        chosenWho = null;
        chosenWhy = null;
        chosenProof = null;
        verdict.text = "Choose who, why, and one clue.";
        var people = FindObjectsByType<Suspect>(FindObjectsSortMode.None);
        System.Array.Sort(people, (a, b) => string.Compare(a.suspectName, b.suspectName));
        foreach (var person in people)
        {
            var who = person;
            MakeButton(whoChoices, who.suspectName, button =>
            {
                Choose(whoChoices, button);
                chosenWho = who;
            });
            var why = person.motive;
            MakeButton(whyChoices, why, button =>
            {
                Choose(whyChoices, button);
                chosenWhy = why;
            });
        }
        foreach (var clue in examine.found)
        {
            if (clue == null)
                continue;
            var proofClue = clue;
            MakeButton(evidenceChoices, proofClue.clueName, button =>
            {
                Choose(evidenceChoices, button);
                chosenProof = proofClue;
            });
        }
    }
    void Submit()
    {
        if (chosenWho == null || string.IsNullOrEmpty(chosenWhy) || chosenProof == null)
        {
            Result = Verdict.None;
            verdict.text = "Choose who, why, and one clue.";
            return;
        }
        if (chosenWho != killer)
            Result = Verdict.Escaped;
        else if (chosenWhy == killer.motive && chosenProof == proof)
            Result = Verdict.Solved;
        else
            Result = Verdict.Partial;
        if (Result == Verdict.Solved)
            verdict.text = "The case is solved.";
        else if (Result == Verdict.Partial)
            verdict.text = "You named the right person, but the proof does not hold.";
        else
            verdict.text = "The killer walks free.";

        if (Result != Verdict.None && GameScreens.Instance != null)
            GameScreens.Instance.ShowResult(Result);
    }
    void MakeButton(Transform parent, string label, System.Action<Button> onPick)
    {
        var item = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        item.transform.SetParent(parent, false);
        var image = item.GetComponent<Image>();
        image.sprite = box;
        image.color = Normal;
        var layout = item.GetComponent<LayoutElement>();
        layout.preferredWidth = 280f;
        layout.minHeight = 72f;
        layout.preferredHeight = 72f;
        var button = item.GetComponent<Button>();
        button.onClick.AddListener(() => onPick(button));
        var textItem = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textItem.transform.SetParent(item.transform, false);
        var text = textItem.GetComponent<Text>();
        text.font = verdict.font;
        text.fontSize = 16;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.94f, 0.88f, 0.75f, 1f);
        text.text = label;
        text.raycastTarget = false;
        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);
        item.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 52f);
    }
    void Choose(Transform column, Button picked)
    {
        foreach (var other in column.GetComponentsInChildren<Button>())
            other.GetComponent<Image>().color = Normal;
        picked.GetComponent<Image>().color = Picked;
    }
    void Clear(Transform column)
    {
        for (var i = column.childCount - 1; i >= 0; i--)
            Destroy(column.GetChild(i).gameObject);
    }
}
