using UnityEngine;

public class HowToPlayPopup : MonoBehaviour
{
    public void OnClickHomeButton()
    {
        SoundManager.Instance.PlayButtonClick();
        GameManager.Instance.Home();
    }

}
