using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public RectTransform panel;
    public Image darkCover;
    public bool canOpenClose;
    public bool isOpen;
    
    private void Update()
    {
        if (canOpenClose && !isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Open(); 
        }
        if (canOpenClose && isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close(); 
        }
    }

    public void Open()
    {
        canOpenClose = false;
        isOpen = true;
        Time.timeScale = 0;
        panel.gameObject.SetActive(true);
        panel.DOAnchorPosY(0, .5f).SetEase(Ease.OutBack).SetUpdate(true);
        darkCover.enabled = true;
        darkCover.DOFade(0.5f, .5f).OnComplete(()=> canOpenClose = true).SetUpdate(true);
    }
    public void Close()
    {
        canOpenClose = false;
        isOpen = false;
        Time.timeScale = 1;
        panel.DOAnchorPosY(909, 0.5f).SetEase(Ease.InBack).SetUpdate(true);
        darkCover.DOFade(0f, 0.5f).OnComplete(()=> DoneClose()).SetUpdate(true);
    }

    void DoneClose()
    {
        panel.gameObject.SetActive(false);
        darkCover.enabled = false;
        canOpenClose = true;
    }

    public void Quit()
    {
        Application.Quit();
    }
    
}
