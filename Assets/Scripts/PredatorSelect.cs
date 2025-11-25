using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PredatorSelect : MonoBehaviour
{
    public Image darkCover;
    public RectTransform titleText;
    public RectTransform[] twoOptions;
    public RectTransform[] threeOptions;
    public PredatorPanel predatorPanel1;
    public PredatorPanel predatorPanel2;
    public AnimalData[] predatorOptions;

    private DescriptionManager descriptionManager;
    private GameManager gameManager;
    private RectTransform[] options;

    private void Start()
    {
        gameManager = GameController.gameManager;
        descriptionManager = GameController.descriptionManager;
        options = gameManager.predatorOptions == 2 ? twoOptions : threeOptions;
    }

    public IEnumerator Intro()
    {
        List<int> selectedIndexes = Enumerable.Range(0, predatorOptions.Length)
            .OrderBy(_ => Random.value)
            .Take(3)
            .ToList();
        for (int i = 0; i < gameManager.predatorOptions; i++)
        {
            int index = selectedIndexes[i];
            string desc = descriptionManager.GetAnimalDescription(predatorOptions[index]);
            options[i].GetComponent<PredatorPanel>().Initialize(predatorOptions[index].animalName.GetLocalizedString(), desc, predatorOptions[index].sprite, predatorOptions[index]);
        }
        
        
        yield return new WaitForSeconds(1.75f);
        AudioManager.Instance.PlaySFX("predator_select");
        yield return new WaitForSeconds(0.25f);
        darkCover.enabled = true;
        darkCover.DOFade(0.5f, 0.5f);

        titleText.gameObject.SetActive(true);
        titleText.DOAnchorPosY(220, .5f).SetEase(Ease.OutBack);
        for (int i = 0; i < options.Length; i++)
        {
            options[i].gameObject.SetActive(true);
            yield return new WaitForSeconds(.25f);
            options[i].DOAnchorPosY(-70, .5f).SetEase(Ease.OutBack);
        }

    }

    public IEnumerator Exit()
    {
        darkCover.DOFade(0, .5f).OnComplete(() => darkCover.enabled = false);
        yield return new WaitForSeconds(.1f);
        titleText.DOAnchorPosY(530, .5f).SetEase(Ease.InBack).OnComplete(() => titleText.gameObject.SetActive(false));
        for (int i = 0; i < options.Length; i++)
        {
            yield return new WaitForSeconds(.25f);
            options[i].DOAnchorPosY(-1000, .5f).SetEase(Ease.InBack);
        }

        if (GameController.gameManager.roundNumber % GameController.gameManager.challengeRoundFrequency == 0)
        {
            GameController.challengeRewardSelect.StartCoroutine("Intro");
        }
        else
        {
            GameController.pauseMenu.canOpenClose = false;
            GameController.shopManager.InitializeAllUpgrades();
            GameController.gameManager.GoToShop();
        }

        yield return new WaitForSeconds(.5f);
        for (int i = 0; i < options.Length; i++)
        {
           options[i].gameObject.SetActive(false);
        }
    }

}
