using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PredatorPanel : MonoBehaviour
{
    private PredatorSelect predatorSelect;
    public TMP_Text title;
    public TMP_Text desc;
    public Image icon;
    private AnimalData predator;
    public ButtonFX buttonFX;
    [HideInInspector]public bool cantSelect;

    private void Start()
    {
        predatorSelect = GameController.predatorSelect;
    }

    public void Initialize(string titleText, string descText, Sprite iconSprite, AnimalData predatorData)
    {
        title.text = titleText;
        desc.text = descText;
        icon.sprite = iconSprite;
        icon.SetNativeSize();
        predator = predatorData;
        cantSelect = false;
        buttonFX.clickSFX = predator.animalName.GetLocalizedString();
    }

    public void SelectPredator()
    {
        if (cantSelect)
        {
            return;
        }

        cantSelect = true;
        predatorSelect.StartCoroutine("Exit");
        GameController.player.AddAnimalToDeck(predator);
    }
}
