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
        colorAdjustments.postExposure.value = -1.07f;
        vignette.smoothness.value = 1;
    }

    public void NightModeOff()
    {
        isNight = false;
        colorAdjustments.postExposure.value = basePostExposure;
        vignette.smoothness.value = baseVignetteSmoothness;
    }
}
