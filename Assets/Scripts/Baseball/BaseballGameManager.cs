using System.Collections;
using UnityEngine;

namespace Baseball
{
    public enum PitchType { Fastball, BreakingBall }

    public enum BallResult { Success, Foul, PenaltyMiss, PenaltyNoSwing }

    public class BaseballGameManager : MonoBehaviour
    {
        public static BaseballGameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] PitchController pitchController;

        [Header("Game Settings")]
        [SerializeField] int totalSets = 3;
        [SerializeField] int pitchesPerSet = 10;
        [SerializeField] float delayBetweenPitches = 2f;

        int currentSet;
        int currentPitch;

        // [set][0=success, 1=foul, 2=penalty]
        int[,] setScores;

        bool waitingForResult;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            setScores = new int[totalSets, 3];
            StartGame();
        }

        void StartGame()
        {
            currentSet = 0;
            StartSet();
        }

        void StartSet()
        {
            currentPitch = 0;
            Debug.Log($"=== Set {currentSet + 1} Start ===");
            StartCoroutine(PitchSequence());
        }

        IEnumerator PitchSequence()
        {
            while (currentPitch < pitchesPerSet)
            {
                yield return new WaitForSeconds(delayBetweenPitches);
                waitingForResult = true;
                pitchController.ThrowPitch();
                yield return new WaitUntil(() => !waitingForResult);
                currentPitch++;
            }
            EndSet();
        }

        public void ReportResult(BallResult result)
        {
            switch (result)
            {
                case BallResult.Success:
                    setScores[currentSet, 0]++;
                    Debug.Log("Success +1");
                    break;
                case BallResult.Foul:
                    setScores[currentSet, 1]++;
                    Debug.Log("Foul +1");
                    break;
                case BallResult.PenaltyMiss:
                case BallResult.PenaltyNoSwing:
                    setScores[currentSet, 2]++;
                    Debug.Log("Penalty +1");
                    break;
            }
            waitingForResult = false;
        }

        void EndSet()
        {
            int s = currentSet;
            Debug.Log($"Set {s + 1} End — Success:{setScores[s, 0]} Foul:{setScores[s, 1]} Penalty:{setScores[s, 2]}");
            currentSet++;

            if (currentSet < totalSets)
                StartSet();
            else
                EndGame();
        }

        void EndGame()
        {
            int totalSuccess = 0, totalFoul = 0, totalPenalty = 0;
            for (int i = 0; i < totalSets; i++)
            {
                totalSuccess  += setScores[i, 0];
                totalFoul     += setScores[i, 1];
                totalPenalty  += setScores[i, 2];
            }
            Debug.Log($"=== Game Over === Success:{totalSuccess} Foul:{totalFoul} Penalty:{totalPenalty}");
        }
    }
}
