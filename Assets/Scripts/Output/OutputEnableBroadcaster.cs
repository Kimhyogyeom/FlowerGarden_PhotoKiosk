using System;
using System.Collections;
using UnityEngine;

public class OutputEnableBroadcaster : MonoBehaviour
{
    public static event Action OnOutputEnabled;

    private Coroutine _pending;        // 대기 중인 코루틴 참조
    private bool _firedThisEnable;     // 이번 Enable 사이클에서 이미 발사했는지

    private void OnEnable()
    {
        _firedThisEnable = false;

        // 이미 대기 중이면 또 시작하지 않음(중복 방지)
        if (_pending == null)
            _pending = StartCoroutine(InvokeNextFrame());
    }

    private void OnDisable()
    {
        // 비활성화되면 대기 코루틴 취소(유령 발사 방지)
        if (_pending != null)
        {
            StopCoroutine(_pending);
            _pending = null;
        }
        _firedThisEnable = false;
    }

    private IEnumerator InvokeNextFrame()
    {
        // “같은 프레임에 구독 미완료” 문제 회피
        yield return null; // next frame (필요시 WaitForEndOfFrame()로 더 늦출 수도 있음)

        if (!_firedThisEnable)
        {
            _firedThisEnable = true;
            OnOutputEnabled?.Invoke();
        }

        _pending = null;
    }
}
