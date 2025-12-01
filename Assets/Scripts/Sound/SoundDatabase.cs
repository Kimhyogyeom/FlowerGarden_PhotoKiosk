using UnityEngine;

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Sound/SoundDatabase")]
public class SoundDatabase : ScriptableObject
{
    [Header("BGM")]
    public AudioClip _basicBGM;         // 기본 BGM

    [Header("SFX")]
    public AudioClip _buttonClickSound; // 버튼 클릭 사운드
    public AudioClip _shutterSound;     // 셔터 음

    [Header("TTS")]
    public AudioClip _windowReady;
    public AudioClip _windowModeSound;
    public AudioClip _windowSelectSound;
    public AudioClip _windowChromaSound;
    public AudioClip _windowQuantitySound;
    public AudioClip _windowCameraStartSound;
    public AudioClip _windowCamearaPlayingSound;
    public AudioClip _windowPhotoSelectSound;
    public AudioClip _windowPrintSound;

    [Header("UI")]
    public AudioClip _failSound;


    // SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._photoSFX);
}
