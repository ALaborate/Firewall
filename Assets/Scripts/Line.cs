using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface ILine
{
    bool busy { get; }
    bool showSignal { get; }

    event System.Action<Packet.Data> OnPacketDrop;
    event System.Action<Packet.Data> OnPacketClear;
    void CreatePacket(Packet.Data data);
    int ClearPackets(string bodyContent);
    string ToString();
}

public class Line : MonoBehaviour, ILine
{
    public GameObject packetPrefab;
    public float paddingLeft = 70f;
    public float packetPadding = 10f;
    [Header("Calculated programmatically")]
    public GameObject signal;

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
        private set { signal.SetActive(value); }
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
            pgo.transform.SetParent(transform);
            pgo.transform.localScale = Vector3.one;
            signal.transform.SetParent(null);
            signal.transform.SetParent(transform);
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
        var vHalf = new Vector2(0.5f, 0.5f);
        prt.anchorMin = vHalf;
        prt.anchorMax = vHalf;
        prt.anchoredPosition = enterPos;
        bool pushingBlock = showedPackets.Count >= anchoredPacketPositions.Count;
        bool lastBlock = showedPackets.Count >= anchoredPacketPositions.Count - 1;

        if (pushingBlock)
        {
            var fp = showedPackets[0];
            OnPacketDrop?.Invoke(fp.data);
            fp.onFire = true;
            Debug.Assert(!fp.onTheMove);
            showedPackets.RemoveAt(0);
            //TODO add fp to onFirePackets list
            fp.MoveTo(exitPos, UtilizePacket);
            PlacePackets();
        }

        p.MoveTo(anchoredPacketPositions[showedPackets.Count]);
        showedPackets.Add(p);
    }

    private void PlacePackets()
    {
        for (int i = 0; i < showedPackets.Count; i++)
        {
            showedPackets[i].MoveTo(anchoredPacketPositions[i]);
        }
    }
    private void ClearPacket(Packet p)
    {
        UtilizePacket(p.gameObject);
    }
    public int ClearPackets(string bodyContent)
    {
        int c = 0;
        for(int i=0; i<showedPackets.Count; i++)
        {
            if (showedPackets[i].data.body == bodyContent)
            {
                OnPacketClear?.Invoke(showedPackets[i].data);
                ClearPacket(showedPackets[i]);
                showedPackets.RemoveAt(i--);
                c++;
            }
        }
        PlacePackets();
        //TODO check onFIre packet
        return c;
    }

    public event System.Action<Packet.Data> OnPacketDrop;
    public event System.Action<Packet.Data> OnPacketClear;
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
        int i = 0;
        while (true)
        {
            var pos = new Vector2(initialPosition.x + i++ * (prt.rect.width + packetPadding), initialPosition.y);
            //Debug.DrawLine(Vector3.zero, rt.position + new Vector3(pos.x, 0f) * transform.root.localScale.x, Color.red, float.PositiveInfinity);
            if (pos.x > limit)
                break;
            anchoredPacketPositions.Add(pos);
        }
        enterPos = new Vector2(prt.rect.width, 0f);
        exitPos = new Vector2(-parentrt.rect.width - prt.rect.width, 0f);

        creationPeriod = (prt.rect.width + 2 * packetPadding) / Packet.maxSpeed;
        showSignal = false;
    }

    private void Awake()
    {
        signal = transform.GetChild(0).gameObject;
        showedPackets = new List<Packet>();
        anchoredPacketPositions = new List<Vector2>();
        packetPool = new List<GameObject>();
    }

    private void Update()
    {
        if (Time.time < nextTimeToCreate && !showSignal)
            showSignal = true;
        else if (Time.time >= nextTimeToCreate && showSignal)
            showSignal = false;
    }
    public override string ToString()
    {
        return $"{nameof(Line)} {showedPackets.Count}";
    }
}
