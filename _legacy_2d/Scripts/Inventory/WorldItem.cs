using Godot;

public partial class WorldItem : Area2D
{
    [Export] public ItemData Data;
    [Export] public int Count = 1;

    public Sprite2D Sprite;
    public Sprite2D Outline;
    public Label NameLabel;

    public override void _Ready()
    {
        Sprite = GetNode<Sprite2D>("Sprite");
        Outline = GetNode<Sprite2D>("Outline");
        NameLabel = GetNode<Label>("NameLabel");

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        UpdateVisual();
    }

    public void Setup(ItemData data, int count)
    {
        Data = data;
        Count = count;
        if (IsInsideTree()) UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (Data == null || Sprite == null) return;

        // Основной спрайт предмета
        Sprite.Texture = Data.Icon;

        // Обводка — та же текстура, что и у спрайта.
        // Реальную «кайму» по контуру рисует шейдер на этом Sprite2D.
        if (Outline != null)
        {
            Outline.Texture = Data.Icon;

            // Передаём размер текстуры в шейдер — он нужен, чтобы
            // правильно вычислить texel (1 пиксель в UV-координатах)
            if (Outline.Material is ShaderMaterial mat)
            {
                Vector2 texSize = Data.Icon.GetSize();
                mat.SetShaderParameter("texture_size", texSize);
            }
        }

        // Label над предметом
        if (NameLabel != null)
        {
            NameLabel.Text = Count > 1
                ? $"{Data.DisplayName} ({Count})"
                : Data.DisplayName;
            NameLabel.Visible = false;
        }
    }

    public void SetHighlighted(bool on)
    {
        // Обводка (Outline с шейдером) включается/выключается
        if (Outline != null)
            Outline.Visible = on;

        // Label тоже показывается только при подсветке
        if (NameLabel != null)
            NameLabel.Visible = on;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Character player)
            player.RegisterNearbyItem(this);
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is Character player)
            player.UnregisterNearbyItem(this);
    }
}