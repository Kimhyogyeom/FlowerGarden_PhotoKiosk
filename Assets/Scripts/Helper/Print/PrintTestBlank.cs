using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 테스트용 공백(하얀 배경) 인쇄 버튼 컨트롤러
/// - 버튼 클릭 시 PrintController의 PrintTestBlank()를 호출해서
///   실제 프린터로 빈 사진을 출력 테스트할 때 사용
/// </summary>
public class PrintTestBlank : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private PrintController _printCtrl;   // 실제 인쇄 로직을 담당하는 PrintController

    [Header("Object Setting")]
    [SerializeField] private Button _printButton;          // 테스트 인쇄 버튼

    /// <summary>
    /// 버튼 클릭 리스너 등록
    /// </summary>
    private void Awake()
    {
        _printButton.onClick.AddListener(OnTestPrintStart);
    }

    /// <summary>
    /// 테스트 인쇄 시작 (PrintController에 공백 인쇄 요청)
    /// </summary>
    private void OnTestPrintStart()
    {
        _printCtrl.PrintTestBlank();
    }
}
