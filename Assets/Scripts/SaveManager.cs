using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private const string MuteKey = "IsMute";
    private const string HasSaveKey = "HasSaveData";
    private const string GameDataKey = "GameData";
    private const string BestScoreKey = "BestScore";

    public static SaveManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 음소거
    public void SaveMute(bool isMuted)
    {
        PlayerPrefs.SetInt(MuteKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool LoadMute()
    {
        return PlayerPrefs.GetInt(MuteKey, 0) == 1;
    }

    // 최고 점수
    public void SaveBestScore(int score)
    {
        PlayerPrefs.SetInt(BestScoreKey, score);
        PlayerPrefs.Save();
    }

    public int LoadBestScore()
    {
        return PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    // 현재 판: 점수, 보드 블록, 하단 블록
    public void SaveGame(GameSaveData data)
    {
        string json = JsonUtility.ToJson(data);

        PlayerPrefs.SetString(GameDataKey, json);
        PlayerPrefs.SetInt(HasSaveKey, 1);
        PlayerPrefs.Save();
    }

    public bool HasSaveData()
    {
        return PlayerPrefs.GetInt(HasSaveKey, 0) == 1
            && PlayerPrefs.HasKey(GameDataKey);
    }

    public GameSaveData LoadGame()
    {
        if (!HasSaveData()) return null;

        string json = PlayerPrefs.GetString(GameDataKey);
        return JsonUtility.FromJson<GameSaveData>(json);
    }

    public void DeleteGameSave()
    {
        // 현재 판만 삭제. 최고 점수와 음소거는 유지.
        PlayerPrefs.DeleteKey(GameDataKey);
        PlayerPrefs.DeleteKey(HasSaveKey);
        PlayerPrefs.Save();
    }

    [ContextMenu("Clear All Save Data")]
    private void ClearAllSaveData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("저장 데이터 초기화 완료");
    }

    public void SaveBoard(
    BoardSlot[,] slots,
    BrickController[] preparedBricks,
    ScoreManager scoreManager)
    {
        GameSaveData data = new GameSaveData
        {
            score = scoreManager.Score,
            isNewBestScore = scoreManager.IsNewBestScore,
            prevBestScore = scoreManager.PrevBestScore,
            preparedKinds = new int[preparedBricks.Length]
        };

        // 보드 위 블록 정보 수집
        foreach (BoardSlot slot in slots)
        {
            if (!slot.IsPlaced || slot.PlacedBrick == null) continue;

            data.boardBricks.Add(new BrickSaveData
            {
                kind = slot.PlacedBrick.Kind,
                row = slot.Row,
                column = slot.Column
            });
        }

        // 하단 블록 정보 수집
        for (int i = 0; i < preparedBricks.Length; i++)
        {
            data.preparedKinds[i] = preparedBricks[i] == null
                ? -1
                : (int)preparedBricks[i].Kind;
        }

        // 기존 저장 함수 호출
        SaveGame(data);
    }

}
