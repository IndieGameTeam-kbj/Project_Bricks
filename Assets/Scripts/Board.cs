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
        if (startSlot.PlacedBrick == null) return false;

        _destructionOrder.Clear();
        
        HashSet<BoardSlot> connectedSlots = BuildCompleteLineNetwork(startSlot);
        HashSet<BoardSlot> destroyableSlots = ResolveDestroyableSlots(connectedSlots);
        if (!destroyableSlots.Contains(startSlot)) return false;

        BuildDestructionOrder(startSlot, destroyableSlots);
        if (_destructionOrder.Count == 0) return false;

        List<List<BoardSlot>> destructionOrder = _destructionOrder;
        _destructionOrder = new List<List<BoardSlot>>();

        int targetCount = CountDestroyTargets(destructionOrder);
        if (targetCount == 0) return false;

        LineDestroyed?.Invoke(targetCount);
        StartCoroutine(DestroyLine(destructionOrder));
        return true;
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
            BrickController brick = currentSlot.PlacedBrick;

            if (!IsPlacedBrick(brick)) continue;

            foreach (BrickType type in brick.Types)
            {
                if (!TryGetCompletedLine(currentSlot, type, out List<List<BoardSlot>> lineOrder)) continue;

                foreach (List<BoardSlot> level in lineOrder)
                {
                    foreach (BoardSlot slot in level)
                    {
                        if (!connectedSlots.Add(slot)) continue;

                        queue.Enqueue(slot);
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
                if (!CanDestroy(slot, destroyableSlots))
                {
                    invalidSlots.Add(slot);
                }
            }

            foreach (BoardSlot slot in invalidSlots)
            {
                if (destroyableSlots.Remove(slot))
                {
                    changed = true;
                }
            }

        } while (changed);

        return destroyableSlots;
    }

    private bool CanDestroy(BoardSlot slot, HashSet<BoardSlot> destroyableSlots)
    {
        BrickController brick = slot.PlacedBrick;

        if (!IsPlacedBrick(brick)) return false;

        foreach (BrickType type in brick.Types)
        {
            if (!TryGetCompletedLine(slot, type, out List<List<BoardSlot>> lineOrder)) return false;

            if (!AreAllSlotsDestroyable(lineOrder, destroyableSlots)) return false;
        }

        return true;
    }

    private bool AreAllSlotsDestroyable(List<List<BoardSlot>> lineOrder, HashSet<BoardSlot> destroyableSlots)
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

    private void BuildDestructionOrder(BoardSlot startSlot, HashSet<BoardSlot> destroyableSlots)
    {
        Dictionary<BoardSlot, int> levels = new Dictionary<BoardSlot, int>();
        Queue<BoardSlot> queue = new Queue<BoardSlot>();

        levels[startSlot] = 0;
        queue.Enqueue(startSlot);

        while (queue.Count > 0)
        {
            BoardSlot currentSlot = queue.Dequeue();

            if (!levels.TryGetValue(currentSlot, out int currentLevel)) continue;

            BrickController brick = currentSlot.PlacedBrick;

            if (!IsPlacedBrick(brick)) continue;

            foreach (BrickType type in brick.Types)
            {
                if (!TryGetCompletedLine(currentSlot, type, out List<List<BoardSlot>> lineOrder)) continue;

                if (!AreAllSlotsDestroyable(lineOrder, destroyableSlots)) continue;

                for (int level = 1; level < lineOrder.Count; level++)
                {
                    int targetLevel = currentLevel + level;

                    foreach (BoardSlot slot in lineOrder[level])
                    {
                        if (!destroyableSlots.Contains(slot)) continue;

                        if (!levels.TryGetValue(slot, out int previousLevel))
                        {
                            levels.Add(slot, targetLevel);
                            queue.Enqueue(slot);
                        }
                        else if (targetLevel < previousLevel)
                        {
                            levels[slot] = targetLevel;
                            queue.Enqueue(slot);
                        }
                    }
                }
            }
        }

        CreateDestructionOrder(levels);
    }

    private void CreateDestructionOrder(Dictionary<BoardSlot, int> levels)
    {
        _destructionOrder.Clear();
        int maxLevel = 0;

        foreach (int level in levels.Values)
        {
            maxLevel = Mathf.Max(maxLevel, level);
        }

        for (int level = 0; level <= maxLevel; level++)
        {
            _destructionOrder.Add(new List<BoardSlot>());
        }

        foreach (KeyValuePair<BoardSlot, int> pair in levels)
        {
            _destructionOrder[pair.Value].Add(pair.Key);
        }
    }

    private bool TryGetCompletedLine(BoardSlot startSlot, BrickType type, out List<List<BoardSlot>> lineOrder)
    {
        lineOrder = new List<List<BoardSlot>>
        {
            new List<BoardSlot> { startSlot }
        };

        (int rowOffset, int columnOffset) = GetOffset(type);

        List<BoardSlot> plusSlots = new List<BoardSlot>();
        List<BoardSlot> minusSlots = new List<BoardSlot>();

        bool plusWall = CollectDirection(startSlot, type, rowOffset, columnOffset, plusSlots );
        bool minusWall = CollectDirection(startSlot, type, -rowOffset, -columnOffset, minusSlots);
        if (!plusWall || !minusWall) return false;

        int maxCount = Mathf.Max(plusSlots.Count, minusSlots.Count );
        for (int i = 0; i < maxCount; i++)
        {
            List<BoardSlot> level = new List<BoardSlot>();

            if (i < plusSlots.Count) level.Add(plusSlots[i]);

            if (i < minusSlots.Count) level.Add(minusSlots[i]);

            if (level.Count > 0) lineOrder.Add(level);
        }

        return true;
    }

    private bool CollectDirection(BoardSlot startSlot, BrickType type, int rowOffset, int columnOffset, List<BoardSlot> slots)
    {
        int row = startSlot.Row;
        int column = startSlot.Column;

        int distance = 1;
        while (true)
        {
            int targetRow = row + rowOffset * distance;
            int targetColumn = column + columnOffset * distance;
            if (IsOutsideBoard(targetRow, targetColumn)) return true;

            BoardSlot slot = _slots[targetRow, targetColumn];
            if (!IsLineSlot(slot, type)) return false;

            slots.Add(slot);
            distance++;
        }
    }

    private bool IsLineSlot(BoardSlot slot, BrickType type)
    {
        if (slot == null || !slot.IsPlaced) return false;

        BrickController brick = slot.PlacedBrick;
        if (!IsPlacedBrick(brick)) return false;

        return HasBrickType(brick, type);
    }

    private bool IsPlacedBrick(BrickController brick)
    {
        return brick != null && brick.State == BrickState.Placed;
    }

    private int CountDestroyTargets(List<List<BoardSlot>> destructionOrder)
    {
        int count = 0;

        foreach (List<BoardSlot> level in destructionOrder)
        {
            foreach (BoardSlot slot in level)
            {
                BrickController brick = slot.PlacedBrick;

                if (brick != null && brick.State != BrickState.Destroying)
                {
                    count++;
                }
            }
        }

        return count;
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
        foreach (List<BoardSlot> level in destructionOrder)
        {
            foreach (BoardSlot slot in level)
            {
                BrickController brick = slot.PlacedBrick;
                if (brick == null) continue;

                brick.BeforeDestroy();
            }
        }

        int destroyedBrickCount = 0;

        foreach (List<BoardSlot> level in destructionOrder)
        {
            foreach (BoardSlot slot in level)
            {
                BrickController brick = slot.PlacedBrick;
                if (brick == null) continue;

                brick.Destroy();
                slot.Clear();
                SoundManager.Instance.PlayBlockDestroy(destroyedBrickCount);
                destroyedBrickCount++;
            }

            yield return new WaitForSeconds(_destroyInterval);
        }

        BoardManager.Instance.RequestSave();
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
