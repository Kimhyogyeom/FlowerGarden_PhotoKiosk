using UnityEngine;

/// <summary>
/// 사운드 매니저
/// - 전역에서 쉽게 BGM / SFX 를 재생하기 위한 싱글톤 매니저
/// - 씬 어디서든 SoundManager.Instance.PlayBGM / PlaySFX 로 접근
/// </summary>
public class SoundManager : MonoBehaviour
{
    // 전역 접근용 싱글톤 인스턴스
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;
    // 배경 음악(BGM)을 재생할 AudioSource

    [SerializeField] private AudioSource _sfxSource;
    // 효과음(SFX)을 재생할 AudioSource

    [Header("Database")]
    public SoundDatabase _soundDatabase;
    // 재생에 사용할 각종 사운드 클립을 묶어둔 데이터베이스
    // 예) 버튼 클릭, 카운트다운, 촬영음 등

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance != null && Instance != this)
        {
            // 이미 다른 SoundManager 가 존재하면 자기 자신은 제거
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 씬 이동 시에도 유지하고 싶으면 아래 주석 해제
        // DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// BGM 재생
    /// - clip 이 null 이면 아무 것도 하지 않음
    /// - 기존 BGM 은 교체되고 새 클립부터 재생
    /// </summary>  
    public void PlayBGM(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            // Debug.Log("BGM Clip is null");
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
    /// 효과음(SFX) 재생
    /// - clip 이 null 이면 아무 것도 하지 않음
    /// - PlayOneShot 사용으로, 기존 재생 중인 SFX 와 겹쳐서 재생 가능
    /// </summary>    
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            // Debug.Log("SFX Clip is null");
            return;
        }
        else
        {
            _sfxSource.PlayOneShot(clip, volume);
        }
    }
}
