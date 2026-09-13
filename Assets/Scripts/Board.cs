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

    private bool CheckLine(BoardSlot startSlot)
    {
        BrickController startBrick = startSlot.PlacedBrick;

        if (startBrick == null) return false;

        _destructionOrder.Clear();

        foreach (BrickType type in startBrick.Types)
        {
            List<List<BoardSlot>> lineDestructionOrder = new List<List<BoardSlot>>
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

            if (!plusWall || !minusWall)
            {
                continue;
            }

            for (int level = 0; level < lineDestructionOrder.Count; level++)
            {
                if (_destructionOrder.Count <= level)
                {
                    _destructionOrder.Add(new List<BoardSlot>());
                }

                foreach (BoardSlot slot in lineDestructionOrder[level])
                {
                    if (!_destructionOrder[level].Contains(slot))
                    {
                        _destructionOrder[level].Add(slot);
                    }
                }
            }
        }

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

        LineDestroyed?.Invoke(targetCount);
        StartCoroutine(DestroyLine(destructionOrder));
        
        return true;
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
