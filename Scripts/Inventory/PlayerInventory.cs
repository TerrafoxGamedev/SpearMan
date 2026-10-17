using Godot;

public partial class PlayerInventory : Node
{
	const string BackpackPath = "res://Data/Containers/backpack.tres";

	CanvasLayer _layer;
	InventoryUI _ui;
	bool _open;

	public override void _Ready()
	{
		var data = GD.Load<InventoryContainerData>(BackpackPath);
		if (data == null)
		{
			GD.PushError($"Не найден рюкзак {BackpackPath}");
			return;
		}

		var container = new InventoryContainer(data);
		Give(container, "res://Data/Items/apple.tres", 4);
		Give(container, "res://Data/Items/travel_pot.tres", 1);
		Give(container, "res://Data/Items/peasant_pitchfork.tres", 1);

		var preview = new ItemPreviewRenderer();
		AddChild(preview);

		_layer = new CanvasLayer { Layer = 20, Visible = false };
		AddChild(_layer);

		var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		_layer.AddChild(center);
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		_ui = new InventoryUI { MouseFilter = Control.MouseFilterEnum.Stop };
		center.AddChild(_ui);
		_ui.Bind(container, data.Icon);
		_ui.SetOwnerPlayer(GetParent() as Node3D);
		_ui.SetPreview(preview);
		preview.Watch(_ui);
		preview.Prefetch(container, _ui.CellSize);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_ui == null)
			return;
		if (@event is not InputEventKey key || !key.Pressed || key.Echo)
			return;
		if (key.Keycode != Key.I && key.PhysicalKeycode != Key.I)
			return;

		if (_open)
			Close();
		else
			Open();
		GetViewport().SetInputAsHandled();
	}

	void Open()
	{
		_open = true;
		_layer.Visible = true;
		_ui.QueueRedraw();
	}

	void Close()
	{
		_ui.CancelDrag();
		_open = false;
		_layer.Visible = false;
	}

	static void Give(InventoryContainer container, string path, int count)
	{
		var data = GD.Load<ItemData>(path);
		if (data == null)
		{
			GD.PushWarning($"Не найден предмет {path}");
			return;
		}

		var item = new ItemInstance(data) { Count = count };
		if (!container.TryAddItem(item))
			GD.PushWarning($"Нет места для {data.DisplayName}");
	}
}
