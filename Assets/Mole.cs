using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Mole : MonoBehaviour
{
    public BoxCollider2D collider2D;
    IEnumerator Start()
    {
        transform.DOScaleY(1, .25f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(.25f);
        collider2D.enabled = true;
        yield return new WaitForSeconds(2);
        if (transform.parent==null)
        {
            transform.DOScaleY(0, .25f).SetEase(Ease.InBack);
            yield return new WaitForSeconds(.25f);
            collider2D.enabled = false;   
        }
    }
}
