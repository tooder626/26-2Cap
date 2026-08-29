using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public float xLimit = 12f;


    public int score = 0;
    public bool gameOver = false;
    public bool gamePaused = false;
    public enum CameraPosition
    {
        Left,
        Right,
        Mid
    }

    public CameraPosition currentCamPos = CameraPosition.Mid;

    public List<GameObject> ballList = new List<GameObject>();
    public List<float> kgList = new List<float>() { 0.5f, 1.0f, 2.0f, 3.5f, 5.0f, 7.0f, 9.0f, 12.0f,13.0f,14.0f};


}
