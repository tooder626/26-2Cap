using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SaveManager : Singleton<SaveManager>
{
    [System.Serializable]
    private class BallSaveData
    {
        public int level;
        public float posX;
        public float posY;
        public float rotZ;
        public bool isDropped;
    }

    [System.Serializable]
    private class GameSaveData
    {
        public int score;
        public float boardRotZ;
        public List<int> nextBalls;
        public List<BallSaveData> ballsInBag;
    }

    private string SavePath => Path.Combine(Application.persistentDataPath, "savefile.json");

    public void SaveGame()
    {
        if (GameManager.Inst.gameOver) return;

        GameSaveData data = new GameSaveData();
        data.score = GameManager.Inst.score;
        data.nextBalls = new List<int>(SpawnManager.Inst.nextBallQueue);
        data.ballsInBag = new List<BallSaveData>();

        GameObject bottleObj = GameObject.Find("Bottle");
        if (bottleObj == null)
        {
            Debug.LogError("씬에 'Bottle'이라는 이름의 오브젝트가 없습니다! 이름을 확인해주세요.");
            return;
        }
        Transform boardRoot = bottleObj.transform;

        data.boardRotZ = boardRoot.eulerAngles.z;

        BallBehaviour[] allBalls = FindObjectsOfType<BallBehaviour>();
        foreach (BallBehaviour ball in allBalls)
        {
            if (ball.isMerged) continue;

            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
            if (rb == null || rb.isKinematic) continue;

            Vector3 localPos = boardRoot.InverseTransformPoint(ball.transform.position);
            float localRotZ = ball.transform.eulerAngles.z - boardRoot.eulerAngles.z;

            BallSaveData ballData = new BallSaveData
            {
                level = ball.level,
                posX = localPos.x,
                posY = localPos.y,
                rotZ = localRotZ,
                isDropped = true
            };
            data.ballsInBag.Add(ballData);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("게임 저장 완료: " + SavePath);
    }

    public bool LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("저장된 파일이 없습니다. 새로 시작합니다.");
            return false;
        }

        string json = File.ReadAllText(SavePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        GameManager.Inst.SetScoreFromLoad(data.score);
        SpawnManager.Inst.nextBallQueue = new Queue<int>(data.nextBalls);

        GameObject bottleObj = GameObject.Find("Bottle");
        if (bottleObj == null)
        {
            Debug.LogError("씬에 'Bottle'이라는 이름의 오브젝트가 없습니다! 이름을 확인해주세요.");
            return false;
        }
        Transform boardRoot = bottleObj.transform;

        // 물리를 멈추고 각도를 맞춥니다.
        boardRoot.rotation = Quaternion.Euler(0, 0, data.boardRotZ);

        Rigidbody2D boardRb = boardRoot.GetComponent<Rigidbody2D>();
        if (boardRb != null)
        {
            boardRb.velocity = Vector2.zero;
            boardRb.angularVelocity = 0f;
        }

        foreach (BallSaveData ballData in data.ballsInBag)
        {
            Vector3 worldPos = boardRoot.TransformPoint(new Vector3(ballData.posX, ballData.posY, 0f));
            Quaternion worldRot = Quaternion.Euler(0, 0, boardRoot.eulerAngles.z + ballData.rotZ);

            GameObject newBall = Instantiate(GameManager.Inst.ballList[ballData.level], worldPos, worldRot);
            SpawnManager.Inst.SetupBallProperties(newBall, ballData.level, ballData.isDropped);

            Rigidbody2D rb = newBall.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        Debug.Log("게임 불러오기 성공!");
        return true;
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("게임 오버 - 저장 파일 삭제 완료");
        }
    }
}