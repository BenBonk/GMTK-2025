using System;
using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using JetBrains.Annotations;

public class GameManager : MonoBehaviour
{
    public bool isTesting;
    
    public Player player;
    public int harvestLevel = 1;
    public double startingPointRequirement = 65;
    public int roundNumber;
    public bool roundInProgress;
    public bool playerReady;
    public bool roundCompleted;
    public float roundDuration = 20f;
    public int roundsToWin = 20;
    public int maxSynergies;
    [HideInInspector] public float elapsedTime;
    public int predatorRoundFrequency;

    public GameObject wordPrefab; // Assign in inspector
    public GameObject endRoundPrefab;
    public float wordScaleDuration = 0.3f;
    public float wordDisplayDuration = 0.7f;
    [SerializeField] private Material lassoMaterialPreset;
    [SerializeField] private Material defaultMaterialPreset;

    [SerializeField] private CameraController cameraController;
    [SerializeField] private SpriteRenderer barn;
    [SerializeField] private RectTransform barnCameraTarget;
    [SerializeField] private Animator barnAnimator;
    private BoonManager boonManager;
    private LocalizationManager localization;
    public AnimalShopItem animalShopItem;


    private int lastDisplayedSecond = -1;
    [SerializeField] private Color timerNormalColor = Color.white;
    [SerializeField] private Color timerWarningColor = Color.red;

    public GameObject shopButtonBlocker;
    private CaptureManager captureManager;

    public int endDayCash = 50;
    private int cashInterest = 0;
    private bool endlessSelected = false;
    public bool isTutorial;


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
    public LocalizedString cashLocalString;
    public LocalizedString pointsLocalString;
    private double _pointsThisRound;
    public double pointsThisRound
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

    public event System.Action<double> OnPointsChanged;
    
    public TextMeshProUGUI scoreDisplay;
    public TextMeshProUGUI timerDisplay;
    public TextMeshProUGUI currencyDisplay;
    public TextMeshProUGUI lassosDisplay;
    public RectTransform deathPanel;
    public RectTransform winPanel;
    public RectTransform playArea;
    public TMP_Text roundNumberDeath;
    public TMP_Text winRoundsText;
    public LassoController lassoController;
    public float pointsRequirementGrowthRate;
    public LocalizedString localReady;
    public LocalizedString localSet;
    public LocalizedString localLasso;
    private SaveManager saveManager;
    private PauseMenu pauseMenu;
    public SchemeManager schemeManager;
    public Boon fairyBottleInstance;
    [HideInInspector] public AnimalData foxThiefStolenStats;
    private void Start()
    {
        pauseMenu = GameController.pauseMenu;
        saveManager = GameController.saveManager;
        localization = GameController.localizationManager;
        boonManager = GameController.boonManager;
        captureManager = GameController.captureManager;
        if (isTutorial)
        {
            return;
        }
        saveManager.LoadGameData();
        ApplyHarvestLevel();
        if (isTesting)
        {
            pointsRequirementGrowthRate = 0;
            startingPointRequirement = 0;
            roundDuration = 3;
            player.playerCurrency = 10000;
        }
        elapsedTime = 0;
        //lassosUsed = 0;
        player.OnCurrencyChanged += UpdatecurrencyDisplay;
        OnPointsChanged += UpdateScoreDisplay;
        Invoke("StartRound", 1);
    }

    private void ApplyHarvestLevel()
    {
        roundDuration = saveManager.harvestDatas[harvestLevel - 1].roundLength;
        endDayCash = saveManager.harvestDatas[harvestLevel - 1].dailyCash;
        roundsToWin = saveManager.harvestDatas[harvestLevel - 1].numberOfDays;
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
        if (!pauseMenu.isOpen)
        {
            pauseMenu.canOpenClose = true;   
        }
        if (boonManager.ContainsBoon("Pocketwatch"))
        {
            roundDuration += 5;
        }
        if (boonManager.ContainsBoon("BountifulHarvest"))
        {
            endDayCash += 20;
        }
        captureManager.herdPointMultBonus = 0.1f;
        if (boonManager.ContainsBoon("HerdMentality"))
        {
            captureManager.herdPointMultBonus = .2f;
        }

        if (boonManager.ContainsBoon("CoinPouch"))
        {
            cashInterest = Mathf.RoundToInt(((int)player.playerCurrency + endDayCash) * .1f);
        }
        pointsThisRound = 0;
        UpdateUI();
        //lassosDisplay.text = "Lassos: " + player.lassosPerRound;
        saveManager.SaveGameData();
        roundNumber++;
        UpdateScoreDisplay(0);
        roundInProgress = true;
        roundCompleted = false;
        barnAnimator.Play("Closed", 0, 0.1f);
        StartCoroutine(ShowReadySetLassoSequence());
        if (roundNumber > FBPP.GetInt("highestRound"))
        {
            FBPP.SetInt("highestRound", roundNumber);
        }
    }

    public void UpdateUI()
    {
        if (roundNumber==0)
        {
            return;
        }
        //scoreDisplay.text = "POINTS: " + LassoController.FormatNumber(pointsThisRound)  + " / " + LassoController.FormatNumber(roundsPointsRequirement[roundNumber]);
        //timerDisplay.text = "TIME: " + roundDuration.ToString("F1") + "s";
        //currencyDisplay.text = "CASH: " + LassoController.FormatNumber(player.playerCurrency);
    }

    public void EndRound()
    {
        if (boonManager.ContainsBoon("Pocketwatch"))
        {
            roundDuration -= 5;
        }
        roundCompleted = true;
        roundInProgress = false;
        elapsedTime = 0;
        playerReady = false;
        

        //UNCOMMENT BELOW FOR PROD

        if (pointsThisRound < GetPointsRequirement() )
        {
            //GameOver
            roundNumberDeath.text = localization.localDeathRound.GetLocalizedString() + " " + roundNumber;
            deathPanel.gameObject.SetActive(true);
            deathPanel.DOAnchorPosY(0, 1f).SetEase(Ease.InOutBack);
            GameController.predatorSelect.darkCover.gameObject.SetActive(true);
            GameController.predatorSelect.darkCover.DOFade(0.5f, 1f);
            StartCoroutine(CheckIfStillDead());
            return;
        }
        StartCoroutine(EndRoundRoutine());
    }

    public double GetPointsRequirement()
    {
        double value = startingPointRequirement * Math.Pow(pointsRequirementGrowthRate, roundNumber);
        return Math.Round(value / 5.0) * 5.0;
    }
    

    IEnumerator CheckIfStillDead()
    {
        yield return new WaitForSeconds(0.25f);
        lassoController.canLasso = false;
        if (lassoController.lineRenderer != null)
        {
            Destroy(lassoController.lineRenderer.gameObject);
        }
        yield return new WaitForSeconds(1.75f);
        if (pointsThisRound >= GetPointsRequirement() || boonManager.ContainsBoon("FairyBottle"))
        {
            deathPanel.DOAnchorPosY(909, 0.5f).SetEase(Ease.InBack);
            GameController.predatorSelect.darkCover.DOFade(0f, 0.5f).OnComplete(()=>GameController.predatorSelect.darkCover.enabled=false);
            yield return new WaitForSeconds(.5f);
            if (boonManager.ContainsBoon("FairyBottle"))
            {
                DisplayPopupWord(localization.fairyBottle.GetLocalizedString(), .3f, 1f, false);
                boonManager.RemoveBoon(fairyBottleInstance);
            }
            else
            {
                DisplayPopupWord(localization.closeCall, .3f, .5f, false);   
            }
            //think we need some sfx here
            FBPP.SetInt("closeCalls", FBPP.GetInt("closeCalls")+1);
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(EndRoundRoutine());
            deathPanel.gameObject.SetActive(false);
        }
        saveManager.ClearGame();
    }

    private IEnumerator EndRoundRoutine()
    {
        AudioManager.Instance.PlayMusicWithFadeOutOld("ambient", 1f);
        // First message
        DisplayPopupWord(localization.timesUp, wordScaleDuration, wordDisplayDuration, true);
        AudioManager.Instance.PlaySFX("time_up");
        yield return new WaitForSeconds(0.25f);
        lassoController.canLasso = false;
        if (lassoController.lineRenderer != null)
        {
            Destroy(lassoController.lineRenderer.gameObject);
        }
        yield return new WaitForSeconds(wordDisplayDuration + wordScaleDuration + 0.25f); // wait before next
        if (roundNumber == roundsToWin)
        {
            AudioManager.Instance.PlaySFX("round_win");
            endlessSelected = false;
            winRoundsText.text = localization.localWinRound.GetLocalizedString() + " " + roundsToWin + " " + localization.localRound.GetLocalizedString();
            winPanel.gameObject.SetActive(true);
            foreach (var ps in winPanel.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Play();
            }
            RectTransform children = winPanel.Find("Children") as RectTransform;
            children.DOAnchorPosY(0, 1f).SetEase(Ease.InOutBack);
            GameController.predatorSelect.darkCover.gameObject.SetActive(true);
            GameController.predatorSelect.darkCover.DOFade(0.5f, 1f);
            while (!endlessSelected)
            {
                yield return null;
            }
            children.DOAnchorPosY(909, 0.5f).SetEase(Ease.InBack);
            GameController.predatorSelect.darkCover.DOFade(0f, 0.5f).OnComplete(() => GameController.predatorSelect.darkCover.enabled = false);
            foreach (var ps in winPanel.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop();
            }
            yield return new WaitForSeconds(.5f);
        }

        // Second message
        double cashGained = endDayCash + cashInterest;
        //if farmer = farmer0
        if (true)
        {
            cashGained *= 2;
        }
        //localization.localPointsString.Arguments[0] = pointsThisRound;
        //localization.localPointsString.RefreshString();
        //localization.localDayComplete.Arguments[0] = cashGained;
        //localization.localDayComplete.RefreshString();
        DisplayCashWord(localization.dayComplete, wordScaleDuration, wordDisplayDuration, false);
        AudioManager.Instance.PlaySFX("cash_register");
        GameController.player.playerCurrency += cashGained;
        yield return new WaitForSeconds(wordDisplayDuration + wordScaleDuration + 0.5f); // final wait
        winPanel.gameObject.SetActive(false);
        if (boonManager.ContainsBoon("BountifulHarvest"))
        {
            endDayCash -= 20;
        }

        captureManager.firstCapture = false;
        cashInterest = 0;
        captureManager.mootiplierMult = 0;

        predatorRoundFrequency = 3;
        if (boonManager.ContainsBoon("PredatorPurge"))
        {
            predatorRoundFrequency = 5;
        }
        // After both messages
        if (roundNumber % predatorRoundFrequency == 0)
        {
            GameController.predatorSelect.StartCoroutine("Intro");
        }
        else
        {
            pauseMenu.canOpenClose = false;
            GameController.shopManager.InitializeAllUpgrades();
            GoToShop();
        }
    }

    public void GoEndless()
    {
        endlessSelected = true;
    }

    public void GoToShop()
    {
        GameController.rerollManager.Reset();
        saveManager.SaveGameData();
        cameraController.AnimateToRect(
            barnCameraTarget,
            delay: .5f,
        onZoomMidpoint: () =>
        {
            barnAnimator.Play("Open", 0, 0.1f);
            AudioManager.Instance.PlaySFX("barn_door");
            AudioManager.Instance.PlayMusicWithFadeOutOld("shop_theme", 1f, true);
        },
            onZoomEndpoint: () =>
            {
                barn.DOFade(0f, 1f)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    GameController.shopManager.cantPurchaseItem = false;
                    shopButtonBlocker.SetActive(false);
                    pauseMenu.canOpenClose = true;
                });
            }
        );
    }

    public void LeaveShop()
    {
        if (GameController.shopManager.cantPurchaseItem)
        {
            return;
        }

        pauseMenu.canOpenClose = false;
        schemeManager.SetRandomScheme();
        AudioManager.Instance.PlayMusicWithFadeOutOld("ambient", 1f);
        shopButtonBlocker.SetActive(true);
        UpdateUI();
        barn.DOFade(1f, 1f).SetEase(Ease.OutSine).OnComplete(()=>Invoke("StartRound", 2.25f));
        cameraController.ResetToStartPosition(1f);
    }

    private void UpdateScoreDisplay(double newPoints)
    {
        scoreDisplay.text = pointsLocalString.GetLocalizedString() + " " + LassoController.FormatNumber(newPoints) + " / " + LassoController.FormatNumber(GetPointsRequirement());
        scoreDisplay.transform.localScale = Vector3.one * 1.1f;
        scoreDisplay.transform.localRotation = Quaternion.identity; // reset

        float angle = UnityEngine.Random.Range(-10f, 10f); 

        Sequence pulse = DOTween.Sequence();
        pulse.Append(scoreDisplay.transform.DOScale(1.10f, 0.15f).SetEase(Ease.OutBack));
        pulse.Append(scoreDisplay.transform.DOShakeRotation(
            duration: 0.15f,
            strength: new Vector3(0f, 0f, 6f), 
            vibrato: 5,
            randomness: 90,
            fadeOut: true
        ));
        pulse.Append(scoreDisplay.transform.DOScale(1f, 0.2f).SetEase(Ease.OutExpo));
        pulse.Join(scoreDisplay.transform.DOLocalRotate(Vector3.zero, 0.2f, RotateMode.Fast));
    }

    private void UpdatecurrencyDisplay(double newcurrency)
    {
        if (!roundInProgress)
        {
            saveManager.SaveGameData();
        }
        currencyDisplay.text = $"CASH: {LassoController.FormatNumber(newcurrency)}";
        currencyDisplay.transform.localScale = Vector3.one * 1.1f;
        currencyDisplay.transform.localRotation = Quaternion.identity; // reset
        if (newcurrency > FBPP.GetFloat("highestCash"))
        {
            FBPP.SetFloat("highestCash", (float)newcurrency);
        }

        float angle = UnityEngine.Random.Range(-10f, 10f);

        Sequence pulse = DOTween.Sequence();
        pulse.Append(currencyDisplay.transform.DOScale(1.10f, 0.15f).SetEase(Ease.OutBack));
        pulse.Append(currencyDisplay.transform.DOShakeRotation(
            duration: 0.15f,
            strength: new Vector3(0f, 0f, 6f),
            vibrato: 5,
            randomness: 90,
            fadeOut: true
        ));
        pulse.Append(currencyDisplay.transform.DOScale(1f, 0.2f).SetEase(Ease.OutExpo));
        pulse.Join(currencyDisplay.transform.DOLocalRotate(Vector3.zero, 0.2f, RotateMode.Fast));
    }

    private void UpdateTimerDisplay()
    {
        float remaining = roundDuration - elapsedTime;
        int currentSecond = Mathf.FloorToInt(remaining);

        timerDisplay.text = $"TIME: {remaining:F1}s";
        localization.localTimeString.Arguments[0] = remaining.ToString("F1");
        localization.localTimeString.RefreshString();

        if (remaining <= 10f && currentSecond != lastDisplayedSecond)
        {
            lastDisplayedSecond = currentSecond;

            timerDisplay.color = timerWarningColor;
            timerDisplay.transform.localScale = Vector3.one * 1.3f;
            AudioManager.Instance.PlaySFX("tick");


            Sequence pulse = DOTween.Sequence();
            pulse.Append(timerDisplay.transform.DOScale(1.5f, 0.15f).SetEase(Ease.OutBack));
            pulse.Append(timerDisplay.transform.DOScale(1f, 0.2f).SetEase(Ease.OutExpo));

            pulse.Join(timerDisplay.DOColor(timerNormalColor, 0.4f));
        }
    }

    public IEnumerator ShowReadySetLassoSequence()
    {
        pauseMenu.canOpenClose = true;
        string[] readySetLasso = { localReady.GetLocalizedString(), localSet.GetLocalizedString(), localLasso.GetLocalizedString() };
        for (int i = 0; i < 3; i++)
        {
            if (i == 2)
            {
                // Use lasso material for the last word
                DisplayPopupWord(readySetLasso[i], wordScaleDuration, wordDisplayDuration, true,
                    lassoMaterialPreset);
                AudioManager.Instance.PlayNextPlaylistTrack();
                AudioManager.Instance.PlaySFX("rooster");
                lassoController.canLasso = true;
                playerReady = true;
            }
            else
            {
                // Use default material for other words
                DisplayPopupWord(readySetLasso[i], wordScaleDuration, wordDisplayDuration, false,
                    defaultMaterialPreset);
                AudioManager.Instance.PlaySFX("ready");
            }
            yield return new WaitForSeconds(wordDisplayDuration + wordScaleDuration + 0.5f); // small delay before next word
        }
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


    public void DisplayCashWord(string word, float scaleDuration = 0.3f, float displayDuration = 1.2f, bool shake = false, Material overrideMaterial = null)
    {
        GameObject wordObj = Instantiate(endRoundPrefab, transform);
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

    private void OnApplicationQuit()
    {
        if (pointsThisRound < GetPointsRequirement() && deathPanel.gameObject.activeInHierarchy)
        {
            saveManager.ClearGame();
        }
    }
}