using DG.Tweening;
using TMPro;
using UnityEngine;

public class GameOverPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _bestScoreText;

    private float _scoreCountDuration = 0.4f;
    private float _bestPunchScale = 1.15f;
    private float _bestPunchDuration = 0.2f;
    private int _score;
    private int _bestScore;

    public void Init()
    {
        _score = ScoreManager.Instance.Score;
        _bestScore = ScoreManager.Instance.PrevBestScore;

        _scoreText.text = "0";
        _bestScoreText.text = _bestScore.ToString("N0");

        _scoreText.transform.localScale = Vector3.one;
        _bestScoreText.transform.localScale = Vector3.one;
    }

    public void Refresh()
    {
        PlayScoreAnimation();
    }

    private void PlayScoreAnimation()
    {
        _scoreText.DOKill();
        int currentScore = 0;

        DOTween.To(() => currentScore, value =>
            {
                currentScore = value;
                _scoreText.text = currentScore.ToString("N0");
            }, _score, _scoreCountDuration
        )
        .SetEase(Ease.OutQuad)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            if (ScoreManager.Instance.IsNewBestScore)
            {
                _bestScoreText.text = ScoreManager.Instance.BestScore.ToString("N0");
                PlayBestScoreAnimation();
            }
        });
    }

    private void PlayBestScoreAnimation()
    {
        Transform target = _bestScoreText.transform;
        target.DOKill();
        target.localScale = Vector3.one;
        target.DOScale(Vector3.one * _bestPunchScale, _bestPunchDuration * 0.5f).SetEase(Ease.OutQuad).SetUpdate(true)
            .OnComplete(() =>
            {
                target.DOScale(Vector3.one, _bestPunchDuration * 0.5f).SetEase(Ease.OutQuad).SetUpdate(true);
            });
    }

    public void OnClickHomeButton()
    {
        SoundManager.Instance.PlayButtonClick();
        GameManager.Instance.Home();
    }

    public void OnClickRestartButton()
    {
        SoundManager.Instance.PlayButtonClick();
        GameManager.Instance.Restart();
    }

}
