using UnityEngine;

public struct SkillResolved
{
    public int level;
    public float duration;
    public float cooldown;
    public int mpCost;
    public float value;
    public float attackMultiplier;
    public float damageFlat;
    public float range;
    public float radius;
}

public class SkillLeveling : MonoBehaviour
{
    private static int GetLevel(SkillData data)
    {
        var D = DataManager.Instance;
        int lv = D ? D.GetSkillLevel(data) : 1;
        return Mathf.Clamp(lv <= 0 ? 1 : lv, 1, data.maxLevel);
    }

    private static T Pick<T>(T[] arr, int levelIndex, T fallback)
    {
        if (arr != null && arr.Length > 0)
        {
            int i = Mathf.Clamp(levelIndex, 0, arr.Length - 1);
            return arr[i];
        }
        return fallback;
    } 

    public static SkillResolved Resolve(SkillData data)
    {
        int lv = GetLevel(data);
        int idx = lv - 1;

        return new SkillResolved
        {
            level = lv,
            duration = Pick(data.levelDuration, idx, data.duration),
            cooldown = Pick(data.levelCooldown, idx, data.cooldown),
            mpCost = Pick(data.levelMPCost, idx, data.cost),
            value = Pick(data.levelValue, idx, data.value),
            attackMultiplier = Pick(data.levelAtkMul, idx, data.attackMultiplier),
            damageFlat = Pick(data.levelDmgFlat, idx, data.damageFlat),
            range = Pick(data.levelRange, idx, data.range),
            radius = Pick(data.levelRadius, idx, data.radius),
        };
    }
}
