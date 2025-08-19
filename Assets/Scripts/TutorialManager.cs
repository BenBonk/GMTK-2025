using DG.Tweening;
using Level;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
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


    public  GameObject cowPrefab;
    public GameObject wolfPrefab;
    public GameObject wordPrefab2;

    public LevelLoader levelLoader;

    public GameObject buttonBlocker;
    public GameObject buttonBlocker2;
    public GameObject buttonBlocker3;
    public GameObject buttonBlocker4;
    public GameObject buttonBlocker5;
    public GameObject buttonBlocker6;
    public GameObject buttonBlocker7;

    public GameObject arrowSet1;
    public GameObject arrowSet2;
    public GameObject arrowSet3;
    public GameObject arrowSet4;

    public static TutorialManager _instance;
    public LocalizedString[] tutorialStrings;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("Duplicate TutorialManager found. Destroying new instance.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }


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

    private void Start()
    {
        elapsedTime = 0;
        //lassosUsed = 0;
        player.OnCurrencyChanged += UpdatecurrencyDisplay;
        OnPointsChanged += UpdateScoreDisplay;
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
        scoreDisplay.text = "POINTS: " + pointsThisRound + " / 50";
        timerDisplay.text = "TIME: " + roundDuration.ToString("F1") + "s";
        currencyDisplay.text = "CASH: " + player.playerCurrency;
        //lassosDisplay.text = "Lassos: " + player.lassosPerRound;
        roundNumber++;
        roundInProgress = true;
        roundCompleted = false;
        barnAnimator.Play("Closed", 0, 0.1f);
        //StartCoroutine(ShowReadySetLassoSequence());
        StartCoroutine(RunTutorial());
    }

    public void EndRound()
    {
        roundCompleted = true;
        roundInProgress = false;
        elapsedTime = 0;
        playerReady = false;
        GameController.shopManager.InitializeAllUpgrades();
        DisplayPopupWord("TIME'S UP!", wordScaleDuration, wordDisplayDuration, true);
        if (roundNumber % predatorRoundFrequency == 0)
        {
            GameController.predatorSelect.StartCoroutine("Intro");
        }
        else
        {
            GoToShop();
        }
    }

    public void GoToShop()
    {
        cameraController.AnimateToTarget(
            barnCameraTarget.transform,
            delay: 0.1f,
        onZoomMidpoint: () =>
        {
            barnAnimator.Play("Open", 0, 0.1f);
            AudioManager.Instance.PlaySFX("barn_door");
        },
            onZoomEndpoint: () =>
            {
                barn.DOFade(0f, 1f)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    GameController.shopManager.cantPurchaseItem = false;
                });
            }
        );
    }

    public void LeaveShop()
    {
        barn.DOFade(1f, 1f).SetEase(Ease.OutSine).OnComplete(() => Invoke("StartRound", 2.25f));
        cameraController.ResetToStartPosition(1f);
    }

    private void UpdateScoreDisplay(double newPoints)
    {
        if (scoreDisplay == null || roundsPointsRequirement == null || roundNumber >= roundsPointsRequirement.Length)
        {
            Debug.LogWarning("Cannot update score display – scoreDisplay or roundsPointsRequirement invalid.");
            return;
        }

        scoreDisplay.text = $"POINTS: {newPoints} / {roundsPointsRequirement[roundNumber]}";
    }
    private void UpdatecurrencyDisplay(double newcurrency)
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

        wordObj.transform.position = GetCenterScreenWorldPosition() + new Vector3(0f, 2.75f, 0f); // Adjust Y as needed
        wordObj.transform.localScale = Vector3.zero;
        wordObj.transform.rotation = Quaternion.identity;
        wordObj.SetActive(true);

        Sequence seq = DOTween.Sequence();

        // Scale pop-in
        seq.Append(wordObj.transform.DOScale(1.05f, scaleDuration).SetEase(Ease.OutBack));
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

    public void DisplayPopupWord2(string word, float scaleDuration = 0.3f, float displayDuration = 1.2f, bool shake = false, Material overrideMaterial = null)
    {
        GameObject wordObj = Instantiate(wordPrefab2, transform);
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
        seq.Append(wordObj.transform.DOScale(1.05f, scaleDuration).SetEase(Ease.OutBack));
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


    private GameObject SpawnAnimal(GameObject ToSpawn)
    {
        GameObject animal = Instantiate(ToSpawn);

        // Get vertical bounds of the camera in world space
        float z = Mathf.Abs(Camera.main.transform.position.z - animal.transform.position.z);
        Vector3 screenBottom = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, z));
        Vector3 screenTop = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1f, z));

        // Account for the sprite's vertical size
        SpriteRenderer sr = animal.GetComponent<SpriteRenderer>();
        float halfHeight = sr.bounds.extents.y;

        float minY = screenBottom.y + halfHeight;
        float maxY = screenTop.y - halfHeight;

        float variationRange = 0.5f; // you can expose this in the inspector if you want

        // Get center position with a small variation
        float centerY = (minY + maxY) / 2f;
        float variedY = Mathf.Clamp(centerY + Random.Range(-variationRange, variationRange), minY, maxY);

        //  Set spawn position at the right edge
        float rightEdgeX = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x + sr.bounds.extents.x;

        animal.transform.position = new Vector3(rightEdgeX, variedY, 0f);

        return animal;
    }


    private GameObject cow1, cow2, cow3, wolf1;
    private IEnumerator RunTutorial()
    {
        yield return ShowMessage(tutorialStrings[0].GetLocalizedString());
        cow1 = SpawnAnimal(cowPrefab);
        yield return ShowMessage(tutorialStrings[1].GetLocalizedString(),-0.5f);
        yield return ShowMessage(tutorialStrings[2].GetLocalizedString(),+1f);

        yield return new WaitUntil(() => IsAnimalLassoed(cow1) || cow1 == null);
        yield return new WaitForSeconds(2.5f);
        timerDisplay.gameObject.SetActive(true);
        yield return ShowMessage(tutorialStrings[3].GetLocalizedString(),3f);
        scoreDisplay.gameObject.SetActive(true);
        yield return ShowMessage(tutorialStrings[4].GetLocalizedString(), 2f);

        cow2 = SpawnAnimal(cowPrefab);
        cow3 = SpawnAnimal(cowPrefab);
        cow1 = SpawnAnimal(cowPrefab);
        yield return ShowMessage(tutorialStrings[5].GetLocalizedString(), 1f);
        yield return new WaitUntil(() => AreAllLassoed(cow2, cow3, cow1) || (cow2 == null && cow3 == null && cow1 == null));
        yield return new WaitForSeconds(1f);

        cow1 = SpawnAnimal(cowPrefab);
        wolf1 = SpawnAnimal(wolfPrefab);
        yield return new WaitForSeconds(1.5f);
        yield return ShowMessage(tutorialStrings[6].GetLocalizedString(), 0.5f);
        yield return new WaitUntil(() => AreAllLassoed(cow1, wolf1));
        yield return new WaitForSeconds(2.5f);
        yield return ShowMessage(tutorialStrings[7].GetLocalizedString(), 2f);
        yield return ShowMessage(tutorialStrings[8].GetLocalizedString(),1f);

        GoToShop();
        yield return new WaitForSeconds(4f);

        yield return ShowMessage2(tutorialStrings[9].GetLocalizedString(),1f);
        buttonBlocker.SetActive(false);
        buttonBlocker2.SetActive(true);
        arrowSet1.SetActive(true);
        yield return ShowMessage2(tutorialStrings[10].GetLocalizedString(), 7.5f);
        buttonBlocker2.SetActive(false);
        buttonBlocker3.SetActive(true);
        arrowSet2.SetActive(true);
        yield return ShowMessage2(tutorialStrings[11].GetLocalizedString(), 7.5f);
        buttonBlocker3.SetActive(false);
        buttonBlocker4.SetActive(true);
        buttonBlocker5.SetActive(true);
        buttonBlocker7.SetActive(true);
        arrowSet3.SetActive(true);
        yield return ShowMessage2(tutorialStrings[12].GetLocalizedString(),7.5f);
        buttonBlocker5.SetActive(false);
        buttonBlocker6.SetActive(true);
        arrowSet4.SetActive(true);  
        yield return ShowMessage2(tutorialStrings[13].GetLocalizedString(), 7.5f);
        yield return ShowMessage2(tutorialStrings[14].GetLocalizedString(),0.5f);
        levelLoader.LoadCertainScene("TitleScreen");
    }


    private IEnumerator ShowMessage(string text, float durationOffset = 0f)
    {
        AudioManager.Instance?.PlaySFX("ui_click");
        float finalDuration = wordDisplayDuration + durationOffset;
        DisplayPopupWord(text, wordScaleDuration, finalDuration, false, defaultMaterialPreset);
        yield return new WaitForSeconds(finalDuration + 0.7f); // includes a small buffer before the next message
    }

    private IEnumerator ShowMessage2(string text, float durationOffset = 0f)
    {
        AudioManager.Instance?.PlaySFX("ready");
        float finalDuration = wordDisplayDuration + durationOffset;
        DisplayPopupWord2(text, wordScaleDuration, finalDuration, false, defaultMaterialPreset);
        yield return new WaitForSeconds(finalDuration + 0.9f); // includes a small buffer before the next message
    }
    private bool IsAnimalLassoed(GameObject animal)
    {
        if (animal == null) return true; // Treat null as "already lassoed"
        var a = animal.GetComponent<Animal>();
        return a == null || a.isLassoed;
    }

    private bool AreAllLassoed(params GameObject[] animals)
    {
        return animals.All(IsAnimalLassoed);
    }

}

