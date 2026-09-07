using UnityEngine;

public class GuideLineController : MonoBehaviour
{
    [SerializeField] private GameObject guideLineVisual;
    private Transform targetBall;

    private void OnEnable()
    {
        SpawnManager.OnAimStart += ShowGuideLine;
        SpawnManager.OnAimEnd += HideGuideLine;
    }

    private void OnDisable()
    {
        SpawnManager.OnAimStart -= ShowGuideLine;
        SpawnManager.OnAimEnd -= HideGuideLine;
    }

    private void Start()
    {
        HideGuideLine();
    }

    private void ShowGuideLine(Transform ball)
    {
        targetBall = ball;
        if (guideLineVisual != null) guideLineVisual.SetActive(true);
    }

    private void HideGuideLine()
    {
        targetBall = null;
        if (guideLineVisual != null) guideLineVisual.SetActive(false);
    }
    
    private void LateUpdate()
    {
        // 공을 따라다니되, Y축으로 yOffset만큼 내려서 선의 윗부분이 공 중앙에 오도록 맞춤
        if (targetBall != null)
        {
            Vector3 newPos = targetBall.position;
            transform.position = newPos;
        }
    }
}