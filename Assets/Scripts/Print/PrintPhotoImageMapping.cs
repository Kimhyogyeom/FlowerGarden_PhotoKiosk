using System.Drawing;
using UnityEngine;

public class ImageMapping : MonoBehaviour
{
    [Header("Grid Red")]
    [SerializeField] private GameObject _redObject;
    [SerializeField] private Image[] _gridRedImagesCurrent;
    [SerializeField] private Image[] _gridRedImagesChange;

    [Header("Grid Blue")]
    [SerializeField] private GameObject _blueObject;
    [SerializeField] private Image[] _gridBlueImagesCurrent;
    [SerializeField] private Image[] _gridBlueImagesChange;

    [Header("Grid Black")]
    [SerializeField] private GameObject _blackObject;
    [SerializeField] private Image[] _gridBlackImagesCurrent;
    [SerializeField] private Image[] _gridBlackImagesChange;


}
