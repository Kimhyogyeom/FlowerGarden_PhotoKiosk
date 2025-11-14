using UnityEngine;
using UnityEngine.UI;

public class PrintTestBlank : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private PrintController _printCtrl;

    [Header("Object Setting")]
    [SerializeField] private Button _printButton;

    private void Awake()
    {
        _printButton.onClick.AddListener(OnTestPrintStart);
    }

    private void OnTestPrintStart()
    {
        _printCtrl.PrintTestBlank();
    }
}
