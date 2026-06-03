using UnityEngine;
using UnityEngine.UI;

// 제트스키 핸들 위 World Space Canvas 에 붙여서 게이지 표시.
// Canvas 안에 Image(Filled, Horizontal) 한 개 만들어서 fillImage 에 연결.
public class JetSkiHUD : MonoBehaviour
{
    [Header("Source")]
    public JetSkiBoost boost;

    [Header("Fill Bar")]
    [Tooltip("Image Type = Filled / Fill Method = Horizontal 로 세팅된 Image.")]
    public Image fillImage;

    [Header("Colors")]
    public Color chargingColor   = new Color(1f, 0.4f, 0.6f);   // 분홍
    public Color readyColor      = new Color(1f, 0.9f, 0.2f);   // 게이지 풀 (노랑)
    public Color boostingColor   = new Color(0.3f, 0.9f, 1f);   // 부스트 중 (시안)

    [Header("Ready Pulse")]
    [Tooltip("게이지 풀 차면 깜빡이는 주기(초). 0이면 깜빡 안 함.")]
    public float readyPulseHz = 3f;

    void Reset()
    {
        boost = GetComponentInParent<JetSkiBoost>();
    }

    void Awake()
    {
        if (boost == null) boost = GetComponentInParent<JetSkiBoost>();
    }

    void Update()
    {
        if (boost == null || fillImage == null) return;

        if (boost.IsBoosting)
        {
            fillImage.fillAmount = 1f;
            fillImage.color = boostingColor;
        }
        else
        {
            fillImage.fillAmount = boost.Gauge;

            if (boost.Gauge >= 1f && readyPulseHz > 0f)
            {
                // ping-pong 으로 charging<->ready 사이를 깜빡임
                float t = Mathf.PingPong(Time.time * readyPulseHz, 1f);
                fillImage.color = Color.Lerp(chargingColor, readyColor, t);
            }
            else
            {
                fillImage.color = Color.Lerp(chargingColor, readyColor, boost.Gauge);
            }
        }
    }
}
