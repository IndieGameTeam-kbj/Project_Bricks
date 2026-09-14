using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] Board _board;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private GameObject _newBestIcon;

    private int _score = 0;
    private int _bestScore = 0;
    private int _prevBestScore = 0;
    private bool _isNewBestScore = false;
    private float _punchScale = 1.2f;
    private float _punchDuration = 0.2f;
    private float _countDuration = 0.1f;

    public int Score => _score;
    public int BestScore => _bestScore;
    public int PrevBestScore => _prevBestScore;
    public bool IsNewBestScore => _isNewBestScore;

    public static ScoreManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Reset()
    {
        _score = 0;
        _scoreText.text = _score.ToString();

        _prevBestScore = _bestScore;
        _isNewBestScore = false;

        _newBestIcon.SetActive(false);
    }

    public void RestoreScore(GameSaveData data)
    {
        _score = data.score;
        _isNewBestScore = data.isNewBestScore;

        _newBestIcon.SetActive(_isNewBestScore);
        RefreshScore();
    }

    public void RefreshScore()
    {
        _scoreText.transform.DOKill();
        _scoreText.transform.localScale = Vector3.one;
        _scoreText.text = _score.ToString();
    }

    private void AddScore(int amount)
    {
        float combo = 1.0f;
        if(amount >= 9)
        {
            combo = 3.0f;
        }
        else if (amount >= 5)
        {
            combo = 2.0f;
        }

        _score += (int)(amount * combo);

        if (_score > _bestScore)
        {
            // 이번 판에서 신기록을 처음 달성한 순간에만 실행
            if (!_isNewBestScore)
            {
                SoundManager.Instance.PlayBestScores();
                _isNewBestScore = true;
                _newBestIcon.SetActive(true);
            }
            SaveManager.Instance.SaveBestScore(_score);
            _bestScore = _score;
        }
    }

    public void ShowScore()
    {
        int.TryParse(_scoreText.text, out int displayedScore);
        PlayScoreAnimation(displayedScore, _score);
    }

    private void PlayScoreAnimation(int previousScore, int targetScore)
    {
        Transform target = _scoreText.transform;

        target.DOKill();
        target.localScale = Vector3.one;

        DOTween.To(() => previousScore, value =>
            {
                _scoreText.text = value.ToString();
            },
            targetScore, _countDuration
        )
        .SetEase(Ease.OutQuad);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(target.DOScale(Vector3.one * _punchScale, _punchDuration * 0.5f).SetEase(Ease.OutQuad));
        sequence.Append(target.DOScale(Vector3.one * 0.95f, _punchDuration * 0.2f).SetEase(Ease.InOutQuad));
        sequence.Append(target.DOScale(Vector3.one, _punchDuration * 0.3f).SetEase(Ease.OutQuad));
    }

    private void OnEnable()
    {
        _board.LineDestroyed += AddScore;
    }

    private void OnDisable()
    {
        _board.LineDestroyed -= AddScore;
    }

}
