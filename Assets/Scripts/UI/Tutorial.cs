using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class Tutorial : MonoBehaviour
{

    [SerializeField] PlayableDirector direct;
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

        direct.Play();
    }
}
