using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RetartProgram : MonoBehaviour
{
    [Header("Game Object")]
    [SerializeField] private GameObject _restartAgainCheckObj;

    [Header("Button")]
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _restartAgainYButton;
    [SerializeField] private Button _restartAgainNButton;

    private void Awake()
    {
        _restartButton.onClick.AddListener(OnRestartBtn);
        _restartAgainYButton.onClick.AddListener(OnRestartAgainYBtn);
        _restartAgainNButton.onClick.AddListener(OnRestartAgainNBtn);
    }

    private void OnRestartBtn()
    {
        _restartAgainCheckObj.SetActive(true);
    }

    private void OnRestartAgainYBtn()
    {
        // ¾À ¿ÏÀü ¸®¼Â
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
        if (!_restartAgainCheckObj.activeSelf) _restartAgainCheckObj.SetActive(true);
    }

    private void OnRestartAgainNBtn()
    {
        _restartAgainCheckObj.SetActive(false);
    }
}
