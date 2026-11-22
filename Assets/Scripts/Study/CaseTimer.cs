using UnityEngine;
using UnityEngine.UI;

public class CaseTimer : MonoBehaviour
{
    public float seconds = 300f;
    public Text readout;

    float left;
    bool finished;

    void Awake()
    {
        left = seconds;
        if (readout != null)
        {
            readout.fontSize = 52;
            readout.fontStyle = FontStyle.Bold;
            readout.alignment = TextAnchor.UpperRight;
            readout.horizontalOverflow = HorizontalWrapMode.Overflow;
            readout.verticalOverflow = VerticalWrapMode.Overflow;
            readout.color = new Color(0.98f, 0.84f, 0.45f, 1f);
            var rect = readout.rectTransform;
            rect.sizeDelta = new Vector2(280f, 90f);
            rect.anchoredPosition = new Vector2(-28f, -20f);
        }
        Show();
    }

    void Update()
    {
        if (finished || GameScreens.Instance == null || !GameScreens.Instance.Playing)
            return;
        if (GameScreens.Instance.BlocksPlay)
            return;

        left -= Time.deltaTime;
        if (left <= 0f)
        {
            left = 0f;
            finished = true;
            Show();
            GameScreens.Instance.ShowResult(Accusation.Verdict.Escaped);
            return;
        }

        Show();
    }

    public void ResetClock()
    {
        left = seconds;
        finished = false;
        Show();
    }

    void Show()
    {
        if (readout == null)
            return;
        var total = Mathf.CeilToInt(left);
        var minutes = total / 60;
        var secs = total % 60;
        readout.text = "TIME\n" + minutes.ToString("0") + ":" + secs.ToString("00");
        if (left <= 60f)
            readout.color = new Color(1f, 0.35f, 0.28f, 1f);
        else
            readout.color = new Color(0.98f, 0.84f, 0.45f, 1f);
    }
}
