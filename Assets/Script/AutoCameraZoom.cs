using UnityEngine;

public class AutoCameraZoom : MonoBehaviour
{
    [SerializeField] private GameObject target;

    [Header("기울기 조건 (각도)")]
    [SerializeField] private float tiltThreshold1 = 10f; // 1단계 경계 
    [SerializeField] private float tiltThreshold2 = 20f; // 2단계 경계 

    [Header("카메라 사이즈 (Zoom)")]
    [SerializeField] private float defaultZoom = 30f;    // 기본 카메라 사이즈 
    [SerializeField] private float midZoom = 40f;        // 중간 줌아웃 사이즈 
    [SerializeField] private float maxZoom = 50f;        // 최대 줌아웃 사이즈 

    [SerializeField] private float zoomSpeed = 2f;       // 줌 전환 속도

    private Camera myCam;

    private void Start()
    {
        myCam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (GameManager.Inst.gameOver) return;

        if (target != null && myCam != null)
        {
            // 1. 바구니의 현재 Z축 회전값을 가져옵니다.
            float currentZRotation = target.transform.eulerAngles.z;

            // 2. 🚨 유니티 각도 변환 (-180 ~ 180도 체계로 변경)
            if (currentZRotation > 180f) currentZRotation -= 360f;

            // 3. 절댓값을 씌워 양쪽(좌/우) 기울기를 동일하게 계산합니다.
            float tiltMagnitude = Mathf.Abs(currentZRotation);

            // 4. 기울기에 따른 목표 줌(Target Zoom) 크기 결정 (3단계)
            float targetZoom = defaultZoom; // 기본값

            if (tiltMagnitude >= tiltThreshold2)
            {
                // 3단계: 20도 이상 크게 기울어짐 -> 최대 줌아웃
                targetZoom = maxZoom;
            }
            else if (tiltMagnitude >= tiltThreshold1)
            {
                // 2단계: 10도 이상 ~ 20도 미만 기울어짐 -> 중간 줌아웃
                targetZoom = midZoom;
            }
            else
            {
                // 1단계: 10도 미만 (안정적) -> 기본 줌
                targetZoom = defaultZoom;
            }

            // 5. 현재 카메라 사이즈에서 목표 줌 사이즈로 부드럽게 전환
            myCam.orthographicSize = Mathf.Lerp(myCam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
        }
    }
}