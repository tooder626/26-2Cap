using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI angleText;
    [SerializeField] private Image nextBall;

    public void FixedUpdate()
    {
        if (GameManager.Inst.gameOver) return;

        printScore();
        printAngle();
        printImage();
    }

    private void printScore()
    {
        int currentScore = GameManager.Inst.score;

        if (scoreText != null)
        {
            scoreText.text = $"Score : {currentScore:D4}";
        }
    }

    private void printAngle()
    {
        Transform target = SpawnManager.Inst.GetCurrentTargetBag();

        if (target != null && angleText != null)
        {
            float currentAngle = target.eulerAngles.z;
            if (currentAngle > 180f) currentAngle -= 360f;

            // 기울기의 절대값(크기)을 구합니다.
            float tiltMagnitude = Mathf.Abs(currentAngle);

            // 텍스트는 원래 각도(+값, -값)를 그대로 표시합니다.
            angleText.text = $"{currentAngle:F0}%";

            if (tiltMagnitude > 20)
            {
                angleText.color = Color.red;
            }
            else if (tiltMagnitude > 10)
            {
                // 주황색
                angleText.color = new Color(1f, 0.5f, 0f);
            }
            else
            {
                angleText.color = Color.black;
            }
        }
    }

    public void printImage()
    {
        int nextcurrent = SpawnManager.Inst.getNextBall();

        // 큐에서 가져온 값이 유효한지(0 이상이고, ballList 크기보다 작은지) 안전하게 확인
        if (nextcurrent >= 0 && nextcurrent < GameManager.Inst.ballList.Count)
        {
            // 1. 프리팹(게임 오브젝트)에서 SpriteRenderer를 찾아서 이미지(sprite)만 추출합니다.
            Sprite nextSprite = GameManager.Inst.ballList[nextcurrent].GetComponent<SpriteRenderer>().sprite;

            // 2. 추출한 이미지를 UI의 Image 컴포넌트에 덮어씌웁니다.
            nextBall.sprite = nextSprite;   
            nextBall.color = Color.white;

        }
    }
}