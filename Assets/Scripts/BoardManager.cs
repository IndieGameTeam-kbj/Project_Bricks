using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private BrickSpawner _brickSpawner;
    [SerializeField] private Board _board;
    [SerializeField] private GameObject _brickPreview;

    private BrickController[] _preparedBricks;
    private Camera _mainCamera;
    private BrickController _draggingBrick;
    private float _dragScreenYOffset = 150.0f;
    private bool _savePending = false;

    public static BoardManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _mainCamera = Camera.main;
    }

    public void Reset()
    {
        _savePending = false;
        _brickSpawner.Reset();
        _board.Reset();
        _brickPreview.SetActive(false);

        if (_preparedBricks == null) return;

        foreach (BrickController brick in _preparedBricks)
        {
            if (brick == null) continue;
            Destroy(brick.gameObject);
        }

        _preparedBricks = null;
        _draggingBrick = null;
    }

    public void RequestSave()
    {
        _savePending = true;
    }

    public void StartGame()
    {
        _preparedBricks = _brickSpawner.SpawnBricks();
        RequestSave();
    }

    private void Update()
    {
        if (InputManager.Instance.IsPointerPressed)
        {
            TryBeginDrag();
        }

        if (_draggingBrick != null && InputManager.Instance.IsPointerHeld)
        {
            Vector2 dragWorldPosition = GetDragWorldPosition();
            _draggingBrick.Drag(dragWorldPosition);
            UpdateBrickPreview(dragWorldPosition);
        }

        if (_draggingBrick != null && InputManager.Instance.IsPointerReleased)
        {
            TryPlaceBrick();
            _draggingBrick = null;
            _brickPreview.SetActive(false);
        }
    }

    private void TryBeginDrag()
    {
        if (_draggingBrick != null) return;

        Vector2 pointerWorldPosition = ScreenToWorldPosition(InputManager.Instance.PointerScreenPosition, 0.0f);

        Collider2D collider = Physics2D.OverlapPoint(pointerWorldPosition);
        if (collider == null) return;

        BrickController brick = collider.GetComponent<BrickController>();
        if (brick == null || brick.State != BrickState.Prepared) return;
        
        _draggingBrick = brick;
        Vector2 dragWorldPosition = GetDragWorldPosition();
        _draggingBrick.BeginDrag(dragWorldPosition);
    }

    private Vector2 GetDragWorldPosition()
    {
        Vector2 screenPosition = InputManager.Instance.PointerScreenPosition;
        screenPosition.y += _dragScreenYOffset;
        return ScreenToWorldPosition(screenPosition, _draggingBrick.DragZ);
    }

    private Vector2 ScreenToWorldPosition(Vector2 screenPosition, float worldZ)
    {
        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        Plane plane = new Plane(Vector3.forward, new Vector3(0.0f, 0.0f, worldZ));

        if (!plane.Raycast(ray, out float distance))
        {
            return Vector2.zero;
        }

        Vector3 worldPosition = ray.GetPoint(distance);
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    private void TryPlaceBrick()
    {
        Vector2 dropPosition = GetDragWorldPosition();

        if (!_board.TryPlaceBrick(_draggingBrick, dropPosition))
        {
            _draggingBrick.CancelDrag();
            return;
        }

        RemovePreparedBrick(_draggingBrick);
        RequestSave();
    }

    private void RemovePreparedBrick(BrickController brick)
    {
        for (int i = 0; i < _preparedBricks.Length; i++)
        {
            if (_preparedBricks[i] != brick) continue;

            _preparedBricks[i] = null;
            break;
        }

        if (AreAllBricksPlaced())
        {
            _preparedBricks = _brickSpawner.SpawnBricks();
        }
    }

    private bool AreAllBricksPlaced()
    {
        foreach (BrickController brick in _preparedBricks)
        {
            if (brick != null) return false;
        }

        return true;
    }

    private void LateUpdate() // Update 후에 저장 시도
    {
        if (!_savePending) return;

        // 끝난 판을 다시 저장하지 않음
        if (GameManager.Instance.State == GameState.GameOver)
        {
            _savePending = false;
            return;
        }

        if (_draggingBrick != null) return;
        if (_preparedBricks == null) return;

        // 하단 블록 등장 애니메이션이 끝나야 저장
        foreach (BrickController brick in _preparedBricks)
        {
            if (brick != null && brick.State != BrickState.Prepared)
            {
                return;
            }
        }

        SaveCurrentGame();
        _savePending = false;
    }

    private void SaveCurrentGame()
    {
        SaveManager.Instance.SaveBoard( _board.Slots, _preparedBricks, ScoreManager.Instance );
    }

    public void RestoreGame(GameSaveData data)
    {
        Reset();
        _brickSpawner.RestoreBoard( data.boardBricks, _board.Slots );
        _preparedBricks = _brickSpawner.RestorePrepared( data.preparedKinds );
    }

    private void UpdateBrickPreview(Vector2 position)
    {
        if (!_board.TryGetPlacementSlot(position, out BoardSlot slot))
        {
            _brickPreview.SetActive(false);
            return;
        }

        _brickPreview.transform.position = slot.transform.position;
        _brickPreview.SetActive(true);
    }

}
