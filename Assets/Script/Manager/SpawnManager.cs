using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // UI 클릭 감지를 위해 반드시 추가해야 합니다!

public class SpawnManager : Singleton<SpawnManager>
{
    public Queue<int> nextBallQueue = new Queue<int>();

    private List<Ball> ballBasket = new List<Ball>();
    [SerializeField] private Transform leftParent;
    [SerializeField] private Transform rightParent;
    [SerializeField] private AudioClip mergeClip;
    private int lastMergeFrame = -1;
    private GameObject previewBall;
    private int currentLevel;
    private int previewCount = 1;


    public Camera currentCamera;

    public bool canSpawn = true;
    private int forcedNextLevel = -1;


    void Start()
    {
        if (currentCamera == null)
        {
            currentCamera = Camera.main;
        }


        for (int i = 0; i < previewCount; i++)
        {
            nextBallQueue.Enqueue(Random.Range(0, 3));
        }
    }

    void Update()
    {
        if (GameManager.Inst.gameOver) return;

        // 공이 떨어지는 중이면 입력을 받지 않음
        if (!canSpawn) return;

        if (Input.GetMouseButtonDown(0))
        {
            // UI(버튼 등) 위에 마우스가 있다면 클릭 이벤트를 무시합니다.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Transform targetBag = GetCurrentTargetBag();
            if (targetBag != null)
            {
                Vector3 mouseWorldPos = currentCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector3 localMousePos = targetBag.InverseTransformPoint(mouseWorldPos);
                
                if (localMousePos.x < -GameManager.Inst.xLimit || localMousePos.x > GameManager.Inst.xLimit)
                {
                    return;
                }
            }
            

            CreatePreviewBall();
        }

        if (Input.GetMouseButton(0) && previewBall != null)
        {
            Transform targetBag = GetCurrentTargetBag();

            if (targetBag != null)
            {
                Vector3 mouseWorldPos = currentCamera.ScreenToWorldPoint(Input.mousePosition);
                float topY = currentCamera.ScreenToWorldPoint(new Vector3(0, Screen.height, 0)).y;

                Vector3 localMousePos = targetBag.InverseTransformPoint(mouseWorldPos);
                localMousePos.x = Mathf.Clamp(localMousePos.x, -GameManager.Inst.xLimit, GameManager.Inst.xLimit);
                float clampedWorldX = targetBag.TransformPoint(localMousePos).x;

                previewBall.transform.position = new Vector2(clampedWorldX, topY - 10.0f);
                previewBall.transform.SetParent(targetBag);
            }
        }

        if (Input.GetMouseButtonUp(0) && previewBall != null)
        {
            DropPreviewBall();
        }
    }

    private void CreatePreviewBall()
    {
        if (forcedNextLevel != -1)
        {
            currentLevel = forcedNextLevel;
            forcedNextLevel = -1;
        }
        else
        {
            currentLevel = nextBallQueue.Dequeue();
            nextBallQueue.Enqueue(Random.Range(0, 3));
        }

        Transform targetBag = GetCurrentTargetBag();

        if (targetBag != null)
        {
            Vector3 mouseWorldPos = currentCamera.ScreenToWorldPoint(Input.mousePosition);
            float topY = currentCamera.ScreenToWorldPoint(new Vector3(0, Screen.height, 0)).y;

            Vector3 localMousePos = targetBag.InverseTransformPoint(mouseWorldPos);
            localMousePos.x = Mathf.Clamp(localMousePos.x, -GameManager.Inst.xLimit, GameManager.Inst.xLimit);
            float clampedWorldX = targetBag.TransformPoint(localMousePos).x;

            Vector2 spawnPos = new Vector2(clampedWorldX, topY - 1.0f);

            previewBall = Instantiate(GameManager.Inst.ballList[currentLevel], spawnPos, Quaternion.identity);
            previewBall.transform.SetParent(targetBag);

            Rigidbody2D rb = previewBall.GetComponent<Rigidbody2D>();
            if (rb == null) rb = previewBall.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;

            Collider2D col = previewBall.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }

    private void DropPreviewBall()
    {
        // 공을 떨어뜨리는 순간 생성 잠금
        canSpawn = false;

        float kg = GameManager.Inst.kgList[currentLevel];

        Ball droppedBallData = new Ball(currentLevel, kg, previewBall.transform.position);
        ballBasket.Add(droppedBallData);

        Rigidbody2D rb = previewBall.GetComponent<Rigidbody2D>();
        rb.isKinematic = false;
        rb.mass = kg;

        Collider2D col = previewBall.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        BallBehaviour bb = previewBall.GetComponent<BallBehaviour>();
        if (bb == null) bb = previewBall.AddComponent<BallBehaviour>();
        bb.level = currentLevel;

        // 플레이어가 떨어뜨린 공임을 표시
        bb.isDroppedByPlayer = true;

        previewBall.name = $"Circle (Level: {currentLevel})";
        SetBallParent(previewBall);

        previewBall = null;
    }

    public void OnBallLanded()
    {
        canSpawn = true; // 정상 착지 완료 -> 다음 공 생성 허용
    }

    public void OnBallHitGroundAndRetry(int level, GameObject ball)
    {
        forcedNextLevel = level; // 똑같은 공을 쥐여주도록 예약
        canSpawn = true;         // 다시 클릭할 수 있도록 허용
        Destroy(ball);           // 땅에 닿은 공 삭제
    }

    public void SpawnMergedBall(int level, Vector2 pos)
    {
        float newKg = GameManager.Inst.kgList[level];

        Ball mergedBallData = new Ball(level, newKg, pos);
        ballBasket.Add(mergedBallData);
        SpawnCircle(mergedBallData);
        if (mergeClip != null && lastMergeFrame != Time.frameCount)
        {
            AudioSource.PlayClipAtPoint(mergeClip, currentCamera.transform.position);

            lastMergeFrame = Time.frameCount;
        }
    }

    private void SpawnCircle(Ball data)
    {
        GameObject newCircle = Instantiate(GameManager.Inst.ballList[data.Level], data.Pos, Quaternion.identity);
        newCircle.name = $"Circle (Level: {data.Level})";

        Rigidbody2D rb = newCircle.GetComponent<Rigidbody2D>();
        if (rb == null) rb = newCircle.AddComponent<Rigidbody2D>();
        rb.mass = data.Kg;

        BallBehaviour bb = newCircle.GetComponent<BallBehaviour>();
        if (bb == null) bb = newCircle.AddComponent<BallBehaviour>();
        bb.level = data.Level;

        // 머지(Merge)로 생성된 공은 isDroppedByPlayer가 기본값(false)이므로 
        // 바닥 충돌 이벤트를 발생시키지 않습니다.

        SetBallParent(newCircle);
    }

    private void SetBallParent(GameObject ball)
    {
        Transform bottleTransform = leftParent.parent;
        Vector3 localPos = bottleTransform.InverseTransformPoint(ball.transform.position);

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


    public int getNextBall()
    {
        if (forcedNextLevel != -1)
        {
            return forcedNextLevel;
        }

        return nextBallQueue.Peek();
    }

}