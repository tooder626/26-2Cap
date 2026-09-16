using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpawnManager : Singleton<SpawnManager>
{
    public static event Action<Transform> OnAimStart;
    public static event Action OnAimEnd;
    public static event Action<int> OnNextBallChanged;

    public Queue<int> nextBallQueue = new Queue<int>();

    [SerializeField] private Transform leftParent;
    [SerializeField] private Transform rightParent;
    [SerializeField] private AudioClip mergeClip;

    [Header("스폰 위치 제한")]
    [SerializeField] private Transform leftBag_LeftPoint;
    [SerializeField] private Transform leftBag_RightPoint;
    [SerializeField] private Transform rightBag_LeftPoint;
    [SerializeField] private Transform rightBag_RightPoint;

    [Header("스폰 레벨 설정")]
    [SerializeField] private int minSpawnLevel = 0;
    [SerializeField] private int baseMaxSpawnLevel = 3;


    [SerializeField] private float spawnXOffset = 0.5f;

    public Camera currentCamera;
    public bool canSpawn = true;

    private int lastMergeFrame = -1;
    private GameObject previewBall;
    private int currentLevel;
    private int forcedNextLevel = -1;

    private int CurrentMaxSpawnLevel => baseMaxSpawnLevel + GameManager.Inst.currentRound;

    private void Start()
    {
        if (currentCamera == null)
            currentCamera = Camera.main;

        if (nextBallQueue.Count == 0)
        {
            nextBallQueue.Enqueue(
                UnityEngine.Random.Range(minSpawnLevel, CurrentMaxSpawnLevel + 1)
            );
        }

        OnNextBallChanged?.Invoke(getNextBall());
    }

    private void Update()
    {
        if (GameManager.Inst.gameOver || !canSpawn)
            return;

        Transform targetBag = GetCurrentTargetBag();

        if (targetBag == null)
            return;

        bool isLeftBag = targetBag == leftParent;

        // 마우스 클릭 시작
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null)
            {
                if (Input.touchCount > 0)
                {
                    if (EventSystem.current.IsPointerOverGameObject(
                        Input.GetTouch(0).fingerId))
                    {
                        return;
                    }
                }
                else if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
            }

            Vector2 spawnPos;

            if (CalculateSpawnPosition(
                targetBag,
                isLeftBag,
                true,
                out spawnPos))
            {
                CreatePreviewBall(spawnPos);
            }
        }

        // 마우스 드래그
        else if (Input.GetMouseButton(0))
        {
            if (previewBall != null)
            {
                Vector2 dragPos;

                if (CalculateSpawnPosition(
                    targetBag,
                    isLeftBag,
                    false,
                    out dragPos))
                {
                    previewBall.transform.position = dragPos;
                }
            }
        }

        // 마우스 떼기
        else if (Input.GetMouseButtonUp(0))
        {
            if (previewBall != null)
            {
                DropPreviewBall();
            }
        }
    }

    private bool CalculateSpawnPosition(
        Transform targetBag,
        bool isLeftBag,
        bool isInitialClick,
        out Vector2 pos)
    {
        pos = Vector2.zero;

        Transform leftPoint = isLeftBag
            ? leftBag_LeftPoint
            : rightBag_LeftPoint;

        Transform rightPoint = isLeftBag
            ? leftBag_RightPoint
            : rightBag_RightPoint;

        if (leftPoint == null || rightPoint == null)
            return false;

        // 마우스의 월드 좌표
        Vector3 mouseWorldPos =
            currentCamera.ScreenToWorldPoint(Input.mousePosition);

        float mouseX = mouseWorldPos.x;

        // 실제 Point의 월드 X
        float leftX = leftPoint.position.x;
        float rightX = rightPoint.position.x;

        float minX = Mathf.Min(leftX, rightX) + spawnXOffset;
        float maxX = Mathf.Max(leftX, rightX) - spawnXOffset;

        // 최초 클릭 시 범위 밖이면 생성하지 않음
        if (isInitialClick &&
            (mouseX < minX || mouseX > maxX))
        {
            return false;
        }

        // 마우스 X를 컵의 범위 안으로 제한
        float spawnX = Mathf.Clamp(
            mouseX,
            minX,
            maxX
        );

        // 공 생성 Y
        float spawnY = targetBag.position.y + 12f;

        Renderer[] renderers =
            targetBag.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            float highestY = float.MinValue;

            foreach (Renderer renderer in renderers)
            {
                if (renderer.bounds.max.y > highestY)
                {
                    highestY = renderer.bounds.max.y;
                }
            }

            spawnY = highestY + 2f;
        }

        pos = new Vector2(
            spawnX,
            spawnY
        );

        return true;
    }
    private void CreatePreviewBall(Vector2 spawnPos)
    {
        currentLevel =
            (forcedNextLevel != -1)
            ? forcedNextLevel
            : nextBallQueue.Dequeue();

        forcedNextLevel = -1;

        if (nextBallQueue.Count == 0)
        {
            nextBallQueue.Enqueue(
                UnityEngine.Random.Range(
                    minSpawnLevel,
                    CurrentMaxSpawnLevel + 1
                )
            );
        }

        OnNextBallChanged?.Invoke(getNextBall());

        previewBall = Instantiate(
            GameManager.Inst.ballList[currentLevel],
            spawnPos,
            Quaternion.identity
        );

        Rigidbody2D rb =
            previewBall.GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.isKinematic = true;

        Collider2D col =
            previewBall.GetComponent<Collider2D>();

        if (col != null)
            col.enabled = false;

        OnAimStart?.Invoke(previewBall.transform);
    }

    private void DropPreviewBall()
    {
        canSpawn = false;

        SetBallParent(previewBall);

        OnAimEnd?.Invoke();

        SetupBallProperties(
            previewBall,
            currentLevel,
            true
        );

        previewBall = null;
    }

    public void SpawnMergedBall(int level, Vector2 pos)
    {
        GameObject newCircle =
            Instantiate(
                GameManager.Inst.ballList[level],
                pos,
                Quaternion.identity
            );

        SetupBallProperties(
            newCircle,
            level,
            false
        );

        if (mergeClip != null &&
            lastMergeFrame != Time.frameCount)
        {
            AudioSource.PlayClipAtPoint(
                mergeClip,
                currentCamera.transform.position
            );

            lastMergeFrame = Time.frameCount;
        }
    }

    private void SetupBallProperties(
        GameObject ball,
        int level,
        bool isDroppedByPlayer)
    {
        Rigidbody2D rb =
            ball.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.mass = GameManager.Inst.kgList[level];
        }

        Collider2D col =
            ball.GetComponent<Collider2D>();

        if (col != null)
            col.enabled = true;

        BallBehaviour bb =
            ball.GetComponent<BallBehaviour>();

        if (bb == null)
            bb = ball.AddComponent<BallBehaviour>();

        bb.level = level;
        bb.isDroppedByPlayer = isDroppedByPlayer;

        ball.name =
            string.Format(
                "Circle (Level: {0})",
                level
            );

        SetBallParent(ball);
    }

    private void SetBallParent(GameObject ball)
    {
        Transform bottleTransform =
            leftParent.parent;

        Vector3 localPos =
            bottleTransform.InverseTransformPoint(
                ball.transform.position
            );

        if (localPos.x < 0)
            ball.transform.SetParent(leftParent);
        else
            ball.transform.SetParent(rightParent);
    }

    public Transform GetCurrentTargetBag()
    {
        switch (GameManager.Inst.currentCamPos)
        {
            case GameManager.CameraPosition.Left:
                return leftParent;

            case GameManager.CameraPosition.Right:
                return rightParent;

            default:
                return null;
        }
    }

    public void OnBallLanded()
    {
        canSpawn = true;
    }

    public void OnBallHitGroundAndRetry(
        int level,
        GameObject ball)
    {
        forcedNextLevel = level;

        canSpawn = true;

        Destroy(ball);

        OnNextBallChanged?.Invoke(
            getNextBall()
        );
    }

    // UIManager에서 사용하는 함수
    public int getNextBall()
    {
        if (forcedNextLevel != -1)
            return forcedNextLevel;

        if (nextBallQueue.Count > 0)
            return nextBallQueue.Peek();

        return -1;
    }
}