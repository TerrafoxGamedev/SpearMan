using Godot;

public partial class WorldItem : RigidBody3D
{
	public ItemData Data;
	public int Count = 1;

	Node3D _player;
	Vector2 _mouse;
	bool _drop;

	public void PrepareDrop(Node3D player, Vector2 mouse)
	{
		_player = player;
		_mouse = mouse;
		_drop = true;
	}

	public override void _Ready()
	{
		CollisionLayer = 1;
		CollisionMask = 1;
		ContinuousCd = true;
		Mass = Data != null && Data.Mass > 0f ? Data.Mass : 1f;
		if (!string.IsNullOrEmpty(Data?.Id))
			Name = $"{Data.Id}_{GetInstanceId()}";

		Node3D visual = ItemVisual.BuildCentered(Data);
		AddChild(visual);
		ItemVisual.AddCollision(this, Data);

		if (_drop && _player != null)
			DropInFront();
	}

	void DropInFront()
	{
		Vector3 forward = Aim(_player, _mouse);
		Vector3 collision = ItemVisual.CollisionExtent(Data);
		float reach = 0.45f + Mathf.Max(collision.X, collision.Z) * 0.5f;
		if (ItemVisual.TryAabbIn(this, out Aabb box))
			reach = Mathf.Max(reach, 0.45f + Mathf.Max(box.Size.X, box.Size.Z) * 0.5f);

		GlobalPosition = _player.GlobalPosition + forward * reach + Vector3.Up * 0.55f;
		ForceUpdateTransform();
		LiftAboveFloor();
		LinearVelocity = forward * 1.1f;
	}

	void LiftAboveFloor()
	{
		World3D world = GetWorld3D();
		if (world == null)
			return;

		Vector3 from = GlobalPosition + Vector3.Up * 8f;
		Vector3 to = GlobalPosition + Vector3.Down * 40f;
		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithAreas = false;
		query.CollideWithBodies = true;
		query.CollisionMask = 1;
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
		if (_player is CollisionObject3D playerBody)
			query.Exclude.Add(playerBody.GetRid());

		Godot.Collections.Dictionary hit = world.DirectSpaceState.IntersectRay(query);
		if (hit.Count == 0)
			return;

		float floorY = hit["position"].AsVector3().Y;
		float lift = floorY + 0.02f - CollisionBottom();
		if (lift > 0f)
			GlobalPosition += new Vector3(0f, lift, 0f);
	}

	float CollisionBottom()
	{
		ForceUpdateTransform();
		float bottom = GlobalPosition.Y;
		bool any = false;
		foreach (Node child in GetChildren())
		{
			if (child is not CollisionShape3D collision)
				continue;
			float candidate = collision.GlobalPosition.Y - HalfHeight(collision);
			if (!any || candidate < bottom)
				bottom = candidate;
			any = true;
		}
		return bottom;
	}

	static float HalfHeight(CollisionShape3D collision)
	{
		float scaleY = Mathf.Abs(collision.Scale.Y);
		switch (collision.Shape)
		{
			case BoxShape3D box:
				return box.Size.Y * 0.5f * scaleY;
			case SphereShape3D sphere:
				return sphere.Radius * scaleY;
			case CylinderShape3D cylinder:
				return cylinder.Height * 0.5f * scaleY;
			default:
				return MinHalfHeight;
		}
	}

	const float MinHalfHeight = 0.005f;

	static Vector3 Aim(Node3D player, Vector2 mouse)
	{
		Vector3 forward = -player.GlobalBasis.Z;
		forward.Y = 0f;
		if (forward.LengthSquared() < 0.0001f)
			forward = Vector3.Forward;
		forward = forward.Normalized();

		Camera3D camera = player.GetViewport()?.GetCamera3D();
		if (camera == null)
			return forward;

		Vector3 origin = camera.ProjectRayOrigin(mouse);
		Vector3 direction = camera.ProjectRayNormal(mouse);
		if (Mathf.Abs(direction.Y) < 0.0001f)
			return forward;

		float t = (player.GlobalPosition.Y - origin.Y) / direction.Y;
		if (t <= 0f)
			return forward;

		Vector3 flat = origin + direction * t - player.GlobalPosition;
		flat.Y = 0f;
		if (flat.LengthSquared() < 0.04f)
			return forward;
		return flat.Normalized();
	}
}
