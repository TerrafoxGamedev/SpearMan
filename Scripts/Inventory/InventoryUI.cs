using Godot;

public partial class InventoryUI : Control
{
	[Export] public int CellSize = 48; // Размер одной клетки в пикселях

	// НАСТРОЙКИ ЦВЕТОВ
	[Export] public Color ZoneBgColor = new(0.15f, 0.15f, 0.15f, 0.85f);
    [Export] public Color GridLineColor = new(0.35f, 0.35f, 0.35f, 1f);
    [Export] public Color BorderColor = Colors.Orange;
    [Export] public Color ItemBgColor = Colors.SlateGray;

	// ЦВЕТА ПОДСВЕТКИ КЛЕТОК ПОД КУРСОРОМ
	[Export] public Color HighlightValidColor = new(0.3f, 1f, 0.3f, 0.35f);     // Заливка — можно положить
	[Export] public Color HighlightBorderValid = new(0.3f, 1f, 0.3f, 1f);       // Контур — можно положить
    [Export] public Color HighlightSwapColor = new(1f, 0.9f, 0.3f, 0.35f);      // Заливка — можно обменять
    [Export] public Color HighlightSwapBorder = new(1f, 0.9f, 0.3f, 1f);        // Контур — можно обменять
    [Export] public Color HighlightInvalidColor = new(1f, 0.3f, 0.3f, 0.35f);   // Заливка — нельзя положить
	[Export] public Color HighlightBorderInvalid = new(1f, 0.3f, 0.3f, 1f);     // Контур — нельзя положить

	private InventoryContainer _container;   // Ссылка на логику (зоны, предметы)
    private Texture2D _backpackIcon;         // Изображение рюкзака

	// === СОСТОЯНИЕ DRAG & DROP ===
	private ItemInstance _dragging = null;   // Что тащим (null = ничего не тащим)
	private Vector2 _dragOffsetInCells;      // Сдвиг курсора от левого-верхнего угла предмета (в клетках)
	private InventoryZone _sourceZone;       // Из какой зоны взяли предмет
	private Vector2I _sourcePosition;        // Где именно он лежал (для возврата)
	private bool _sourceRotated;             // Был ли предмет повёрнут при захвате
	private Vector2 _lastMouseLocal;         // Последняя позиция мыши (в координатах Control)

	// Структура для предпросмотра дропа — вычисляется каждый кадр, не хранится
	private struct DropPreview
	{
		public bool IsActive;             // Есть ли что подсвечивать
		public InventoryZone Zone;        // В какой зоне
		public Vector2I Position;         // Левый-верхний угол (в клетках зоны)
		public bool Rotated;              // Поворот
		public bool IsValid;              // Можно ли положить (свободно)
        public ItemInstance SwapTarget;   // Предмет, с которым можно обменяться (или null)
	}

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

    // ==================== ОБРАБОТКА ВВОДА ====================

    public override void _GuiInput(InputEvent @event)
    {
        if (_container == null) return;

        // Движение мыши — обновляем позицию тащимого предмета
        if (@event is InputEventMouseMotion mm)
        {
            _lastMouseLocal = mm.Position;
            if (_dragging != null) QueueRedraw();
            return;
        }

        if (@event is not InputEventMouseButton mb)
            return;

        if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
        {
            // Первое нажатие — взять. Второе — положить.
            if (_dragging == null)
                TryStartDrag(mb.Position);
            else
                TryDrop(mb.Position);
        }
        else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed && _dragging != null)
        {
            // ПКМ — поворот тащимого предмета
            if (_dragging.Data.Rotatable)
            {
                _dragging.Rotated = !_dragging.Rotated;

                // Пересчитываем смещение — теперь центр снова под курсором
                Vector2I newEffSize = _dragging.EffectiveSize;
               _dragOffsetInCells = new Vector2(newEffSize.X / 2f, newEffSize.Y / 2f);

                QueueRedraw();
            }
        }
    }

    // ==================== ЛОГИКА DRAG & DROP ====================

    private void TryStartDrag(Vector2 local)
    {
        if (_dragging != null) return;                                     // Уже что-то тащим — игнор
        if (!TryGetZoneAndCell(local, out var zone, out var cell)) return; // Мышь не над зоной

        var item = zone.Grid[cell.X, cell.Y];   // Что лежит в этой клетке?
        if (item == null) return;               // Пустая клетка

        // Запоминаем источник — на случай, если некуда будет положить
        _sourceZone = zone;
        _sourcePosition = item.Position;
        _sourceRotated = item.Rotated;

        // Всегда центрируем курсор на предмете — так дроп будет ровно под курсором
        Vector2I effSize = item.EffectiveSize;
        _dragOffsetInCells = new Vector2(effSize.X / 2f, effSize.Y / 2f);

        /*// Считаем смещение курсора от левого-верхнего угла предмета
        Vector2 cellF = (local - (Vector2)(zone.Data.Offset * CellSize)) / CellSize;
        _dragOffsetInCells = cellF - (Vector2)item.Position;*/

        zone.Remove(item);   // Извлекаем предмет из зоны — теперь он «в воздухе»

        _dragging = item;
        _lastMouseLocal = local;
        QueueRedraw();
    }

    private void TryDrop(Vector2 local)
    {
        if (_dragging == null) return;   // Нечего класть

        // Пробуем положить в зону под курсором
        if (TryGetZoneAndCell(local, out var zone, out var _)
            && zone.Data.Accepts(_dragging.Data))   // Зона принимает этот тег?
        {
            Vector2I dropPos = GetDropPositionInZone(local, zone);
            
            // Попытка положить на свободное место
            if (zone.CanPlace(_dragging.Data, dropPos, _dragging.Rotated))
            {
                zone.Place(_dragging, dropPos, _dragging.Rotated);
                _dragging = null;
                QueueRedraw();
                return;
            }

            // Попытка замены
            var swapTarget = FindSwapTarget(zone, dropPos, _dragging.Rotated);
            if (swapTarget != null)
            {
                PerformSwap(zone, dropPos, swapTarget);
                return;
            }
        }

        // Не получилось — возвращаем как было
        ReturnToSource();
        _dragging = null;
        QueueRedraw();
    }

    private void PerformSwap(InventoryZone targetZone, Vector2I dropPos, ItemInstance swapTarget)
    {
        // Забираем swap-цель из зоны
        targetZone.Remove(swapTarget);

        // Кладём наш текущий предмет на её место
        targetZone.Place(_dragging, dropPos, _dragging.Rotated);

        // Swap-цель теперь в руке
        _dragging = swapTarget;

        // Пересчитываем offset — у нового предмета может быть другой размер
        Vector2I newEffSize = _dragging.EffectiveSize;
        _dragOffsetInCells = new Vector2(newEffSize.X / 2f, newEffSize.Y / 2f);

        // Если swap-цели выбрать не валидное место, то она попадёт в первую свободную клетку
        _sourceZone = null;
        _sourcePosition = Vector2I.Zero;
        _sourceRotated = false;

        QueueRedraw();
    }

    private void ReturnToSource()
    {
        // Пробуем вернуть ровно туда, откуда взяли
        if (_sourceZone != null &&
            _sourceZone.CanPlace(_dragging.Data, _sourcePosition, _sourceRotated))
        {
            _sourceZone.Place(_dragging, _sourcePosition, _sourceRotated);
            return;
        }

        // Если не получается — ищем место в контейнере
        if (!_container.TryAddItem(_dragging))
            GD.Print("Не удалось вернуть предмет!");
    }

    // ==================== ХЕЛПЕРЫ ====================

    // Возвращает зону и клетку под курсором. Если мышь не над зоной — zone = null
    private bool TryGetZoneAndCell(Vector2 local, out InventoryZone zone, out Vector2I cell)
    {
        zone = null;
        cell = Vector2I.Zero;

        foreach (var z in _container.Zones)
        {
            Vector2 origin = (Vector2)(z.Data.Offset * CellSize);
            Vector2 zoneSize = (Vector2)(z.Data.Size * CellSize);
            Rect2 rect = new(origin, zoneSize);   // Прямоугольник зоны в пикселях

            if (!rect.HasPoint(local)) continue;   // Мышь не в этой зоне

            Vector2 cellF = (local - origin) / CellSize;   // Координата в клетках
            cell = new Vector2I((int)cellF.X, (int)cellF.Y);
            zone = z;
            return true;
        }
        return false;
    }

    // Вычисляет, куда встанет ЛЕВЫЙ-ВЕРХНИЙ угол, если положить здесь
    private Vector2I GetDropPositionInZone(Vector2 local, InventoryZone zone)
    {
        Vector2 origin = (Vector2)(zone.Data.Offset * CellSize);
        Vector2 cellF = (local - origin) / CellSize;
        return new Vector2I(
            (int)Mathf.Round(cellF.X - _dragOffsetInCells.X),
            (int)Mathf.Round(cellF.Y - _dragOffsetInCells.Y));
    }

    // Вычисляет, занят ли выделенный прямоугольник ровно одним предметом
    private ItemInstance FindSwapTarget(InventoryZone zone, Vector2I pos, bool rotated)
    {
        // Проверяем, что взятый предмет вообще влезает в границы зоны
        Vector2I mySize = rotated
            ? new Vector2I(_dragging.Data.Size.Y, _dragging.Data.Size.X)
            : _dragging.Data.Size;

        if (pos.X < 0 || pos.Y < 0) return null;
        if (pos.X + mySize.X > zone.Data.Size.X) return null;
        if (pos.Y + mySize.Y > zone.Data.Size.Y) return null;

        // Зона должна принимать тег взятого предмета
        if (!zone.Data.Accepts(_dragging.Data)) return null;

        // Ищем всех «соседей» в нашем прямоугольнике
        ItemInstance found = null;

        for (int x = 0; x < mySize.X; x++)
            for (int y = 0; y < mySize.Y; y++)
            {
                var occupant = zone.Grid[pos.X + x, pos.Y + y];
                if (occupant == null) continue;   // пустая клетка — ок

                if (found == null)
                {
                    found = occupant;             // первый найденный — кандидат
                }
                else if (found != occupant)
                {
                    return null;                  // нашли ДРУГОЙ предмет → нельзя
                }
            }

        if (found == null) return null;           // вообще пусто — сюда не должны попасть
        if (found == _dragging) return null;      // себя не свапаем (защита от багов)

        return found;
    }

    // Возвращает предпросмотр дропа для подсветки клеток
    private DropPreview GetDropPreview(Vector2 mouseLocal)
    {
        var result = new DropPreview { IsActive = false, SwapTarget = null };

        if (_dragging == null) return result;

        if (!TryGetZoneAndCell(mouseLocal, out var zone, out var _))
            return result;

        Vector2I dropPos = GetDropPositionInZone(mouseLocal, zone);

        result.IsActive = true;
        result.Zone = zone;
        result.Position = dropPos;
        result.Rotated = _dragging.Rotated;

        // Сначала проверяем можно ли положить на свободное место
        if (zone.Data.Accepts(_dragging.Data)
            && zone.CanPlace(_dragging.Data, dropPos, _dragging.Rotated))
        {
            result.IsValid = true;
            return result;
        }

        // Если не влезло — пробуем найти swap-цель
        result.SwapTarget = FindSwapTarget(zone, dropPos, _dragging.Rotated);
        result.IsValid = false;

        return result;
    }

    // ==================== ОТРИСОВКА ====================

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

        // === ПОДСВЕТКА КЛЕТОК ПОД КУРСОРОМ ===
        // Рисуется поверх зон и предметов, но под тащимым предметом
        if (_dragging != null)
        {
            var preview = GetDropPreview(_lastMouseLocal);

            if (preview.IsActive)
            {
                // Пиксельные координаты левого-верхнего угла предпросмотра
                Vector2 zoneOrigin = (Vector2)(preview.Zone.Data.Offset * CellSize);
                Vector2 previewPos = zoneOrigin + (Vector2)(preview.Position * CellSize);

                // Эффективный размер с учётом поворота
                Vector2I effSize = preview.Rotated
                    ? new Vector2I(_dragging.Data.Size.Y, _dragging.Data.Size.X)
                    : _dragging.Data.Size;

                Rect2 previewRect = new(previewPos, (Vector2)(effSize * CellSize));

                // Цвета
                Color fill, border;

                if (preview.IsValid)
                {
                    // Свободное место — зелёный
                    fill = HighlightValidColor;
                    border = HighlightBorderValid;
                }
                else if (preview.SwapTarget != null)
                {
                    // Место занято ровно одним предметом и swap возможен — жёлтый
                    fill = HighlightSwapColor;
                    border = HighlightSwapBorder;
                }
                else
                {
                    // Никак не положить — красный
                    fill = HighlightInvalidColor;
                    border = HighlightBorderInvalid;
                }

                // Рисуем: сначала заливку, потом контур
                DrawRect(previewRect, fill);
                DrawRect(previewRect, border, false, 2);
            }
        }

        // === ТАЩИМЫЙ ПРЕДМЕТ — рисуем ПОВЕРХ всего в самом конце ===
        if (_dragging != null)
        {
            Vector2 texSize = _dragging.Data.Icon.GetSize();

            // Позиция левого-верхнего угла иконки на экране
            Vector2 dragTopLeftInCells = _lastMouseLocal / CellSize - _dragOffsetInCells;
            Vector2 center = (dragTopLeftInCells * CellSize)
                             + (Vector2)(_dragging.EffectiveSize * CellSize) * 0.5f;

            if (_dragging.Rotated)
            {
                DrawSetTransform(center, Mathf.Pi / 2, Vector2.One);
                DrawTextureRect(_dragging.Data.Icon,
                    new Rect2(-texSize * 0.5f, texSize), false);
                DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            }
            else
            {
                Vector2 topLeft = center - texSize * 0.5f;
                DrawTextureRect(_dragging.Data.Icon,
                    new Rect2(topLeft, texSize), false);
            }
        }
    }
}