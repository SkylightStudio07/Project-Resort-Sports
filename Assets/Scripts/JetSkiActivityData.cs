using UnityEngine;

[CreateAssetMenu(fileName = "JetSkiActivityData", menuName = "JetSki/Activity Data")]
public class JetSkiActivityData : ScriptableObject
{
    public string courseID = "course_01";

    [Header("Tutorial")]
    public string tutorialTitle = "\u0048\u0054\u0043 \u0056\u0069\u0076\u0065 \uC81C\uD2B8\uC2A4\uD0A4 \uC870\uC791 \uBC29\uBC95";

    [TextArea(3, 5)]
    public string tutorialBody =
        "\uCEE8\uD2B8\uB864\uB7EC \uC785\uB825\uC73C\uB85C \uC804\uC9C4\uD558\uACE0 \uC88C\uC6B0\uB85C \uC870\uD5A5\uD569\uB2C8\uB2E4.\n" +
        "\uAC8C\uC774\uC9C0\uAC00 \uCC28\uBA74 \uC624\uB978\uC190 \uCEE8\uD2B8\uB864\uB7EC\uB97C \uBE44\uD2C0\uC5B4 \uBD80\uC2A4\uD2B8\uD558\uC138\uC694.\n" +
        "\uAC8C\uC774\uD2B8\uB97C \uC21C\uC11C\uB300\uB85C \uD1B5\uACFC\uD558\uC138\uC694.";

    [Header("Game Defaults")]
    public float defaultGateTime = 5.9f;
    public int defaultBaseScore = 100;
    public int defaultPerfectBonus = 50;

    [Tooltip("Gate center pass ratio. 0.3 means the inner 30% of the gate counts as perfect.")]
    [Range(0f, 1f)]
    public float perfectThreshold = 0.3f;

    [Header("Rank Scores")]
    public int bronzeScore = 500;
    public int silverScore = 1000;
    public int goldScore = 1500;

    public string GetRank(int score)
    {
        if (score >= goldScore) return "Gold";
        if (score >= silverScore) return "Silver";
        if (score >= bronzeScore) return "Bronze";
        return "None";
    }
}
