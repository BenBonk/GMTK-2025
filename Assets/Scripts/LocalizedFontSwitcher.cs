using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedFontSwitcher : MonoBehaviour
{
    private TMP_Text text;
    [Header("Material Overriding")]
    public Material defaultMaterialOverride;
    public Material localMaterialOverride;

    void Start()
    {
        text = GetComponent<TMP_Text>();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        ApplyFont(LocalizationSettings.SelectedLocale.Identifier.Code);
    }

    void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        ApplyFont(locale.Identifier.Code);
    }

    private void ApplyFont(string localeCode)
    {
        if (localeCode=="en")
        {
            text.font = GameController.defaultFont;
            if (defaultMaterialOverride!=null)
            {
                text.fontSharedMaterial = defaultMaterialOverride;
            }
        }
        else
        {
            text.font = GameController.localizedFont;
            if (localMaterialOverride!=null)
            {
                text.fontSharedMaterial = localMaterialOverride;
            }
        }
        text.ForceMeshUpdate();
    }
}