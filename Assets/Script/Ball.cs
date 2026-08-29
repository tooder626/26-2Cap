using UnityEngine;

public class Ball
{
    public int Level { get; private set; }
    public float Kg { get; private set; }
    public Vector2 Pos { get; private set; }

    public float speed = 1.0f;
    // 생성자에 level 추가
    public Ball(int level, float kg, Vector2 pos)
    {
        Level = level;
        Kg = kg;
        Pos = pos;
    }
}