using System.Collections;
using UnityEngine;

public class Mineable : MonoBehaviour
{
    [Header("광물 SO 데이터")]
    [SerializeField] private OreData oreData;

    [Header("사운드")]
    [SerializeField] private AudioSource audioSource;  // Prefab에 붙이기 (PlayOnAwake 꺼두기)
    [SerializeField] private AudioClip hitClip;        // 타격(HP>0) 시 재생
    [SerializeField] private AudioClip breakClip;      // 파괴(HP<=0) 시 재생
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private Vector2 hitPitchRange = new Vector2(0.95f, 1.05f); // 피치 랜덤
    [SerializeField] private Material Hitmaterial;
    [SerializeField] private Material defaultmaterial;

    private float currentHP;
    private Sprite oreIcon;

    public OreData OreData
    {
        set
        {
            oreData = value;
            currentHP = oreData.MaxHP;
            oreIcon = oreData.OreIcon;
        }
    }

    private void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        if (oreData != null)
        {
            currentHP = oreData.MaxHP;
            oreIcon = oreData.OreIcon;
        }
        else
        {
            Debug.LogWarning("[Ore] " + gameObject.name + "에 OreData가 할당되지 않음");
        }

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = oreIcon;
    }

    public bool TryMine(float damage, Inventory playerInventory, out OreData ore, out int count)
    {
        ore = null; count = 0;

        // HP 감소 + 히트 사운드
        currentHP -= damage;
        PlayHitSfx();

        StartCoroutine(Hit());

        if (currentHP > 0f)
            return false;

        // 파괴 사운드 → 아이템 드롭 → 파괴
        PlayBreakSfx();

        if (playerInventory != null && oreData != null)
        {
            playerInventory.AddItem(oreData, oreData.DropAmount);
            ore = oreData;
            count = oreData.DropAmount;
        }

        // 파괴 직후에도 break 사운드가 끝까지 나가도록,
        // PlayClipAtPoint를 사용했으므로 바로 Destroy 해도 OK
        Destroy(gameObject);
        return true;
    }

    private void PlayHitSfx()
    {
        if (!hitClip || !audioSource) return;

        float p = Random.Range(hitPitchRange.x, hitPitchRange.y);
        float prevPitch = audioSource.pitch;
        audioSource.pitch = p;
        audioSource.PlayOneShot(hitClip, sfxVolume);
        audioSource.pitch = prevPitch; // 원복
    }

    private IEnumerator Hit (){
        GetComponent<SpriteRenderer>().material = Hitmaterial;
        yield return new WaitForSeconds(0.08f);
        GetComponent<SpriteRenderer>().material = defaultmaterial;
    }

    private void PlayBreakSfx()
    {
        if (!breakClip) return;
        AudioSource.PlayClipAtPoint(breakClip, transform.position, sfxVolume);
    }
}
