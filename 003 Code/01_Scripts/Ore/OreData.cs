using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "OreData", menuName = "OreScriptable/OreData")]
public class OreData : ScriptableObject
{
    [SerializeField]
    private string oreId;
    public string OreId { get { return oreId; } }

    [SerializeField] 
    private string oreName;
    public string OreName { get { return oreName; } }

    [SerializeField] 
    private Sprite oreIcon;
    public Sprite OreIcon { get { return oreIcon; } }

    [SerializeField] 
    private float maxHP;
    public float MaxHP { get { return maxHP; } }

    [SerializeField] 
    private int dropAmount;
    public int DropAmount { get { return dropAmount; } }

    [SerializeField]
    private int price;
    public int Price { get { return price; } }

    [SerializeField]
    private string rarity;
    public string Rarity { get { return rarity; } }

    [SerializeField]
    private float weight;
    public float Weight { get { return weight; } }
}
