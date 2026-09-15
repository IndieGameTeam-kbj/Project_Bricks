using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] Board _board;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private GameObject _newBestIcon;
    [SerializeField] private TMP_Text _comboText;

    private int _score = 0;
    private int _bestScore = 0;
    private int _prevBestScore = 0;
    private bool _isNewBestScore = false;
    private float _punchScale = 1.2f;
    private float _punchDuration = 0.2f;
    private float _countDuration = 0.1f;
    private float _comboScale = 1.0f;
    private float _comboScaleIncrease = 0.2f;
    private float _comboDuration = 0.4f;
    private float _comboDisplayDuration = 0.4f;

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

    private void Start()
    {
        _bestScore = SaveManager.Instance.LoadBestScore();
    }

    public void Reset()
    {
        _score = 0;
        _scoreText.text = _score.ToString("N0");
        _prevBestScore = _bestScore;
        _isNewBestScore = false;
        _newBestIcon.SetActive(false);
        _comboText.DOKill();
        _comboText.gameObject.SetActive(false);
    }

    public void RestoreScore(GameSaveData data)
    {
        _score = data.score;
        _prevBestScore = data.prevBestScore;
        _bestScore = SaveManager.Instance.LoadBestScore();

        RefreshScore();

        _isNewBestScore = data.isNewBestScore;
        _newBestIcon.SetActive(_isNewBestScore);
        _comboText.DOKill();
        _comboText.gameObject.SetActive(false);
    }

    public void RefreshScore()
    {
        _scoreText.transform.DOKill();
        _scoreText.transform.localScale = Vector3.one;
        _scoreText.text = _score.ToString("N0");
    }

    private void AddScore(int amount)
    {
        float combo = 1.0f;
        if(amount >= 13)
        {
            combo = 4.0f;
        }
        else if(amount >= 9)
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

        if (combo > 1.0f)
        {
            PlayComboAnimation((int)combo);
        }
    }

    public void ShowScore()
    {
        int.TryParse(_scoreText.text, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int displayedScore);
        PlayScoreAnimation(displayedScore, _score);
    }

    private void PlayScoreAnimation(int previousScore, int targetScore)
    {
        Transform target = _scoreText.transform;

        target.DOKill();
        target.localScale = Vector3.one;

        DOTween.To(() => previousScore, value =>
            {
                _scoreText.text = value.ToString("N0");
            },
            targetScore, _countDuration
        )
        .SetEase(Ease.OutQuad);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(target.DOScale(Vector3.one * _punchScale, _punchDuration * 0.5f).SetEase(Ease.OutQuad));
        sequence.Append(target.DOScale(Vector3.one * 0.95f, _punchDuration * 0.2f).SetEase(Ease.InOutQuad));
        sequence.Append(target.DOScale(Vector3.one, _punchDuration * 0.3f).SetEase(Ease.OutQuad));
    }

    private void PlayComboAnimation(int combo)
    {
        _comboText.DOKill();
        _comboText.text = $"x{combo} Combo!";
        _comboText.gameObject.SetActive(true);

        Transform target = _comboText.transform;
        target.localScale = Vector3.zero;
        _comboText.alpha = 1.0f;

        float targetScale = _comboScale + combo * _comboScaleIncrease;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(target.DOScale(Vector3.one * targetScale, _comboDuration * 0.6f).SetEase(Ease.OutBack));
        sequence.Append(target.DOScale(Vector3.one, _comboDuration * 0.4f).SetEase(Ease.OutQuad));
        sequence.AppendInterval(_comboDisplayDuration);
        sequence.Append(_comboText.DOFade(0.0f, 0.2f));
        sequence.OnComplete(() =>
        {
            _comboText.gameObject.SetActive(false);
        });
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
