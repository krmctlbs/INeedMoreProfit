using UnityEngine;

public class Factory : BaseBuilding
{
    public float[] moneyProduction = { 6f, 13f, 25f };
    public float[] pollutionRate = { 0.18f, 0.30f, 0.45f };
    public float[] workerNeed = { 5f, 8f, 12f };

    protected override void Awake()
    {
        base.Awake();
        if (upgradeCosts == null || upgradeCosts.Length < 3 || upgradeCosts[1] <= 0f)
        {
            upgradeCosts = new float[] { 0f, 150f, 300f };
        }
        // 8-10 dakikalık oyun temposuna uygun kirlilik ve gelir değerlerini garanti altına al
        pollutionRate = new float[] { 0.18f, 0.30f, 0.45f };
        moneyProduction = new float[] { 6f, 13f, 25f };
    }
}
