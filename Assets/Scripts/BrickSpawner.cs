using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] _brickPrefabs;
    [SerializeField] private Transform _brickSpawnPoint;
    [SerializeField] private Transform[] _brickPreparedPoints;
    [SerializeField] private Transform _brickParent;

    private Coroutine _spawnCoroutine;
    private float _spawnInterval = 0.2f;
    private float _spawnWeight = 0.0f;
    private float[] _oneLineWeights = { 35.0f, 35.0f, 15.0f, 15.0f };
    private float[] _twoLineWeights = { 55.0f, 10.0f, 10.0f, 10.0f, 10.0f, 5.0f };
    private int _lastOneLineIndex = -1;
    private int _lastTwoLineIndex = -1;
    private float _repeatChance = 5.0f;

    public Transform[] PreparedPoints => _brickPreparedPoints;

    public void Reset()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        _spawnWeight = 0.0f;
        _lastOneLineIndex = -1;
        _lastTwoLineIndex = -1;
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
        int randomIndex = GetRandomBrickIndex();
        GameObject spawnedObject = Instantiate(_brickPrefabs[randomIndex], _brickSpawnPoint.position, Quaternion.identity, _brickParent);
        BrickController brick = spawnedObject.GetComponent<BrickController>();
        brick.Init(_brickPreparedPoints[index].position);
        return brick;
    }

    private int GetRandomBrickIndex()
    {
        float randomValue = Random.Range(0.0f, 100.0f);

        if (randomValue < 95.0f - _spawnWeight)
        {
            int localIndex = GetWeightedIndex(_oneLineWeights, _lastOneLineIndex);
            _lastOneLineIndex = localIndex;

            if (_spawnWeight < 0.0f)
            {
                _spawnWeight = 0.0f;
            }
            else
            {
                _spawnWeight += 1.0f;
            }

            return localIndex;
        }

        else
        {
            int localIndex = GetWeightedIndex(_twoLineWeights, _lastTwoLineIndex);
            _lastTwoLineIndex = localIndex;

            if (_spawnWeight > 0.0f)
            {
                _spawnWeight = 0.0f;
            }
            else
            {
                _spawnWeight -= 1.0f;
            }

            return localIndex + 4;
        }
    }

    private int GetWeightedIndex(float[] baseWeights, int lastIndex)
    {
        while (true)
        {
            float randomValue = Random.Range(0.0f, 100.0f);
            int randomIndex = -1;

            for (int i = 0; i < baseWeights.Length; i++)
            {
                if (randomValue < baseWeights[i])
                {
                    randomIndex = i;
                    break;
                }

                randomValue -= baseWeights[i];
            }

            if (randomIndex != lastIndex) return randomIndex;

            if (Random.Range(0.0f, 100.0f) < _repeatChance)
            {
                return randomIndex;
            }
        }
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

            if (brick.Kind == kind) return prefab;
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

            bricks[i] = Create((BrickKind)kinds[i], _brickPreparedPoints[i].position, false);
        }

        return bricks;
    }

}
