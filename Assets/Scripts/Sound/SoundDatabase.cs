using UnityEngine;

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Sound/SoundDatabase")]
public class SoundDatabase : ScriptableObject
{
    [Header("Fade")]
    public AudioClip _fadeIn;                   // 화면 전환 페이드 인 사운드
    public AudioClip _fadeOut;                  // 화면 전환 페이드 아웃 사운드

    [Header("Ready Window")]
    public AudioClip _startButton;              // 시작 버튼 클릭 사운드 (Ready 화면)

    [Header("Select Window")]
    public AudioClip _frameChangeButton;        // 프레임 변경 버튼 클릭 사운드
    public AudioClip _filmingStartButton;       // 촬영 화면으로 넘어갈 때 사용되는 버튼 사운드

    [Header("Frame Application Window")]
    public AudioClip _frameSelectButton;        // 프레임 선택 버튼 사운드
    public AudioClip _frameApplicationButton;   // 선택한 프레임 적용 버튼 사운드

    [Header("Filming Window")]
    public AudioClip _filmingButton;            // 실제 촬영(사진 촬영) 버튼 사운드
    public AudioClip _backButton;               // 뒤로가기/이전 화면 버튼 사운드
    public AudioClip _filmingOutputButton;      // 촬영 후 출력 단계로 넘어가는 버튼 사운드

    [Space(5)]
    public AudioClip _numberSfx1;               // 카운트다운 숫자 1 사운드
    public AudioClip _numberSfx2;               // 카운트다운 숫자 2 사운드
    public AudioClip _numberSfx3;               // 카운트다운 숫자 3 사운드
    public AudioClip _photoSFX;                 // 셔터 소리, 사진 촬영 효과음

    [Header("Success Window")]
    public AudioClip _outputSuccess;            // 인쇄 완료, 작업 성공 알림 사운드
}
