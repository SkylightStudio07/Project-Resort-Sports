using UnityEngine;

public class ArcheryGameManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private int arrowsPerRound = 3;
    [SerializeField]
    private int totalRounds = 3;

    [Header("UI Settings")]
    [SerializeField]
    private ArcheryUI ui;

    public int ArrowPerRound => arrowsPerRound;
    public int TotalRounds => totalRounds;
    public int CurrentRound { get; private set; }
    public int TotalScore { get; private set; }
    public int RoundScore { get; private set; }
    public int ArrowsLeft { get; private set; }

    public enum GameState { Idle, Playing, RoundEnd, GameOver }
    public GameState State { get; private set; }

    public event System.Action OnRoundStarted;

    void Start() => StartGame();

    public void StartGame()
    {
        CurrentRound = 1;
        TotalScore = 0;
        StartRound();
    }

    public void AddScore(int score)
    {
        RoundScore += score;
        TotalScore += score;
        ui.UpdateUI();
    }

    public void OnArrowFired()
    {
        ArrowsLeft--;
        ui.UpdateUI();

        if (ArrowsLeft <= 0)
        {
            State = GameState.RoundEnd;
            Invoke(nameof(NextRound), 2f);
        }
    }

    public void Restart() => StartGame();

    private void NextRound()
    {
        if (CurrentRound >= totalRounds)
        {
            State = GameState.GameOver;
            ui.ShowFinalScore(TotalScore);
            Invoke(nameof(Restart), 3f);
            return;
        }
        CurrentRound++;
        StartRound();
    }

    private void StartRound()
    {
        RoundScore = 0;
        ArrowsLeft = arrowsPerRound;
        State = GameState.Playing;
        OnRoundStarted?.Invoke();
        ui.UpdateUI();
    }
}