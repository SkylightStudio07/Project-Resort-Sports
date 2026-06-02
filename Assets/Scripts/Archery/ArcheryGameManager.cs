using UnityEngine;

public class ArcheryGameManager : MonoBehaviour
{
    [System.Serializable]
    public class SetConfig
    {
        public Vector3 targetPosition;
        public bool patrol;
        public float patrolDistance = 3f;
        public float patrolSpeed = 1.5f;
    }

    [Header("Game Settings")]
    [SerializeField] private int arrowsPerSet = 5;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private TargetPatrol targetPatrol;

    [Header("Set Configs (5 sets)")]
    [SerializeField] private SetConfig[] setConfigs = new SetConfig[5];

    [Header("UI")]
    [SerializeField] private ArcheryUI ui;

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
            ui.ShowFinalScore(TotalScore);
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
            target.gameObject.SetActive(false);
            target.position = config.targetPosition;
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
