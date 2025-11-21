using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Mole : MonoBehaviour
{
    public BoxCollider2D col;
    IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        transform.DOScaleY(1, .25f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(.25f);
        gameObject.tag = "NonAnimalLassoable";
        col.enabled = true;
        yield return new WaitForSeconds(2);
        if (transform.parent.name == "MoleDig(Clone)")
        {
            gameObject.tag = "Untagged";
            col.enabled = false;  
            transform.DOScaleY(0, .25f).SetEase(Ease.InBack);
            yield return new WaitForSeconds(.25f);
            Destroy(transform.parent.gameObject);
        }
    }
}
