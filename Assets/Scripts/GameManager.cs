using UnityEngine;

public enum GameState
{
    Opening,
    MainMenu,
    Playing,
    Pause,
    GameOver,
}

public class GameManager : MonoBehaviour
{
    private GameState _state;
    public GameState State => _state;

    private bool _hasCurrentGame;
    private bool _isTransitioning;

    public bool CanContinue => _hasCurrentGame || SaveManager.Instance.HasSaveData();
    public bool CanControlBoard => _state == GameState.Playing && !_isTransitioning;

    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        int targetWidth = 1080;
        int targetHeight = (int)(((float)Screen.height / Screen.width) * targetWidth);
        Screen.SetResolution(targetWidth, targetHeight, true);
    }

    private void Start()
    {
        ChangeState(GameState.Opening);
        //PlayerPrefs.DeleteAll();
        //PlayerPrefs.Save();
    }

    public void StartNewGame()
    {
        if (_isTransitioning) return;

        _isTransitioning = true;
        SoundManager.Instance.PlaySceneTransition();

        ViewManager.Instance.Transition(
            () =>
            {
                SaveManager.Instance.DeleteGameSave();

                BoardManager.Instance.Reset();
                ScoreManager.Instance.Reset();

                _hasCurrentGame = true;
                ChangeState(GameState.Playing);
            },
            () =>
            {
                BoardManager.Instance.StartGame();
                _isTransitioning = false;
            }
        );
    }

    public void ContinueGame()
    {
        if (_isTransitioning || !CanContinue) return;

        GameSaveData data = null;

        if (!_hasCurrentGame)
        {
            data = SaveManager.Instance.LoadGame();
            if (data == null) return;
        }

        _isTransitioning = true;
        SoundManager.Instance.PlaySceneTransition();

        ViewManager.Instance.Transition(
            () =>
            {
                // 현재 판이 없을 때만 저장 데이터 복원
                if (!_hasCurrentGame)
                {
                    BoardManager.Instance.RestoreGame(data);
                    ScoreManager.Instance.RestoreScore(data);
                    _hasCurrentGame = true;
                }
                else
                {
                    ScoreManager.Instance.RefreshScore();
                }

                ChangeState(GameState.Playing);
            },
            () =>
            {
                _isTransitioning = false;
            }
        );
    }

    public void GameOver()
    {
        _hasCurrentGame = false;
        SaveManager.Instance.DeleteGameSave();
        ChangeState(GameState.GameOver);
    }

    public void Home()
    {
        if (_isTransitioning) return;

        if (_state == GameState.Opening)
        {
            ChangeState(GameState.MainMenu);
            return;
        }

        _isTransitioning = true;
        SoundManager.Instance.PlaySceneTransition();
        ViewManager.Instance.Transition(
            () =>
            {
                ChangeState(GameState.MainMenu);
            },
            () =>
            {
                _isTransitioning = false;
            }
        );
    }

    public void Restart()
    {
        StartNewGame();
    }

    public void Resume()
    {
        ChangeState(GameState.Playing);
    }

    public void OnClickPauseButton()
    {
        SoundManager.Instance.PlayButtonClick();
        ChangeState(GameState.Pause);
    }

    private void ChangeState(GameState state)
    {
        _state = state;

        switch (_state)
        {
            case GameState.Opening:
                Time.timeScale = 1.0f;
                ViewManager.Instance.ShowOpening();
                break;

            case GameState.MainMenu:
                Time.timeScale = 1.0f;
                ViewManager.Instance.ShowMainMenu();
                break;

            case GameState.Playing:
                Time.timeScale = 1.0f;
                ViewManager.Instance.ShowGame();
                break;

            case GameState.Pause:
                Time.timeScale = 0.0f;
                ViewManager.Instance.ShowPause();
                break;

            case GameState.GameOver:
                Time.timeScale = 0.0f;
                SoundManager.Instance.PlayGameOver();
                ViewManager.Instance.ShowGameOver();
                break;
        }
    }

    private void Update()
    {
        if (InputManager.Instance.IsBackPressed)
        {
            switch (_state)
            {
                case GameState.MainMenu:
                    Application.Quit();
                    break;

                case GameState.Playing:
                    OnClickPauseButton();
                    break;

                case GameState.Pause:
                    Resume();
                    break;

                case GameState.GameOver:
                    Home();
                    break;
            }
        }
    }

}
