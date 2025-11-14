using UnityEngine;

/// <summary>
/// 인쇄중 -> 인쇄 완료 변경 스크립트
/// </summary>
public class OutputSuccessCtrl : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private InitCtrl _initCtrl;

    [Header("Object Settings")]
    [SerializeField] private GameObject _outputtingObjParent;
    [SerializeField] private GameObject _outputSuccessObjParent;
    [SerializeField] private GameObject _backButtonObject;

    public void OutputSuccessObjChange()
    {
        _outputtingObjParent.SetActive(false);
        _outputSuccessObjParent.SetActive(true);

        _backButtonObject.SetActive(true);
        _initCtrl.ResetCallBack();
    }
        
    public void ObjectChangeReset()
    {
        _outputtingObjParent.SetActive(true);
        _outputSuccessObjParent.SetActive(false);

        _backButtonObject.SetActive(false);
    }
}
