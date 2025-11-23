using UnityEngine;

public class EquipmentTrigger : MonoBehaviour
{
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private EquipmentData weapon;
    [SerializeField] private EquipmentData pickaxe;

    private void Update()
    {
        if (!playerEquipment)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                playerEquipment = go.GetComponent<PlayerEquipment>();
            } 
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (playerEquipment && weapon)
        {
            playerEquipment.Equip(weapon);
            Debug.Log("¹«±â ÀåÂø: " + weapon.name);
        }
        if (playerEquipment && pickaxe)
        {
            playerEquipment.Equip(pickaxe);
            Debug.Log("Ã¤±¤ µµ±¸: " + pickaxe.name);
        }
    }
}
