using TMPro;
using UnityEngine;

public class HelporTextReset : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] _resetTexts;

    public void ResetTexts()
    {
        for (int i = 0; i < _resetTexts.Length; i++)
        {
            _resetTexts[i].text = "";
        }
    }
}
