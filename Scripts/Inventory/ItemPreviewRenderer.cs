using System.Collections.Generic;
using Godot;

public partial class ItemPreviewRenderer : Node
{
	readonly Dictionary<string, Texture2D> _cache = new();
	readonly HashSet<string> _pending = new();
	readonly Queue<Job> _queue = new();

	SubViewport _viewport;
	Node3D _stage;
	Node3D _pivot;
	Camera3D _camera;
	InventoryUI _ui;
	Job _job;
	int _wait;
	int _attempts;

	readonly struct Job
	{
		public Job(string key, ItemData data, int cellSize)
		{
			Key = key;
			Data = data;
			CellSize = cellSize;
		}

		public string Key { get; }
		public ItemData Data { get; }
		public int CellSize { get; }
	}

	public void Watch(InventoryUI ui)
	{
		_ui = ui;
	}

	public void Prefetch(InventoryContainer container, int cellSize)
	{
		foreach (InventoryZone zone in container.Zones)
		{
			foreach (ItemInstance item in zone.Items)
				Request(item.Data, cellSize);
		}
	}

	public Texture2D Request(ItemData data, int cellSize)
	{
		if (data == null)
			return null;

		string key = KeyFor(data, cellSize);
		if (_cache.TryGetValue(key, out Texture2D ready))
			return ready;
		if (_pending.Add(key))
			_queue.Enqueue(new Job(key, data, cellSize));
		return null;
	}

	public override void _Ready()
	{
		_viewport = new SubViewport
		{
			OwnWorld3D = true,
			TransparentBg = true,
			HandleInputLocally = false,
			Size = new Vector2I(64, 64),
			RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled
		};
		AddChild(_viewport);

		var environment = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Canvas,
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = Colors.White,
			AmbientLightEnergy = 0.7f
		};
		_viewport.AddChild(new WorldEnvironment { Environment = environment });

		_stage = new Node3D();
		_viewport.AddChild(_stage);

		_camera = new Camera3D
		{
			Projection = Camera3D.ProjectionType.Orthogonal,
			Current = true,
			Near = 0.01f,
			Far = 200f
		};
		_stage.AddChild(_camera);

		var keyLight = new DirectionalLight3D { RotationDegrees = new Vector3(-48f, -32f, 0f), LightEnergy = 1.3f };
		var fillLight = new DirectionalLight3D { RotationDegrees = new Vector3(-18f, 150f, 0f), LightEnergy = 0.45f };
		_stage.AddChild(keyLight);
		_stage.AddChild(fillLight);

		_pivot = new Node3D();
		_stage.AddChild(_pivot);
	}

	public override void _Process(double delta)
	{
		if (_wait > 0)
		{
			_wait--;
			if (_wait > 0)
				return;
			Capture();
		}

		if (_queue.Count == 0 || _viewport == null)
			return;

		_job = _queue.Dequeue();
		Begin(_job);
		_wait = 2;
	}

	void Begin(Job job)
	{
		Vector2I pixels = PixelSize(job.Data, job.CellSize);
		_viewport.Size = pixels;
		ClearPivot();

		Node3D visual = ItemVisual.BuildCentered(job.Data);
		_pivot.Rotation = Vector3.Zero;
		_pivot.AddChild(visual);
		// Сверху это 45° против часовой. Модель остаётся стоймя в клетке, видны перед и бок.
		_pivot.Rotation = new Vector3(0f, Mathf.Pi / 4f, 0f);
		FrameCamera();
		_viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
	}

	void Capture()
	{
		Image image = _viewport.GetTexture().GetImage();
		_viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
		ClearPivot();

		if (HasPixels(image))
		{
			_cache[_job.Key] = ImageTexture.CreateFromImage(image);
			_pending.Remove(_job.Key);
			_attempts = 0;
			_ui?.QueueRedraw();
			return;
		}

		_attempts++;
		if (_attempts < 6)
		{
			_queue.Enqueue(_job);
			return;
		}

		Vector2I pixels = PixelSize(_job.Data, _job.CellSize);
		_cache[_job.Key] = FlatGray(pixels);
		_pending.Remove(_job.Key);
		_attempts = 0;
		GD.PushWarning($"Не удалось снять проекцию предмета {_job.Data?.Id}");
		_ui?.QueueRedraw();
	}

	void FrameCamera()
	{
		if (!ItemVisual.TryAabbIn(_stage, out Aabb box))
			box = new Aabb(new Vector3(-0.2f, -0.2f, -0.2f), new Vector3(0.4f, 0.4f, 0.4f));

		Vector3 center = box.GetCenter();
		Vector3 view = new Vector3(0f, 0.42f, 1f).Normalized();
		_camera.Position = center + view * (box.Size.Length() + 2f);
		_camera.LookAt(_stage.ToGlobal(center), Vector3.Up);

		float halfWidth = 0.001f;
		float halfHeight = 0.001f;
		foreach (Vector3 corner in Corners(box))
		{
			Vector3 local = _camera.ToLocal(_stage.ToGlobal(corner));
			halfWidth = Mathf.Max(halfWidth, Mathf.Abs(local.X));
			halfHeight = Mathf.Max(halfHeight, Mathf.Abs(local.Y));
		}

		float aspect = _viewport.Size.X / (float)Mathf.Max(_viewport.Size.Y, 1);
		float height = Mathf.Max(halfHeight * 2f, halfWidth * 2f / Mathf.Max(aspect, 0.001f));
		_camera.Size = height * 1.12f;
	}

	static Vector3[] Corners(Aabb box)
	{
		Vector3 origin = box.Position;
		Vector3 size = box.Size;
		return new[]
		{
			origin,
			origin + new Vector3(size.X, 0f, 0f),
			origin + new Vector3(0f, size.Y, 0f),
			origin + new Vector3(0f, 0f, size.Z),
			origin + new Vector3(size.X, size.Y, 0f),
			origin + new Vector3(size.X, 0f, size.Z),
			origin + new Vector3(0f, size.Y, size.Z),
			origin + size
		};
	}

	void ClearPivot()
	{
		foreach (Node child in _pivot.GetChildren())
			child.Free();
	}

	static bool HasPixels(Image image)
	{
		if (image == null || image.IsEmpty())
			return false;

		int stepX = Mathf.Max(1, image.GetWidth() / 32);
		int stepY = Mathf.Max(1, image.GetHeight() / 32);
		for (int y = 0; y < image.GetHeight(); y += stepY)
		{
			for (int x = 0; x < image.GetWidth(); x += stepX)
			{
				if (image.GetPixel(x, y).A > 0.04f)
					return true;
			}
		}
		return false;
	}

	static Texture2D FlatGray(Vector2I size)
	{
		Image image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
		image.Fill(new Color(0.58f, 0.58f, 0.58f));
		return ImageTexture.CreateFromImage(image);
	}

	static Vector2I PixelSize(ItemData data, int cellSize)
	{
		int cellsX = Mathf.Max(data?.Size.X ?? 1, 1);
		int cellsY = Mathf.Max(data?.Size.Y ?? 1, 1);
		int pixels = Mathf.Max(cellSize, 32);
		return new Vector2I(
			Mathf.Clamp(cellsX * pixels, 16, 512),
			Mathf.Clamp(cellsY * pixels, 16, 512));
	}

	static string KeyFor(ItemData data, int cellSize)
	{
		Vector2I pixels = PixelSize(data, cellSize);
		string item = string.IsNullOrEmpty(data.ResourcePath) ? data.Id : data.ResourcePath;
		string model = data.Model?.ResourcePath ?? "cube";
		return $"{item}|{model}|{pixels.X}x{pixels.Y}";
	}
}
