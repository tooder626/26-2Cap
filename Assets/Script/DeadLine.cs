using System.Collections.Generic;
using UnityEngine;

public class DeadLine : MonoBehaviour
{
    [SerializeField]
    private float timeLimit = 2.0f;

    private float overTime = 0f;

    private List<Collider2D> ballsInZone = new List<Collider2D>();

    void Update()
    {
        // 이미 게임 오버 상태면 계산 중지
        if (GameManager.Inst.gameOver) return;

        if (ballsInZone.Count > 0)
        {
            overTime += Time.deltaTime;


            // 제한 시간이 지나면 게임 오버
            if (overTime >= timeLimit)
            {
                GameManager.Inst.TriggerGameOver();
            }
        }
        else
        {
            overTime = 0f;
        }
    }

    // 공이 데드라인 영역에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 부딪힌 오브젝트가 공(BallBehaviour)인지 확인
        if (collision.GetComponent<BallBehaviour>() != null)
        {
            ballsInZone.Add(collision);
        }
    }

    // 공이 데드라인 영역에서 빠져나갔을 때
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (ballsInZone.Contains(collision))
        {
            ballsInZone.Remove(collision);
        }
    }
}