using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 출력 완료 표시 후 지정 시간 대기하고 대기(Ready) 패널로 복귀
/// </summary>
public class FilmingEndCtrl : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _statusText;

    private Coroutine _routine;
    private bool _isRunning;

    [Header("Out & Put")]
    [SerializeField] private GameObject _outPutObj;
    [SerializeField] private GameObject _backPutObj;
    //[SerializeField] private Button _outPutBtn;
    [SerializeField] private TextMeshProUGUI _outPutTxt;

    [Header("Object Setting")]
    [SerializeField] private GameObject _descriptionFingerObject;

    private void Awake()
    {
        Debug.Log(Application.persistentDataPath);
    }

    /// <summary>
    /// 호출용 함수
    /// </summary>
    public void StartReturn(string message = "출력하기")
    {
        //print("dddddddddddddd");
        if (_isRunning)
        {
            return;
        }
        else
        {
            _routine = StartCoroutine(CompleteAndReturnCoroutine(message));
        }
    }

    /// <summary>
    /// 캡처 완료 후 버튼 변경 함수
    /// </summary>
    /// <param name="message">Message</param>
    /// <returns></returns>
    private IEnumerator CompleteAndReturnCoroutine(string message)
    {
        _isRunning = true;

        if (_outPutTxt != null)
        {
            //_statusText.gameObject.SetActive(true);
            _outPutTxt.text = message; // "출력완료" => 출력하기로 변격ㅇ
        }
        else
        {
            Debug.LogWarning("_outPutTxt reference is missing");
        }

        // yield return new WaitForSeconds(_waitSeconds);
        //_animator.SetBool("Fade", true);

        // 출력하기 버튼 On
        _descriptionFingerObject.SetActive(true);
        _outPutObj.SetActive(true);

        // 배경 날리는거

        _isRunning = false;
        _routine = null;

        yield return null;
    }
}