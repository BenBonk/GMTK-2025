using UnityEngine;

public abstract class SynergyEffect : ScriptableObject
{
    public abstract void Apply(SynergyManager controller);
}

[CreateAssetMenu(menuName = "Synergy Effects/Add Cash Per Round")]
public class AddCashEffect : SynergyEffect
{
    public int amount;
    public override void Apply(SynergyManager synergyManager)
    {
        //synergyManager.AddCash(amount);
    }
}

[CreateAssetMenu(menuName = "Synergy Effects/Add Time Per Round")]
public class AddTimeEffect : SynergyEffect
{
    public int amount;
    public override void Apply(SynergyManager synergyManager)
    {
        //synergyManager.AddTime(amount);
    }
}