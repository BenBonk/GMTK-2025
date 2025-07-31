using TMPro;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Player player;
    public int[] roundsPointsRequirement;
    public int roundNumber;
    public bool roundInProgress;
    public float roundDuration = 20f;
    private float elapsedTime;

    private int _lassosUsedThisRound;
    public int lassosUsed
        {
        get => _lassosUsedThisRound;
        set
        {
            if (_lassosUsedThisRound != value)
            {
                _lassosUsedThisRound = value;
                OnLassosChanged?.Invoke(_lassosUsedThisRound);
            }
        }
    }

    private int _pointsThisRound;
    public int pointsThisRound
    {
        get => _pointsThisRound;
        set
        {
            if (_pointsThisRound != value)
            {
                _pointsThisRound = value;
                OnPointsChanged?.Invoke(_pointsThisRound);
            }
        }
    }

    public event System.Action<int> OnPointsChanged;
    public event System.Action<int> OnLassosChanged;


    public TextMeshProUGUI scoreDisplay;
    public TextMeshProUGUI timerDisplay;
    public TextMeshProUGUI cashDisplay;
    public TextMeshProUGUI lassosDisplay;

    private void Start()
    {
        elapsedTime = 0;
        lassosUsed = 0;
        player.OnCurrencyChanged += UpdateCashDisplay;
        OnPointsChanged += UpdateScoreDisplay;
        OnLassosChanged += UpdateLassosDisplay;
    }

    private void Update()
    {
        if (roundInProgress)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
    }


    public void StartRound()
    {
        pointsThisRound = 0;
        scoreDisplay.text = "Score: " + pointsThisRound  + " / " + roundsPointsRequirement[roundNumber];
        timerDisplay.text = "Time: " + roundDuration.ToString("F1") + "s";
        cashDisplay.text = "Cash: " + player.playerCurrency;
        lassosDisplay.text = "Lassos: " + player.lassosPerRound;
        roundNumber++;
        roundInProgress = true;
    }

    private void UpdateScoreDisplay(int newPoints)
    {
        scoreDisplay.text = $"Score: {newPoints} / {roundsPointsRequirement[roundNumber]}";
    }

    private void UpdateCashDisplay(int newCash)
    {
        cashDisplay.text = $"Cash: {newCash}";
    }

    private void UpdateTimerDisplay()
    {
        timerDisplay.text = $"Time: {roundDuration-elapsedTime:F1}s";
    }

    private void UpdateLassosDisplay(int usedLassos)
    {
        lassosDisplay.text = $"Lassos: {player.lassosPerRound  - usedLassos}";
    }



}