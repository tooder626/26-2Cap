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

        // 데드라인 구역에 공이 1개라도 머물러 있다면? 타이머 시작!
        if (ballsInZone.Count > 0)
        {
            overTime += Time.deltaTime;

            // 💡 여기에 텍스트를 빨갛게 깜빡이게 하거나 경고음을 넣으면 좋습니다!

            // 제한 시간이 지나면 게임 오버
            if (overTime >= timeLimit)
            {
                GameManager.Inst.gameOver = true;
                Debug.Log(" 공이 데드라인을 넘었습니다! -> Game Over");

                // GameManager에 있는 GameOver 관련 UI를 띄우는 함수를 호출하면 됩니다.
            }
        }
        else
        {
            // 선을 넘었던 공이 다시 아래로 굴러떨어지거나 합쳐져서 구역이 비워지면 타이머 초기화
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