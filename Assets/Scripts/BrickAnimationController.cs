using System;
using UnityEngine;
using DG.Tweening;

public class BrickAnimationController : MonoBehaviour
{
    private Sequence _spawnSequence;
    private Sequence _destroySequence;
    private float _destroyDuration = 0.5f;
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

        float duration = UnityEngine.Random.Range(0.48f, 0.52f);
        float rotation = UnityEngine.Random.Range(300.0f, 420.0f);
        float rotationStart = UnityEngine.Random.Range(0.0f, 0.1f);

        _spawnSequence = DOTween.Sequence();
        _spawnSequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.OutCubic));
        _spawnSequence.Join(
            DOVirtual.Float(0.0f, 1.0f, duration, value =>
            {
                float rotationAmount;

                if (value < rotationStart)
                {
                    rotationAmount = 0.0f;
                }
                else
                {
                    float t = Mathf.InverseLerp(rotationStart, 1.0f, value);

                    if (t < 0.5f)
                    {
                        float rotateT = t / 0.5f;
                        rotationAmount = Mathf.Lerp(0.0f, rotation, rotateT);
                    }
                    else
                    {
                        float rotateT = (t - 0.5f) / 0.5f;
                        rotationAmount = Mathf.Lerp(rotation, 0.0f, rotateT);
                    }
                }

                transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -rotationAmount);
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

        float compressScale = UnityEngine.Random.Range(0.90f, 0.96f);
        float expandScale = UnityEngine.Random.Range(1.08f, 1.16f);
        float rotation = UnityEngine.Random.Range(-12.0f, 12.0f);

        if (Mathf.Abs(rotation) < 4.0f)
        {
            rotation = rotation < 0.0f ? -4.0f : 4.0f;
        }

        float compressDuration = _destroyDuration * 0.2f;
        float expandDuration = _destroyDuration * 0.25f;
        float shrinkDuration = _destroyDuration * 0.55f;

        _destroySequence = DOTween.Sequence();
        _destroySequence.Append(transform.DOScale(_originalScale * compressScale, compressDuration).SetEase(Ease.OutQuad));
        _destroySequence.Append(transform.DOScale(_originalScale * expandScale, expandDuration).SetEase(Ease.OutQuad));
        _destroySequence.Append(transform.DOScale(Vector3.zero, shrinkDuration).SetEase(Ease.InBack));
        _destroySequence.Join(transform.DORotate(new Vector3(0.0f, 0.0f, rotation), shrinkDuration).SetEase(Ease.InQuad));
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
