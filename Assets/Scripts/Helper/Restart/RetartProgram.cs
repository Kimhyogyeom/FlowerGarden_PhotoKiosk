using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 프로그램(현재 씬) 재시작 컨트롤러
/// - "재시작" 버튼 클릭 시 확인 팝업 활성화
/// - "예" 선택 시 현재 씬을 다시 로드(완전 리셋)
/// - "아니오" 선택 시 확인 팝업 닫기
/// </summary>
public class RetartProgram : MonoBehaviour
{
    [Header("Game Object")]
    [SerializeField] private GameObject _restartAgainCheckObj;   // 재시작 확인 팝업 오브젝트

    [Header("Button")]
    [SerializeField] private Button _restartButton;              // 재시작 버튼
    [SerializeField] private Button _restartAgainYButton;        // 재시작 확인 "예" 버튼
    [SerializeField] private Button _restartAgainNButton;        // 재시작 확인 "아니오" 버튼

    /// <summary>
    /// 버튼에 대한 클릭 리스너 등록
    /// </summary>
    private void Awake()
    {
        _restartButton.onClick.AddListener(OnRestartBtn);
        _restartAgainYButton.onClick.AddListener(OnRestartAgainYBtn);
        _restartAgainNButton.onClick.AddListener(OnRestartAgainNBtn);
    }

    /// <summary>
    /// 재시작 버튼 클릭 시: 재시작 확인 팝업 열기
    /// </summary>
    private void OnRestartBtn()
    {
        _restartAgainCheckObj.SetActive(true);
    }

    /// <summary>
    /// 재시작 확인 "예" 버튼 클릭 시:
    /// - 현재 활성 씬을 다시 로드하여 완전 초기화
    /// - (추가 방어용) 팝업이 비활성화 되어 있으면 다시 활성화
    /// </summary>
    private void OnRestartAgainYBtn()
    {
        // 씬 완전 리셋
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
        if (!_restartAgainCheckObj.activeSelf) _restartAgainCheckObj.SetActive(true);
    }

    /// <summary>
    /// 재시작 확인 "아니오" 버튼 클릭 시: 팝업 닫기
    /// </summary>
    private void OnRestartAgainNBtn()
    {
        _restartAgainCheckObj.SetActive(false);
    }
}
