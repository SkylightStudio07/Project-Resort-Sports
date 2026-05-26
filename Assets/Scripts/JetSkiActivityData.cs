using UnityEngine;

[CreateAssetMenu(fileName = "JetSkiActivityData", menuName = "JetSki/Activity Data")]
public class JetSkiActivityData : ScriptableObject
{
    public string courseID = "course_01";

    [Header("게이트 기본값 (GateData가 없는 게이트에 사용)")]
    public float defaultGateTime = 5.9f;
    public int defaultBaseScore = 100;
    public int defaultPerfectBonus = 50;

    [Tooltip("게이트 너비 중 이 비율(0~1) 이내 통과 시 퍼펙트. 0.3 = 중앙 30% 이내.")]
    [Range(0f, 1f)]
    public float perfectThreshold = 0.3f;

    [Header("랭크 기준 점수")]
    public int bronzeScore = 500;
    public int silverScore = 1000;
    public int goldScore = 1500;

    public string GetRank(int score)
    {
        if (score >= goldScore)   return "Gold";
        if (score >= silverScore) return "Silver";
        if (score >= bronzeScore) return "Bronze";
        return "None";
    }
}
