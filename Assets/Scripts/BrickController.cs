using UnityEngine;

public enum BrickState
{
    Preparing,
    Prepared,
    Placing,
    Placed,
    Destroying,
}

public enum BrickType
{
    Horizontal,
    Vertical,
    DiagonalUpward,
    DiagonalDownward,
}

public enum BrickKind
{
    H,
    V,
    DU,
    DD,

    HV,
    HDU,
    HDD,
    VDU,
    VDD,
    DUDD,
}

public class BrickController : MonoBehaviour
{
    [SerializeField] private BrickKind _kind;
    [SerializeField] private BrickType[] _types;

    private BrickAnimationController _animationController;
    private float _originalZ;
    private Vector3 _originalPosition;
    private Vector3 _originalScale;
    private float _liftZ = -0.5f;
    private float _liftScale = 1.05f;
    private BrickState _state;

    public BrickType[] Types => _types;
    public BrickKind Kind => _kind;
    public BrickAnimationController AnimationController => _animationController;
    public float DragZ => _originalZ + _liftZ;
    public BrickState State => _state;

    public void Init(Vector3 targetPosition)
    {
        _animationController = GetComponent<BrickAnimationController>();
        _originalZ = transform.position.z;
        _originalPosition = targetPosition;
        _originalScale = transform.localScale;
        _state = BrickState.Preparing;
    }

    public void BeginDrag(Vector2 worldPosition)
    {
        transform.localScale = _originalScale * _liftScale;
        Drag(worldPosition);
        _state = BrickState.Placing;
        SoundManager.Instance.PlayBlockPickUp();
    }

    public void Drag(Vector2 worldPosition)
    {
        transform.position = new Vector3(worldPosition.x, worldPosition.y, _originalZ + _liftZ);
    }

    public void EndDrag()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, _originalZ);
        transform.localScale = _originalScale;
    }

    public void Place(Vector3 position)
    {
        transform.position = position;
        transform.localScale = _originalScale;
        _state = BrickState.Placed;
        SoundManager.Instance.PlayBlockPlace();
    }

    public void CancelDrag()
    {
        transform.position = _originalPosition;
        transform.localScale = _originalScale;
        _state = BrickState.Prepared;
        SoundManager.Instance.PlayBlockReturn();
    }

    public void Spawn()
    {
        _state = BrickState.Preparing;
        _animationController.PlaySpawnAnimation(_originalPosition, OnSpawnAnimationComplete);
    }

    public void BeforeDestroy()
    {
        _state = BrickState.Destroying;
    }

    public void Destroy()
    {
        _animationController.PlayDestroyAnimation(OnDestroyAnimationComplete);
    }

    private void OnSpawnAnimationComplete()
    {
        _state = BrickState.Prepared;
    }

    private void OnDestroyAnimationComplete()
    {
        Destroy(gameObject);
    }

    // 저장된 블록을 소리 없이 복원하는 메서드   
    public void RestoreAt(Vector3 position, bool placed)
    {
        transform.position = position;
        transform.localScale = _originalScale;
        _state = placed ? BrickState.Placed : BrickState.Prepared;
    }

}
