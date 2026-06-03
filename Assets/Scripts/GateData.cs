using UnityEngine;

[CreateAssetMenu(fileName = "GateData", menuName = "JetSki/Gate Data")]
public class GateData : ScriptableObject
{
    [Tooltip("이 게이트의 기본 제한 시간 (초). 이전 게이트 잔여 시간이 더해짐.")]
    public float gateTime = 5.9f;

    [Tooltip("통과 기본 점수")]
    public int baseScore = 100;

    [Tooltip("퍼펙트 통과(중앙) 보너스 점수")]
    public int perfectBonus = 50;
}
