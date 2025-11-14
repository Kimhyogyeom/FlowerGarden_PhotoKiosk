using UnityEngine;

/// <summary>
/// 사운드 매니저
/// 사운드 추가 안될수도 있음
/// 혹시 모를 임시 작업용
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Database")]
    public SoundDatabase _soundDatabase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); 
    }
    /// <summary>
    /// BGM 플레이
    /// </summary>  
    public void PlayBGM(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            //print("BGM Clip is null");
            return;
        }
        else
        {            
            _bgmSource.clip = clip;
            _bgmSource.volume = volume;            
            _bgmSource.Play();
        }            
    }

    /// <summary>
    /// SFX 플레이
    /// </summary>    
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            //print("SFX Clip is null");
            return;
        }
        else
        {
            _sfxSource.PlayOneShot(clip, volume);
        }                
    }
}