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
    public float maxPacketSpeed;

    #region publicAPI
    public bool busy
    {
        get
        {
            return profiles.Count >= anchoredPacketPositions.Count && profiles[profiles.Count - 1].transition != null;
        }
    }
    public bool showSignal
    {
        get { return signal.activeSelf; }
        set { signal.SetActive(value); }
    }

    public struct PacketProfile
    {
        public Packet packet;
        public Coroutine transition { get; set; }
        public Vector2 targetPosition;
        public PacketProfile(Packet packet, Coroutine transition, Vector2 targetPosition)
        {
            this.packet = packet;
            this.transition = transition;
            this.targetPosition = targetPosition;
        }

    }

    public void CreatePacket(Packet.Data data)
    {
        if (busy) return;
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
        bool pushingBlock = profiles.Count == anchoredPacketPositions.Count;

        PacketProfile prof;
        if (pushingBlock)
        {
            var fp = profiles[0].packet;
            fp.onFire = true;

            profiles.RemoveAt(0);
            StartCoroutine(Transition(fp.transform as RectTransform, exitPos, maxPacketSpeed, () =>
            {
                packetPool.Add(fp.gameObject);
                fp.gameObject.SetActive(false);
            }));
            for (int i = 0; i < profiles.Count; i++)
            {
                prof = profiles[i];
                Debug.Assert(prof.transition == null);
                prof.transition = StartCoroutine(Transition(profiles[i].packet.transform as RectTransform, anchoredPacketPositions[i], maxPacketSpeed, () =>
                {
                    var lambdaProf = profiles[i];
                    lambdaProf.transition = null;
                    profiles[i] = lambdaProf;
                }));
                profiles[i] = prof;
            }

        }
        prof = new PacketProfile(p, null, anchoredPacketPositions[profiles.Count]);
        var inx = profiles.Count;
        prof.transition = StartCoroutine(Transition(prt, anchoredPacketPositions[inx], maxPacketSpeed, () =>
        {
            var b = profiles[inx];
            b.transition = null;
            profiles[inx] = b;
        }));
        profiles.Add(prof);
    }
    #endregion
    List<Vector2> anchoredPacketPositions;
    //List<GameObject> showedPackets;
    List<GameObject> packetPool;
    List<PacketProfile> profiles;
    RectTransform rt;
    Vector2 enterPos, exitPos;

    void Start()
    {
        var prt = packetPrefab.transform as RectTransform;
        var parentrt = transform.parent as RectTransform;
        rt = transform as RectTransform;

        Vector2 initialPosition = new Vector2(-parentrt.rect.width + (rt.rect.width + prt.rect.width) * 0.5f + paddingLeft, 0f);
        var limit = -(prt.rect.width + rt.rect.width) * 0.5f - packetPadding;
        //Debug.DrawLine(Vector3.zero, rt.position + new Vector3(limit, 0f)*transform.root.localScale.x, Color.green, float.PositiveInfinity);
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
        exitPos = new Vector2(-parentrt.rect.width, 0f);
    }

    public IEnumerator Transition(RectTransform rectTransform, Vector2 targetPosition, float maxSpeed, System.Action onBreak = null)
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

    private void Awake()
    {
        signal = transform.GetChild(0).gameObject;
        profiles = new List<PacketProfile>();
        anchoredPacketPositions = new List<Vector2>();
        packetPool = new List<GameObject>();
    }
}
