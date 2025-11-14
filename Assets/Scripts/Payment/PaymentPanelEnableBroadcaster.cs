using System;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// 결제 패널 활성화 브로드캐스터
/// - 이 스크립트가 붙은 오브젝트가 활성화(OnEnable)될 때
///   정적 이벤트(OnPaymentPanelEnabled)를 호출하여
///   결제 시작 로직(PaymentCtrl 등)에 "지금 결제 패널이 켜졌다"는 신호를 보냄.
/// </summary>
public class PaymentPanelEnableBroadcaster : MonoBehaviour
{
    /// <summary>
    /// 결제 패널이 활성화되었음을 알리는 정적 이벤트
    /// - 예) PaymentCtrl 에서 이 이벤트를 구독하고 있다가
    ///   패널이 켜지는 순간 결제 시도를 시작.
    /// </summary>
    public static event Action OnPaymentPanelEnabled;

    /// <summary>
    /// GameObject 가 활성화될 때 자동 호출
    /// - 결제 패널이 켜지는 시점이라고 보고 이벤트를 브로드캐스트함.
    /// </summary>
    private void OnEnable()
    {
        OnPaymentPanelEnabled?.Invoke();
    }
}
