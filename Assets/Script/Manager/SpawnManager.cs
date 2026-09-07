using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class SpawnManager : Singleton<SpawnManager>
{

    public static event Action<Transform> OnAimStart;
    public static event Action OnAimEnd;

    public Queue<int> nextBallQueue = new Queue<int>();
    private List<Ball> ballBasket = new List<Ball>();

    [SerializeField] private Transform leftParent;
    [SerializeField] private Transform rightParent;
    [SerializeField] private AudioClip mergeClip;

    private float spawnYOffset = 12.0f; // 공이 생성될 때 카메라 최상단에서 얼마나 떨어질지 결정하는 값

    public Camera currentCamera;
    public bool canSpawn = true; // 공을 생성할 수 있는 상태인지 확인

    private int lastMergeFrame = -1; // 동일 프레임에 소리가 여러 번 나는 것을 방지하기 위한 변수
    private GameObject previewBall; // 마우스를 따라다니는 미리보기 공
    private int currentLevel; // 현재 선택된 공의 레벨
    private int forcedNextLevel = -1; // 바닥에 닿아 다시 생성해야 할 때 강제로 지정되는 공 레벨 (-1이면 강제 아님)

    void Start()
    {
        // 카메라가 지정되지 않았다면 메인 카메라를 찾아 넣습니다.
        currentCamera = Camera.main;

        // 게임 시작 시 미리 큐(대기열)에 다음 공의 레벨(0~2)을 하나 뽑아둡니다.
        nextBallQueue.Enqueue(UnityEngine.Random.Range(0, 3));
    }

    void Update()
    {
        // 게임 오버 상태이거나, 공을 떨어뜨리고 있어서 스폰할 수 없는 상태라면 입력을 받지 않고 종료합니다.
        if (GameManager.Inst.gameOver) return;
        if (canSpawn == false) return;

        // 마우스 왼쪽 버튼을 방금 눌렀을 때
        if (Input.GetMouseButtonDown(0))
        {
            // UI(버튼 등) 위에 마우스가 있다면 클릭 이벤트를 무시합니다.
            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
            }

            // 마우스 위치가 화면을 벗어나지 않고 유효한 곳인지 검사합니다.
            Vector2 spawnPos;
            bool isValid = TryGetValidSpawnPosition(out spawnPos, true);

            // 유효한 위치라면 미리보기 공을 생성합니다.
            if (isValid == true)
            {
                CreatePreviewBall(spawnPos);
            }
        }
        // 마우스 왼쪽 버튼을 누른 채로 드래그하고 있을 때 (미리보기 공이 있을 때만)
        else if (Input.GetMouseButton(0))
        {
            if (previewBall != null)
            {
                // 현재 마우스 위치를 계산해서 공의 위치를 업데이트합니다.
                Vector2 dragPos;
                bool isValid = TryGetValidSpawnPosition(out dragPos, false);

                if (isValid == true)
                {
                    previewBall.transform.position = dragPos;
                }
            }
        }
        // 마우스 왼쪽 버튼에서 손을 뗐을 때 (미리보기 공이 있을 때만)
        else if (Input.GetMouseButtonUp(0))
        {
            if (previewBall != null)
            {
                DropPreviewBall(); // 공을 바닥으로 떨어뜨립니다.
            }
        }
    }

    // 마우스 위치를 월드 좌표로 변환하고, 화면 밖으로 나가지 않도록 제한(Clamp)하는 함수
    private bool TryGetValidSpawnPosition(out Vector2 pos, bool isInitialClick)
    {
        pos = Vector2.zero;

        // 현재 카메라 위치에 따라 공이 들어갈 가방(부모 Transform)을 가져옵니다.
        Transform targetBag = GetCurrentTargetBag();
        if (targetBag == null)
        {
            return false;
        }

        // 스크린(마우스) 좌표를 게임 월드 좌표로 변환합니다.
        Vector3 mouseWorldPos = currentCamera.ScreenToWorldPoint(Input.mousePosition);

        // 월드 좌표를 가방을 기준으로 한 지역(Local) 좌표로 바꿉니다.
        Vector3 localMousePos = targetBag.InverseTransformPoint(mouseWorldPos);

        // 첫 클릭인데 마우스가 지정된 X 제한 범위를 벗어났다면 스폰하지 않습니다.
        if (isInitialClick == true)
        {
            if (Mathf.Abs(localMousePos.x) > GameManager.Inst.xLimit)
            {
                return false;
            }
        }

        // 공이 X 제한 범위를 벗어나지 않도록 값을 고정(Clamp)합니다.
        localMousePos.x = Mathf.Clamp(localMousePos.x, -GameManager.Inst.xLimit, GameManager.Inst.xLimit);

        // 다시 지역 좌표를 월드 좌표로 변환하여 최종 X값을 구합니다.
        float clampedWorldX = targetBag.TransformPoint(localMousePos).x;

        // 화면 가장 위쪽의 Y 좌표를 구합니다.
        float topY = currentCamera.ScreenToWorldPoint(new Vector3(0, Screen.height, 0)).y;

        // 최종적으로 공이 위치할 X, Y 좌표를 완성하여 밖(out)으로 보냅니다.
        pos = new Vector2(clampedWorldX, topY - spawnYOffset);
        return true;
    }

    // 마우스를 클릭했을 때 따라다닐 미리보기 공을 생성하는 함수
    private void CreatePreviewBall(Vector2 spawnPos)
    {
        // 강제로 배정된 공이 있다면 그 레벨을 쓰고, 없다면 대기열(큐)에서 꺼내서 씁니다.
        if (forcedNextLevel != -1)
        {
            currentLevel = forcedNextLevel;
            forcedNextLevel = -1; // 사용했으니 다시 초기화
        }
        else
        {
            currentLevel = nextBallQueue.Dequeue();
        }

        // 대기열이 비어있으면 다음 공을 또 하나 랜덤으로 뽑아 채워넣습니다.
        if (nextBallQueue.Count == 0)
        {
            nextBallQueue.Enqueue(UnityEngine.Random.Range(0, 3));
        }

        // 공 프리팹을 스폰 위치에 생성합니다.
        previewBall = Instantiate(GameManager.Inst.ballList[currentLevel], spawnPos, Quaternion.identity);

        // 미리보기 상태이므로 물리 효과(중력)를 끄고, 충돌체도 꺼서 떨어지지 않게 합니다.
        previewBall.GetComponent<Rigidbody2D>().isKinematic = true;
        previewBall.GetComponent<Collider2D>().enabled = false;

        // 어느 가방(부모)에 속할지 정해줍니다.
        SetBallParent(previewBall);

        // 가이드라인 켜기 이벤트를 발생시킵니다 (구독중인 가이드라인 스크립트가 있다면 작동함).
        if (OnAimStart != null)
        {
            OnAimStart(previewBall.transform);
        }
    }

    // 마우스에서 손을 떼서 공을 떨어뜨리는 함수
    private void DropPreviewBall()
    {
        canSpawn = false; // 공이 바닥에 닿을 때까지 다음 공 생성을 막습니다.

        // 가이드라인 끄기 이벤트를 발생시킵니다.
        if (OnAimEnd != null)
        {
            OnAimEnd();
        }

        // 공에 물리 효과를 켜서 아래로 떨어지게 만듭니다.
        SetupBallProperties(previewBall, currentLevel, true);

        // 떨어뜨린 공의 정보를 바구니(리스트)에 저장합니다.
        float currentKg = GameManager.Inst.kgList[currentLevel];
        Ball droppedBallData = new Ball(currentLevel, currentKg, previewBall.transform.position);
        ballBasket.Add(droppedBallData);

        // 미리보기 공 변수를 비워줍니다.
        previewBall = null;
    }

    // 두 공이 합쳐져서 새로운 공이 만들어질 때 호출되는 함수
    public void SpawnMergedBall(int level, Vector2 pos)
    {
        float newKg = GameManager.Inst.kgList[level];
        Ball mergedBallData = new Ball(level, newKg, pos);
        ballBasket.Add(mergedBallData);

        // 합쳐진 새 공을 생성합니다.
        GameObject newCircle = Instantiate(GameManager.Inst.ballList[level], pos, Quaternion.identity);

        // 물리 효과 등을 설정합니다 (플레이어가 직접 떨어뜨린 것이 아님을 false로 전달).
        SetupBallProperties(newCircle, level, false);

        // 합쳐지는 소리를 재생합니다 (같은 프레임에 중복 재생 방지 포함).
        if (mergeClip != null)
        {
            if (lastMergeFrame != Time.frameCount)
            {
                AudioSource.PlayClipAtPoint(mergeClip, currentCamera.transform.position);
                lastMergeFrame = Time.frameCount;
            }
        }
    }

    // 떨어뜨리거나 합쳐져서 생성된 공의 공통 속성을 세팅해주는 함수
    private void SetupBallProperties(GameObject ball, int level, bool isDroppedByPlayer)
    {
        // 물리 효과(중력)를 켭니다.
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        rb.isKinematic = false;
        rb.mass = GameManager.Inst.kgList[level]; // 무게 설정

        // 충돌체를 켭니다.
        Collider2D col = ball.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }

        // BallBehaviour 컴포넌트가 있는지 찾고, 없다면 코드로 새로 붙여줍니다.
        BallBehaviour bb = ball.GetComponent<BallBehaviour>();
        if (bb == null)
        {
            bb = ball.AddComponent<BallBehaviour>();
        }

        // 공의 레벨과, 플레이어가 직접 떨어뜨린 건지 여부를 저장합니다.
        bb.level = level;
        bb.isDroppedByPlayer = isDroppedByPlayer;

        // 유니티 에디터에서 보기 편하도록 이름을 바꿉니다.
        ball.name = string.Format("Circle (Level: {0})", level);

        // 부모(왼쪽 가방인지 오른쪽 가방인지)를 지정합니다.
        SetBallParent(ball);
    }

    // 공의 위치를 기반으로 왼쪽 가방에 넣을지 오른쪽 가방에 넣을지 결정하는 함수
    private void SetBallParent(GameObject ball)
    {
        Transform bottleTransform = leftParent.parent;

        // 공의 월드 좌표를 가방 전체 부모 기준의 지역 좌표로 변환합니다.
        Vector3 localPos = bottleTransform.InverseTransformPoint(ball.transform.position);

        // X 위치가 0보다 작으면 왼쪽, 아니면 오른쪽으로 판단합니다.
        if (localPos.x < 0)
        {
            ball.transform.SetParent(leftParent);
        }
        else
        {
            ball.transform.SetParent(rightParent);
        }
    }

    // 현재 카메라가 보는 방향(왼쪽/오른쪽)에 따라 타겟 가방을 반환하는 함수
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

    // 공이 바닥에 무사히 닿았을 때 호출 (다시 클릭할 수 있게 만듦)
    public void OnBallLanded()
    {
        canSpawn = true;
    }

    // 공이 바닥에 닿았는데 게임 오버 등으로 인해 재시도해야 할 때 호출
    public void OnBallHitGroundAndRetry(int level, GameObject ball)
    {
        // 똑같은 공을 다시 주도록 예약하고, 기존 공은 삭제합니다.
        forcedNextLevel = level;
        canSpawn = true;
        Destroy(ball);
    }

    // 다음에 나올 공의 레벨이 무엇인지 외부에서 물어볼 때 알려주는 함수 (UI 표시용 등)
    public int getNextBall()
    {
        // 강제로 배정된 공이 있다면 그 레벨을 알려줍니다.
        if (forcedNextLevel != -1)
        {
            return forcedNextLevel;
        }
        else
        {
            // 강제 공이 없다면 대기열 맨 앞에 있는 공을 엿보고(Peek) 알려줍니다.
            return nextBallQueue.Peek();
        }
    }
}