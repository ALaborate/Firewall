using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Difficulter : MonoBehaviour
{

    [Header("Level challenges")]
    public Text levelText;
    public ErrorWeights errorWeights;
    public List<Croupier.DifficultyLevel> levels;
    public int levelIndex = 0;
    public float challengeLength = 60f;

    public UnityEvent levelup, leveldown, levelFloor, levelMax, challengeStart;
    private void Start()
    {
        //TODO subscribe to croupier packet death event
    }
    private void DecLevel()
    {
        //StopLongSounds();
        if (levelIndex == 0)
        {
            //collission.Play();
            levelFloor.Invoke();
        }
        else
        {
            levelIndex--;
            leveldown.Invoke();
            //StartCoroutine(PlayDecAfterFailure());
        }
        challengeEndTime = -1f;
    }
    private void UpdateLevelText()
    {
        levelText.text = $"Level: {levelIndex}{(challengeEndTime > 0f ? $" challenged. Time left: {Mathf.FloorToInt(challengeEndTime - Time.time):D2}. Errors: {errorPoints:F2}/{levels[levelIndex + 1].errorsPointsToFailure:F2}" : "")}";
    }

    float challengeEndTime = -1f;
    float errorPoints = 0;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (challengeEndTime < 0f)
                {
                    if (levelIndex >= levels.Count - 1)
                    {
                        //victory.Play();
                        levelMax.Invoke();
                    }
                    else
                    {
                        challengeEndTime = Time.time + challengeLength;
                        errorPoints = 0f;
                        challengeStart.Invoke();
                    }
                }
                else
                {
                    DecLevel();
                }
            } 
        }

        if (challengeEndTime > 0f)
        {
            if (errorPoints >= levels[levelIndex + 1].errorsPointsToFailure)
            {
                DecLevel();
            }
            else if (challengeEndTime <= Time.time)
            {
                levelIndex++;
                levelup.Invoke();
                challengeEndTime = -1f;
            }
        }





        UpdateLevelText();
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
    //private IEnumerator PlayDecAfterFailure()
    //{
    //    while (true)
    //    {
    //        if (fail.isPlaying)
    //            yield return null;
    //        else break;
    //    }
    //    leveldown.Play();
    //    yield break;
    //}
    //AudioSource[] longSounds;
    //private void StopLongSounds()
    //{
    //    foreach (var item in longSounds)
    //    {
    //        if (item.isPlaying)
    //        {
    //            item.Stop();
    //        }
    //    }
    //}
}
