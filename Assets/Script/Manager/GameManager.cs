using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : Singleton<GameManager>
{
    public static event Action<int> OnScoreChanged;
    public int score { get; private set; } = 0;
    public int currentRound { get; private set; }
    public bool gameOver { get; private set; } = false;

    public bool gamePaused { get; set; } = false;
    public enum CameraPosition { Left, Right, Mid }
    public CameraPosition currentCamPos { get; set; } = CameraPosition.Mid;

    [field: SerializeField]
    public List<GameObject> ballList { get; private set; } = new List<GameObject>();

    [field: SerializeField]
    public List<float> kgList { get; private set; } = new List<float>() { 0.5f, 1.0f, 2.0f, 3.5f, 5.0f, 7.0f, 9.0f, 12.0f, 13.0f, 14.0f };

    private void Start()
    {
        SaveManager.Inst.LoadGame();
    }

    private void OnApplicationQuit()
    {
        SaveManager.Inst.SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveManager.Inst.SaveGame();
        }
    }

    public void TriggerGameOver()
    {
        if (gameOver) return;

        gameOver = true;
        SaveManager.Inst.DeleteSave();
    }

    private int RoundCheck(int currentScore)
    {
        int round = currentScore / 500;
        if (round > 3) return 3;
        return round;
    }



    public void AddScore(int addedScore)
    {
        score += addedScore;
        currentRound = RoundCheck(score);
        OnScoreChanged?.Invoke(score);
    }

    public void SetScoreFromLoad(int savedScore)
    {
        score = savedScore;
        currentRound = RoundCheck(score);
        OnScoreChanged?.Invoke(score);

    }
}