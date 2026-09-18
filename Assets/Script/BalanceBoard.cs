using UnityEngine;

public class BalanceBoard : MonoBehaviour
{
    [SerializeField] private Transform leftParent;
    [SerializeField] private Transform rightParent;

    [SerializeField] private float tiltSensitivity = 2f;
    [SerializeField] private float maxTiltAngle = 30f;   
    [SerializeField] private float smoothSpeed = 3f;     
    void Update()
    {
        if (GameManager.Inst.gameOver) return;

        // 1. 공들이 선(X=0)을 넘어갔다면 알맞은 폴더로 다시 옮겨줍니다.
        UpdateBallParents();

        // 2. 왼쪽과 오른쪽 폴더의 총 무게를 계산합니다.
        float leftWeight = GetTotalWeight(leftParent);
        float rightWeight = GetTotalWeight(rightParent);

        float weightDifference = leftWeight - rightWeight;

        // 4. 목표 각도 계산
        float targetAngle = weightDifference * tiltSensitivity;

        // 5. 현재 각도에서 목표 각도로 부드럽게 회전 적용
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f; 

        float newAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Euler(0, 0, newAngle);

        if (Mathf.Abs(newAngle) >= maxTiltAngle)
        {
            GameOver();
        }
    }

    // 선을 넘어간 공들의 부모를 다시 설정해 주는 함수
    private void UpdateBallParents()
    {
        // 왼쪽 폴더 검사 (오른쪽으로 넘어간 공이 있는지)
        for (int i = leftParent.childCount - 1; i >= 0; i--)
        {
            Transform child = leftParent.GetChild(i);
            if (child.position.x >= 0) child.SetParent(rightParent);
        }

        // 오른쪽 폴더 검사 (왼쪽으로 넘어간 공이 있는지)
        for (int i = rightParent.childCount - 1; i >= 0; i--)
        {
            Transform child = rightParent.GetChild(i);
            if (child.position.x < 0) child.SetParent(leftParent);
        }
    }

    // 특정 폴더 안에 있는 모든 공의 무게 합을 구하는 함수
    private float GetTotalWeight(Transform parentFolder)
    {
        float totalWeight = 0f;
        foreach (Transform child in parentFolder)
        {
            Rigidbody2D rb = child.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                totalWeight += rb.mass;
            }
        }
        return totalWeight;
    }

    private void GameOver()
    {
        GameManager.Inst.TriggerGameOver();
        Debug.Log("무게 균형이 무너졌습니다! -> Game Over");

    }
}