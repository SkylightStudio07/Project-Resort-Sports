using UnityEngine;

public class ArcheryGameManager : MonoBehaviour
{
    [System.Serializable]
    public class SetConfig
    {
        // Set 1은 (0, 0) 고정. Set 2~5는 Set 1 기준 X/Z 오프셋
        public float offsetX;
        public float offsetZ;
        public bool patrol;
        public float patrolDistance = 3f;
        public float patrolSpeed = 1.5f;
    }

    [Header("Game Settings")]
    [SerializeField] private int arrowsPerSet = 5;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private TargetPatrol targetPatrol;

    [Header("Set Configs (5 sets) — offset from Set 1 position (X/Z only)")]
    [SerializeField] private SetConfig[] setConfigs = new SetConfig[5];

    [Header("UI")]
    [SerializeField] private ArcheryUI ui;

    private Vector3 basePosition;

    public int ArrowsPerSet => arrowsPerSet;
    public int TotalSets => setConfigs.Length;
    public int CurrentSet { get; private set; }
    public int TotalScore { get; private set; }
    public int SetScore { get; private set; }
    public int LastArrowScore { get; private set; }
    public int ArrowsLeft { get; private set; }

    public enum GameState { Idle, Playing, SetEnd, GameOver }
    public GameState State { get; private set; }

    public event System.Action OnSetStarted;

    private void Awake()
    {
        if (target != null)
            basePosition = target.position;
        else
            Debug.LogWarning("[ArcheryGameManager] target이 연결되지 않았습니다.");
    }

    void Start() => StartGame();

    public void StartGame()
    {
        CurrentSet = 0;
        TotalScore = 0;
        StartSet();
    }

    public void AddScore(int score)
    {
        LastArrowScore = score;
        SetScore += score;
        TotalScore += score;
        ui.UpdateUI();
    }

    public void OnArrowFired()
    {
        ArrowsLeft--;
        ui.UpdateUI();

        if (ArrowsLeft <= 0)
        {
            State = GameState.SetEnd;
            Invoke(nameof(NextSet), 2f);
        }
    }

    public void Restart() => StartGame();

    private void NextSet()
    {
        CurrentSet++;
        if (CurrentSet >= setConfigs.Length)
        {
            State = GameState.GameOver;
            Invoke(nameof(Restart), 5f);
            return;
        }
        StartSet();
    }

    private void StartSet()
    {
        SetScore = 0;
        LastArrowScore = 0;
        ArrowsLeft = arrowsPerSet;
        State = GameState.Playing;

        ApplySetConfig(setConfigs[CurrentSet]);
        OnSetStarted?.Invoke();
        ui.UpdateUI();
    }

    private void ApplySetConfig(SetConfig config)
    {
        if (target != null)
        {
            foreach (var arrow in target.GetComponentsInChildren<ArrowController>())
                Destroy(arrow.gameObject);

            target.gameObject.SetActive(false);
            target.position = new Vector3(
                basePosition.x + config.offsetX,
                basePosition.y,
                basePosition.z + config.offsetZ
            );
            target.gameObject.SetActive(true);
        }

        if (targetPatrol != null)
        {
            if (config.patrol)
            {
                targetPatrol.SetConfig(config.patrolDistance, config.patrolSpeed);
                targetPatrol.StartPatrol();
            }
            else
            {
                targetPatrol.StopPatrol();
            }
        }
    }
}
