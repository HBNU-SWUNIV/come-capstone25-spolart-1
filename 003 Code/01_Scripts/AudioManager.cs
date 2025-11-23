using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("#BGM")]
    public AudioClip[] bgmClips;
    public float bgmVolume;
    AudioSource bgmPlayer;
    public float[] pitchList;
    private int currentSongIndex = 0;
    private AudioClip lastPlayedClip;


    [Header("#SFX")]
    public AudioClip[] sfxClips;
    public AudioClip[] footstepClips;
    public float sfxVolume;
    public int channels;
    AudioSource[] sfxPlayers;
    AudioSource footStepPlayer;
    public float footstepVolume;
    int channelIndex;

    public enum Sfx {Attack, Hit, Dead}
    
    void Awake(){
        // Singleton Pattern
        if (instance == null)
        {
            instance = this;
            // 씬이 로드될 때마다 이 오브젝트가 파괴되지 않도록 설정 (선택 사항이지만, 보통 AudioManager는 DontDestroyOnLoad 사용)
            // DontDestroyOnLoad(gameObject);
            Init();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start()를 추가하여 BGM 재생 로직을 시작합니다.
    // 씬이 로드된 직후에 호출됩니다. (Awake -> OnEnable -> Start 순)
    void Start()
    {
        // Init()에서 AudioSource 설정 후, BGM 재생을 시작합니다.
        // BGM이 현재 재생 중이 아니라면 음악 재생 코루틴을 시작합니다.
        // 이렇게 하면 씬 로드 시마다 BGM 재생이 시도됩니다.
        if (bgmClips.Length > 0 && !bgmPlayer.isPlaying)
        {
            // 재생 목록을 섞고 코루틴을 시작합니다.
            ShuffleMusicList();
            // 기존에 실행 중인 코루틴이 있다면 중지하고 새로 시작하여 중복 실행을 방지합니다.
            StopCoroutine(nameof(PlayMusicCoroutine)); 
            StartCoroutine(nameof(PlayMusicCoroutine));
        }
    }

    private void Init()
    {
        // BgmPlayer 초기화 (기존 코드와 동일)
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = false;
        bgmPlayer.volume = bgmVolume;

        // **주의**: 기존 코드에서 BGM 재생을 시작하는 조건문이 제거되었습니다. 
        // BGM 재생 시작은 이제 Start()에서 담당합니다.
        /*
        if (bgmClips.Length > 0 && GameManager.Instance.State == GameManager.GameState.InTown)
        {
            ShuffleMusicList();
            StartCoroutine(PlayMusicCoroutine());
        }
        */

        // SfxPlayer 초기화 (기존 코드와 동일)
        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];
        
        footStepPlayer = sfxObject.AddComponent<AudioSource>();
        footStepPlayer.playOnAwake=false;
        footStepPlayer.loop = false;
        footStepPlayer.volume = footstepVolume;

        for (int index = 0; index < sfxPlayers.Length; index++){
            sfxPlayers[index] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[index].playOnAwake = false;
            sfxPlayers[index].loop = false;
            sfxPlayers[index].volume = sfxVolume;
        }
    }

    // PlaySfx, PlayFootstep, PlayMusicCoroutine, ShuffleMusicList 메서드는 기존과 동일합니다.
    // ... (나머지 메서드는 기존 코드 유지)
    
    public void PlaySfx(Sfx sfx){
        for (int i=0; i < sfxPlayers.Length; i++){
            int loopIndex = (i + channelIndex) % sfxPlayers.Length;

            if (sfxPlayers[loopIndex].isPlaying) continue;

            channelIndex = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClips[(int) sfx];
            sfxPlayers[loopIndex].Play();
            break;
        }
    }

    public void PlayFootstep(){
        if (footStepPlayer.isPlaying && footStepPlayer.time < 0.35f) return;

        int r = Random.Range(0,4);
        footStepPlayer.clip = footstepClips[r];
        footStepPlayer.Play();
    }

    private IEnumerator PlayMusicCoroutine()
    {
        // (기존 코드와 동일)
        while (bgmClips.Length > 0)
        {
            // 마지막으로 재생된 곡 저장
            if (bgmPlayer.clip != null)
            {
                lastPlayedClip = bgmPlayer.clip;
            }

            // 현재 인덱스에 해당하는 곡을 재생
            bgmPlayer.clip = bgmClips[currentSongIndex];
            
            // 피치 리스트의 크기가 음악 리스트와 같을 때만 피치를 적용
            if (pitchList.Length > currentSongIndex)
            {
                bgmPlayer.pitch = pitchList[currentSongIndex];
            }
            else
            {
                bgmPlayer.pitch = 1.0f; // 기본 피치
            }
            
            
            bgmPlayer.Play();
            
            float waitTime = bgmPlayer.clip.length;

            // 곡이 끝날 때까지 기다립니다.
            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }
            else
            {
                // waitTime이 0 이하일 경우 즉시 다음 곡으로
                yield return null;
            }


            // 다음 곡 인덱스로 이동
            currentSongIndex++;

            // 리스트의 끝에 도달하면 처음으로 돌아감
            if (currentSongIndex >= bgmClips.Length)
            {
                ShuffleMusicList();
                currentSongIndex = 0;
            }
        }
    }

    private void ShuffleMusicList()
    {
        // (기존 코드와 동일)
        // 피셔-예이츠 셔플 알고리즘을 사용해 리스트를 무작위로 섞음
        for (int i = 0; i < bgmClips.Length; i++)
        {
            AudioClip temp = bgmClips[i];
            float tempPitch = pitchList.Length > i ? pitchList[i] : 1.0f;
            
            int randomIndex = Random.Range(i, bgmClips.Length);
            
            bgmClips[i] = bgmClips[randomIndex];
            bgmClips[randomIndex] = temp;

            if (pitchList.Length > randomIndex)
            {
                pitchList[i] = pitchList[randomIndex];
                pitchList[randomIndex] = tempPitch;
            }
        }
        
        // 셔플 후 첫 번째 곡이 마지막으로 재생된 곡과 같다면
        if (bgmClips.Length> 1 && bgmClips[0] == lastPlayedClip)
        {
            // 리스트 내 다른 곡과 위치를 바꿈
            AudioClip tempClip = bgmClips[0];
            float tempPitch = pitchList.Length > 0 ? pitchList[0] : 1.0f;

            bgmClips[0] = bgmClips[bgmClips.Length - 1];
            bgmClips[bgmClips.Length - 1] = tempClip;

            if (pitchList.Length > 0)
            {
                pitchList[0] = pitchList[bgmClips.Length - 1];
                pitchList[bgmClips.Length - 1] = tempPitch;
            }
        }
    }
}