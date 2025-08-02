using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Player player;
    public int[] roundsPointsRequirement;
    public int roundNumber;
    public bool roundInProgress;
    public bool playerReady;
    public bool roundCompleted;
    public float roundDuration = 20f;
    private float elapsedTime;

    public GameObject wordPrefab; // Assign in inspector
    public float wordScaleDuration = 0.3f;
    public float wordDisplayDuration = 0.7f;

    [SerializeField] private CameraController cameraController;
    [SerializeField] private GameObject barn;

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
        if (!roundCompleted && roundDuration - elapsedTime <= 0)
        {
            EndRound();
        }
        else
        {
            if (roundInProgress && playerReady)
            {
                elapsedTime += Time.deltaTime;
                UpdateTimerDisplay();
            }
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
        roundCompleted = false;
        StartCoroutine(ShowReadySetLassoSequence());
    }

    public void EndRound()
    {
        roundCompleted = true;
        roundInProgress = false;
        elapsedTime = 0;
        playerReady = false;
        DisplayPopupWord("TIME'S UP!", wordScaleDuration, wordDisplayDuration, true);
        cameraController.AnimateToTarget(barn.transform, 3f);
    }

    public void LeaveShop()
    {
        cameraController.ResetToStartPosition(1f);
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

    public IEnumerator ShowReadySetLassoSequence()
    {
        string[] words = { "READY?", "SET", "LASSO!" };

        for (int i = 0; i < words.Length; i++)
        {
            DisplayPopupWord(words[i], wordScaleDuration, wordDisplayDuration, i == 2);

            yield return new WaitForSeconds(wordDisplayDuration + wordScaleDuration + 0.5f); // small delay before next word
        }

        playerReady = true;
    }

    private Vector3 GetCenterScreenWorldPosition()
    {
        float z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, z);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenCenter);
        worldPos.z = 0f;
        return worldPos;
    }


    public void DisplayPopupWord(string word, float scaleDuration = 0.3f, float displayDuration = 1.2f, bool shake = false)
    {
        GameObject wordObj = Instantiate(wordPrefab, transform);
        TMP_Text wordText = wordObj.GetComponent<TMP_Text>();

        wordText.text = word;
        wordObj.transform.position = GetCenterScreenWorldPosition();
        wordObj.transform.localScale = Vector3.zero;
        wordObj.transform.rotation = Quaternion.identity;
        wordObj.SetActive(true);

        Sequence seq = DOTween.Sequence();

        // Scale pop-in
        seq.Append(wordObj.transform.DOScale(1.2f, scaleDuration).SetEase(Ease.OutBack));
        seq.Append(wordObj.transform.DOScale(1f, 0.15f).SetEase(Ease.OutCubic));

        // Optional shake
        if (shake)
        {
            seq.Append(wordObj.transform.DOShakeRotation(
                duration: 0.4f,
                strength: new Vector3(0f, 0f, 20f), // Shake on Z axis
                vibrato: 10,
                randomness: 90,
                fadeOut: true
            ));
        }

        // Fade out and destroy
        seq.AppendInterval(displayDuration);
        seq.AppendCallback(() => wordText.DOFade(0f, 0.5f));
        seq.AppendCallback(() => Destroy(wordObj, 0.6f));
    }


}