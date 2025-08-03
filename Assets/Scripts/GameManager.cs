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
    public int predatorRoundFrequency;

    public GameObject wordPrefab; // Assign in inspector
    public float wordScaleDuration = 0.3f;
    public float wordDisplayDuration = 0.7f;
    [SerializeField] private Material lassoMaterialPreset;
    [SerializeField] private Material defaultMaterialPreset;

    [SerializeField] private CameraController cameraController;
    [SerializeField] private SpriteRenderer barn;
    [SerializeField] private Transform barnCameraTarget;
    [SerializeField] private Animator barnAnimator;


    private int lastDisplayedSecond = -1;
    [SerializeField] private Color timerNormalColor = Color.white;
    [SerializeField] private Color timerWarningColor = Color.red;

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
    public RectTransform deathPanel;
    public TMP_Text roundNumberDeath;
    public LassoController lassoController;
    public float pointsRequirementGrowthRate;

    private void Start()
    {
        for (int i = 0; i < roundsPointsRequirement.Length; i++)
        {
            float rawScore = 25 * Mathf.Pow(pointsRequirementGrowthRate, i);
            int roundedToFive = Mathf.RoundToInt(rawScore / 5f) * 5;
            roundsPointsRequirement[i] = roundedToFive;
        }
        elapsedTime = 0;
        //lassosUsed = 0;
        player.OnCurrencyChanged += UpdatecurrencyDisplay;
        OnPointsChanged += UpdateScoreDisplay;
        OnLassosChanged += UpdateLassosDisplay;
        Invoke("StartRound", 1);
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
        UpdateUI();
        //lassosDisplay.text = "Lassos: " + player.lassosPerRound;
        roundNumber++;
        roundInProgress = true;
        roundCompleted = false;
        barnAnimator.Play("Closed", 0, 0.1f);
        StartCoroutine(ShowReadySetLassoSequence());
    }

    public void UpdateUI()
    {
        scoreDisplay.text = "POINTS: " + pointsThisRound  + " / " + roundsPointsRequirement[roundNumber];
        timerDisplay.text = "TIME: " + roundDuration.ToString("F1") + "s";
        currencyDisplay.text = "CASH: " + player.playerCurrency;
    }

    public void EndRound()
    {
        if (lassoController.lineRenderer!=null)
        {
            Destroy(lassoController.lineRenderer.gameObject);
        }
        roundCompleted = true;
        roundInProgress = false;
        elapsedTime = 0;
        playerReady = false;
        lassoController.canLasso = false;
        
        //UNCOMMENT BELOW FOR PROD
        /*
        if (pointsThisRound < roundsPointsRequirement[roundNumber-1])
        {
            //GameOver
            roundNumberDeath.text = "Round: " + roundNumber;
            deathPanel.gameObject.SetActive(true);
            deathPanel.DOAnchorPosY(0,1f).SetEase(Ease.InOutBack);
            GameController.predatorSelect.darkCover.DOFade(0.5f, 1f);
            return;
        }
        */
        
        DisplayPopupWord("TIME'S UP!", wordScaleDuration, wordDisplayDuration, true);
        if (roundNumber%predatorRoundFrequency==0)
        {
            GameController.predatorSelect.StartCoroutine("Intro");
        }
        else
        {
            GameController.shopManager.InitializeAllUpgrades();
            GoToShop();
        }
    }

    public void GoToShop()
    {
        cameraController.AnimateToTarget(
            barnCameraTarget.transform,
            delay: .5f,
        onZoomMidpoint: () =>
        {
            barnAnimator.Play("Open", 0, 0.1f);
            AudioManager.Instance.CrossfadeMusic("shop_theme", 2f); // fade duration = 2s (or whatever you like)
        },
            onZoomEndpoint: () =>
            {
                barn.DOFade(0f, 1f).SetEase(Ease.OutSine).OnComplete(()=> GameController.shopManager.cantPurchaseItem = false);
            }
        );
    }

    public void LeaveShop()
    {
        AudioManager.Instance.CrossfadeToRandomPlaylistTrack();
        if (GameController.shopManager.cantPurchaseItem)
        {
            return;
        }
        UpdateUI();
        barn.DOFade(1f, 1f).SetEase(Ease.OutSine).OnComplete(()=>Invoke("StartRound", 2.25f));
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
        float remaining = roundDuration - elapsedTime;
        int currentSecond = Mathf.FloorToInt(remaining);

        timerDisplay.text = $"TIME: {remaining:F1}s";

        if (remaining <= 10f && currentSecond != lastDisplayedSecond)
        {
            lastDisplayedSecond = currentSecond;

            timerDisplay.color = timerWarningColor;
            timerDisplay.transform.localScale = Vector3.one * 1.3f;

            Sequence pulse = DOTween.Sequence();
            pulse.Append(timerDisplay.transform.DOScale(1.5f, 0.15f).SetEase(Ease.OutBack));
            pulse.Append(timerDisplay.transform.DOScale(1f, 0.2f).SetEase(Ease.OutExpo));

            pulse.Join(timerDisplay.DOColor(timerNormalColor, 0.4f));
        }
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
            if (i == 2)
            {
                // Use lasso material for the last word
                DisplayPopupWord(words[i], wordScaleDuration, wordDisplayDuration, i == 2, lassoMaterialPreset);
            }
            else
            {
                // Use default material for other words
                DisplayPopupWord(words[i], wordScaleDuration, wordDisplayDuration, i == 2, defaultMaterialPreset);
            }

            yield return new WaitForSeconds(wordDisplayDuration + wordScaleDuration + 0.5f); // small delay before next word
        }
        playerReady = true;
        lassoController.canLasso = true;
    }

    private Vector3 GetCenterScreenWorldPosition()
    {
        float z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, z);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenCenter);
        worldPos.z = 0f;
        return worldPos;
    }


    public void DisplayPopupWord(string word, float scaleDuration = 0.3f, float displayDuration = 1.2f, bool shake = false, Material overrideMaterial = null)
    {
        GameObject wordObj = Instantiate(wordPrefab, transform);
        TMP_Text wordText = wordObj.GetComponent<TMP_Text>();

        wordText.text = word;

        // Apply material override if provided
        if (overrideMaterial != null)
        {
            wordText.fontSharedMaterial = overrideMaterial;
        }

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
                strength: new Vector3(0f, 0f, 20f), // Z-axis only
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