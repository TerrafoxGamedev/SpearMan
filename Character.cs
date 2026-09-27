using Godot;

public partial class Character : CharacterBody2D
{
    // === НАСТРОЙКИ ДВИЖЕНИЯ ===
    [Export] public float MaxSpeed = 500f;        // Максимальная скорость
    [Export] public float Acceleration = 1500f;   // Ускорение
    [Export] public float Deceleration = 2000f;   // Торможение

    // === НАСТРОЙКИ ДЭША ===
    [Export] public float DashSpeed = 1500f;      // Скорость дэша (пикс/сек)
    [Export] public float DashDuration = 0.1f;    // Длительность дэша (сек)
    [Export] public float DashCooldown = 1f;      // Кулдаун после дэша (сек)

    private float _dashTimer = 0f;                // Сколько осталось от текущего дэша
    private float _dashCooldownTimer = 0f;        // Сколько осталось до следующего дэша
    private float _dashRotationProgress = 0f;     // Прогресс вращения во время дэша (0..1)
    private float _dashSpinSign = 1f;             // +1 = по часовой, -1 = против
    private Vector2 _dashDirection = Vector2.Zero;   // Направление текущего дэша

    [Export] public float CameraSmoothSpeed = 5f; // Скорость движения камеры
    private Camera2D _camera;                     // Камера
    private Label _speedLabel;                    // Спидометр
    private Spear _spear;                         // Копьё
    private AnimatedSprite2D _sprite;             // Теперь это AnimatedSprite2D, заменил "Sprite2D _sprite;" 

    public override void _Ready()
    {
        // Находим узлы
        _speedLabel = GetNode<Label>("../CanvasLayer/SpeedLabel");
        _camera = GetNode<Camera2D>("../Camera2D");
        _spear = GetNode<Spear>("Spear");
        _sprite = GetNode<AnimatedSprite2D>("Григорий");    // Теперь это AnimatedSprite2D, заменил "Sprite2D _sprite; на Григорий"
    }
    public override void _PhysicsProcess(double delta)
    {
        // Переменная, что бы каждый раз не писать (float)delta
        float dt = (float)delta;

        // Обнуление таймеров 
        if (_dashTimer > 0f)
            _dashTimer -= dt;

        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= dt;

        // Собираем направление из ввода
        Vector2 inputDirection = Vector2.Zero;

        if (Input.IsActionPressed("ui_left"))
            inputDirection.X -= 1;
        if (Input.IsActionPressed("ui_right"))
            inputDirection.X += 1;
        if (Input.IsActionPressed("ui_up"))
            inputDirection.Y -= 1;
        if (Input.IsActionPressed("ui_down"))
            inputDirection.Y += 1;

        // Нормализуем, чтобы по диагонали не было быстрее
        if (inputDirection != Vector2.Zero)
            inputDirection = inputDirection.Normalized();

        // Поворот к курсору + направление взгляда
        Vector2 mousePos = GetGlobalMousePosition();
        Vector2 toMouse = mousePos - GlobalPosition;
        Vector2 aimDirection = Vector2.Right;

        if (toMouse.LengthSquared() > 1f)
        {
            LookAt(mousePos);
            aimDirection = toMouse.Normalized();
            _spear.SetAimDirection(aimDirection);
        }

        // Дэш 
        if (Input.IsActionJustPressed("dash") && _dashTimer <= 0f && _dashCooldownTimer <= 0f)
        {
            // Направление: движение, если есть, иначе — взгляд
            _dashDirection = inputDirection != Vector2.Zero ? inputDirection : aimDirection;
            
            _dashSpinSign = _dashDirection.X >= 0f ? 1f : -1f;  // Направление вращения
            _dashTimer = DashDuration;                          // Длительность дэша
            _dashCooldownTimer = DashCooldown + DashDuration;   // Кулдаун дэша
        }

        // Движение
        if (_dashTimer > 0f)
        {
            // Во время дэша — фиксированная скорость, управление отключено
            Velocity = _dashDirection * DashSpeed;

            // Прогресс вращения: 0 → 1 за время DashDuration
            _dashRotationProgress = 1f - (_dashTimer / DashDuration);
        }
        else
        {
            // Обычное управление
            Vector2 targetVelocity = inputDirection * MaxSpeed;
            float rate = inputDirection != Vector2.Zero ? Acceleration : Deceleration;
            Velocity = Velocity.MoveToward(targetVelocity, rate * dt);

            // Управление анимацией бега
            if (Velocity.Length() > 10f) // Если персонаж движется
            {
                if (!_sprite.IsPlaying())
                 _sprite.Play("run"); // Запускаем анимацию бега
            }
            else
            {
                _sprite.Stop(); // Если стоит — останавливаем
            }

            // После дэша — сброс вращения
            _dashRotationProgress = 0f;
        }

        // Вращаем спрайт
        _sprite.Rotation = _dashRotationProgress * Mathf.Tau * _dashSpinSign;

        // Отображение кулдауна дэша
        if (_dashCooldownTimer > 0f && _dashTimer <= 0f)
        {
            _sprite.Modulate = new Color(0.7f, 0.7f, 0.7f, 1f);
        }
        else
        {
            // Кулдаун закончился (или идёт дэш) — возвращаем нормальный цвет
            _sprite.Modulate = Colors.White;
        }

        // Двигаем спрайт
        MoveAndSlide();
        
        // HUD
        _speedLabel.Text = $"Speed: {Velocity.Length():F0}";

        // Плавно двигаем камеру за персонажем
        _camera.GlobalPosition = _camera.GlobalPosition.Lerp(GlobalPosition, CameraSmoothSpeed * dt);
    }
}
