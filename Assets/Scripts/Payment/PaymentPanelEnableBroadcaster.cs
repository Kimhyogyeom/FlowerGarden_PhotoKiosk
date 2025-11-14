using System;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class PaymentPanelEnableBroadcaster : MonoBehaviour
{
    public static event Action OnPaymentPanelEnabled;    

    private void OnEnable()
    {        
        OnPaymentPanelEnabled?.Invoke();
    }
}
