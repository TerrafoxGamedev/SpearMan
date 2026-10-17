using Godot;

[GlobalClass]
public partial class PlayerCharacter : CharacterBody3D
{
	const float WalkSpeed = 1.5f;
	const float Gravity = 9.8f;

	// Ось X ноги качает её назад. Положительный угол разгибает колено назад.
	// Ось Z руки качает вперёд-назад. Поза кости — это покой, домноженный на шаг.
	static readonly Vector3 BendAxis = Vector3.Right;
	static readonly Vector3 SwingAxis = new(0f, 0f, 1f);

	Skeleton3D _skeleton;
	float _cycle;
	float _weight;

	int _hips = -1;
	int _leftUpperLeg = -1;
	int _leftLowerLeg = -1;
	int _leftFoot = -1;
	int _rightUpperLeg = -1;
	int _rightLowerLeg = -1;
	int _rightFoot = -1;
	int _leftUpperArm = -1;
	int _leftLowerArm = -1;
	int _rightUpperArm = -1;
	int _rightLowerArm = -1;

	public override void _Ready()
	{
		FloorSnapLength = 0.2f;
		AddChild(new PlayerInventory());
		_skeleton = FindSkeleton(this);
		if (_skeleton == null)
			return;

		_hips = _skeleton.FindBone("Hips");
		_leftUpperLeg = _skeleton.FindBone("LeftUpperLeg");
		_leftLowerLeg = _skeleton.FindBone("LeftLowerLeg");
		_leftFoot = _skeleton.FindBone("LeftFoot");
		_rightUpperLeg = _skeleton.FindBone("RightUpperLeg");
		_rightLowerLeg = _skeleton.FindBone("RightLowerLeg");
		_rightFoot = _skeleton.FindBone("RightFoot");
		_leftUpperArm = _skeleton.FindBone("LeftUpperArm");
		_leftLowerArm = _skeleton.FindBone("LeftLowerArm");
		_rightUpperArm = _skeleton.FindBone("RightUpperArm");
		_rightLowerArm = _skeleton.FindBone("RightLowerArm");
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		Vector3 direction = ReadDirection();
		Vector3 velocity = Velocity;
		if (!IsOnFloor())
			velocity.Y -= Gravity * step;

		velocity.X = direction.X * WalkSpeed;
		velocity.Z = direction.Z * WalkSpeed;
		Velocity = velocity;

		if (direction.LengthSquared() > 0.0001f)
		{
			Vector3 look = GlobalPosition + direction;
			look.Y = GlobalPosition.Y;
			LookAt(look, Vector3.Up);
		}

		MoveAndSlide();
		Vector3 horizontal = new(Velocity.X, 0f, Velocity.Z);
		Animate(step, horizontal.Length());
	}

	Vector3 ReadDirection()
	{
		float x = 0f;
		float y = 0f;
		if (Input.IsKeyPressed(Key.D))
			x += 1f;
		if (Input.IsKeyPressed(Key.A))
			x -= 1f;
		if (Input.IsKeyPressed(Key.W))
			y += 1f;
		if (Input.IsKeyPressed(Key.S))
			y -= 1f;
		if (x == 0f && y == 0f)
			return Vector3.Zero;

		Camera3D camera = GetViewport().GetCamera3D();
		Vector3 forward = Vector3.Forward;
		Vector3 right = Vector3.Right;
		if (camera != null)
		{
			forward = -camera.GlobalBasis.Z;
			right = camera.GlobalBasis.X;
			forward.Y = 0f;
			right.Y = 0f;
		}

		if (forward.LengthSquared() < 0.0001f)
			forward = Vector3.Forward;
		if (right.LengthSquared() < 0.0001f)
			right = Vector3.Right;

		Vector3 direction = right.Normalized() * x + forward.Normalized() * y;
		if (direction.LengthSquared() > 1f)
			direction = direction.Normalized();
		return direction;
	}

	void Animate(float step, float speed)
	{
		if (_skeleton == null)
			return;

		float moving = speed > 0.05f ? 1f : 0f;
		_weight = Mathf.MoveToward(_weight, moving, step * 6f);
		if (speed > 0.05f)
			_cycle += step * speed * 8f;

		float swing = Mathf.Sin(_cycle) * _weight;
		float leftLift = Mathf.Max(0f, Mathf.Sin(_cycle)) * _weight;
		float rightLift = Mathf.Max(0f, -Mathf.Sin(_cycle)) * _weight;
		float leftThigh = -swing * 0.45f;
		float rightThigh = swing * 0.45f;
		float leftKnee = -leftLift * 0.7f;
		float rightKnee = -rightLift * 0.7f;

		SetBend(_leftUpperLeg, leftThigh);
		SetBend(_rightUpperLeg, rightThigh);
		SetBend(_leftLowerLeg, leftKnee);
		SetBend(_rightLowerLeg, rightKnee);
		// Локальная ось X ступни смотрит в обратную сторону, поэтому компенсация совпадает по знаку с ногой.
		SetBend(_leftFoot, leftThigh + leftKnee);
		SetBend(_rightFoot, rightThigh + rightKnee);

		// Плечи на этой сетке рвутся от большого поворота, поэтому руки только качаются.
		Quaternion armSwing = new Quaternion(SwingAxis, swing * 0.3f);
		Quaternion elbow = new Quaternion(BendAxis, 0.15f * _weight);
		SetLocal(_leftUpperArm, armSwing);
		SetLocal(_rightUpperArm, armSwing);
		SetLocal(_leftLowerArm, elbow);
		SetLocal(_rightLowerArm, elbow);

		if (_hips >= 0)
		{
			float bob = Mathf.Abs(Mathf.Sin(_cycle)) * 0.02f * _weight;
			Vector3 hipsRest = _skeleton.GetBoneRest(_hips).Origin;
			_skeleton.SetBonePosePosition(_hips, hipsRest + new Vector3(0f, bob, 0f));
		}
	}

	void SetBend(int bone, float angle)
	{
		SetLocal(bone, new Quaternion(BendAxis, angle));
	}

	void SetLocal(int bone, Quaternion rotation)
	{
		if (bone < 0)
			return;
		Quaternion rest = _skeleton.GetBoneRest(bone).Basis.GetRotationQuaternion();
		_skeleton.SetBonePoseRotation(bone, rest * rotation);
	}

	static Skeleton3D FindSkeleton(Node node)
	{
		if (node is Skeleton3D skeleton)
			return skeleton;
		foreach (Node child in node.GetChildren())
		{
			Skeleton3D found = FindSkeleton(child);
			if (found != null)
				return found;
		}
		return null;
	}
}
