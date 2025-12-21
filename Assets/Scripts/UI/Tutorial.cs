using UnityEngine;
using UnityEngine.InputSystem;

public class Tutorial : MonoBehaviour
{
    private void OnEnable()
    {
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (Keyboard.current.enterKey.isPressed) ClearTutorial();
    }

    public void ClearTutorial()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }
}
