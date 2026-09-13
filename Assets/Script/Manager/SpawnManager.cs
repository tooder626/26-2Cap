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

    public Camera currentCamera;
    public bool canSpawn = true;

    private int lastMergeFrame = -1;
    private GameObject previewBall;
    private int currentLevel;

    private int forcedNextLevel = -1;

    private void Start()
    {
        if (currentCamera == null)
            currentCamera = Camera.main;

        if (nextBallQueue.Count == 0)
            nextBallQueue.Enqueue(UnityEngine.Random.Range(0, 3));

        OnNextBallChanged?.Invoke(getNextBall());
    }

    private void Update()
    {
        if (GameManager.Inst.gameOver || !canSpawn)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null)
            {
                if (Input.touchCount > 0)
                {
                    if (EventSystem.current.IsPointerOverGameObject(
                        Input.GetTouch(0).fingerId))
                        return;
                }
                else if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
            }

            Vector2 spawnPos;

            if (TryGetValidSpawnPosition(out spawnPos, true))
            {
                CreatePreviewBall(spawnPos);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (previewBall != null)
            {
                Vector2 dragPos;

                if (TryGetValidSpawnPosition(out dragPos, false))
                {
                    previewBall.transform.position = dragPos;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (previewBall != null)
            {
                DropPreviewBall();
            }
        }
    }

    private bool TryGetValidSpawnPosition(out Vector2 pos, bool isInitialClick)
    {
        pos = Vector2.zero;

        Transform targetBag = GetCurrentTargetBag();

        if (targetBag == null)
            return false;

        Vector3 worldMousePos =
            currentCamera.ScreenToWorldPoint(Input.mousePosition);

        Vector3 localMousePos =
            targetBag.InverseTransformPoint(worldMousePos);

        float angle = targetBag.eulerAngles.z;

        if (angle > 180f)
            angle -= 360f;

        // 기울기에 따라 안전한 X 범위 계산
        float safeXLimit = GameManager.Inst.xLimit;

        safeXLimit -=
            Mathf.Max(0f, Mathf.Abs(angle) - 10f) * 0.15f;

        safeXLimit = Mathf.Max(safeXLimit, 5f);

        // 처음 클릭한 위치가 범위를 벗어나면 생성하지 않음
        if (isInitialClick &&
            Mathf.Abs(localMousePos.x) > safeXLimit)
        {
            return false;
        }

        // X는 마우스 위치 사용
        localMousePos.x = Mathf.Clamp(
            localMousePos.x,
            -safeXLimit,
            safeXLimit
        );

        // --------------------------------
        // 바구니 자식들의 가장 높은 위치 찾기
        // --------------------------------

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

            // 바구니의 가장 높은 위치를 로컬 좌표로 변환
            Vector3 topWorldPos = new Vector3(
                targetBag.position.x,
                highestY,
                targetBag.position.z
            );

            Vector3 topLocalPos =
                targetBag.InverseTransformPoint(topWorldPos);

            // 바구니 위쪽 + 공이 들어갈 여유 공간
            localMousePos.y = topLocalPos.y + 2f;
        }
        else
        {
            // Renderer가 없으면 기존 방식의 기본값
            localMousePos.y = 12f;
        }

        pos = targetBag.TransformPoint(localMousePos);

        return true;
    }

    private void CreatePreviewBall(Vector2 spawnPos)
    {
        if (forcedNextLevel != -1)
        {
            currentLevel = forcedNextLevel;
            forcedNextLevel = -1;
        }
        else
        {
            currentLevel = nextBallQueue.Dequeue();
        }

        if (nextBallQueue.Count == 0)
        {
            nextBallQueue.Enqueue(UnityEngine.Random.Range(0, 3));
        }

        OnNextBallChanged?.Invoke(getNextBall());

        previewBall = Instantiate(
            GameManager.Inst.ballList[currentLevel],
            spawnPos,
            Quaternion.identity
        );

        Rigidbody2D rb = previewBall.GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.isKinematic = true;

        Collider2D col = previewBall.GetComponent<Collider2D>();

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

        float currentKg =
            GameManager.Inst.kgList[currentLevel];

        previewBall = null;
    }

    public void SpawnMergedBall(int level, Vector2 pos)
    {
        GameObject newCircle = Instantiate(
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
            string.Format("Circle (Level: {0})", level);

        SetBallParent(ball);
    }

    private void SetBallParent(GameObject ball)
    {
        Transform bottleTransform = leftParent.parent;

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

    public int getNextBall()
    {
        if (forcedNextLevel != -1)
            return forcedNextLevel;

        if (nextBallQueue.Count > 0)
            return nextBallQueue.Peek();

        return -1;
    }
}