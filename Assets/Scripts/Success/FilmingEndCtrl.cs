using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 출력 관련 최종 단계 컨트롤러
/// - 촬영/인쇄 시퀀스가 끝난 후
///   "출력하기" 버튼 상태로 전환하고, Ready 패널로 돌아갈 준비를 한다.
/// </summary>
public class FilmingEndCtrl : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _statusText;
    // 필요 시 상태 메시지를 표시할 수 있는 텍스트 (현재는 사용 X)

    private Coroutine _routine;
    // 진행 중인 코루틴 보관용

    private bool _isRunning;
    // 이미 실행 중인지 체크 (중복 호출 방지용)

    [Header("Out & Put")]
    [SerializeField] private GameObject _outPutObj;
    // "출력하기" 버튼이 포함된 오브젝트 (출력 단계로 이동하는 UI)

    [SerializeField] private GameObject _nextObj;
    // 이거 위에 "_outPutObj" 대신에 사용될 녀석
    // 기존 흐름 -> 촬영 -> 촬영중 -> 촬영 완료 -> 인쇄
    // 변경 흐름 -> 촬영 -> 다음으로 -> 사진 선택 -> 인쇄

    [SerializeField] private GameObject _backPutObj;
    // 필요 시 사용 가능한 배경/뒤쪽 UI 오브젝트 (현재 로직에서는 직접 사용 X)

    //[SerializeField] private Button _outPutBtn;
    [SerializeField] private TextMeshProUGUI _outPutTxt;
    // "출력하기", "출력완료" 등 출력 관련 텍스트를 표시하는 TMP

    // [Header("Object Setting")]
    // [SerializeField] private GameObject _descriptionFingerObject;
    // 출력 버튼을 안내하는 손가락 가이드 오브젝트

    private void Awake()
    {
        // 저장 경로 확인용 로그 (디버그)
        Debug.Log(Application.persistentDataPath);
    }

    /// <summary>
    /// 외부에서 호출하는 시작 함수
    /// - 출력 단계로 전환할 때 호출
    /// - 기본 메시지는 "출력하기"
    /// </summary>
    public void StartReturn(string message = "출력하기")
    {
        //print("dddddddddddddd");
        if (_isRunning)
        {
            // 이미 코루틴이 실행 중이면 무시
            return;
        }
        else
        {
            // 출력 완료 후 UI를 세팅하는 코루틴 시작
            _routine = StartCoroutine(CompleteAndReturnCoroutine(message));
        }
    }

    /// <summary>
    /// 캡처/인쇄 완료 후 버튼 및 안내 UI 변경을 처리하는 코루틴
    /// </summary>
    /// <param name="message">버튼/텍스트에 표시할 메시지 (예: "출력하기")</param>
    private IEnumerator CompleteAndReturnCoroutine(string message)
    {
        _isRunning = true;

        if (_outPutTxt != null)
        {
            //_statusText.gameObject.SetActive(true);
            // 출력 관련 텍스트 갱신 (예: "출력하기", "출력완료" 등)
            _outPutTxt.text = message;
        }
        else
        {
            Debug.LogWarning("_outPutTxt reference is missing");
        }

        // 출력까지 대기하고 애니메이션을 넣고 싶다면 이쪽에 WaitForSeconds 및 Animator 사용 가능
        // yield return new WaitForSeconds(_waitSeconds);
        //_animator.SetBool("Fade", true);

        // "출력하기" 버튼 및 안내 손가락 가이드 노출
        // _descriptionFingerObject.SetActive(true);
        // _outPutObj.SetActive(true);
        _nextObj.SetActive(true);

        // 배경 정리/전환 등 추가 연출이 필요하다면 이 아래에서 처리

        _isRunning = false;
        _routine = null;

        yield return null;
    }
}
