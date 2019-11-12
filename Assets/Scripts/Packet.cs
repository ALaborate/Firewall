using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Packet : MonoBehaviour
{
    public GameObject fireSign;
    bool _onFire = false;
    public bool onFire
    {
        get
        {
            return _onFire;
        }
        set
        {
            _onFire = value;
            fireSign.SetActive(value);
        }
    }
    public Text HeaderText { get { return _HeaderText; } }
    public Text BodyText { get { return _BodyText; } }
    public bool onTheMove { get { return transition != null; } }
    public Data data
    {
        get { return _data; }
        set
        {
            _data = value;
            HeaderText.text = _data.header;
            BodyText.text = _data.body;
        }
    }

    private void ClearTransition() { transition = null; }
    public void MoveTo(Vector2 anchoredPosition)
    {
        transition = StartCoroutine(Transition(transform as RectTransform, anchoredPosition, ClearTransition));
    }

    Data _data;
    private Text _HeaderText, _BodyText;
    private Coroutine transition = null;
    private void Start()
    {
        onFire = false;
    }
    void Awake()
    {
        Debug.Assert(transform.childCount >= 2, "Packet has two children for text");
        _HeaderText = transform.GetChild(0).GetComponentInChildren<Text>();
        _BodyText = transform.GetChild(1).GetComponentInChildren<Text>();
    }

    public static float maxSpeed=1f;
    public static IEnumerator Transition(RectTransform rectTransform, Vector2 targetPosition, System.Action onBreak = null)
    {
        while (true)
        {
            var change = targetPosition - rectTransform.anchoredPosition;
            var maxChange = change.normalized * maxSpeed * Time.deltaTime;
            if (change.sqrMagnitude <= maxChange.sqrMagnitude)
            {
                rectTransform.anchoredPosition = targetPosition;
                if (onBreak != null)
                    onBreak();
                yield break;
            }
            else
            {
                rectTransform.anchoredPosition += maxChange;
                yield return null;
            }
        }
    }

    [System.Serializable]
    public class Data
    {
        public string header;
        public string body;
        public Data(string _header, string _body)
        {
            header = _header;
            body = _body;
        }
    }
    public override string ToString()
    {
        return $"{data.header} {data.body}";
    }

}
