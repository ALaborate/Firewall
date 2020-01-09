using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Croupier : MonoBehaviour
{
    [Header("Packet creation")]
    public TextAsset vocabularyFile;
    public GameObject linePrefab;
    public float creationPeriod = 0.05f;
    public DifficultyLevel difficultyLevel;
    public Color goodPacketColor;

    [Header("Events")]
    public UnityEvent tacticalSuccess;
    public UnityEvent tacticalFail, noMatchError;
    public UnityEvent<Packet, Packet.DeathCause> OnPacketDeath;


    [Header("Typing")]
    public InputField field;
    public GameObject helpPanel;

    //[Header("Sound")]
    //public AudioSource success;
    //public AudioSource fail, levelup, leveldown, collission, challengeStarted, dryShot, victory;

    [Header("Bufer handling")]
    public float verticalPadding = 0;
    public float relativeVerticalPadding = 0.1f;

    RectTransform rt;
    Line[] lines;
    private List<string> gWords, bWords;

    static readonly char[] textAssetDelimiters = new char[] { '\n', '\r' };
    const string vocabularyFileName = "words.txt";
    const char delimiterInVocabulary = ' ';
    private static void ParseTextAsset(TextAsset textAsset, List<string> good, List<string> bad)
    {
        //TODO ensure many to one types of bounds in text file.
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
        //TODO streaming assets
        ParseTextAsset(vocabularyFile, gWords, bWords);

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
            lines[i].OnPacketDeath += OnPacketDeathInternal;
        }
        field.Select();
        helpPanel.SetActive(true);//TODO memorize setting to playerprefs
    }
    private void OnPacketDeathInternal(Packet p, Packet.DeathCause cause)
    {
        if (cause == Packet.DeathCause.Internal)
            return;
        OnPacketDeath.Invoke(p, cause);

        var data = p.data;
        if (cause == Packet.DeathCause.Drop)
        {
            if (data.good)
            {
                tacticalFail.Invoke();
            }
        }
        else if (cause == Packet.DeathCause.Clear)
        {
            if (data.good)
            {
                tacticalSuccess.Invoke();
            }
            else //bad
            {
                tacticalFail.Invoke();
            }
        }
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

        var probability = difficultyLevel.creationIntensity * creationPeriod;
        nextCreationTime = Time.time + creationPeriod;
        if (Random.value <= probability)
        {
            var lineInx = Mathf.FloorToInt(Random.Range(0f, freeLines.Count - 0.1f));
            bool goodPacket = Random.value < difficultyLevel.goodPacketsRatio;
            var words = gWords;
            var color = goodPacketColor;
            if (!goodPacket)
            {
                words = bWords;
                for (int i = 0; i < 3; i++)
                {
                    color[i] *= difficultyLevel.badColorCoef;
                }
            }
            var wordInx = Mathf.FloorToInt(Random.Range(0f, words.Count));
            string w = words[wordInx];
            freeLines[lineInx].CreatePacket(new Packet.Data(w, goodPacket, color));
        }
    }



    string last = "";
    float lastMaxSpeed = 1f;
    void Update()
    {
        if (Packet.maxSpeed != 0f && difficultyLevel.screenCrossingTime > 0f)
            Packet.maxSpeed = rt.rect.width / difficultyLevel.screenCrossingTime;

        CreatePackets();


        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H))
        {
            helpPanel.SetActive(!helpPanel.activeSelf);
            if (!helpPanel.activeSelf)
            {
                field.Select();
                ResumeTime();
            }
            else
            {
                StopTime();
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
        }

        if (Packet.maxSpeed != 0f)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    field.text = last;
                    field.Select();
                    field.caretPosition = last.Length;
                    field.selectionAnchorPosition = field.caretPosition;
                }
            }

            if (Input.GetKey(KeyCode.LeftAlt))
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
                    noMatchError.Invoke();
                }
                field.text = "";
                field.Select();
            }
        }
    }

    public void ResumeTime()
    {
        Packet.maxSpeed = lastMaxSpeed;
        field.Select();
    }
    //TODO make those public. Add victory method

    public void StopTime()
    {
        lastMaxSpeed = Packet.maxSpeed;
        Packet.maxSpeed = 0f;
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    [System.Serializable]
    public struct DifficultyLevel
    {
        public float creationIntensity;
        public float screenCrossingTime;
        public float goodPacketsRatio;
        public float errorsPointsToFailure;
        public float badColorCoef;
        public DifficultyLevel(float _creationIntensity = 2f, float _screeenCrossingTime = 6f, float _goodPacketsRatio = 0.5f, float _errorsToFailure = 1, float _badColorCoef = 0.98f)
        {
            creationIntensity = _creationIntensity;
            screenCrossingTime = _screeenCrossingTime;
            goodPacketsRatio = _goodPacketsRatio;
            errorsPointsToFailure = _errorsToFailure;
            badColorCoef = _badColorCoef;
        }
    }
}
