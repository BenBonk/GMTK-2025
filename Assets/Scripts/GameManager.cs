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

    //private int _lassosUsedThisRound;
    /*public int lassosUsed
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
    }*/

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
    public TextMeshProUGUI currencyDisplay;
    public TextMeshProUGUI lassosDisplay;

    private void Start()
    {
        elapsedTime = 0;
        //lassosUsed = 0;
        player.OnCurrencyChanged += UpdatecurrencyDisplay;
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
        scoreDisplay.text = "POINTS: " + pointsThisRound  + " / " + roundsPointsRequirement[roundNumber];
        timerDisplay.text = "TIME: " + roundDuration.ToString("F1") + "s";
        currencyDisplay.text = "CASH: " + player.playerCurrency;
        //lassosDisplay.text = "Lassos: " + player.lassosPerRound;
        roundNumber++;
        roundInProgress = true;
    }

    private void UpdateScoreDisplay(int newPoints)
    {
        scoreDisplay.text = $"POINTS: {newPoints} / {roundsPointsRequirement[roundNumber]}";
    }

    private void UpdatecurrencyDisplay(int newcurrency)
    {
        currencyDisplay.text = $"CASH: {newcurrency}";
    }

    private void UpdateTimerDisplay()
    {
        timerDisplay.text = $"TIME: {roundDuration-elapsedTime:F1}s";
    }

    private void UpdateLassosDisplay(int usedLassos)
    {
        lassosDisplay.text = $"Lassos: {player.lassosPerRound  - usedLassos}";
    }



}