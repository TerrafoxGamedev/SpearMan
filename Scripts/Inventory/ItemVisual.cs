using Godot;

public static class ItemVisual
{
	const float MinCollisionSize = 0.01f;

	public static Node3D BuildCentered(ItemData data)
	{
		Node3D content = BuildRaw(data);
		var root = new Node3D();
		root.AddChild(content);
		if (TryAabbIn(root, out Aabb box))
			content.Position -= box.GetCenter();
		return root;
	}

	// Форма и размер берутся из шаблона. Ноль и минус по оси становятся 1 см, чтобы тело не провалилось.
	public static void AddCollision(CollisionObject3D body, ItemData data)
	{
		Vector3 size = CollisionExtent(data);
		switch (data?.CollisionShape ?? ItemCollisionShape.Cube)
		{
			case ItemCollisionShape.Sphere:
				body.AddChild(ScaledShape(new SphereShape3D { Radius = 0.5f }, size));
				return;
			case ItemCollisionShape.Cylinder:
				body.AddChild(ScaledShape(new CylinderShape3D { Radius = 0.5f, Height = 1f }, size));
				return;
			case ItemCollisionShape.OpenCube:
				AddOpenCube(body, size);
				return;
			case ItemCollisionShape.OpenCylinder:
				AddOpenCylinder(body, size);
				return;
			default:
				body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
				return;
		}
	}

	// Дно и четыре стенки. Верхней грани нет, внутрь можно уронить другой предмет.
	static void AddOpenCube(CollisionObject3D body, Vector3 size)
	{
		float t = WallThickness(size);
		float wallH = Mathf.Max(size.Y - t, MinCollisionSize);
		float wallY = t * 0.5f;
		AddBox(body, new Vector3(size.X, t, size.Z), new Vector3(0f, -size.Y * 0.5f + t * 0.5f, 0f));
		AddBox(body, new Vector3(t, wallH, size.Z), new Vector3(-size.X * 0.5f + t * 0.5f, wallY, 0f));
		AddBox(body, new Vector3(t, wallH, size.Z), new Vector3(size.X * 0.5f - t * 0.5f, wallY, 0f));
		AddBox(body, new Vector3(size.X, wallH, t), new Vector3(0f, wallY, -size.Z * 0.5f + t * 0.5f));
		AddBox(body, new Vector3(size.X, wallH, t), new Vector3(0f, wallY, size.Z * 0.5f - t * 0.5f));
	}

	// Дно-диск и кольцо из стенок. Цилиндр по-прежнему стоит вдоль Y.
	static void AddOpenCylinder(CollisionObject3D body, Vector3 size)
	{
		float t = WallThickness(size);
		float rx = size.X * 0.5f;
		float rz = size.Z * 0.5f;
		var floor = ScaledShape(new CylinderShape3D { Radius = 0.5f, Height = 1f }, new Vector3(size.X, t, size.Z));
		floor.Position = new Vector3(0f, -size.Y * 0.5f + t * 0.5f, 0f);
		body.AddChild(floor);

		float wallH = Mathf.Max(size.Y - t, MinCollisionSize);
		float wallY = t * 0.5f;
		const int segments = 12;
		float step = Mathf.Tau / segments;
		for (int i = 0; i < segments; i++)
		{
			float angle = i * step;
			Vector3 mid = CylinderMid(rx, rz, t, angle, wallY);
			Vector3 next = CylinderMid(rx, rz, t, angle + step, wallY);
			Vector3 previous = CylinderMid(rx, rz, t, angle - step, wallY);
			Vector3 tangent = next - previous;
			tangent.Y = 0f;
			if (tangent.LengthSquared() < 0.000001f)
				tangent = Vector3.Right;
			tangent = tangent.Normalized();

			Vector3 outward = new Vector3(mid.X, 0f, mid.Z);
			if (outward.LengthSquared() < 0.000001f)
				outward = Vector3.Forward;
			outward = outward.Normalized();
			Vector3 side = Vector3.Up.Cross(tangent).Normalized();
			if (side.Dot(outward) < 0f)
				side = -side;

			float depth = Mathf.Max(t, MinCollisionSize);
			float width = mid.DistanceTo(next) * 1.2f;
			var wall = new CollisionShape3D
			{
				Shape = new BoxShape3D { Size = new Vector3(width, wallH, depth) },
				Transform = new Transform3D(new Basis(tangent, Vector3.Up, side), mid)
			};
			body.AddChild(wall);
		}
	}

	static Vector3 CylinderMid(float rx, float rz, float thickness, float angle, float y)
	{
		float cos = Mathf.Cos(angle);
		float sin = Mathf.Sin(angle);
		return new Vector3((rx - thickness * 0.5f) * cos, y, (rz - thickness * 0.5f) * sin);
	}

	static float WallThickness(Vector3 size)
	{
		float smallest = Mathf.Min(size.X, Mathf.Min(size.Y, size.Z));
		float thickness = smallest * 0.15f;
		float cap = smallest * 0.35f;
		return Mathf.Clamp(thickness, MinCollisionSize, Mathf.Max(cap, MinCollisionSize));
	}

	static CollisionShape3D ScaledShape(Shape3D shape, Vector3 scale)
	{
		return new CollisionShape3D { Shape = shape, Scale = scale };
	}

	static void AddBox(CollisionObject3D body, Vector3 size, Vector3 position)
	{
		body.AddChild(new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = size },
			Position = position
		});
	}

	public static Vector3 CollisionExtent(ItemData data)
	{
		Vector3 size = data?.CollisionSize ?? new Vector3(0.25f, 0.25f, 0.25f);
		return new Vector3(
			Mathf.Max(Mathf.Abs(size.X), MinCollisionSize),
			Mathf.Max(Mathf.Abs(size.Y), MinCollisionSize),
			Mathf.Max(Mathf.Abs(size.Z), MinCollisionSize));
	}

	public static bool TryAabbIn(Node3D space, out Aabb merged)
	{
		merged = default;
		bool any = false;
		foreach (Node child in space.GetChildren())
			Collect(child, Transform3D.Identity, ref merged, ref any);
		return any;
	}

	static void Collect(Node node, Transform3D parentToSpace, ref Aabb merged, ref bool any)
	{
		Transform3D toSpace = parentToSpace;
		if (node is Node3D spatial)
			toSpace = parentToSpace * spatial.Transform;

		if (node is MeshInstance3D mesh && mesh.Mesh != null)
		{
			Aabb box = TransformAabb(toSpace, mesh.GetAabb());
			merged = any ? merged.Merge(box) : box;
			any = true;
		}

		foreach (Node child in node.GetChildren())
			Collect(child, toSpace, ref merged, ref any);
	}

	static Aabb TransformAabb(Transform3D transform, Aabb aabb)
	{
		Vector3 origin = aabb.Position;
		Vector3 size = aabb.Size;
		Vector3 min = transform * origin;
		Vector3 max = min;
		Include(transform * (origin + new Vector3(size.X, 0f, 0f)), ref min, ref max);
		Include(transform * (origin + new Vector3(0f, size.Y, 0f)), ref min, ref max);
		Include(transform * (origin + new Vector3(0f, 0f, size.Z)), ref min, ref max);
		Include(transform * (origin + new Vector3(size.X, size.Y, 0f)), ref min, ref max);
		Include(transform * (origin + new Vector3(size.X, 0f, size.Z)), ref min, ref max);
		Include(transform * (origin + new Vector3(0f, size.Y, size.Z)), ref min, ref max);
		Include(transform * (origin + size), ref min, ref max);
		return new Aabb(min, max - min);
	}

	static void Include(Vector3 point, ref Vector3 min, ref Vector3 max)
	{
		min = new Vector3(Mathf.Min(min.X, point.X), Mathf.Min(min.Y, point.Y), Mathf.Min(min.Z, point.Z));
		max = new Vector3(Mathf.Max(max.X, point.X), Mathf.Max(max.Y, point.Y), Mathf.Max(max.Z, point.Z));
	}

	static Node3D BuildRaw(ItemData data)
	{
		if (data?.Model != null)
		{
			Node instanced = data.Model.Instantiate();
			if (instanced is Node3D node && ContainsMesh(node))
				return node;
			instanced?.Free();
		}

		return CreateGrayCube();
	}

	static bool ContainsMesh(Node node)
	{
		if (node is MeshInstance3D mesh && mesh.Mesh != null)
			return true;
		foreach (Node child in node.GetChildren())
		{
			if (ContainsMesh(child))
				return true;
		}
		return false;
	}

	static MeshInstance3D CreateGrayCube()
	{
		var material = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.58f, 0.58f, 0.58f),
			Roughness = 1f,
			Metallic = 0f
		};
		var mesh = new BoxMesh { Size = new Vector3(0.22f, 0.22f, 0.22f) };
		return new MeshInstance3D { Mesh = mesh, MaterialOverride = material };
	}
}
