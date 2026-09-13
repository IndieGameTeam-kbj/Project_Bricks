using System.Collections;
using UnityEngine;

public class Opening : MonoBehaviour
{
    [SerializeField] private LogoAlphabet[] _logoAlphabets = new LogoAlphabet[6];

    private float _logoAnimationDelay = 0.05f;
    private bool _isOpening;

    public void StartOpening()
    {
        _isOpening = true;
        StartCoroutine(PlayLogoAnimation());
    }

    private IEnumerator PlayLogoAnimation()
    {
        for (int i = 0; i < _logoAlphabets.Length; i++)
        {
            _logoAlphabets[i].gameObject.SetActive(false);
        }
        yield return new WaitForSeconds(_logoAnimationDelay);

        for (int i = 0; i < _logoAlphabets.Length; i++)
        {
            _logoAlphabets[i].gameObject.SetActive(true);
            _logoAlphabets[i].Init();
        }

        for (int i = 0; i < _logoAlphabets.Length; i++)
        {
            _logoAlphabets[i].PlayAnimation();
            yield return new WaitForSeconds(_logoAnimationDelay);
        }

        yield return new WaitForSeconds(_logoAnimationDelay * 10);
        _isOpening = false;
        GameManager.Instance.Home();
    }

    private void Update()
    {
        if (!_isOpening) return;

        if (InputManager.Instance.IsPointerPressed)
        {
            SkipOpening();
        }
    }

    private void SkipOpening()
    {
        _isOpening = false;
        StopAllCoroutines();
        GameManager.Instance.Home();
    }

}
