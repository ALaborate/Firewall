﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Croupier : MonoBehaviour
{
    [Header("Packet creation")]
    public TextAsset vocabularyFile;
    public TextAsset headersFile;
    public float packetsPerSecond;
    public float creationPeriod = 0.05f;
    public GameObject linePrefab;

    [Header("Bufer handling")]
    public float verticalPadding = 0;
    public float relativeVerticalPadding = 0.1f;
    public float screenCrossingTime = 3f;


    RectTransform rt;
    Line[] lines;
    string[] headers, vocabulary;

    void Start()
    {
        rt = transform as RectTransform;
        Debug.Assert(rt != null);
        var lrt = linePrefab.transform as RectTransform;
        var padding = (verticalPadding + relativeVerticalPadding * rt.rect.height);

        int linesCount = Mathf.FloorToInt((rt.rect.height - padding * 2) / lrt.rect.height);
        lines = new Line[linesCount];

        Packet.maxSpeed = rt.rect.width / screenCrossingTime;
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
            lines[i].showSignal = false;
        }
        vocabulary = (from s in vocabularyFile.text.Split('\n', '\r', ' ') where !string.IsNullOrEmpty(s) select s).ToArray();
        headers = (from s in headersFile.text.Split('\n', '\r', ' ') where !string.IsNullOrEmpty(s) select s).ToArray();
    }

    private float nextCreationTime = 0f;
    void Update()
    {
        if (Time.time < nextCreationTime)
            return;
        var freeLines = (from l in lines where l != null && !l.busy select l).ToList();
        if (freeLines.Count == 0) return;

        var probability = packetsPerSecond * creationPeriod;
        nextCreationTime = Time.time + creationPeriod;
        if (Random.value <= probability)
        {
            var lineInx = Mathf.FloorToInt(Random.Range(0f, freeLines.Count));
            var headerInx = Mathf.FloorToInt(Random.Range(0f, headers.Length));
            var wordInx = Mathf.FloorToInt(Random.Range(0f, vocabulary.Length));

            freeLines[lineInx].CreatePacket(new Packet.Data(headers[headerInx], vocabulary[wordInx]));
        }
    }
}
