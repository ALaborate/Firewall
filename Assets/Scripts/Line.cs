using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Line : MonoBehaviour
{
    public GameObject packetPrefab;
    public float paddingLeft = 70f;
    public float packetPadding = 10f;
    [Header("Calculated programmatically")]
    public GameObject signal;
    //public int capacity;
    //public float maxPacketSpeed;

    #region publicAPI
    public bool busy
    {
        get
        {
            var r = Time.time <= nextTimeToCreate || (showedPackets.Count >= anchoredPacketPositions.Count && showedPackets[showedPackets.Count - 1].onTheMove);
            return r;
        }
    }
    public bool showSignal
    {
        get { return signal.activeSelf; }
        set { signal.SetActive(value); }
    }
    private void UtilizePacket(GameObject pgo)
    {
        packetPool.Add(pgo);
        pgo.SetActive(false);
    }
    public void CreatePacket(Packet.Data data)
    {
        if (busy) return;
        nextTimeToCreate = Time.time + creationPeriod;

        GameObject pgo = null;
        if (packetPool.Count == 0)
        {
            pgo = Instantiate(packetPrefab);
        }
        else
        {
            pgo = packetPool[packetPool.Count - 1];
            packetPool.RemoveAt(packetPool.Count - 1);
            pgo.SetActive(true);
        }
        var p = pgo.GetComponent<Packet>();
        p.onFire = false;
        p.data = data;

        var prt = pgo.transform as RectTransform;
        prt.SetParent(transform);
        prt.localScale = Vector3.one;
        var vHalf = new Vector2(0.5f, 0.5f);
        prt.anchorMin = vHalf;
        prt.anchorMax = vHalf;
        prt.anchoredPosition = enterPos;
        bool pushingBlock = showedPackets.Count >= anchoredPacketPositions.Count;
        bool lastBlock = showedPackets.Count >= anchoredPacketPositions.Count - 1;

        if (pushingBlock)
        {
            var fp = showedPackets[0];
            fp.onFire = true;
            Debug.Assert(!fp.onTheMove);
            showedPackets.RemoveAt(0);
            fp.MoveTo(exitPos, UtilizePacket);
            for (int i = 0; i < showedPackets.Count; i++)
            {
                Debug.Assert(!showedPackets[i].onTheMove);
                showedPackets[i].MoveTo(anchoredPacketPositions[i]);
            }
        }

        p.MoveTo(anchoredPacketPositions[showedPackets.Count]);
        showedPackets.Add(p);
    }
    #endregion
    float nextTimeToCreate = -3f, creationPeriod = 1f;
    List<Vector2> anchoredPacketPositions;
    List<Packet> showedPackets;
    List<GameObject> packetPool;
    RectTransform rt;
    Vector2 enterPos, exitPos;

    void Start()
    {
        var prt = packetPrefab.transform as RectTransform;
        var parentrt = transform.parent as RectTransform;
        rt = transform as RectTransform;

        Vector2 initialPosition = new Vector2(-parentrt.rect.width + (rt.rect.width + prt.rect.width) * 0.5f + paddingLeft, 0f);
        var limit = -(prt.rect.width + rt.rect.width) * 0.5f - packetPadding;
        //Debug.DrawLine(Vector3.zero, rt.position + new Vector3(limit, 0f) * transform.root.localScale.x, Color.green, float.PositiveInfinity);
        int i = 0;
        while (true)
        {
            var pos = new Vector2(initialPosition.x + i++ * (prt.rect.width + packetPadding), initialPosition.y);
            //Debug.DrawLine(Vector3.zero, rt.position + new Vector3(pos.x, 0f) * transform.root.localScale.x, Color.red, float.PositiveInfinity);
            if (pos.x > limit)
                break;
            anchoredPacketPositions.Add(pos);
        }
        enterPos = new Vector2(prt.rect.width * 0.7f, 0f);
        exitPos = new Vector2(-parentrt.rect.width - prt.rect.width, 0f);
        
        creationPeriod = (prt.rect.width+2*packetPadding) / Packet.maxSpeed;
    }



    private void Awake()
    {
        signal = transform.GetChild(0).gameObject;
        showedPackets = new List<Packet>();
        anchoredPacketPositions = new List<Vector2>();
        packetPool = new List<GameObject>();
    }

    public override string ToString()
    {
        return $"{nameof(Line)} {showedPackets.Count}";
    }
}
