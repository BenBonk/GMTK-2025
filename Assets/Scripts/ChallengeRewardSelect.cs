using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ChallengeRewardSelect : MonoBehaviour
{
    public Image darkCover;
    public RectTransform titleText;
    public LocalizedString chooseReward;
    public LocalizedString chooseRemovalA;
    public LocalizedString chooseRemovalB;
    public RectTransform panel1;
    public RectTransform panel2;
    public RectTransform panel3;
    public RectTransform deckRemoval;
    public RectTransform deckParent;
    int animalsToRemove = 2;

    public IEnumerator Intro()
    {
        titleText.GetComponent<TextMeshProUGUI>().text = ""; //this doesn't work for some reason
        yield return new WaitForSeconds(0.35f);
        AudioManager.Instance.PlaySFX("challenge_win");
        GameController.gameManager.HideRoundUI();
        yield return new WaitForSeconds(0.15f);
        darkCover.enabled = true;
        darkCover.DOFade(0.5f, 0.5f);
        ChallengeReward.cantSelect = false;
        panel1.gameObject.SetActive(true);
        panel2.gameObject.SetActive(true);
        panel3.gameObject.SetActive(true);
        titleText.gameObject.SetActive(true);
        titleText.DOAnchorPosY(280, .5f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(.15f);
        panel1.DOAnchorPosY(0, .5f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(.15f);
        panel2.DOAnchorPosY(0, .5f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(.15f);
        panel3.DOAnchorPosY(0, .5f).SetEase(Ease.OutBack);
        titleText.GetComponent<TMPTypewriterSwap>().ChangeTextAnimated(chooseReward.GetLocalizedString());

    }

    public IEnumerator Exit()
    {
        darkCover.DOFade(0, .5f).OnComplete(() => darkCover.enabled = false);
        yield return new WaitForSeconds(.1f);
        titleText.DOAnchorPosY(530, .5f).SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                titleText.GetComponent<TMPTypewriterSwap>().InstantSet("");
                titleText.gameObject.SetActive(false);
            });
        yield return new WaitForSeconds(.15f);
        panel1.DOAnchorPosY(-1000, .5f).SetEase(Ease.InBack).OnComplete(() => panel1.gameObject.SetActive(false));
        yield return new WaitForSeconds(.15f);
        panel2.DOAnchorPosY(-1000, .5f).SetEase(Ease.InBack).OnComplete(() => panel2.gameObject.SetActive(false));
        yield return new WaitForSeconds(.15f);
        panel3.DOAnchorPosY(-1000, .5f).SetEase(Ease.InBack).OnComplete(() => panel3.gameObject.SetActive(false));
        GameController.pauseMenu.canOpenClose = false;
        GameController.shopManager.InitializeAllUpgrades();
        GameController.gameManager.GoToShop();
    }
    public IEnumerator OpenDeckRemoval()
    {
        animalsToRemove = 2;
        titleText.gameObject.GetComponent<TMPTypewriterSwap>().ChangeTextAnimated(chooseRemovalA.GetLocalizedString() + " " +animalsToRemove + " " + chooseRemovalB.GetLocalizedString());
        GameController.shopManager.UpdateDeck(deckParent);
        yield return new WaitForSeconds(.15f);
        panel1.DOAnchorPosY(-1000, .5f).SetEase(Ease.InBack).OnComplete(() => panel1.gameObject.SetActive(false));
        yield return new WaitForSeconds(.15f);
        panel2.DOAnchorPosY(-1000, .5f).SetEase(Ease.InBack).OnComplete(() => panel2.gameObject.SetActive(false));
        yield return new WaitForSeconds(.15f);
        panel3.DOAnchorPosY(-1000, .5f).SetEase(Ease.InBack).OnComplete(() => panel3.gameObject.SetActive(false));
        yield return new WaitForSeconds(0.25f);
        deckRemoval.gameObject.SetActive(true);
        deckRemoval.DOAnchorPosY(-150, .5f).SetEase(Ease.OutBack);
        foreach (Transform child in deckParent.transform)
        {
            child.gameObject.AddComponent<ClickRemoveAnimal>();
        }
    }

    public IEnumerator CloseDeckRemoval()
    {
        titleText.gameObject.GetComponent<TMPTypewriterSwap>().ChangeTextAnimated("");
        darkCover.DOFade(0, .5f).OnComplete(() => darkCover.enabled = false);
        yield return new WaitForSeconds(.1f);
        titleText.DOAnchorPosY(530, .5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            titleText.GetComponent<TMPTypewriterSwap>().InstantSet("");
            titleText.gameObject.SetActive(false);
        });
        yield return new WaitForSeconds(.15f);
        deckRemoval.DOAnchorPosY(-1000, .5f).SetEase(Ease.InBack).OnComplete(() => deckRemoval.gameObject.SetActive(false));
        GameController.pauseMenu.canOpenClose = false;
        GameController.shopManager.InitializeAllUpgrades();
        GameController.gameManager.GoToShop();
    }

    public void RemoveAnimal(AnimalData animal)
    {
        animalsToRemove--;
        GameController.player.RemoveAnimalFromDeck(animal);
        GameController.shopManager.UpdateDeck(deckParent);
        if (animalsToRemove <= 0)
        {
            StartCoroutine(CloseDeckRemoval());
        }
        else
        {
            titleText.gameObject.GetComponent<TMPTypewriterSwap>().ChangeTextAnimated(chooseRemovalA.GetLocalizedString() + " " + animalsToRemove + " " + chooseRemovalB.GetLocalizedString());
            foreach (Transform child in deckParent.transform)
            {
                child.gameObject.AddComponent<ClickRemoveAnimal>();
            }
        }
    }

}

