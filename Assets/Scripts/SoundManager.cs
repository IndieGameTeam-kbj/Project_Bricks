using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _pitchSfxSource;

    [Header("Block Sounds")]
    [SerializeField] private AudioClip[] _blockPickUpSounds;
    [SerializeField] private AudioClip[] _blockPlaceSounds;
    [SerializeField] private AudioClip[] _blockDestroySounds;
    [SerializeField] private AudioClip _blockReturnSound;
    [SerializeField] private AudioClip _blockSpawnSound;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip _buttonClickSound;
    [SerializeField] private AudioClip _gameOverSound;

    [Header("Best Scores")]
    [SerializeField] private AudioClip _bestScores;

    [Header("Scene Transition")]
    [SerializeField] private AudioClip _sceneTransitionSound;

    [Header("Opening")]
    [SerializeField] private AudioClip _openingSound;
    [SerializeField] private AudioClip _openingEndSound;

    [Header("Combo")]
    [SerializeField] private AudioClip _comboSound;

    [Header("Settings")]
    [SerializeField] private float _minPitch = 0.95f;
    [SerializeField] private float _maxPitch = 1.05f;

    public static SoundManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 블록 pickup, place, return, spawn, destroy 사운드 재생
    public void PlayBlockPickUp()
    {
        PlayRandom(_blockPickUpSounds);
    }

    public void PlayBlockPlace()
    {
        PlayRandom(_blockPlaceSounds);
    }

    public void PlayBlockReturn()
    {
        Play(_blockReturnSound);
    }

    public void PlayBlockSpawn()
    {
        Play(_blockSpawnSound, volume: 0.5f);
    }

    public void PlayBlockDestroy(int comboCount = 0)
    {
        if (_blockDestroySounds.Length == 0)
        {
            return;
        }

        AudioClip clip = _blockDestroySounds[Random.Range(0, _blockDestroySounds.Length)];

        // 연쇄 파괴가 진행될수록 음이 높아짐
        float comboPitch = 1f + comboCount * 0.03f;
        Play(clip, comboPitch);
    }

    // UI 버튼 클릭, 게임 오버 사운드 재생
    public void PlayButtonClick()
    {
        Play(_buttonClickSound, volume: 3f);
    }

    public void PlayGameOver()
    {
        Play(_gameOverSound);
    }

    public void PlayBestScores()
    {
        Play(_bestScores);
    }

    public void PlaySceneTransition()
    {
        Play(_sceneTransitionSound);
    }

    public void PlayOpeningSound()
    {
        Play(_openingSound);
    }

    public void PlayOpeningEndSound()
    {
        Play(_openingEndSound);
    }

    public void PlayComboSound(int combo)
    {
        float pitch = combo switch
        {
            2 => 1.0f,
            3 => 1.15f,
            4 => 1.3f,
            _ => 1.0f
        };

        PlayWithPitch(_comboSound, pitch);
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        float pitch = Random.Range(_minPitch, _maxPitch);

        Play(clip, pitch);
    }

    private void Play(AudioClip clip, float pitch = 1f, float volume = 1f)
    {
        if (clip == null || _sfxSource == null)
        {
            return;
        }

        _sfxSource.pitch = pitch;

        _sfxSource.PlayOneShot(clip, volume);
    }

    private void PlayWithPitch(
    AudioClip clip,
    float pitch,
    float volume = 1.0f)
    {
        if (clip == null || _pitchSfxSource == null)
        {
            return;
        }

        _pitchSfxSource.pitch = pitch;
        _pitchSfxSource.PlayOneShot(clip, volume);
    }

    // 사운드 볼륨 조절 및 음소거 기능
    public void SetSfxVolume(float volume)
    {
        _sfxSource.volume = Mathf.Clamp01(volume);
    }

    // 음소거 상태 설정
    public void SetMute(bool isMuted)
    {
        _sfxSource.mute = isMuted;
    }

}
