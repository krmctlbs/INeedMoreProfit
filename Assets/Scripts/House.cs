using UnityEngine;

public class House : BaseBuilding
{
    public float[] populationCapacity = { 10f, 15f, 20f };

    protected override void Awake()
    {
        base.Awake();
        if (upgradeCosts == null || upgradeCosts.Length < 3 || upgradeCosts[1] <= 0f)
        {
            upgradeCosts = new float[] { 0f, 75f, 150f };
        }
    }
}
