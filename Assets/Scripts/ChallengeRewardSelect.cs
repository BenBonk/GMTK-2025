using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ChallengeRewardSelect : MonoBehaviour
{
    public Image darkCover;
    public RectTransform titleText;
    public RectTransform panel1;
    public RectTransform panel2;
    public RectTransform panel3;

    public IEnumerator Intro()
    {
        yield return new WaitForSeconds(1.75f);
        AudioManager.Instance.PlaySFX("challenge_win");
        yield return new WaitForSeconds(0.25f);
        darkCover.enabled = true;
        darkCover.DOFade(0.5f, 0.5f);
        panel1.gameObject.SetActive(true);
        panel2.gameObject.SetActive(true);
        panel3.gameObject.SetActive(true);
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
        GameController.pauseMenu.canOpenClose = false;
        GameController.shopManager.InitializeAllUpgrades();
        GameController.gameManager.GoToShop();
    }

}

