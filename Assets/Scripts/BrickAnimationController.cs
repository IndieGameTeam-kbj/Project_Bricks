using System;
using UnityEngine;
using DG.Tweening;

public class BrickAnimationController : MonoBehaviour
{
    private Sequence _spawnSequence;
    private float _spawnDuration = 0.5f;
    private float _spawnRotation = 360.0f;
    private Sequence _destroySequence;
    private float _destroyDuration = 0.5f;
    private float _destroyScale = 1.1f;
    private Vector3 _originalScale;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    public void PlaySpawnAnimation(Vector3 targetPosition, Action onComplete)
    {
        _spawnSequence?.Kill();
        transform.DOKill();

        transform.localScale = _originalScale;
        transform.localRotation = Quaternion.identity;

        _spawnSequence = DOTween.Sequence();
        _spawnSequence.Append(transform.DOMove(targetPosition, _spawnDuration).SetEase(Ease.OutCubic));
        _spawnSequence.Join(
            DOVirtual.Float(0.0f, 1.0f, _spawnDuration, value =>
            {
                float rotationAmount;

                if (value < 0.5f)
                {
                    float t = value / 0.5f;
                    rotationAmount = Mathf.Lerp(0.0f, _spawnRotation, t);
                }
                else
                {
                    float t = (value - 0.5f) / 0.5f;
                    rotationAmount = Mathf.Lerp(_spawnRotation, 0.0f, t);
                }

                transform.localRotation =Quaternion.Euler(0.0f, 0.0f, -rotationAmount);
            })
            .SetEase(Ease.OutCubic)
        );
        _spawnSequence.OnComplete(() =>
        {
            transform.position = targetPosition;
            transform.localRotation = Quaternion.identity;
            onComplete?.Invoke();
        });
    }

    public void PlayDestroyAnimation(Action onComplete)
    {
        _spawnSequence?.Kill();
        _destroySequence?.Kill();
        transform.DOKill();

        _destroySequence = DOTween.Sequence();
        _destroySequence.Append(transform.DOScale(_originalScale * _destroyScale, _destroyDuration * 0.3f).SetEase(Ease.OutQuad));
        _destroySequence.Append(transform.DOScale(Vector3.zero, _destroyDuration * 0.7f).SetEase(Ease.InBack));
        _destroySequence.Join(
            transform.DORotate(new Vector3(0.0f, 0.0f, UnityEngine.Random.Range(-8.0f, 8.0f)), _destroyDuration).SetEase(Ease.InQuad)
            );
        _destroySequence.OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    private void OnDestroy()
    {
        _spawnSequence?.Kill();
        _destroySequence?.Kill();
    }

}
