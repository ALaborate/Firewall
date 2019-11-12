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

    Data _data;
    private Text _HeaderText, _BodyText;
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
}
