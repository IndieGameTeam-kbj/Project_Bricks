using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private int _rowCount;
    [SerializeField] private int _columnCount;
    [SerializeField] private BoardSlot[] _slotReferences;

    private Collider2D _boardArea;
    private BoardSlot[,] _slots;
    private List<List<BoardSlot>> _destructionOrder = new List<List<BoardSlot>>();
    private float _destroyInterval = 0.2f;

    public BoardSlot[,] Slots => _slots;
    public event Action<int> LineDestroyed;

    private void Awake()
    {
        _boardArea = GetComponent<Collider2D>();
        _slots = new BoardSlot[_rowCount, _columnCount];

        for (int row = 0; row < _rowCount; row++)
        {
            for (int column = 0; column < _columnCount; column++)
            {
                int index = row * _columnCount + column;
                _slots[row, column] = _slotReferences[index];
                _slots[row, column].Init(row, column);
            }
        }
    }

    public void Reset()
    {
        StopAllCoroutines();
        _destructionOrder.Clear();

        foreach (BoardSlot slot in _slots)
        {
            if (slot == null || !slot.IsPlaced) continue;

            Destroy(slot.PlacedBrick.gameObject);
            slot.Clear();
        }
    }

    public bool TryPlaceBrick(BrickController brick, Vector2 position)
    {
        if (!TryGetPlacementSlot(position, out BoardSlot slot)) return false;

        slot.Place(brick);
        brick.Place(slot.transform.position);
        bool lineDestroyed = CheckLine(slot);
        
        if (!lineDestroyed && IsFull())
        {
            GameManager.Instance.GameOver();
        }

        return true;
    }

    public bool TryGetPlacementSlot(Vector2 position, out BoardSlot slot)
    {
        slot = null;

        if (!_boardArea.OverlapPoint(position)) return false;

        float nearestDistance = Mathf.Infinity;

        foreach (BoardSlot candidate in _slots)
        {
            float distance = Vector2.Distance(position, candidate.transform.position);

            if (distance >= nearestDistance) continue;

            nearestDistance = distance;
            slot = candidate;
        }

        return slot != null && !slot.IsPlaced;
    }

    //private bool CheckLine(BoardSlot startSlot)
    //{
    //    BrickController startBrick = startSlot.PlacedBrick;

    //    if (startBrick == null) return false;

    //    _destructionOrder.Clear();

    //    foreach (BrickType type in startBrick.Types)
    //    {
    //        List<List<BoardSlot>> lineDestructionOrder = new List<List<BoardSlot>>
    //        {
    //            new List<BoardSlot> { startSlot }
    //        };

    //        var offset = GetOffset(type);

    //        int row = startSlot.Row;
    //        int column = startSlot.Column;

    //        int rowOffsetPlus = offset.rowOffset;
    //        int columnOffsetPlus = offset.columnOffset;

    //        int rowOffsetMinus = -offset.rowOffset;
    //        int columnOffsetMinus = -offset.columnOffset;

    //        bool plus = true;
    //        bool minus = true;

    //        bool plusWall = false;
    //        bool minusWall = false;

    //        int count = 0;

    //        while (plus || minus)
    //        {
    //            count++;

    //            List<BoardSlot> currentLevel = new List<BoardSlot>();

    //            if (plus)
    //            {
    //                if (TryGetLineSlot(row, column, rowOffsetPlus, columnOffsetPlus, count, type, out BoardSlot slot))
    //                {
    //                    currentLevel.Add(slot);
    //                }
    //                else
    //                {
    //                    int targetRow = row + rowOffsetPlus * count;
    //                    int targetColumn = column + columnOffsetPlus * count;

    //                    if (IsOutsideBoard(targetRow, targetColumn))
    //                    {
    //                        plus = false;
    //                        plusWall = true;
    //                    }
    //                    else
    //                    {
    //                        plus = false;
    //                    }
    //                }
    //            }

    //            if (minus)
    //            {
    //                if (TryGetLineSlot(row, column, rowOffsetMinus, columnOffsetMinus, count, type, out BoardSlot slot))
    //                {
    //                    currentLevel.Add(slot);
    //                }
    //                else
    //                {
    //                    int targetRow = row + rowOffsetMinus * count;
    //                    int targetColumn = column + columnOffsetMinus * count;

    //                    if (IsOutsideBoard(targetRow, targetColumn))
    //                    {
    //                        minus = false;
    //                        minusWall = true;
    //                    }
    //                    else
    //                    {
    //                        minus = false;
    //                    }
    //                }
    //            }

    //            if (currentLevel.Count > 0)
    //            {
    //                lineDestructionOrder.Add(currentLevel);
    //            }

    //            if (plusWall && minusWall)
    //            {
    //                break;
    //            }

    //            if (!plus && !minus)
    //            {
    //                break;
    //            }
    //        }

    //        if (!plusWall || !minusWall)
    //        {
    //            continue;
    //        }

    //        for (int level = 0; level < lineDestructionOrder.Count; level++)
    //        {
    //            if (_destructionOrder.Count <= level)
    //            {
    //                _destructionOrder.Add(new List<BoardSlot>());
    //            }

    //            foreach (BoardSlot slot in lineDestructionOrder[level])
    //            {
    //                if (!_destructionOrder[level].Contains(slot))
    //                {
    //                    _destructionOrder[level].Add(slot);
    //                }
    //            }
    //        }
    //    }

    //    if (_destructionOrder.Count == 0) return false;

    //    List<List<BoardSlot>> destructionOrder = _destructionOrder;
    //    _destructionOrder = new List<List<BoardSlot>>();

    //    int targetCount = 0;

    //    foreach (List<BoardSlot> level in destructionOrder)
    //    {
    //        foreach (BoardSlot slot in level)
    //        {
    //            if (slot.PlacedBrick != null && slot.PlacedBrick.State != BrickState.Destroying)
    //            {
    //                targetCount++;
    //            }
    //        }
    //    }

    //    LineDestroyed?.Invoke(targetCount);
    //    StartCoroutine(DestroyLine(destructionOrder));

    //    return true;
    //}

    private bool CheckLine(BoardSlot startSlot)
    {
        BrickController startBrick = startSlot.PlacedBrick;

        if (startBrick == null) return false;

        _destructionOrder.Clear();

        // 시작 브릭에서 연결될 수 있는 모든 완성 줄을 찾는다.
        HashSet<BoardSlot> connectedSlots = BuildCompleteLineNetwork(startSlot);

        // 각 브릭의 모든 줄 조건을 검사해서
        // 실제로 파괴 가능한 브릭만 남긴다.
        HashSet<BoardSlot> destroyableSlots = ResolveDestroyableSlots(connectedSlots);

        // 시작 브릭이 파괴 가능한 상태가 아니라면
        // 이번 배치에서는 아무것도 터지지 않는다.
        if (!destroyableSlots.Contains(startSlot)) return false;

        // 실제 파괴 순서를 만든다.
        BuildDestructionOrder(startSlot, destroyableSlots);

        if (_destructionOrder.Count == 0) return false;

        List<List<BoardSlot>> destructionOrder = _destructionOrder;
        _destructionOrder = new List<List<BoardSlot>>();

        int targetCount = 0;

        foreach (List<BoardSlot> level in destructionOrder)
        {
            foreach (BoardSlot slot in level)
            {
                if (slot.PlacedBrick != null && slot.PlacedBrick.State != BrickState.Destroying)
                {
                    targetCount++;
                }
            }
        }

        if (targetCount == 0) return false;

        LineDestroyed?.Invoke(targetCount);
        StartCoroutine(DestroyLine(destructionOrder));
        return true;
    }

    private void BuildDestructionOrder(BoardSlot startSlot, HashSet<BoardSlot> destroyableSlots)
    {
        Dictionary<BoardSlot, int> destructionLevels = new Dictionary<BoardSlot, int>();
        Queue<BoardSlot> queue = new Queue<BoardSlot>();

        destructionLevels[startSlot] = 0;
        queue.Enqueue(startSlot);

        while (queue.Count > 0)
        {
            BoardSlot currentSlot = queue.Dequeue();

            if (!destructionLevels.TryGetValue(currentSlot, out int currentLevel)) continue;

            BrickController currentBrick = currentSlot.PlacedBrick;

            if (currentBrick == null || currentBrick.State != BrickState.Placed) continue;

            foreach (BrickType type in currentBrick.Types)
            {
                if (!TryGetCompletedLine(currentSlot, type, out List<List<BoardSlot>> lineOrder)) continue;

                // 이 줄에 포함된 모든 브릭이 실제 파괴 가능한 상태인지 확인한다.
                // 하나라도 아니면 이 줄 전체를 따라가지 않는다.
                if (!IsLineDestroyable(lineOrder, destroyableSlots)) continue;

                // 현재 브릭은 이미 파괴 순서에 들어있으므로
                // 다음 칸부터 추가한다.
                for (int level = 1; level < lineOrder.Count; level++)
                {
                    foreach (BoardSlot nextSlot in lineOrder[level])
                    {
                        if (!destroyableSlots.Contains(nextSlot)) continue;

                        int targetLevel = currentLevel + level;

                        if (!destructionLevels.TryGetValue(nextSlot, out int previousLevel))
                        {
                            destructionLevels.Add(nextSlot, targetLevel);
                            queue.Enqueue(nextSlot);
                        }
                        else if (targetLevel < previousLevel)
                        {
                            // 더 짧은 경로를 발견했다면
                            // 더 빠른 파괴 레벨로 변경한다.
                            destructionLevels[nextSlot] = targetLevel;
                            queue.Enqueue(nextSlot);
                        }
                    }
                }
            }
        }

        // 계산된 레벨을 실제 List<List<BoardSlot>> 형태로 만든다.
        _destructionOrder.Clear();

        int maxLevel = 0;

        foreach (KeyValuePair<BoardSlot, int> pair in destructionLevels)
        {
            if (pair.Value > maxLevel) maxLevel = pair.Value;
        }

        for (int level = 0; level <= maxLevel; level++)
        {
            _destructionOrder.Add(new List<BoardSlot>());
        }

        foreach (KeyValuePair<BoardSlot, int> pair in destructionLevels)
        {
            if (!_destructionOrder[pair.Value].Contains(pair.Key))
            {
                _destructionOrder[pair.Value].Add(pair.Key);
            }
        }
    }

    private HashSet<BoardSlot> BuildCompleteLineNetwork(BoardSlot startSlot)
    {
        HashSet<BoardSlot> connectedSlots = new HashSet<BoardSlot>();
        Queue<BoardSlot> queue = new Queue<BoardSlot>();

        connectedSlots.Add(startSlot);
        queue.Enqueue(startSlot);

        while (queue.Count > 0)
        {
            BoardSlot currentSlot = queue.Dequeue();

            BrickController currentBrick = currentSlot.PlacedBrick;

            if (currentBrick == null || currentBrick.State != BrickState.Placed) continue;

            foreach (BrickType type in currentBrick.Types)
            {
                if (!TryGetCompletedLine(currentSlot, type, out List<List<BoardSlot>> lineOrder)) continue;

                foreach (List<BoardSlot> level in lineOrder)
                {
                    foreach (BoardSlot slot in level)
                    {
                        if (connectedSlots.Add(slot))
                        {
                            queue.Enqueue(slot);
                        }
                    }
                }
            }
        }

        return connectedSlots;
    }

    private HashSet<BoardSlot> ResolveDestroyableSlots(HashSet<BoardSlot> connectedSlots)
    {
        HashSet<BoardSlot> destroyableSlots = new HashSet<BoardSlot>(connectedSlots);

        bool changed;

        do
        {
            changed = false;

            List<BoardSlot> invalidSlots = new List<BoardSlot>();

            foreach (BoardSlot slot in destroyableSlots)
            {
                BrickController brick = slot.PlacedBrick;

                if (brick == null || brick.State != BrickState.Placed)
                {
                    invalidSlots.Add(slot);
                    continue;
                }

                // 이 브릭이 가지고 있는 모든 줄을 검사한다.
                foreach (BrickType type in brick.Types)
                {
                    if (!TryGetCompletedLine(slot, type, out List<List<BoardSlot>> lineOrder))
                    {
                        // 자기 줄 중 하나라도 완성되지 않았다면
                        // 이 브릭은 파괴 불가능하다.
                        invalidSlots.Add(slot);
                        break;
                    }

                    // 이 줄에 들어있는 모든 브릭이
                    // 파괴 가능한 상태여야 한다.
                    foreach (List<BoardSlot> level in lineOrder)
                    {
                        foreach (BoardSlot lineSlot in level)
                        {
                            if (!destroyableSlots.Contains(lineSlot))
                            {
                                invalidSlots.Add(slot);
                                break;
                            }
                        }

                        if (invalidSlots.Contains(slot)) break;
                    }

                    if (invalidSlots.Contains(slot)) break;
                }
            }

            // 조건을 만족하지 못하는 브릭을 제거한다.
            foreach (BoardSlot invalidSlot in invalidSlots)
            {
                if (destroyableSlots.Remove(invalidSlot))
                {
                    changed = true;
                }
            }

        } while (changed);

        return destroyableSlots;
    }

    private bool IsLineDestroyable(List<List<BoardSlot>> lineOrder, HashSet<BoardSlot> destroyableSlots)
    {
        foreach (List<BoardSlot> level in lineOrder)
        {
            foreach (BoardSlot slot in level)
            {
                if (!destroyableSlots.Contains(slot)) return false;
            }
        }

        return true;
    }

    private bool TryGetCompletedLine(BoardSlot startSlot, BrickType type, out List<List<BoardSlot>> lineDestructionOrder)
    {
        lineDestructionOrder = new List<List<BoardSlot>>
        {
            new List<BoardSlot> { startSlot }
        };

        var offset = GetOffset(type);

        int row = startSlot.Row;
        int column = startSlot.Column;

        int rowOffsetPlus = offset.rowOffset;
        int columnOffsetPlus = offset.columnOffset;

        int rowOffsetMinus = -offset.rowOffset;
        int columnOffsetMinus = -offset.columnOffset;

        bool plus = true;
        bool minus = true;

        bool plusWall = false;
        bool minusWall = false;

        int count = 0;

        while (plus || minus)
        {
            count++;

            List<BoardSlot> currentLevel = new List<BoardSlot>();

            if (plus)
            {
                if (TryGetLineSlot(row, column, rowOffsetPlus, columnOffsetPlus, count, type, out BoardSlot slot))
                {
                    currentLevel.Add(slot);
                }
                else
                {
                    int targetRow = row + rowOffsetPlus * count;
                    int targetColumn = column + columnOffsetPlus * count;

                    if (IsOutsideBoard(targetRow, targetColumn))
                    {
                        plus = false;
                        plusWall = true;
                    }
                    else
                    {
                        plus = false;
                    }
                }
            }

            if (minus)
            {
                if (TryGetLineSlot(row, column, rowOffsetMinus, columnOffsetMinus, count, type, out BoardSlot slot))
                {
                    currentLevel.Add(slot);
                }
                else
                {
                    int targetRow = row + rowOffsetMinus * count;
                    int targetColumn = column + columnOffsetMinus * count;

                    if (IsOutsideBoard(targetRow, targetColumn))
                    {
                        minus = false;
                        minusWall = true;
                    }
                    else
                    {
                        minus = false;
                    }
                }
            }

            if (currentLevel.Count > 0)
            {
                lineDestructionOrder.Add(currentLevel);
            }

            if (plusWall && minusWall)
            {
                break;
            }

            if (!plus && !minus)
            {
                break;
            }
        }

        return plusWall && minusWall;
    }

    private bool TryGetLineSlot(int row, int column, int rowOffset, int columnOffset, int count, BrickType type, out BoardSlot slot)
    {
        slot = null;

        int targetRow = row + rowOffset * count;
        int targetColumn = column + columnOffset * count;

        if (IsOutsideBoard(targetRow, targetColumn))
        {
            return false;
        }

        slot = _slots[targetRow, targetColumn];

        if (slot == null || !slot.IsPlaced)
        {
            slot = null;
            return false;
        }

        BrickController brick = slot.PlacedBrick;

        if (brick == null)
        {
            slot = null;
            return false;
        }

        if (brick.State != BrickState.Placed)
        {
            slot = null;
            return false;
        }

        if (!HasBrickType(brick, type))
        {
            slot = null;
            return false;
        }

        return true;
    }

    private bool IsOutsideBoard(int row, int column)
    {
        return row < 0 || row >= _rowCount || column < 0 || column >= _columnCount;
    }

    private bool HasBrickType(BrickController brick, BrickType type)
    {
        foreach (BrickType brickType in brick.Types)
        {
            if (brickType == type) return true;
        }

        return false;
    }

    private (int rowOffset, int columnOffset) GetOffset(BrickType type)
    {
        switch (type)
        {
            case BrickType.Horizontal:
                return (0, 1);

            case BrickType.Vertical:
                return (1, 0);

            case BrickType.DiagonalUpward:
                return (-1, 1);

            case BrickType.DiagonalDownward:
                return (1, 1);

            default:
                return (0, 0);
        }
    }

    private IEnumerator DestroyLine(List<List<BoardSlot>> destructionOrder)
    {
        int destroyedBrickCount = 0;
        foreach (List<BoardSlot> level in destructionOrder)
        {
            foreach (BoardSlot slot in level)
            {
                BrickController brick = slot.PlacedBrick;

                if (brick == null) continue;

                slot.Clear();
                brick.Destroy();
                SoundManager.Instance.PlayBlockDestroy(destroyedBrickCount);
                destroyedBrickCount++;
            }

            yield return new WaitForSeconds(_destroyInterval);
        }

        // 저장 요청
        BoardManager.Instance.RequestSave();
        // 점수는 이미 올랐고, 여기서는 숫자 연출만 실행
        ScoreManager.Instance.ShowScore();
    }

    private bool IsFull()
    {
        foreach (BoardSlot slot in _slots)
        {
            if (!slot.IsPlaced) return false;
        }

        return true;
    }

}
