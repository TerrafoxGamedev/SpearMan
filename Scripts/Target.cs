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

    // === НАСТРОЙКИ ШКАЛЫ ЗДОРОВЬЯ ===
    [Export] public float MaxHealth = 1000f;   
    [Export] public Color HealthBarFullColor = new Color(1f, 0.1f, 0f);          
    [Export] public Color HealthBarDeathColor = new Color(0f, 0f, 0f);
    private float _currentHealth;                
    private ProgressBar _healthBar;              
    private StyleBoxFlat _fillStyle;
    private bool _isDead = false;

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

        _healthBar = GetNode<ProgressBar>("HealthBar");
        _currentHealth = MaxHealth;
        _healthBar.MaxValue = MaxHealth;
        _healthBar.Value = MaxHealth;

        // Создаём стиль заполнения HealthBar
        _fillStyle = new StyleBoxFlat();
        _fillStyle.BgColor = HealthBarFullColor;
        _healthBar.AddThemeStyleboxOverride("fill", _fillStyle);
        UpdateHealthBarColor();

        // Фон полоски — тёмно-серый
        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        _healthBar.AddThemeStyleboxOverride("background", bgStyle);

        if (!IsInGroup("targets"))
            AddToGroup("targets");

        _startPosition = Position;
        MoveAxis = MoveAxis.Normalized();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead) return;   //Мёртвые не ходят
        
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
        if (!IsMoving || _isDead) return Vector2.Zero;
        return MoveAxis * MoveSpeed * _direction;
    }

    public void OnHit(float damage)
    {
        if (_isDead) return;   // мёртвый не получает урон

        _currentHealth -= damage;
        if (_currentHealth < 0f) _currentHealth = 0f;

        _healthBar.Value = _currentHealth;
        UpdateHealthBarColor();

        // Мигание
        _sprite.Modulate = HitColor;
        _flashTimer = FlashDuration;

        GD.Print($"{Name}: получено {damage:F1} урона, HP = {_currentHealth:F1}/{MaxHealth}");

        if (_currentHealth <= 0f)
            Die();
    }

    private void UpdateHealthBarColor()
    {
        float hpPercent = _currentHealth / MaxHealth;

        // Lerp между бордовым и белым
        Color color = HealthBarDeathColor.Lerp(HealthBarFullColor, hpPercent);
        _fillStyle.BgColor = color;
    }

    private void Die()
    {
        _isDead = true;
        RemoveFromGroup("targets");   // Убираем из группы — копьё больше не считает её целью
        SetPhysicsProcess(false);   // Останавливаем движение

        // Плавный поворот и затемнение
        var tween = CreateTween();
        tween.SetParallel(true);   // обе анимации одновременно

        tween.TweenProperty(_sprite, "rotation", Mathf.Pi / 2, 0.3f);
        tween.TweenProperty(_sprite, "modulate", new Color(0.2f, 0.2f, 0.2f, 1f), 2f);

        
        _flashTimer = 0f;   // Сбросить мигание, если оно было
        _healthBar.Visible = false;   // Сделать полоску HP невидимой

        GD.Print($"{Name} уничтожен!");
    }
}