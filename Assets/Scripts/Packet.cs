﻿using System.Collections;
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
    public Text BodyText { get; private set; }
    public bool onTheMove { get { return transition != null; } }
    public Data data
    {
        get { return _data; }
        set
        {
            _data = value;
            BodyText.text = _data.body;
            background.color = data.bgcolor;
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
    private Coroutine transition = null;
    private void Start()
    {
        onFire = false;
    }
    private Image background;
    void Awake()
    {
        Debug.Assert(transform.childCount >= 2, "Packet has two children for text");
        BodyText = transform.GetComponentInChildren<Text>();
        background = transform.GetComponentInChildren<Image>();
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
        public string body;
        public Color bgcolor;
        public readonly bool good;
        public Data(string _body, bool _good, Color? _bgcolor=null)
        {
            body = _body;
            good = _good;
            bgcolor = _bgcolor.HasValue ? _bgcolor.Value : new Color(4f, 99f, 255f);
        }
    }
    public enum DeathCause
    {
        Internal, Drop, Clear
    }
    public override string ToString()
    {
        return $"={data.body}";
    }

}
