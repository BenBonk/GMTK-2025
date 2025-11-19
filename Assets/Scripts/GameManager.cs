using System;
using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using JetBrains.Annotations;
using System.Collections.Generic;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public bool isTesting;
    
    public Player player;
    public int harvestLevel = 1;
    public int farmerID = 0;
    //public double startingPointRequirement = 65;
    public PointQuotaSetting quotaSetting;
    public int roundNumber;
    public bool roundInProgress;
    public bool playerReady;
    public bool roundCompleted;
    public float roundDuration = 20f;
    public int roundsToWin = 20;
    [HideInInspector] public float elapsedTime;
    public int predatorRoundFrequency;
    public int challengeRoundFrequency;

    public GameObject wordPrefab;
    public GameObject dayBeginPrefab;
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
    public AnimalSpawner spawner;

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
    /*public float noviceRate = 1.25f;
    public float veteranRate = 1.4f;
    public float expertRate = 1.6f;
    private float pointsRequirementGrowthRate;*/
    public LocalizedString challengeRound;
    public LocalizedString localReady;
    public LocalizedString localSet;
    public LocalizedString localLasso;
    private SaveManager saveManager;
    private PauseMenu pauseMenu;
    public SchemeManager schemeManager;
    public Boon fairyBottleInstance;
    public GameObject extraUpgradeSlot;
    public GameObject unlockPanel;
    [HideInInspector] public AnimalData foxThiefStolenStats;
    private RandomEventManager randomEventManager;
    private ChallengeEventManager challengeEventManager;
    private SteamIntegration steamIntegration;
    private string roundDescription;
    private int roundID = -1;
    private void Start()
    {
        randomEventManager = GameController.randomEventManager;
        challengeEventManager = GameController.challengeEventManager;
        pauseMenu = GameController.pauseMenu;
        saveManager = GameController.saveManager;
        localization = GameController.localizationManager;
        boonManager = GameController.boonManager;
        captureManager = GameController.captureManager;
        steamIntegration = GameController.steamIntegration;
        if (isTutorial)
        {
            return;
        }
        saveManager.LoadGameData();
        if (farmerID == 1)
        {
            extraUpgradeSlot.SetActive(true);
        }
        else
        {
            extraUpgradeSlot.SetActive(false);
        }
        ApplyHarvestLevel();
        if (isTesting)
        {
            quotaSetting = PointQuotaSetting.None;
            roundDuration = 3;
            player.playerCurrency = 10000;
        }
        elapsedTime = 0;
        //lassosUsed = 0;
        player.OnCurrencyChanged += UpdatecurrencyDisplay;
        OnPointsChanged += UpdateScoreDisplay;
        scoreDisplay.text = pointsLocalString.GetLocalizedString() + " " + LassoController.FormatNumber(0) + " / " + LassoController.FormatNumber(GetPointsRequirement(roundNumber+1));
        RoundSetup();
        Invoke("StartRound", 1);
        UpdateTimerDisplay();
        UpdatecurrencyDisplay(player.playerCurrency);
    }
    
    private void ApplyHarvestLevel()
    {
        roundDuration = saveManager.harvestDatas[harvestLevel - 1].roundLength;
        endDayCash = saveManager.harvestDatas[harvestLevel - 1].dailyCash;
        roundsToWin = saveManager.harvestDatas[harvestLevel - 1].numberOfDays;
        quotaSetting = saveManager.harvestDatas[harvestLevel - 1].pointQuotas;
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
        StartCoroutine(DisplayDayRoutine(GameController.shopManager.roundLocalString.GetLocalizedString() + " " + roundNumber, wordScaleDuration, wordDisplayDuration));
        if (roundNumber==50&& !steamIntegration.IsThisAchievementUnlocked("Keep Going"))
        {
            steamIntegration.UnlockAchievement("Keep Going");
        }
        else if (roundNumber == 100 && !steamIntegration.IsThisAchievementUnlocked("Keep Going++"))
        {
            steamIntegration.UnlockAchievement("Keep Going++");
        }
    }

    public void RoundSetup()
    {
        if (!pauseMenu.isOpen)
        {
            pauseMenu.canOpenClose = true;
        }
        if (boonManager.ContainsBoon("Pocketwatch"))
        {
            roundDuration = saveManager.harvestDatas[harvestLevel - 1].roundLength + 10;
        }
        else
        {
            roundDuration = saveManager.harvestDatas[harvestLevel - 1].roundLength;
        }

        if (isTesting)
        {
            roundDuration = 3;
            quotaSetting = PointQuotaSetting.None;
        }
        pointsThisRound = 0;
        saveManager.SaveGameData();
        if (roundNumber > FBPP.GetInt("highestRound"))
        {
            FBPP.SetInt("highestRound", roundNumber);
        }
        UpdateScoreDisplay(0);
        UpdateTimerDisplay();
        barnAnimator.Play("Closed", 0, 0.1f);
        spawner.spawnRate = FBPP.GetFloat("spawnRate", 1f);
        if (IsChallengeRound())
        {
           roundID = challengeEventManager.GetChallengeEvent();
            roundDescription = challengeEventManager.challengeEventStrings[roundID].GetLocalizedString();
            if (roundID < 4)
            {
                schemeManager.ChangeScheme(roundID);
            }
            else
            {
                schemeManager.SetRandomScheme(roundNumber);
            }
            challengeEventManager.StartChallenge(roundID);
        }
        else
        {
            roundID = randomEventManager.GetRandomEvent();
            schemeManager.SetRandomScheme(roundNumber);
            if (roundID > -1)
            {
                roundDescription = randomEventManager.randomEventStrings[roundID].GetLocalizedString();
                randomEventManager.StartRandomEvent(roundID);
            }
            else
            {
                roundDescription = null;
            }
        }
    }

    public bool IsChallengeRound()
    {
        return roundNumber % challengeRoundFrequency == 0;
    }

    public void EndRound()
    {
        roundCompleted = true;
        roundInProgress = false;
        elapsedTime = 0;
        playerReady = false;

        AudioManager.Instance.FadeMusic(1.25f);
        if(!AudioManager.Instance.IsAmbientPlaying())
        {
            AudioManager.Instance.PlayAmbientWithFadeOutOld("ambient");
        }

        if (player.playerCurrency >= 100000 && !steamIntegration.IsThisAchievementUnlocked("Hoarder"))
        {
            steamIntegration.UnlockAchievement("Hoarder");
        }
        if (pointsThisRound < GetPointsRequirement() )
        {
            //GameOver
            pauseMenu.canOpenClose = false;
            roundNumberDeath.text = localization.localDeathRound.GetLocalizedString() + " " + roundNumber;
            deathPanel.gameObject.SetActive(true);
            deathPanel.DOAnchorPosY(0, 1f).SetEase(Ease.InOutBack);
            GameController.predatorSelect.darkCover.enabled = true;
            GameController.predatorSelect.darkCover.DOFade(0.5f, 1f);
            StartCoroutine(CheckIfStillDead());
            return;
        }
        StartCoroutine(EndRoundRoutine());
    }

    public double GetPointsRequirement()
    {
        double value = 0;
        float R = roundNumber;
        switch (quotaSetting)
        {
            case PointQuotaSetting.None:
                return 0;
            case PointQuotaSetting.Novice:
                value = 55 + 10 * R + 50 * Math.Pow(Mathf.Floor((R - 1) / 5), 2) + (1.5f*R + 5)*(Math.Pow(1.28f,R+4));
                break;
            case PointQuotaSetting.Veteran:
                value = 50 + 20 * R + 50 * Math.Pow(Mathf.Floor((R - 1) / 5), 3) + (1.25f * R + 5) * (Math.Pow(1.38f, R + 3));
                break;
            case PointQuotaSetting.Expert:
                //placeholder
                value = 50 + 20 * R + 50 * Math.Pow(Mathf.Floor((R - 1) / 5), 3) + (1.25f * R + 5) * (Math.Pow(1.38f, R + 3));
                break;
            default:
                value = 0;
                break;
        }
        return Math.Round(value / 5.0) * 5.0; ;
    }

    public double GetPointsRequirement(int round)
    {
        double value = 0;
        float R = round;
        switch (quotaSetting)
        {
            case PointQuotaSetting.None:
                return 0;
            case PointQuotaSetting.Novice:
                value = 55 + 10 * R + 50 * Math.Pow(Mathf.Floor((R - 1) / 5), 2) + (1.5f * R + 5) * (Math.Pow(1.28f, R + 4));
                break;
            case PointQuotaSetting.Veteran:
                value = 50 + 20 * R + 50 * Math.Pow(Mathf.Floor((R - 1) / 5), 3) + (1.25f * R + 5) * (Math.Pow(1.38f, R + 3));
                break;
            case PointQuotaSetting.Expert:
                //placeholder
                value = 50 + 20 * R + 50 * Math.Pow(Mathf.Floor((R - 1) / 5), 3) + (1.25f * R + 5) * (Math.Pow(1.38f, R + 3));
                break;
            default:
                value = 0;
                break;
        }
        return Math.Round(value / 5.0) * 5.0; ;
    }


    IEnumerator CheckIfStillDead()
    {
        yield return new WaitForSeconds(0.25f);
        AudioManager.Instance.PlaySFX("round_lose");
        lassoController.canLasso = false;
        lassoController.DestroyLassoExit(true);
        yield return new WaitForSeconds(1.75f);
        if (pointsThisRound >= GetPointsRequirement() || boonManager.ContainsBoon("FairyBottle"))
        {
            deathPanel.DOAnchorPosY(909, 0.5f).SetEase(Ease.InBack);
            GameController.predatorSelect.darkCover.DOFade(0f, 0.5f).OnComplete(()=>GameController.predatorSelect.darkCover.enabled=false);
            yield return new WaitForSeconds(.5f);
            if (pointsThisRound >= GetPointsRequirement())
            {
                DisplayPopupWord(localization.closeCall, .3f, .5f, false);
            }
            else
            {
                DisplayPopupWord(localization.fairyBottle.GetLocalizedString(), .3f, 1f, false,null,new HashSet<Sprite>() { boonManager.boonDict["FairyBottle"].art });
                boonManager.RemoveBoon(fairyBottleInstance);
            }
            //think we need some sfx here
            AudioManager.Instance.PlaySFX("close_call");
            int closeCalls = FBPP.GetInt("closeCalls") + 1;
            FBPP.SetInt("closeCalls", closeCalls);
            if (closeCalls == 10 && !steamIntegration.IsThisAchievementUnlocked("Close One!"))
            {
                steamIntegration.UnlockAchievement("Close One!");
            }
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(EndRoundRoutine());
            deathPanel.gameObject.SetActive(false);
        }
        else
        {
            saveManager.ClearGame();
        }
    }

    private IEnumerator EndRoundRoutine()
    {
        //AudioManager.Instance.PlayMusicWithFadeOutOld("ambient", 1f);
        // First message
        DisplayPopupWord(localization.timesUp, wordScaleDuration, wordDisplayDuration, true);
        AudioManager.Instance.PlaySFX("time_up");
        yield return new WaitForSeconds(0.25f);
        lassoController.canLasso = false;
        lassoController.DestroyLassoExit(true);
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
            if (!steamIntegration.IsThisAchievementUnlocked("Victory"))
            {
                steamIntegration.UnlockAchievement("Victory");
            }
            if (player.boonsInDeck.Count == 0 && !steamIntegration.IsThisAchievementUnlocked("Boonless"))
            {
                steamIntegration.UnlockAchievement("Boonless");
            }
            if (GameController.animalLevelManager.AllAnimalsAtDefaultLevel() && !steamIntegration.IsThisAchievementUnlocked("Upgradeless"))
            {
                steamIntegration.UnlockAchievement("Upgradeless");
            }
            if (FBPP.GetInt("AnimalPurchasedThisGame") == 0 && !steamIntegration.IsThisAchievementUnlocked("Animalless"))
            {
                steamIntegration.UnlockAchievement("Animalless");
            }
            RectTransform children = winPanel.Find("Children") as RectTransform;
            children.DOAnchorPosY(0, 1f).SetEase(Ease.InOutBack);
            GameController.predatorSelect.darkCover.enabled = true;
            GameController.predatorSelect.darkCover.DOFade(0.5f, 1f);
            yield return new WaitForSeconds(1.5f);
            if (FBPP.GetInt("harvestLevelsUnlocked",1) <= harvestLevel && harvestLevel < saveManager.harvestDatas.Length)
            {
                FBPP.SetInt("harvestLevelsUnlocked", harvestLevel + 1);
                FBPP.Save();
                UnlockHarvestLevel(harvestLevel + 1);
            }
            if (!FBPP.GetBool("farmer1",false))
            {
                FBPP.SetBool("farmer1", true);
                FBPP.Save();
                UnlockFarmer(1);
            }

            while (!endlessSelected || GameController.wishlistPanel.isOpen)
            {
                yield return null;
            }
            children.DOAnchorPosY(950, 0.5f).SetEase(Ease.InBack);
            GameController.predatorSelect.darkCover.DOFade(0f, 0.5f).OnComplete(() => GameController.predatorSelect.darkCover.enabled = false);
            foreach (var ps in winPanel.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop();
            }
            yield return new WaitForSeconds(.5f);
        }

        cashInterest = 0;
        HashSet<Sprite> endDayBoonSprites = new HashSet<Sprite>();
        if (boonManager.ContainsBoon("CoinPouch"))
        {
            cashInterest = Mathf.RoundToInt(((int)player.playerCurrency + endDayCash) * .1f);
            endDayBoonSprites.Add(boonManager.boonDict["CoinPouch"].art);
        }
        if (boonManager.ContainsBoon("BountifulHarvest"))
        {
            cashInterest += 25;
            endDayBoonSprites.Add(boonManager.boonDict["BountifulHarvest"].art);
        }

        // Second message
        double cashGained = endDayCash + cashInterest;
        if (farmerID == 0)
        {
            cashGained *= 2;
        }
        //localization.localPointsString.Arguments[0] = pointsThisRound;
        //localization.localPointsString.RefreshString();
        localization.localDayComplete.Arguments[0] = roundNumber;
        localization.localDayComplete.Arguments[1] = cashGained;
        localization.localDayComplete.RefreshString();
        DisplayCashWord(localization.dayComplete, wordScaleDuration, wordDisplayDuration, false,null,endDayBoonSprites);
        AudioManager.Instance.PlaySFX("cash_register");
        GameController.player.playerCurrency += cashGained;
        yield return new WaitForSeconds(wordDisplayDuration + wordScaleDuration + 0.5f); // final wait
        winPanel.gameObject.SetActive(false);

        captureManager.firstCapture = false;
        captureManager.mootiplier = 1;

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
        else if (IsChallengeRound())
        {
            GameController.challengeRewardSelect.StartCoroutine("Intro");
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
        ShowRoundUI();
        LassoCleaner.CleanupAll();
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
            AudioManager.Instance.FadeAmbient(1f);
            GameController.postProcessingManager.NightModeOff();
            randomEventManager.rainParticles.Stop();
            randomEventManager.cloudParticles.Stop();
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
                    challengeEventManager.EndChallenge();
                    roundNumber++;
                    saveManager.SaveGameData();
                });
            }
        );
    }

    public void LeaveShop()
    {
        if (GameController.shopManager.cantPurchaseItem || shopButtonBlocker.activeInHierarchy)
        {
            return;
        }

        pauseMenu.canOpenClose = false;
        AudioManager.Instance.PlayMusicWithFadeOutOld("ambient", 1f);
        shopButtonBlocker.SetActive(true);
        RoundSetup();
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


            timerDisplay.color = timerWarningColor;
            timerDisplay.transform.localScale = Vector3.one * 1.3f;

            if (remaining <= 3 && currentSecond != lastDisplayedSecond)
            {
                if (remaining <=0)
                {
                }
                else
                {
                    AudioManager.Instance.PlaySimultaneousSFX("tick", "last_seconds");
                }
            }
            else
            {
                AudioManager.Instance.PlaySFX("tick");
            }
            lastDisplayedSecond = currentSecond;
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
                if (IsChallengeRound())
                {
                    switch (roundID)
                    {
                        case 0:
                            AudioManager.Instance.PlayMusicWithFadeOutOld("challenge_wind", 1f, true);
                            break;
                        case 1:
                            AudioManager.Instance.PlayMusicWithFadeOutOld("challenge_desert", 1f, true);
                            break;
                        case 2:
                            AudioManager.Instance.PlayMusicWithFadeOutOld("challenge_desert", 1f, true);
                            break;
                        case 3:
                            AudioManager.Instance.PlayMusicWithFadeOutOld("challenge_wind", 1f, true);
                            break;
                        case 4:
                            AudioManager.Instance.PlayMusicWithFadeOutOld("challenge_night", 1f, true);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    AudioManager.Instance.PlayNextPlaylistTrack();
                }
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


    public void DisplayPopupWord(string word, float scaleDuration = 0.3f, float displayDuration = 1.2f, bool shake = false, Material overrideMaterial = null, HashSet<Sprite> boonSprites = null)
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

        LassoController.CreateBoonIcons(wordObj.transform, boonSprites,1.5f, 1.4f, 0f, 1.4f);

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
        seq.Append(wordText.DOFade(0f, 0.5f));
        foreach (var sr in wordObj.GetComponentsInChildren<SpriteRenderer>())
        {
            seq.Join(sr.DOFade(0f, 0.5f));
        }
        seq.OnComplete(() => Destroy(wordObj));
    }


    public void DisplayCashWord(string word, float scaleDuration = 0.3f, float displayDuration = 1.2f, bool shake = false, Material overrideMaterial = null, HashSet<Sprite> boonSprites = null)
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

        LassoController.CreateBoonIcons(wordObj.transform, boonSprites,1.5f,1.4f,0f,1.4f);

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
        seq.Append(wordText.DOFade(0f, 0.5f));
        foreach (var sr in wordObj.GetComponentsInChildren<SpriteRenderer>())
        {
            seq.Join(sr.DOFade(0f, 0.5f));     
        }
        seq.OnComplete(() => Destroy(wordObj));
    }

    private IEnumerator DisplayDayRoutine(string word, float scaleDuration, float displayDuration)
    {
        roundCompleted = false;

        GameObject wordObj = Instantiate(dayBeginPrefab, transform);
        var wordText = wordObj.GetComponent<TMP_Text>();
        var typer = wordObj.GetComponent<TMPTypewriterSwap>();
        var rich = wordObj.GetComponent<PaletteLetterColorizerRichText>();

        // Add the skipper to the root you want clickable (this object or the word UI)
        var skipper = wordObj.GetComponent<RoutineSkipper>();
        if (!skipper) skipper = wordObj.AddComponent<RoutineSkipper>();
        skipper.useUnscaledTime = false;     
        skipper.holdToSpeedUp = true;
        skipper.holdSpeedMultiplier = 10f;


        wordObj.transform.position = GetCenterScreenWorldPosition();
        wordObj.transform.localScale = Vector3.zero;
        wordObj.transform.rotation = Quaternion.identity;
        wordText.text = word;
        wordObj.SetActive(true);
        AudioManager.Instance.PlaySFX("ready");
        spawner.currentShoe.Clear();

        // Scale in
        yield return skipper.AwaitTween(wordObj.transform.DOScale(1.2f, scaleDuration).SetEase(Ease.OutBack));
        yield return skipper.AwaitTween(wordObj.transform.DOScale(1f, 0.15f).SetEase(Ease.OutCubic));

        // Pause on screen
        yield return skipper.Wait(displayDuration + 0.25f);

        if (roundNumber % challengeRoundFrequency == 0)
        {
            // Erase, enable colors, type title
            typer.ChangeTextAnimated("", 0.04f);
            yield return skipper.AwaitTypewriter(typer);
            yield return skipper.Wait(displayDuration);
            if (rich) rich.enabled = true;
            AudioManager.Instance.PlaySFX("challenge_display");
            typer.ChangeTextAnimated(challengeRound.GetLocalizedString(), 0.04f, 0.04f);
            yield return skipper.AwaitTypewriter(typer);
            yield return skipper.Wait(displayDuration);
        }

        var descTMP = wordObj.transform.Find("ChallengeText")?.GetComponent<TMP_Text>();
        if (descTMP && roundDescription != null)
        {
            typer.SetLabel(descTMP);
            typer.SetRichColorizer(null);
            typer.InstantSet("");
            typer.ChangeTextAnimated(roundDescription,0.04f,0.04f);
            yield return skipper.AwaitTypewriter(typer);
            yield return skipper.Wait(displayDuration*3);
        }
        else
        {
            Debug.Log("No description TMP or no round description");
        }

            // Pause again
            yield return skipper.Wait(displayDuration/2);

        // Fade out text and sprites together (click skips to end)
        var fadeSeq = DG.Tweening.DOTween.Sequence().Join(wordText.DOFade(0f, 0.5f));
        foreach (var sr in wordObj.GetComponentsInChildren<TMP_Text>())
            fadeSeq.Join(sr.DOFade(0f, 0.5f));
        yield return skipper.AwaitTween(fadeSeq);

        Destroy(wordObj);
        roundInProgress = true;
        StartCoroutine(ShowReadySetLassoSequence());
    }


    private void OnApplicationQuit()
    {
        if (pointsThisRound < GetPointsRequirement() && deathPanel.gameObject.activeInHierarchy)
        {
            saveManager.ClearGame();
        }
    }

    private void UnlockFarmer(int farmerID)
    {
        Debug.Log("Unlocking farmer " + farmerID);
        GameObject newPanel = Instantiate(unlockPanel,GameObject.Find("UI").transform);
        newPanel.GetComponent<UnlockPanel>().SetupFarmerUnlock(farmerID);
        newPanel.GetComponent<UnlockPanel>().Open();
    }

    private void UnlockHarvestLevel(int level)
    {
        Debug.Log("Unlocking harvest level " + level);
        GameObject newPanel = Instantiate(unlockPanel, GameObject.Find("UI").transform);
        newPanel.GetComponent<UnlockPanel>().SetupHarvestLevelUnlock(level);
        newPanel.GetComponent<UnlockPanel>().Open();
    }

    public void HideRoundUI()
    {
        scoreDisplay.DOFade(0f, 0.5f).OnComplete(() => scoreDisplay.gameObject.SetActive(false));
        timerDisplay.DOFade(0f, 0.5f).OnComplete(() => timerDisplay.gameObject.SetActive(false));
        currencyDisplay.DOFade(0f, 0.5f).OnComplete(() => currencyDisplay.gameObject.SetActive(false));
    }

    public void ShowRoundUI()
    {
        scoreDisplay.gameObject.SetActive(true);
        scoreDisplay.DOFade(1f, 0.5f);
        timerDisplay.gameObject.SetActive(true);
        timerDisplay.DOFade(1f, 0.5f);
        currencyDisplay.gameObject.SetActive(true);
        currencyDisplay.DOFade(1f, 0.5f);
    }

}