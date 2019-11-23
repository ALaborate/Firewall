using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;

public class Croupier : MonoBehaviour
{
    [Header("Packet creation")]
    public TextAsset vocabularyFile;
    public GameObject linePrefab;
    public float creationPeriod = 0.05f;
    public List<DifficultyLevel> levels;
    public int levelIndex = 0;
    public Color goodPacketColor;
    public Color badPacketColor;

    [Header("Typing")]
    public InputField field;
    public GameObject helpPanel;

    [Header("Level challenges")]
    public Text levelText;
    public ErrorWeights errorWeights;

    [Header("Sound")]
    public AudioSource success;
    public AudioSource fail, levelup, leveldown, collission, challengeStarted, dryShot, victory;

    [Header("Bufer handling")]
    public float verticalPadding = 0;
    public float relativeVerticalPadding = 0.1f;

    RectTransform rt;
    Line[] lines;
    private List<string> gWords, bWords;

    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
    static readonly char[] textAssetDelimiters = new char[] { '\n', '\r' };
    const string levelFilename = "lvl.dat";
    const string vocabularyFileName = "words.txt";
    private void ReadSettingsFromFiles()
    {

        if (File.Exists(levelFilename))
        {
            using (var str = File.OpenRead(levelFilename))
            {
                var data = binaryFormatter.Deserialize(str);
                levelIndex = (int)data;
            }
        }

        if (File.Exists(vocabularyFileName))
        {
            ParseTextAsset(new TextAsset(File.ReadAllText(vocabularyFileName)), gWords, bWords);
        }
    }
    private void WriteSettingsToFiles()
    {
        if (!File.Exists(vocabularyFileName))
            File.WriteAllText(vocabularyFileName, vocabularyFile.text);
        using (var str = File.OpenWrite(levelFilename))
        {
            binaryFormatter.Serialize(str, levelIndex);
        }
    }
    const char delimiterInVocabulary = ' ';
    private static void ParseTextAsset(TextAsset textAsset, List<string> good, List<string> bad)
    {
        foreach (var line in textAsset.text.Split(textAssetDelimiters))
        {
            var parts = from l in line.Split(delimiterInVocabulary) where !string.IsNullOrEmpty(l) select l;
            if (parts.Count() >= 2)
            {
                good.Add(parts.ElementAt(1));
                bad.Add(parts.ElementAt(0));
            }
        }
    }
    void Start()
    {
        gWords = new List<string>();
        bWords = new List<string>();
        ParseTextAsset(vocabularyFile, gWords, bWords);
        ReadSettingsFromFiles();

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
        field.Select();
        helpPanel.SetActive(true);//TODO memorize setting to playerprefs
        longSounds = new AudioSource[] { levelup, leveldown, victory, challengeStarted };
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
                errorPoints += errorWeights.dropGood;
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
                errorPoints += errorWeights.clearBad;
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
    }

    private float nextCreationTime = 0f;
    private void CreatePackets()
    {
        if (Time.time < nextCreationTime || Packet.maxSpeed == 0f)
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
            var words = gWords;
            var color = goodPacketColor;
            if (!goodPacket)
            {
                words = bWords;
                color = badPacketColor;
            }
            var wordInx = Mathf.FloorToInt(Random.Range(0f, words.Count));
            string w = words[wordInx];
            freeLines[lineInx].CreatePacket(new Packet.Data(w, goodPacket, color));
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
    AudioSource[] longSounds;
    private void StopLongSounds()
    {
        foreach (var item in longSounds)
        {
            if (item.isPlaying)
            {
                item.Stop();
            }
        }
    }
    private void DecLevel()
    {
        StopLongSounds();
        if (levelIndex == 0)
        {
            collission.Play();
        }
        else
        {
            levelIndex--;
            StartCoroutine(PlayDecAfterFailure());
        }
        challengeEndTime = -1f;
    }

    private void UpdateLevelText()
    {
        levelText.text = $"Level: {levelIndex}{(challengeEndTime > 0f ? $" challenged. Time left: {Mathf.FloorToInt(challengeEndTime - Time.time):D2}. Errors: {errorPoints:F2}/{levels[levelIndex + 1].errorsPointsToFailure:F2}" : "")}";
    }

    float challengeEndTime = -1f;
    float errorPoints = 0;
    string last = "";
    float lastMaxSpeed = 1f;
    void Update()
    {
        if (Packet.maxSpeed != 0f && levels[levelIndex].screenCrossingTime > 0f)
            Packet.maxSpeed = rt.rect.width / levels[levelIndex].screenCrossingTime;
        if (challengeEndTime > 0f)
        {
            if (errorPoints >= levels[levelIndex + 1].errorsPointsToFailure)
            {
                DecLevel();
            }
            else if (challengeEndTime <= Time.time)
            {
                levelIndex++;
                levelup.Play();
                challengeEndTime = -1f;
            }
        }

        CreatePackets();


        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H))
        {
            helpPanel.SetActive(!helpPanel.activeSelf);
            if (!helpPanel.activeSelf)
            {
                field.Select();
            }
            else
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
        }

        if (Packet.maxSpeed != 0f)
        {
            UpdateLevelText();
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (challengeEndTime < 0f)
                    {
                        if (levelIndex >= levels.Count - 1)
                        {
                            victory.Play();
                        }
                        else
                        {
                            StopLongSounds();
                            challengeStarted.Play();
                            challengeEndTime = Time.time + levels[levelIndex + 1].challengeTime;
                            errorPoints = 0f;
                        }
                    }
                    else
                    {
                        DecLevel();
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Z))
                {
                    field.text = last;
                    field.Select();
                    field.caretPosition = last.Length;
                    field.selectionAnchorPosition = field.caretPosition;
                }
            }
            else if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    last = field.text.Trim();
                    field.text = "";
                    field.Select();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                if (string.IsNullOrEmpty(field.text))
                    return;
                var s = field.text.Trim();
                if (string.IsNullOrEmpty(s))
                {
                    field.text = "";
                    return;
                }
                last = s;
                var occurences = 0;
                foreach (var line in lines)
                {
                    occurences += line.ClearPackets(s);
                }
                if (occurences == 0)
                {
                    errorPoints += errorWeights.dryShot;
                    dryShot.Play();
                }
                field.text = "";
                field.Select();
            }
        }
    }

    private void ResumeTime()
    {
        Packet.maxSpeed = lastMaxSpeed;
        field.Select();
    }

    private void StopTime()
    {
        lastMaxSpeed = Packet.maxSpeed;
        Packet.maxSpeed = 0f;
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnDestroy()
    {
        WriteSettingsToFiles();
    }
    [System.Serializable]
    public struct DifficultyLevel
    {
        public float creationIntensity;
        public float screenCrossingTime;
        public float goodPacketsRatio;
        public float challengeTime;
        public float errorsPointsToFailure;
        public DifficultyLevel(float _creationIntensity = 2f, float _screeenCrossingTime = 6f, float _goodPacketsRatio = 0.5f, float _errorsToFailure = 1, float _challengeTime = 60f)
        {
            creationIntensity = _creationIntensity;
            screenCrossingTime = _screeenCrossingTime;
            goodPacketsRatio = _goodPacketsRatio;
            errorsPointsToFailure = _errorsToFailure;
            challengeTime = _challengeTime;
        }
    }
    [System.Serializable]
    public struct ErrorWeights
    {
        public float dropGood;
        public float clearBad;
        public float dryShot;
        public ErrorWeights(float dropGood, float clearBad, float dryShot)
        {
            this.dropGood = dropGood;
            this.clearBad = clearBad;
            this.dryShot = dryShot;
        }
    }
}
