using UnityEngine;

/// <summary>
/// 인쇄 상태 전환 컨트롤러
/// - "인쇄중" 화면 → "인쇄 완료" 화면으로 전환
/// - 되돌릴 때 다시 "인쇄중" 상태로 복구
/// </summary>
public class OutputSuccessCtrl : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private InitCtrl _initCtrl;
    // 인쇄 완료 후 다시 초기 화면(Ready 등)으로 돌아갈 때 사용할 초기화 컨트롤러

    [Header("Object Settings")]
    [SerializeField] private GameObject _outputtingObjParent;
    // "인쇄중" 상태를 보여주는 부모 오브젝트(로딩 애니메이션, 텍스트 등 포함)

    [SerializeField] private GameObject _outputSuccessObjParent;
    // "인쇄 완료" 상태를 보여주는 부모 오브젝트(완료 메시지, 아이콘 등 포함)

    [SerializeField] private GameObject _backButtonObject;
    // 인쇄 완료 후 다시 처음 화면으로 돌아가는 버튼 오브젝트

    /// <summary>
    /// 인쇄 완료 시 호출
    /// - "인쇄중" 오브젝트 비활성화
    /// - "인쇄 완료" 오브젝트 활성화
    /// - 뒤로가기 버튼 활성화
    /// - InitCtrl 의 ResetCallBack 호출 (다음 흐름 준비)
    /// </summary>
    public void OutputSuccessObjChange()
    {
        // 인쇄중 화면 숨기기
        _outputtingObjParent.SetActive(false);

        // 인쇄 완료 화면 보여주기
        _outputSuccessObjParent.SetActive(true);

        // 되돌아가기 버튼 활성화
        _backButtonObject.SetActive(true);

        // 초기화/되돌리기 로직 호출
        _initCtrl.ResetCallBack();
    }

    /// <summary>
    /// 인쇄 상태를 다시 "인쇄중" 상태로 되돌릴 때 사용
    /// - 인쇄중 오브젝트 활성화
    /// - 인쇄 완료 오브젝트 비활성화
    /// - 뒤로가기 버튼 비활성화
    /// </summary>
    public void ObjectChangeReset()
    {
        // 인쇄중 화면 다시 보이게
        _outputtingObjParent.SetActive(true);

        // 인쇄 완료 화면 숨기기
        _outputSuccessObjParent.SetActive(false);

        // 되돌아가기 버튼 숨기기
        _backButtonObject.SetActive(false);
    }
}
