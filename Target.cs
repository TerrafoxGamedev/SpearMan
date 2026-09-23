using Godot;

public partial class Target : Area2D
{
    // === НАСТРОЙКИ ДВИЖЕНИЯ ===
    [Export] public bool IsMoving = false;
    [Export] public float MoveDistance = 200f;
    [Export] public float MoveSpeed = 150f;
    [Export] public Vector2 MoveAxis = Vector2.Right;

    // === НАСТРОЙКИ ВСПЫШКИ ===
    [Export] public Color HitColor = Colors.Red;
    [Export] public float FlashDuration = 0.15f;

    // === ВНУТРЕННЕЕ ===
    private Color _originalColor;
    private Sprite2D _sprite;
    private float _flashTimer = 0f;

    private Vector2 _startPosition;
    private float _progress = 0f;
    private int _direction = 1;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _originalColor = _sprite.Modulate;

        if (!IsInGroup("targets"))
            AddToGroup("targets");

        _startPosition = Position;
        MoveAxis = MoveAxis.Normalized();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // === Движение ===
        if (IsMoving)
        {
            _progress += _direction * (MoveSpeed / MoveDistance) * dt;

            if (_progress >= 1f)
            {
                _progress = 1f;
                _direction = -1;
            }
            else if (_progress <= 0f)
            {
                _progress = 0f;
                _direction = 1;
            }

            Position = _startPosition + MoveAxis * MoveDistance * _progress;
        }

        // === Мигание ===
        if (_flashTimer > 0f)
        {
            _flashTimer -= dt;
            if (_flashTimer <= 0f)
                _sprite.Modulate = _originalColor;
        }
    }

    public Vector2 GetVelocity()
    {
        if (!IsMoving) return Vector2.Zero;
        return MoveAxis * MoveSpeed * _direction;
    }

    public void OnHit(float damage)
    {
        GD.Print($"Получен урон: {damage:F1}");

        _sprite.Modulate = HitColor;
        _flashTimer = FlashDuration;
    }
}