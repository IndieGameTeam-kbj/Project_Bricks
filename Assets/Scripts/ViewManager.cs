using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ViewManager : MonoBehaviour
{
    [SerializeField] private Opening _opening;
    [SerializeField] private GameObject[] _mainMenu;
    [SerializeField] private MainMenuController _mainMenuController;
    [SerializeField] private GameObject _game;
    [SerializeField] private GameObject _dimBackGround;
    [SerializeField] private HowToPlayPopup _howToPlayPopup;
    [SerializeField] private PausePopup _pausePopup;
    [SerializeField] private GameOverPopup _gameOverPopup;
    [SerializeField] private Image _screenTransition;

    private float _transitionDuration = 0.4f;
    private float _popupDuration = 0.2f;
    private float _popupStartScale = 0.9f;

    public static ViewManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetTransitionAlpha(0.0f);
    }

    public void ShowOpening()
    {
        SetMainMenuActive(false);
        _game.SetActive(false);
        _dimBackGround.SetActive(false);
        _howToPlayPopup.gameObject.SetActive(false);
        _pausePopup.gameObject.SetActive(false);
        _gameOverPopup.gameObject.SetActive(false);

        _opening.gameObject.SetActive(true);
        _opening.StartOpening();
    }

    public void ShowMainMenu()
    {
        _opening.gameObject.SetActive(false);
        _game.SetActive(false);
        _dimBackGround.SetActive(false);
        _howToPlayPopup.gameObject.SetActive(false);
        _pausePopup.gameObject.SetActive(false);
        _gameOverPopup.gameObject.SetActive(false);

        SetMainMenuActive(true);
        _mainMenuController.Init();
    }

    public void ShowGame()
    {
        _opening.gameObject.SetActive(false);
        SetMainMenuActive(false);
        _dimBackGround.SetActive(false);
        _howToPlayPopup.gameObject.SetActive(false);
        _pausePopup.gameObject.SetActive(false);
        _gameOverPopup.gameObject.SetActive(false);

        _game.SetActive(true);
    }

    public void ShowHowToPlay()
    {
        _dimBackGround.SetActive(true);
        _howToPlayPopup.gameObject.SetActive(true);

        PlayPopupOpenAnimation(_howToPlayPopup.GetComponent<RectTransform>(), () =>
        {

        });
    }

    public void ShowPause()
    {
        _dimBackGround.SetActive(true);
        _pausePopup.gameObject.SetActive(true);

        _pausePopup.Init();
        PlayPopupOpenAnimation(_pausePopup.GetComponent<RectTransform>(), () =>
        {

        });
    }

    public void ShowGameOver()
    {
        _dimBackGround.SetActive(true);
        _gameOverPopup.gameObject.SetActive(true);

        _gameOverPopup.Init();
        PlayPopupOpenAnimation(_gameOverPopup.GetComponent<RectTransform>(), () =>
        {
            _gameOverPopup.Refresh();
        });
    }

    public void Transition(Action onSwap, Action onComplete)
    {
        _screenTransition.DOKill();
        SetTransitionAlpha(0.0f);
        _screenTransition.DOFade(1.0f, _transitionDuration).SetEase(Ease.InOutQuad).SetUpdate(true)
            .OnComplete(() =>
            {
                onSwap?.Invoke();
                _screenTransition.DOFade(0.0f, _transitionDuration).SetEase(Ease.InOutQuad).SetUpdate(true)
                    .OnComplete(() =>
                    {
                        onComplete?.Invoke();
                    });
            });
    }

    private void PlayPopupOpenAnimation(RectTransform popup, Action onComplete)
    {
        popup.DOKill();
        popup.localScale = Vector3.one * _popupStartScale;
        popup.DOScale(Vector3.one, _popupDuration).SetEase(Ease.OutBack, 1.2f).SetUpdate(true)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });
    }

    private void SetMainMenuActive(bool active)
    {
        foreach (GameObject menuObject in _mainMenu)
        {
            menuObject.SetActive(active);
        }
    }

    private void SetTransitionAlpha(float alpha)
    {
        Color color = _screenTransition.color;
        color.a = alpha;
        _screenTransition.color = color;
    }

}
