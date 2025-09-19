using DG.Tweening;
using TMPro;
using UnityEngine;

public class SynergyShopItem : ShopItem
{
    public Boon[] possibleSynergies;
    private Boon chosenBoon;
    public SynergySlots synergySlots;
    public TMP_Text desc2;
    private int value;
    private Animal chosenToSteal;
    private int chosenToStealIndex;
    
    public void SetInt(int val)
    {
        value = val;
    }
    public override void Initialize()
    {
        if (GameController.shopManager.isTut)
        {
            value = 0;
        }
        chosenBoon = possibleSynergies[value];
        titleText.text = chosenBoon.synergyName.GetLocalizedString();
        descriptionText.text = chosenBoon.desc.GetLocalizedString();
        priceText.text = chosenBoon.price.ToString();
        price = chosenBoon.price;
        upgradeArt.sprite = chosenBoon.art;
        desc2.text = GameController.descriptionManager.GetBoonDescription(chosenBoon);
        if (chosenBoon.name=="Thief")
        {
            chosenToStealIndex = Random.Range(0, GameController.gameManager.animalShopItem.possibleAnimals.Length);
            chosenToSteal = GameController.gameManager.animalShopItem.possibleAnimals[chosenToStealIndex];
            descriptionText.text = chosenBoon.desc.GetLocalizedString() + " " + chosenToSteal.name + ".";
        }
    }
    public override void PurchaseUpgrade()
    {
        if (!shopManager.cantPurchaseItem && canPurchase && GameController.player.playerCurrency >= price)
        {
            GameController.player.playerCurrency -= price;
            shopManager. UpdateCashText();
            canPurchase = false;
            FBPP.SetInt(chosenBoon.name, FBPP.GetInt(chosenBoon.name)+1);
            FBPP.SetInt("totalBoonsPurchased", FBPP.GetInt("totalBoonsPurchased")+1);
            upgradeArt.transform.parent.DOScale(Vector3.zero, .25f).SetEase(Ease.InOutQuad);
            Instantiate(shopManager.purchaseParticles, rt.position, Quaternion.identity);
            
            if (chosenBoon.name=="Thief")
            {
                GameController.gameManager.foxThiefStolenStats = chosenToSteal.animalData;
                FBPP.SetInt("chosenToStealIndex", chosenToStealIndex);
            }
            if (GameController.player.boonsInDeck.Count<GameController.gameManager.maxSynergies)
            {
                GameController.player.AddBoonToDeck(chosenBoon);
                shopManager.UpdateSynergies();
            }
            else
            {
                shopManager.synergiesOpen = false;
                shopManager.ToggleSynergies();  
                shopManager.darkCover.DOFade(.75f, 0.5f);
                shopManager.instructionsText.DOFade(1, 0.5f);
                shopManager.cantPurchaseItem = true;
                shopManager.overridingBoon = chosenBoon;
                synergySlots.canOverrideBoon = true;
            }

        }
    }
}
