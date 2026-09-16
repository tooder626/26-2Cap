using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;

public class GameManager : Singleton<GameManager>
{
    public static event Action<int> OnScoreChanged;

    public int score = 0;
    public int currentRound;
    public bool gameOver = false;
    public bool gamePaused = false;
    public enum CameraPosition
    {
        Left,
        Right,
        Mid
    }
    
    // 500점씩 볼의 생성 level의 범위가 늘어남 최대 6레벨 까지
    private int RoundCheck(int score)
    {
        int round = score / 500;

        if (round > 3)
        {
            return 3;
        }

        return round;
    }

    public void AddScore(int addedScore)
    {
        score += addedScore;

        currentRound = RoundCheck(score);

        OnScoreChanged?.Invoke(score);
    }
    public CameraPosition currentCamPos = CameraPosition.Mid;
    
    public List<GameObject> ballList = new List<GameObject>();
    public List<float> kgList = new List<float>() { 0.5f, 1.0f, 2.0f, 3.5f, 5.0f, 7.0f, 9.0f, 12.0f,13.0f,14.0f};


}
