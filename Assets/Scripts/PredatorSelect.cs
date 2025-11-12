using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PredatorSelect : MonoBehaviour
{
    public Image darkCover;
    public RectTransform titleText;
    public RectTransform panel1;
    public RectTransform panel2;
    public PredatorPanel predatorPanel1;
    public PredatorPanel predatorPanel2;
    public AnimalData[] predatorOptions;

    private DescriptionManager descriptionManager;

    private void Start()
    {
        descriptionManager = GameController.descriptionManager;
    }

    public IEnumerator Intro()
    {
        int a = Random.Range(0, 3);

        int b;
        do
        {
            b = Random.Range(0, 3);
        } while (b == a);

        string desc = descriptionManager.GetAnimalDescription(predatorOptions[a]);
        string desc2 = descriptionManager.GetAnimalDescription(predatorOptions[b]);

        predatorPanel1.Initialize(predatorOptions[a].animalName.GetLocalizedString(), desc, predatorOptions[a].sprite, predatorOptions[a]);
        predatorPanel2.Initialize(predatorOptions[b].animalName.GetLocalizedString(), desc2, predatorOptions[b].sprite, predatorOptions[b]);
        yield return new WaitForSeconds(1.75f);
        AudioManager.Instance.PlaySFX("predator_select");
        yield return new WaitForSeconds(0.25f);
        darkCover.enabled = true;
        darkCover.DOFade(0.5f, 0.5f);
        panel1.gameObject.SetActive(true);
        panel2.gameObject.SetActive(true);
        titleText.gameObject.SetActive(true);
        titleText.DOAnchorPosY(220, .5f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(.25f);
        panel1.DOAnchorPosY(-70, .5f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(.25f);
        panel2.DOAnchorPosY(-70, .5f).SetEase(Ease.OutBack);

    }

    public IEnumerator Exit()
    {
        darkCover.DOFade(0, .5f).OnComplete(() => darkCover.enabled = false);
        yield return new WaitForSeconds(.1f);
        titleText.DOAnchorPosY(530, .5f).SetEase(Ease.InBack).OnComplete(() => titleText.gameObject.SetActive(false));
        yield return new WaitForSeconds(.25f);
        panel1.DOAnchorPosY(-1000, .5f).SetEase(Ease.InBack).OnComplete(() => panel1.gameObject.SetActive(false));
        yield return new WaitForSeconds(.25f);
        panel2.DOAnchorPosY(-1000, .5f).SetEase(Ease.InBack).OnComplete(() => panel2.gameObject.SetActive(false));

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
    }

}
