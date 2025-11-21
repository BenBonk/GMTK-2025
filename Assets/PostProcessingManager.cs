using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingManager : MonoBehaviour
{
    public Volume volume;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;

    private float basePostExposure;
    private float baseVignetteSmoothness;

    public bool isNight;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out colorAdjustments);
        basePostExposure = colorAdjustments.postExposure.value;
        baseVignetteSmoothness = vignette.smoothness.value;
        //NightModeOn();
    }

    public void NightModeOn()
    {
        isNight = true;
        DOTween.To(
            () => colorAdjustments.postExposure.value,
            x => colorAdjustments.postExposure.value = x,
            -1.07f,
            1f
        );
        DOTween.To(
            () => vignette.smoothness.value,
            x => vignette.smoothness.value = x,
            1,
            1f
        );
    }

    public void NightModeOff()
    {
        isNight = false;
        DOTween.To(
            () => colorAdjustments.postExposure.value,
            x => colorAdjustments.postExposure.value = x,
            basePostExposure,
            1f
        );
        DOTween.To(
            () => vignette.smoothness.value,
            x => vignette.smoothness.value = x,
            baseVignetteSmoothness,
            1f
        );
        //colorAdjustments.postExposure.value = basePostExposure;
        //vignette.smoothness.value = baseVignetteSmoothness;
    }
}
