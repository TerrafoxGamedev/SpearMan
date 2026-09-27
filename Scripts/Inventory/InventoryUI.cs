using Godot;
using System;

public partial class InventoryUI : Control
{
	[Export] public int CellSize = 48; // Размер одной клетки в пикселях

	// НАСТРОЙКИ ЦВЕТОВ
	[Export] public Color ZoneBgColor = new(0.15f, 0.15f, 0.15f, 0.85f);
    [Export] public Color GridLineColor = new(0.35f, 0.35f, 0.35f, 1f);
    [Export] public Color BorderColor = Colors.Orange;
    [Export] public Color ItemBgColor = Colors.SlateGray;

	private InventoryContainer _container; // Ссылка на логику (зоны, предметы)
    private Texture2D _backpackIcon; // Изображение рюкзака

	public void Bind(InventoryContainer container, Texture2D backpackIcon = null)
    {
        _container = container;
        _backpackIcon = backpackIcon;
        QueueRedraw();

		// Автоматически подгоняем размер Control под содержимое
        var maxExtent = Vector2I.Zero;
        foreach (var zone in container.Zones)
        {
            var zoneEnd = zone.Data.Offset + zone.Data.Size;
            maxExtent.X = Mathf.Max(maxExtent.X, zoneEnd.X);
            maxExtent.Y = Mathf.Max(maxExtent.Y, zoneEnd.Y);
        }
        CustomMinimumSize = (Vector2)(maxExtent * CellSize);
    }

    public override void _Draw()
    {
        // Ранний выход. Пока Bind не вызван, рисовать нечего
        if (_container == null) return;

        // Фон рюкзака (если задан)
        if (_backpackIcon != null)
            DrawTextureRect(_backpackIcon,
                new Rect2(Vector2.Zero, _backpackIcon.GetSize()), false);

        // Цикл по зонам
        foreach (var zone in _container.Zones)
        {
            Vector2 origin = (Vector2)(zone.Data.Offset * CellSize);   // Левый-верхний угол зоны в пикселях относительно InventoryUI
            Vector2 zoneSize = (Vector2)(zone.Data.Size * CellSize);   // Размер зоны в пикселях

            // Фон зоны
            DrawRect(new Rect2(origin, zoneSize), ZoneBgColor);

            // Сетка зоны
            for (int x = 0; x <= zone.Data.Size.X; x++)
                DrawLine(origin + new Vector2(x * CellSize, 0),
                        origin + new Vector2(x * CellSize, zoneSize.Y),
                        GridLineColor, 1);
            for (int y = 0; y <= zone.Data.Size.Y; y++)
                DrawLine(origin + new Vector2(0, y * CellSize),
                        origin + new Vector2(zoneSize.X, y * CellSize),
                        GridLineColor, 1);

            // Рамка зоны
            DrawRect(new Rect2(origin, zoneSize), BorderColor, false, 2);

            // Предметы
            foreach (var item in zone.Items)
            {
                var effSize = item.EffectiveSize;
                Vector2 itemPos = origin + (Vector2)(item.Position * CellSize);
                Rect2 itemRect = new(itemPos, (Vector2)(effSize * CellSize));   // Прямоугольник, который занимает предмет

                // Если у предмета нет иконки — заливаем прямоугольник серым
                if (item.Data.Icon == null)
                {
                    DrawRect(itemRect.Grow(-1), ItemBgColor);
                    continue;
                }

                // Натуральный размер текстуры — без масштабирования
                Vector2 texSize = item.Data.Icon.GetSize();

                // Центр прямоугольника — туда хотим поместить иконку
                Vector2 center = itemPos + (Vector2)(effSize * CellSize) * 0.5f;

                // Если предмет повёрнут — рисуем с поворотом на 90° вокруг центра
                if (item.Rotated)
                {
                    DrawSetTransform(center, Mathf.Pi / 2, Vector2.One);
                    DrawTextureRect(item.Data.Icon,
                        new Rect2(-texSize * 0.5f, texSize), false);
                    DrawSetTransform(Vector2.Zero, 0, Vector2.One);
                }
                else
                {
                    // Левый-верхний угол иконки, чтобы она оказалась по центру
                    Vector2 topLeft = center - texSize * 0.5f;
                    DrawTextureRect(item.Data.Icon,
                        new Rect2(topLeft, texSize), false);
                }
            }
        }
    }
}
