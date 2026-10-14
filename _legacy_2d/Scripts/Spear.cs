using Godot;


public partial class Spear : Area2D
{
	[Export] public float SpearMass = 3.0f;           // Масса копья (кг)
    [Export] public float ScaleDistance = 200f;       // Сколько пикселей в метре
    [Export] public float MinImpactSpeed = 50f;       // Порог, ниже которого урон не считается (защита от слабых ударов)
    [Export] public float RestDistance = 0f;          // Насколько копьё вынесено вперёд в покое
    [Export] public float MaxPullback = 80f;          // Насколько оттягивается назад при замахе
    [Export] public float PullbackSpeed = 200f;       // Скорость замаха (пикс/сек)
    [Export] public float ReturnSpeed = 800f;         // Скорость возврата в покой
    [Export] public float StrikeTime = 0.05f;         // Время удара в секундах (фиксированное)
    [Export] public float ChargeTime = 0.6f;          // Время полного заряда
    [Export] public float MinStrikeDistance = 50f;    // Минимальная дистанция удара
    [Export] public float MaxStrikeDistance = 150f;   // Максимальная дистанция удара
    private float _strikeDistance = 0f;               // Текущая дистанция удара
    private float _strikeSpeed = 0f;                  // Текущая скорость удара
    private float _currentOffset = 0f;                // Текущее смещение от RestDistance (может быть отрицательным при замахе)
    private Vector2 _aimDirection = Vector2.Right;   // Направление копья (Задаём (1, 0) что бы в 1 фрейме не сломалась нормализация)

    private enum State { Idle, Pulling, Striking, Returning }   // Список состояний копья
    private State _state = State.Idle;   // Задаём копью начальное состояние покоя
    private Node2D _owner;
    private Label _speedLabel;
    private Label _hitLabel;
    private CharacterBody2D _ownerBody;
    private int _hitCount = 0;
    private bool _hasHitThisStrike = false;
    public bool AttackEnabled = true;   // если false — копьё не начинает новый замах

    public override void _Ready()
    {
        _owner = GetParent<Node2D>();
        _ownerBody = GetParent<CharacterBody2D>();
        _speedLabel = GetNode<Label>("../../CanvasLayer/SpearSpeedLabel");
        _hitLabel = GetNode<Label>("../../CanvasLayer/HitLabel");

        AreaEntered += OnAreaEntered;   // Проверка пересечений Area2D
    }
	public override void _PhysicsProcess(double delta)
    {
		// Переменная, что бы каждый раз не писать (float)delta
        float dt = (float)delta;

        switch (_state)
        {
            case State.Idle:
                // Просто висим, ждём ЛКМ
                if (AttackEnabled && Input.IsActionPressed("attack"))
                    _state = State.Pulling;
                break;

            case State.Pulling:
                // Оттягиваем назад, не дальше порога
                _currentOffset -= PullbackSpeed * dt;
                _currentOffset = Mathf.Max(_currentOffset, -MaxPullback);

                if (!Input.IsActionPressed("attack"))
                {
                    // Насколько сильно оттянули — от 0 до 1
                    float charge = -_currentOffset / MaxPullback;

                    // Дистанция удара от MinStrikeDistance до MaxStrikeDistance
                    _strikeDistance = Mathf.Lerp(MinStrikeDistance, MaxStrikeDistance, charge);
                    
                     // Скорость подбираем так, чтобы путь до цели занял ровно StrikeTime
                    float startOffset = _currentOffset;
                    float targetOffset = _strikeDistance - RestDistance;
                    float path = targetOffset - startOffset;
                    _strikeSpeed = path / StrikeTime;

                    // Обновляем Label сразу после расчёта скорости
                    _speedLabel.Text = $"SpearSpeed: {_strikeSpeed:F0}";

                    _state = State.Striking;
                }
                break;

            case State.Striking:
                // Резко выдвигаем вперёд до StrikeDistance
                _currentOffset += _strikeSpeed * dt;
                // Цель смещения = желаемое расстояние минус RestDistance
                float strikeOffset = _strikeDistance - RestDistance;

                if (_currentOffset >= strikeOffset)
                {
                    _currentOffset = strikeOffset;
                    _state = State.Returning;
                }
                break;

            case State.Returning:
                // Плавно возвращаемся к 0
                _currentOffset = Mathf.MoveToward(_currentOffset, 0f, ReturnSpeed * dt);
                if (Mathf.Abs(_currentOffset) < 0.5f)
                {
                    _currentOffset = 0f;
                    _hasHitThisStrike = false;   // Сброс запрета с регистрации попаданий
                    _state = State.Idle;
                }
                break;
        }

        UpdatePosition();
    }

    public void SetAimDirection(Vector2 direction)
    {
        _aimDirection = direction.Normalized();
    }

    private void UpdatePosition()
    {
        float totalDistance = RestDistance + _currentOffset;

        GlobalPosition = _owner.GlobalPosition + _aimDirection * totalDistance;
        GlobalRotation = _aimDirection.Angle();
    }

    private float CalculateImpactEnergy(Vector2 spearVelocity, Vector2 targetVelocity)
    {
        // Направление атаки
        Vector2 attackDirection = spearVelocity.Normalized();

        // Относительная скорость
        Vector2 relativeVelocity = spearVelocity - targetVelocity;

        // Скорость сближения — проекция на направление атаки
        float impactSpeed = relativeVelocity.Dot(attackDirection);

        // Минимальный допустимый урон
        if (impactSpeed < MinImpactSpeed)
            return 0f;

        // Кинетическая энергия
        float energy = 0.5f * SpearMass * impactSpeed * impactSpeed / (ScaleDistance * ScaleDistance);
        return energy;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (_state != State.Striking) return;   // Бьём только во время состояния удара
        if (_hasHitThisStrike) return;          // За один удар — только одно попадание
        if (!area.IsInGroup("targets")) return; // Только по сущностям из группы "targets"

        _hasHitThisStrike = true;

        // Расчёт урона
        Vector2 characterVelocity = _ownerBody.Velocity;
        Vector2 spearVelocity = characterVelocity + _aimDirection * _strikeSpeed;
        Vector2 targetVelocity = Vector2.Zero;
        if (area is Target target)
            targetVelocity = target.GetVelocity();

        float damage = CalculateImpactEnergy(spearVelocity, targetVelocity);

        _hitCount++;
        _hitLabel.Text = $"Hits: {_hitCount} | DMG: {damage:F0}";

        if (area is Target t)
            t.OnHit(damage);
    }
}
