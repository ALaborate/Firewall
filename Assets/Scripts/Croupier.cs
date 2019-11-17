using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Croupier : MonoBehaviour
{
    [Header("Packet creation")]
    public TextAsset vocabularyFile;
    public TextAsset goodHeadersFile;
    public TextAsset badHeadersFile;

    public float packetsPerSecond;
    public float creationPeriod = 0.05f;
    public float goodBadWordsScrumblePeriod = 20f;
    public float intensityAcceleration = 0.1f;
    public float intensityDecelerationFactor = 0.67f;
    public float screenCrossingAcceleration = 0.01f;
    public GameObject linePrefab;

    [Header("Bufer handling")]
    public float verticalPadding = 0;
    public float relativeVerticalPadding = 0.1f;
    public float initialScreenCrossingTime = 3f;
    public float screenCrossingTime
    {
        get { return initialScreenCrossingTime; }
        set
        {
            initialScreenCrossingTime = value;
            if (value > 0f)
                Packet.maxSpeed = rt.rect.width / initialScreenCrossingTime;
        }
    }

    [Header("Typing")]
    public InputField field;
    public AudioSource success, fail;

    RectTransform rt;
    Line[] lines;
    string[] gHeaders, vocabulary, bHeaders;
    private string[] ParseTextAsset(TextAsset textAsset)
    {
        bws.Clear();
        var ret = (from s in textAsset.text.Split('\n', '\r', ' ') where !string.IsNullOrEmpty(s) && bws.Add(s) select s).ToArray();
        bws.Clear();
        return ret;
    }
    void Start()
    {
        rt = transform as RectTransform;
        Debug.Assert(rt != null);
        var lrt = linePrefab.transform as RectTransform;
        var padding = (verticalPadding + relativeVerticalPadding * rt.rect.height);

        int linesCount = Mathf.FloorToInt((rt.rect.height - padding * 2) / lrt.rect.height);
        lines = new Line[linesCount];

        screenCrossingTime = screenCrossingTime;
        for (int i = 0; i < lines.Length; i++)
        {
            var l = Instantiate(linePrefab);
            lines[i] = l.GetComponent<Line>();
            lrt = l.transform as RectTransform;
            lrt.SetParent(transform);
            lrt.localScale = Vector3.one;
            var w = lrt.rect.width;
            var h = lrt.rect.height;
            lrt.anchorMin = Vector2.one;
            lrt.anchorMax = Vector2.one;
            lrt.anchoredPosition = new Vector2(-w * 0.5f, -padding - h * 0.5f - h * i);
            lines[i].OnPacketClear += OnPacketClear;
            lines[i].OnPacketDrop += OnPacketDrop;
        }
        vocabulary = ParseTextAsset(vocabularyFile);
        gHeaders = ParseTextAsset(goodHeadersFile);
        bHeaders = ParseTextAsset(badHeadersFile);
        gWords = new List<string>();
        bWords = new List<string>();
        ScrumbleWords();
        field.Select();
    }
    private void OnPacketClear(Packet p)
    {
        var data = p.data;
        if (gHeaders.Contains(data.header))
        {
            Accelerate();
        }
        else if (bHeaders.Contains(data.header))
        {
            Decelerate();
        }
        else Debug.LogError($"Packet header {data.header} is not contained");
    }
    private void OnPacketDrop(Packet p)
    {
        var data = p.data;
        if (gHeaders.Contains(data.header))
        {
            Decelerate();
        }
        else if (bHeaders.Contains(data.header))
        {
            //Accelerate(); //sic! no reaction to drop bad packets
        }
        else Debug.LogError($"Packet header {data.header} is not contained");
    }
    private void Accelerate()
    {
        packetsPerSecond += intensityAcceleration;
        success.Play();
    }
    private void Decelerate()
    {
        packetsPerSecond *= intensityDecelerationFactor;
        fail.Play();
    }

    private float nextCreationTime = 0f;
    private void CreatePackets()
    {
        if (Time.time < nextCreationTime)
            return;
        var freeLines = (from l in lines where l != null && !l.busy select l).ToList();
        if (freeLines.Count == 0)
        {
            screenCrossingTime -= screenCrossingAcceleration;
            return;
        }

        if (freeLines.Count >= lines.Length - 1)
            screenCrossingTime += screenCrossingAcceleration;

        var probability = packetsPerSecond * creationPeriod;
        nextCreationTime = Time.time + creationPeriod;
        if (Random.value <= probability)
        {
            var lineInx = Mathf.FloorToInt(Random.Range(0f, freeLines.Count));
            var headerInx = Mathf.FloorToInt(Random.Range(0f, gHeaders.Length + bHeaders.Length));

            bool goodPacket = headerInx < gHeaders.Length;
            var wordInx = Mathf.FloorToInt(Random.Range(0f, goodPacket ? gWords.Count : bWords.Count));
            string h = !goodPacket ? bHeaders[headerInx % bHeaders.Length] : gHeaders[headerInx];
            string w = goodPacket ? gWords[wordInx] : bWords[wordInx];
            freeLines[lineInx].CreatePacket(new Packet.Data(h, w, goodPacket));
        }
    }

    private List<string> gWords, bWords;
    HashSet<string> bws = new HashSet<string>(System.StringComparer.Ordinal);
    private float nextScrumbleTime = -1f;
    private void AddWord(string word, bool reportedlyGood)
    {
        if (reportedlyGood && !bws.Contains(word))
            gWords.Add(word);
        else if (!bws.Contains(word))
        {
            bWords.Add(word);
            bws.Add(word);
        }
    }
    private void ScrumbleWords()
    {
        if (Time.time < nextScrumbleTime)
            return;
        nextScrumbleTime = Time.time + goodBadWordsScrumblePeriod;
        float gRatio = gHeaders.Length / (float)(gHeaders.Length + bHeaders.Length);

        gWords.Clear();
        bWords.Clear();
        bws.Clear();
        foreach (var line in lines)
        {
            foreach (var pack in line.packets)
            {
                AddWord(pack.data.body, pack.data.good);
            }
        }

        foreach (var word in vocabulary)
        {
            AddWord(word, Random.value <= gRatio);
        }
    }

    void Update()
    {
        CreatePackets();

        ScrumbleWords();

        if (Input.anyKeyDown && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != field.gameObject)
            field.Select();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var s = field.text.Trim();
            var occurences = 0;
            foreach (var line in lines)
            {
                occurences += line.ClearPackets(s);
            }
            field.text = "";
            field.Select();
        }
    }
}
