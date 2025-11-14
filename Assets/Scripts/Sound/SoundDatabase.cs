using UnityEngine;

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Sound/SoundDatabase")]
public class SoundDatabase : ScriptableObject
{
    [Header("Fade")]
    public AudioClip _fadeIn;                   // o
    public AudioClip _fadeOut;                  // o

    [Header("Ready Window")]
    public AudioClip _startButton;              // o

    [Header("Select Window")]
    public AudioClip _frameChangeButton;        // o
    public AudioClip _filmingStartButton;       

    [Header("Frame Application Window")]
    public AudioClip _frameSelectButton;        // o
    public AudioClip _frameApplicationButton;   // o

    [Header("Filming Window")]
    public AudioClip _filmingButton;            // o
    public AudioClip _backButton;               // o
    public AudioClip _filmingOutputButton;      // o
    [Space(5)]
    public AudioClip _numberSfx1;               // o
    public AudioClip _numberSfx2;               // o
    public AudioClip _numberSfx3;               // o
    public AudioClip _photoSFX;                 // o

    [Header("Success Window")]
    public AudioClip _outputSuccess;            // o
    
}