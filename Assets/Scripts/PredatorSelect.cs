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
        
        //TRASH CODE BUT WE GOTTA FINISH IN TIME IDC
        Animal animalRef = predatorOptions[a].animalPrefab.GetComponent<Animal>();
        string aStr ="";
        if (animalRef.pointsToGive!=0)
        {
            if (animalRef.pointsToGive<0)
            {
                aStr += ("Points loss: " + animalRef.pointsToGive + "\n");   
            }
            else
            {
                aStr += ("Points bonus: +" + animalRef.pointsToGive + "\n");   
            }
        }
        if (animalRef.pointsMultToGive!=1f)
        {
            aStr += ("Points mult: x" + animalRef.pointsMultToGive + "\n");
        }
        if (animalRef.currencyToGive!=0)
        {
            if (animalRef.currencyToGive < 0)
            {
                aStr += ("Cash loss: " + animalRef.currencyToGive + "\n");
            }
            else
            {
                aStr += ("Cash bonus: +" + animalRef.currencyToGive + "\n");    
            }
            
        }
        if (animalRef.currencyMultToGive!=1f)
        {
            aStr += ("Cash mult: x" + animalRef.currencyMultToGive + "\n");
        }
        Animal animalRef2 = predatorOptions[b].animalPrefab.GetComponent<Animal>();
        string bStr ="";
        if (animalRef2.pointsToGive!=0)
        {
            if (animalRef2.pointsToGive<0)
            {
                bStr += ("Points loss: " + animalRef2.pointsToGive + "\n");   
            }
            else
            {
                bStr += ("Points bonus: +" + animalRef2.pointsToGive + "\n");   
            }
        }
        if (animalRef2.pointsMultToGive!=1f)
        {
            bStr += ("Points mult: x" + animalRef2.pointsMultToGive + "\n");
        }
        if (animalRef2.currencyToGive!=0)
        {
            if (animalRef2.currencyToGive < 0)
            {
                bStr += ("Cash loss: " + animalRef2.currencyToGive + "\n");
            }
            else
            {
                bStr += ("Cash bonus: +" + animalRef2.currencyToGive + "\n");    
            }
        }
        if (animalRef2.currencyMultToGive!=1f)
        {
            bStr += ("Cash mult: x" + animalRef2.currencyMultToGive + "\n");
        }
        predatorPanel1.Initialize(predatorOptions[a].name,aStr, predatorOptions[a].sprite, predatorOptions[a]);
        predatorPanel2.Initialize(predatorOptions[b].name,bStr, predatorOptions[b].sprite, predatorOptions[b]);
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
