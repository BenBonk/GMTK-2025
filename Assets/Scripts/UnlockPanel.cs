using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class UnlockPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform panel;   // the sliding child
    [SerializeField] CanvasGroup cg;        // on the root of this panel

    [Header("Anim")]
    [SerializeField] float openY = 0f;
    [SerializeField] float closedY = -1070f;
    [SerializeField] float openDur = 0.5f;
    [SerializeField] float closeDur = 0.5f;

    static readonly List<UnlockPanel> OpenStack = new List<UnlockPanel>();
    Tween tween;

    public TextMeshProUGUI unlockText;
    public Image unlockImage;
    public LocalizedString harvestLevelText;
    public Sprite harvestLevelSprite;

    public void SetupHarvestLevelUnlock(int levelUnlocked)
    {
        unlockText.text = harvestLevelText.GetLocalizedString().Substring(0, harvestLevelText.GetLocalizedString().Length - 1) + " " + levelUnlocked.ToString();
        unlockImage.sprite = harvestLevelSprite;
        unlockImage.SetNativeSize();
    }

    public void SetupFarmerUnlock(int farmerID)
    {
        unlockText.text = GameController.saveManager.farmerDatas[farmerID].farmerName.GetLocalizedString();
        unlockImage.sprite = GameController.saveManager.farmerDatas[farmerID].sprite;
        unlockImage.SetNativeSize();
    }

    void Awake()
    {
        // start closed and non-interactable
        var p = panel.anchoredPosition;
        panel.anchoredPosition = new Vector2(p.x, closedY);
        SetRaycasts(false);
    }

    public void Open()
    {
        // put on top visually (same parent Canvas)
        transform.SetAsLastSibling();

        // disable others' raycasts
        foreach (var other in OpenStack) other.SetRaycasts(false);

        // add self and enable
        if (!OpenStack.Contains(this)) OpenStack.Add(this);
        SetRaycasts(true);

        tween?.Kill(false);
        panel.gameObject.SetActive(true);
        tween = panel.DOAnchorPosY(openY, openDur).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void Close()
    {
        SetRaycasts(false);
        tween?.Kill(false);
        tween = panel.DOAnchorPosY(closedY, closeDur).SetEase(Ease.InBack).SetUpdate(true)
            .OnComplete(() =>
            {
                OpenStack.Remove(this);
                // re-enable the one now on top
                if (OpenStack.Count > 0) OpenStack[OpenStack.Count - 1].SetRaycasts(true);
                Destroy(gameObject);
            });
    }

    void SetRaycasts(bool on)
    {
        if (!cg) return;
        cg.blocksRaycasts = on;
        cg.interactable = on;
        cg.alpha = 1f; // visual unchanged; alpha does not need to change
    }
}
