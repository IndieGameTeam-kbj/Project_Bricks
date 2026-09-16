using TMPro;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_Text _bestScoreText;
    [SerializeField] private TMP_Text _playButtonText;
    [SerializeField] private SoundToggle _soundToggle;

    public void Init()
    {
        _bestScoreText.text = SaveManager.Instance.LoadBestScore().ToString("N0");
        InitPlayButton();
        _soundToggle.Init();
    }

    private void InitPlayButton()
    {
        bool hasSaveData = SaveManager.Instance.HasSaveData();
        _playButtonText.text = hasSaveData ? "Continue" : "New Game";
    }

    public void OnClickPlayButton()
    {
        SoundManager.Instance.PlayButtonClick();

        if (SaveManager.Instance.HasSaveData())
        {
            GameManager.Instance.ContinueGame();
        }
        else
        {
            GameManager.Instance.StartNewGame();
        }
    }

    public void OnClickHowToPlayButton()
    {
        SoundManager.Instance.PlayButtonClick();
        GameManager.Instance.HowToPlay();
    }

}
