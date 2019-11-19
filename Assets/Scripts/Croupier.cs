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
    public GameObject linePrefab;
    public float creationPeriod = 0.05f;
    public float goodBadWordsScrumblePeriod = 20f;
    public List<DifficultyLevel> levels;
    public int levelIndex = 0;

    [Header("Bufer handling")]
    public float verticalPadding = 0;
    public float relativeVerticalPadding = 0.1f;

    [Header("Typing")]
    public InputField field;


    [Header("Level challenges")]
    public Text levelText;

    [Header("Sound")]
    public AudioSource success, fail, levelup, leveldown, collission, challengeStarted;

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
            lines[i].OnPacketDeath += OnPacketDeath;
        }
        vocabulary = ParseTextAsset(vocabularyFile);
        gHeaders = ParseTextAsset(goodHeadersFile);
        bHeaders = ParseTextAsset(badHeadersFile);
        gWords = new List<string>();
        bWords = new List<string>();
        ScrumbleWords();
        field.Select();
        levelText.text = $"Level: {levelIndex}";
    }
    private void OnPacketDeath(Packet p, Packet.DeathCause cause)
    {
        if (cause == Packet.DeathCause.Internal)
            return;
        var data = p.data;
        if (cause == Packet.DeathCause.Drop)
        {
            if (data.good)
            {
                Decelerate();
            }
        }
        else if (cause == Packet.DeathCause.Clear)
        {
            if (data.good)
            {
                Accelerate();
            }
            else //bad
            {
                Decelerate();
            }
        }
    }
    private void Accelerate()
    {
        success.Play();
    }
    private void Decelerate()
    {
        fail.Play();
        errorCount++;
    }

    private float nextCreationTime = 0f;
    private void CreatePackets()
    {
        if (Time.time < nextCreationTime)
            return;
        var freeLines = (from l in lines where l != null && !l.busy select l).ToList();
        if (freeLines.Count == 0)
        {
            return;
        }

        var probability = levels[levelIndex].creationIntensity * creationPeriod;
        nextCreationTime = Time.time + creationPeriod;
        if (Random.value <= probability)
        {
            var lineInx = Mathf.FloorToInt(Random.Range(0f, freeLines.Count - 0.1f));
            bool goodPacket = Random.value < levels[levelIndex].goodPacketsRatio;
            var headers = gHeaders;
            var words = gWords;
            if (!goodPacket)
            {
                headers = bHeaders;
                words = bWords;
            }

            var headerInx = Mathf.FloorToInt(Random.Range(0f, headers.Length));
            var wordInx = Mathf.FloorToInt(Random.Range(0f, words.Count));
            string h = headers[headerInx];
            string w = words[wordInx];
            freeLines[lineInx].CreatePacket(new Packet.Data(h, w, goodPacket));
        }
    }

    private List<string> gWords, bWords;
    HashSet<string> bws = new HashSet<string>(System.StringComparer.Ordinal), gws = new HashSet<string>(System.StringComparer.Ordinal);
    private float nextScrumbleTime = -1f;
    private void AddWord(string word, bool reportedlyGood)
    {
        if (reportedlyGood)
        {
            gWords.Add(word);
            gws.Add(word);
        }
        else
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
        gws.Clear();
        foreach (var line in lines)
        {
            foreach (var pack in line.packets)
            {
                AddWord(pack.data.body, pack.data.good);
            }
        }

        foreach (var word in vocabulary)
        {
            bool good = gws.Contains(word);
            bool bad = bws.Contains(word);
            if (good && bad)
            {
                Debug.LogError("Word is good and bad simultaneously");
                continue;
            }

            AddWord(word, good || (!bad && Random.value < gRatio));
        }
    }
    private IEnumerator PlayDecAfterFailure()
    {
        while (true)
        {
            if (fail.isPlaying)
                yield return null;
            else break;
        }
        leveldown.Play();
        yield break;
    }
    private void DecLevel()
    {
        if (levelIndex == 0)
        {
            collission.Play();
        }
        else
        {
            levelIndex--;
            StartCoroutine(PlayDecAfterFailure());
        }
        levelText.text = $"Level: {levelIndex}";
    }

    float challengeEndTime = -1f;
    int errorCount = 0;
    void Update()
    {
        if (levels[levelIndex].screenCrossingTime > 0f)
            Packet.maxSpeed = rt.rect.width / levels[levelIndex].screenCrossingTime;
        if (challengeEndTime > 0f)
        {
            if (errorCount >= levels[levelIndex + 1].errorsToFailure)
            {
                DecLevel();
                challengeEndTime = -1f;
            }
            else if (challengeEndTime <= Time.time)
            {
                levelIndex++;
                levelup.Play();
                challengeEndTime = -1f;
                levelText.text = $"Level: {levelIndex}";
            }
        }

        CreatePackets();

        ScrumbleWords();

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (challengeEndTime < 0f)
                {
                    if (levelIndex >= levels.Count - 1)
                    {
                        collission.Play();
                    }
                    else
                    {
                        challengeStarted.Play();
                        challengeEndTime = Time.time + levels[levelIndex + 1].challengeTime;
                        levelText.text = $"Level: {levelIndex} challenged";
                        errorCount = 0;
                    }
                }
                else
                {
                    DecLevel();
                }
            }
        }

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
    [System.Serializable]
    public struct DifficultyLevel
    {
        public float creationIntensity;
        public float screenCrossingTime;
        public float goodPacketsRatio;
        public float challengeTime;
        public int errorsToFailure;
        public DifficultyLevel(float _creationIntensity = 2f, float _screeenCrossingTime = 6f, float _goodPacketsRatio = 0.5f, int _errorsToFailure = 1, float _challengeTime = 60f)
        {
            creationIntensity = _creationIntensity;
            screenCrossingTime = _screeenCrossingTime;
            goodPacketsRatio = _goodPacketsRatio;
            errorsToFailure = _errorsToFailure;
            challengeTime = _challengeTime;
        }
    }
}
