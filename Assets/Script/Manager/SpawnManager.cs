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

    private List<Ball> ballBasket = new List<Ball>();

    [SerializeField] private Transform leftParent;
    [SerializeField] private Transform rightParent;
    [SerializeField] private AudioClip mergeClip;

    private float spawnYOffset = 12.0f;

    public Camera currentCamera;
    public bool canSpawn = true;

    private int lastMergeFrame = -1;

    private GameObject previewBall;
    private int currentLevel;

    // -1이면 강제로 지정된 다음 공이 없음
    private int forcedNextLevel = -1;

    private void Start()
    {
        // 카메라가 지정되지 않았다면 메인 카메라 사용
        if (currentCamera == null)
        {
            currentCamera = Camera.main;
        }

        // 처음 나올 공 하나 생성
        if (nextBallQueue.Count == 0)
        {
            nextBallQueue.Enqueue(UnityEngine.Random.Range(0, 3));
        }

        // 시작할 때 UI에 다음 공 전달
        OnNextBallChanged?.Invoke(getNextBall());
    }

    private void Update()
    {
        if (GameManager.Inst.gameOver)
            return;

        if (!canSpawn)
            return;

        // 마우스 클릭
        if (Input.GetMouseButtonDown(0))
        {
            // UI 클릭이면 무시
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 spawnPos;

            bool isValid =
                TryGetValidSpawnPosition(out spawnPos, true);

            if (isValid)
            {
                CreatePreviewBall(spawnPos);
            }
        }

        // 드래그 중
        else if (Input.GetMouseButton(0))
        {
            if (previewBall != null)
            {
                Vector2 dragPos;

                bool isValid =
                    TryGetValidSpawnPosition(out dragPos, false);

                if (isValid)
                {
                    previewBall.transform.position = dragPos;
                }
            }
        }

        // 마우스를 뗌
        else if (Input.GetMouseButtonUp(0))
        {
            if (previewBall != null)
            {
                DropPreviewBall();
            }
        }
    }

    private bool TryGetValidSpawnPosition(
        out Vector2 pos,
        bool isInitialClick)
    {
        pos = Vector2.zero;

        Transform targetBag = GetCurrentTargetBag();

        if (targetBag == null)
            return false;

        Vector3 mouseWorldPos =
            currentCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        Vector3 localMousePos =
            targetBag.InverseTransformPoint(mouseWorldPos);

        // 첫 클릭 시 X 범위 검사
        if (isInitialClick)
        {
            if (Mathf.Abs(localMousePos.x) >
                GameManager.Inst.xLimit)
            {
                return false;
            }
        }

        // X 범위 제한
        localMousePos.x =
            Mathf.Clamp(
                localMousePos.x,
                -GameManager.Inst.xLimit,
                GameManager.Inst.xLimit
            );

        float clampedWorldX =
            targetBag.TransformPoint(localMousePos).x;

        // 화면 위쪽 Y 계산
        float topY =
            currentCamera.ScreenToWorldPoint(
                new Vector3(
                    0,
                    Screen.height,
                    0
                )
            ).y;

        pos = new Vector2(
            clampedWorldX,
            topY - spawnYOffset
        );

        return true;
    }

    private void CreatePreviewBall(Vector2 spawnPos)
    {
        // 강제로 지정된 공이 있는 경우
        if (forcedNextLevel != -1)
        {
            currentLevel = forcedNextLevel;
            forcedNextLevel = -1;
        }
        else
        {
            // 큐에서 다음 공 가져오기
            currentLevel = nextBallQueue.Dequeue();
        }

        // 다음 공 미리 준비
        if (nextBallQueue.Count == 0)
        {
            nextBallQueue.Enqueue(
                UnityEngine.Random.Range(0, 3)
            );
        }

        // UI에 다음 공 알려주기
        OnNextBallChanged?.Invoke(getNextBall());

        // 공 생성
        previewBall = Instantiate(
            GameManager.Inst.ballList[currentLevel],
            spawnPos,
            Quaternion.identity
        );

        // 미리보기 상태
        Rigidbody2D rb =
            previewBall.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Collider2D col =
            previewBall.GetComponent<Collider2D>();

        if (col != null)
        {
            col.enabled = false;
        }

        // 부모 설정
        SetBallParent(previewBall);

        // 조준 시작
        OnAimStart?.Invoke(previewBall.transform);
    }

    private void DropPreviewBall()
    {
        canSpawn = false;

        // 조준 종료
        OnAimEnd?.Invoke();

        // 실제 공으로 변경
        SetupBallProperties(
            previewBall,
            currentLevel,
            true
        );

        // 바구니에 저장
        float currentKg =
            GameManager.Inst.kgList[currentLevel];

        Ball droppedBallData =
            new Ball(
                currentLevel,
                currentKg,
                previewBall.transform.position
            );

        ballBasket.Add(droppedBallData);

        // 미리보기 공 제거
        previewBall = null;
    }

    public void SpawnMergedBall(
        int level,
        Vector2 pos)
    {
        float newKg =
            GameManager.Inst.kgList[level];

        Ball mergedBallData =
            new Ball(
                level,
                newKg,
                pos
            );

        ballBasket.Add(mergedBallData);

        // 합쳐진 공 생성
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

        // 합치기 효과음
        if (mergeClip != null)
        {
            if (lastMergeFrame != Time.frameCount)
            {
                AudioSource.PlayClipAtPoint(
                    mergeClip,
                    currentCamera.transform.position
                );

                lastMergeFrame = Time.frameCount;
            }
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
            rb.mass =
                GameManager.Inst.kgList[level];
        }

        Collider2D col =
            ball.GetComponent<Collider2D>();

        if (col != null)
        {
            col.enabled = true;
        }

        BallBehaviour bb =
            ball.GetComponent<BallBehaviour>();

        if (bb == null)
        {
            bb = ball.AddComponent<BallBehaviour>();
        }

        bb.level = level;
        bb.isDroppedByPlayer =
            isDroppedByPlayer;

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
        {
            ball.transform.SetParent(leftParent);
        }
        else
        {
            ball.transform.SetParent(rightParent);
        }
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

        // UI의 다음 공도 갱신
        OnNextBallChanged?.Invoke(
            getNextBall()
        );
    }

    public int getNextBall()
    {
        if (forcedNextLevel != -1)
        {
            return forcedNextLevel;
        }

        if (nextBallQueue.Count > 0)
        {
            return nextBallQueue.Peek();
        }

        return -1;
    }
}