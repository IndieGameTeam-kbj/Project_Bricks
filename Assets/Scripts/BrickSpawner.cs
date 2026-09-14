using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] _brickPrefabs;
    [SerializeField] private Transform _brickSpawnPoint;
    [SerializeField] private Transform[] _brickPreparedPoints;
    [SerializeField] private Transform _brickParent;

    public Transform[] PreparedPoints => _brickPreparedPoints;
    private Coroutine _spawnCoroutine;
    private float _spawnInterval = 0.2f;
    private float _spawnWeight = 0.0f;

    public void Reset()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
        _spawnWeight = 0.0f;
    }

    public BrickController[] SpawnBricks()
    {
        BrickController[] bricks = new BrickController[_brickPreparedPoints.Length];

        for (int i = 0; i < _brickPreparedPoints.Length; i++)
        {
            bricks[i] = SpawnBrick(i);
        }

        _spawnCoroutine = StartCoroutine(SpawnBricksRoutine(bricks));
        return bricks;
    }

    private BrickController SpawnBrick(int index)
    {
        // 65, 30, 3, 1, 1
        float randomValue = Random.Range(0.0f, 100.0f);

        int randomIndex;
        if (randomValue < 95.0f - _spawnWeight)
        {
            float oneLineValue = Random.Range(0.0f, 100.0f);

            if (oneLineValue < 65.0f)
            {
                randomIndex = Random.Range(0, 2);
            }
            else
            {
                randomIndex = Random.Range(2, 4);
            }

            _spawnWeight += 1.0f;
        }
        else
        {
            float twoLineValue = Random.Range(0.0f, 100.0f);

            if (twoLineValue < 60.0f)
            {
                randomIndex = 4;
            }
            else if (twoLineValue < 80.0f)
            {
                randomIndex = Random.Range(5, 9);
            }
            else
            {
                randomIndex = 9;
            }

            _spawnWeight = 0.0f;
        }

        GameObject spawnedObject = Instantiate(_brickPrefabs[randomIndex], _brickSpawnPoint.position, Quaternion.identity, _brickParent);
        BrickController brick = spawnedObject.GetComponent<BrickController>();
        brick.Init(_brickPreparedPoints[index].position);
        return brick;
    }

    private IEnumerator SpawnBricksRoutine(BrickController[] bricks)
    {
        SoundManager.Instance.PlayBlockSpawn();

        for (int i = 0; i < bricks.Length; i++)
        {
            bricks[i].Spawn();
            yield return new WaitForSeconds(_spawnInterval);
        }

        _spawnCoroutine = null;
    }

    private GameObject GetPrefabByKind(BrickKind kind)
    {
        foreach (GameObject prefab in _brickPrefabs)
        {
            BrickController brick = prefab.GetComponent<BrickController>();

            if (brick.Kind == kind)
            {
                return prefab;
            }
        }

        throw new System.InvalidOperationException($"타입에 맞는 프리팹이 없습니다: {kind}");
    }

    private BrickController Create(BrickKind kind, Vector3 position, bool placed)
    {
        GameObject spawnedObject = Instantiate(GetPrefabByKind(kind), position, Quaternion.identity, _brickParent);
        BrickController brick = spawnedObject.GetComponent<BrickController>();
        brick.Init(position);
        brick.RestoreAt(position, placed);
        return brick;
    }

    public void RestoreBoard(List<BrickSaveData> data, BoardSlot[,] slots)
    {
        foreach (BrickSaveData saved in data)
        {
            BoardSlot slot = slots[saved.row, saved.column];
            BrickController brick = Create(saved.kind, slot.transform.position, true);
            slot.Place(brick);
        }
    }

    public BrickController[] RestorePrepared(int[] kinds)
    {
        BrickController[] bricks = new BrickController[_brickPreparedPoints.Length];

        for (int i = 0; i < bricks.Length; i++)
        {
            if (kinds[i] == -1) continue;

            bricks[i] = Create( (BrickKind)kinds[i], _brickPreparedPoints[i].position, false );
        }

        return bricks;
    }

}
