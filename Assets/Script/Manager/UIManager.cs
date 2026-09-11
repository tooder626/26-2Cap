using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI angleText;
    [SerializeField] private Image nextBall;

    [SerializeField] private GameObject btnGoLeft;
    [SerializeField] private GameObject btnGoRight;
    [SerializeField] private GameObject btnGoMain;

    private void OnEnable()
    {
        GameManager.OnScoreChanged += UpdateScoreUI;
        SpawnManager.OnNextBallChanged += UpdateNextBallUI;
    }

    private void OnDisable()
    {
        GameManager.OnScoreChanged -= UpdateScoreUI;
        SpawnManager.OnNextBallChanged -= UpdateNextBallUI;
    }

    private void Start()
    {
        // 시작 시 현재 점수 표시
        UpdateScoreUI(GameManager.Inst.score);

        // 시작 시 다음 공 표시
        if (SpawnManager.Inst.nextBallQueue.Count > 0)
        {
            UpdateNextBallUI(SpawnManager.Inst.getNextBall());
        }

        // 카메라 버튼 상태 초기화
        UpdateCameraUI();
    }

    private void FixedUpdate()
    {
        if (GameManager.Inst.gameOver)
            return;

        PrintAngle();
    }

    private void UpdateScoreUI(int currentScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score : {currentScore:D4}";
        }
    }

    private void UpdateNextBallUI(int nextLevel)
    {
        if (nextBall == null)
            return;

        if (nextLevel >= 0 &&
            nextLevel < GameManager.Inst.ballList.Count)
        {
            Sprite nextSprite =
                GameManager.Inst.ballList[nextLevel]
                .GetComponent<SpriteRenderer>()
                .sprite;

            nextBall.sprite = nextSprite;
            nextBall.color = Color.white;
        }
    }

    private void PrintAngle()
    {
        Transform target = SpawnManager.Inst.GetCurrentTargetBag();

        if (target != null && angleText != null)
        {
            float currentAngle = target.eulerAngles.z;

            if (currentAngle > 180f)
                currentAngle -= 360f;

            float tiltMagnitude = Mathf.Abs(currentAngle);

            angleText.text = $"{currentAngle:F0}%";

            if (tiltMagnitude > 20f)
            {
                angleText.color = Color.red;
            }
            else if (tiltMagnitude > 10f)
            {
                angleText.color = new Color(1f, 0.5f, 0f);
            }
            else
            {
                angleText.color = Color.black;
            }
        }
        else if (angleText != null)
        {
            angleText.text = "-";
            angleText.color = Color.black;
        }
    }

    public void UpdateCameraUI()
    {
        switch (GameManager.Inst.currentCamPos)
        {
            case GameManager.CameraPosition.Left:

                btnGoLeft.SetActive(false);
                btnGoRight.SetActive(true);
                btnGoMain.SetActive(true);

                break;

            case GameManager.CameraPosition.Right:

                btnGoLeft.SetActive(true);
                btnGoRight.SetActive(false);
                btnGoMain.SetActive(true);

                break;

            case GameManager.CameraPosition.Mid:

                btnGoLeft.SetActive(true);
                btnGoRight.SetActive(true);
                btnGoMain.SetActive(false);

                break;
        }
    }
}