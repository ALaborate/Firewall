using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IPacket
{
    Packet.Data data { get; }
    bool onFire { get; }
    bool onTheMove { get; }
    void MoveTo(Vector2 anchoredPosition, System.Action<GameObject> endCallback = null);
}

public class Packet : MonoBehaviour, IPacket
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
    public void MoveTo(Vector2 anchoredPosition, System.Action<GameObject> endCallback = null)
    {
        if (onTheMove)
            StopCoroutine(transition);
        transition = StartCoroutine(Transition(transform as RectTransform, anchoredPosition, endCallback, ClearTransition));
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

    public static float maxSpeed = 1f;
    public static IEnumerator Transition(RectTransform rectTransform, Vector2 targetPosition, System.Action<GameObject> onBreak = null, System.Action internalOnBreak = null)
    {
        while (true)
        {
            var change = targetPosition - rectTransform.anchoredPosition;
            var maxChange = change.normalized * maxSpeed * Time.deltaTime;
            if (change.sqrMagnitude <= maxChange.sqrMagnitude)
            {
                rectTransform.anchoredPosition = targetPosition;
                if (onBreak != null)
                    onBreak(rectTransform.gameObject);
                if (internalOnBreak != null)
                    internalOnBreak();
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
    public struct Data
    {
        public string header;
        public string body;
        public readonly bool good;
        public Data(string _header, string _body, bool _good)
        {
            header = _header;
            body = _body;
            good = _good;
        }
    }
    public enum DeathCause
    {
        Internal, Drop, Clear
    }
    public override string ToString()
    {
        return $"{data.header} {data.body}";
    }

}
