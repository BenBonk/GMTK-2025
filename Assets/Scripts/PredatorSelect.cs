using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PredatorSelect : MonoBehaviour
{
    public Image darkCover;
    public RectTransform titleText;
    public RectTransform panel1;
    public RectTransform panel2;
    public PredatorPanel predatorPanel1;
    public PredatorPanel predatorPanel2;
    public AnimalData[] predatorOptions;

    public IEnumerator Intro()
    {
        int a = Random.Range(0, 3); 

        int b;
        do
        {
            b = Random.Range(0, 3);
        } while (b == a);
        predatorPanel1.Initialize(predatorOptions[a].name,predatorOptions[a].description, predatorOptions[a].sprite, predatorOptions[a]);
        predatorPanel2.Initialize(predatorOptions[b].name,predatorOptions[b].description, predatorOptions[b].sprite, predatorOptions[b]);
        yield return new WaitForSeconds(2f);
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
        GameController.gameManager.GoToShop();
        darkCover.DOFade(0, .5f);
        yield return new WaitForSeconds(.1f);
        titleText.DOAnchorPosY(530, .5f).SetEase(Ease.InBack).OnComplete(()=>titleText.gameObject.SetActive(false));
        yield return new WaitForSeconds(.25f);
        panel1.DOAnchorPosY(-900, .5f).SetEase(Ease.InBack).OnComplete(()=>panel1.gameObject.SetActive(false));
        yield return new WaitForSeconds(.25f);
        panel2.DOAnchorPosY(-900, .5f).SetEase(Ease.InBack).OnComplete(()=>panel2.gameObject.SetActive(false));
    }

}
