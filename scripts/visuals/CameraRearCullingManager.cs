using Godot;
using System.Collections.Generic;

public partial class CameraRearCullingManager : Node
{
	private const string ManagerNodeName = "CameraRearCullingManager";
	private const double MovingCheckIntervalSeconds = 1.0 / 15.0;
	private const float CameraPositionThresholdSquared = 0.0004f;
	private const float CameraDirectionDotThreshold = 0.99998f;
	private static readonly Dictionary<ulong, CameraRearCullingManager> PendingManagers = new();

	private sealed class CullEntry
	{
		public Node3D Target;
		public Aabb LocalBounds;
		public float Margin;
		public bool IsCulled;
		public bool VisibleBeforeCull;
	}

	private readonly Dictionary<ulong, CullEntry> _entries = new();
	private readonly List<ulong> _expiredEntries = new();
	private ulong _pendingSceneRootId;
	private ulong _lastCameraInstanceId;
	private Vector3 _lastCameraPosition;
	private Vector3 _lastCameraForward;
	private double _elapsedSinceCullCheck = MovingCheckIntervalSeconds;
	private bool _hasCameraState;
	private bool _entriesChanged = true;

	public static void Configure(Node3D target, bool enabled, float margin)
	{
		if (target == null || !GodotObject.IsInstanceValid(target))
			return;

		CameraRearCullingManager manager = FindManager(target);

		if (!enabled)
		{
			manager?.Unregister(target);
			return;
		}

		if (!target.IsInsideTree())
			return;

		manager ??= CreateManager(target);
		manager?.Register(target, margin);
	}

	public override void _EnterTree()
	{
		if (_pendingSceneRootId == 0)
			return;

		if (PendingManagers.TryGetValue(
			_pendingSceneRootId,
			out CameraRearCullingManager pendingManager) &&
			ReferenceEquals(pendingManager, this))
		{
			PendingManagers.Remove(_pendingSceneRootId);
		}

		_pendingSceneRootId = 0;
	}

	public override void _Process(double delta)
	{
		if (_entries.Count == 0)
			return;

		_elapsedSinceCullCheck += delta;
		Camera3D camera = GetViewport()?.GetCamera3D();
		if (camera == null)
		{
			if (_hasCameraState)
				RestoreAllEntries();

			_hasCameraState = false;
			_elapsedSinceCullCheck = 0.0;
			return;
		}

		Vector3 cameraForward = -camera.GlobalBasis.Z.Normalized();
		Vector3 cameraPosition = camera.GlobalPosition;
		ulong cameraInstanceId = camera.GetInstanceId();
		bool cameraChanged =
			!_hasCameraState ||
			cameraInstanceId != _lastCameraInstanceId ||
			cameraPosition.DistanceSquaredTo(_lastCameraPosition) >=
				CameraPositionThresholdSquared ||
			cameraForward.Dot(_lastCameraForward) <=
				CameraDirectionDotThreshold;
		if (!_entriesChanged && !cameraChanged)
			return;
		if (!_entriesChanged &&
			_elapsedSinceCullCheck < MovingCheckIntervalSeconds)
			return;

		_lastCameraInstanceId = cameraInstanceId;
		_lastCameraPosition = cameraPosition;
		_lastCameraForward = cameraForward;
		_elapsedSinceCullCheck = 0.0;
		_hasCameraState = true;
		_entriesChanged = false;
		_expiredEntries.Clear();

		foreach ((ulong instanceId, CullEntry entry) in _entries)
		{
			if (!GodotObject.IsInstanceValid(entry.Target) ||
				!entry.Target.IsInsideTree())
			{
				_expiredEntries.Add(instanceId);
				continue;
			}

			bool shouldCull = IsEntirelyBehindCamera(
				entry,
				cameraForward,
				cameraPosition);
			ApplyCullState(entry, shouldCull);
		}

		foreach (ulong instanceId in _expiredEntries)
			_entries.Remove(instanceId);

		if (_entries.Count == 0)
			SetProcess(false);
	}

	private static CameraRearCullingManager FindManager(Node context)
	{
		if (!context.IsInsideTree())
			return null;

		Node sceneRoot = context.GetTree().CurrentScene ?? context.GetTree().Root;
		CameraRearCullingManager manager =
			sceneRoot.GetNodeOrNull<CameraRearCullingManager>(ManagerNodeName);
		return manager ?? FindPendingManager(sceneRoot);
	}

	private static CameraRearCullingManager CreateManager(Node context)
	{
		Node sceneRoot = context.GetTree().CurrentScene ?? context.GetTree().Root;
		if (sceneRoot.GetNodeOrNull(ManagerNodeName) != null)
			return null;

		CameraRearCullingManager pendingManager = FindPendingManager(sceneRoot);
		if (pendingManager != null)
			return pendingManager;

		ulong sceneRootId = sceneRoot.GetInstanceId();

		CameraRearCullingManager manager = new()
		{
			Name = ManagerNodeName,
			_pendingSceneRootId = sceneRootId
		};
		PendingManagers.Add(sceneRootId, manager);
		sceneRoot.CallDeferred(Node.MethodName.AddChild, manager);
		return manager;
	}

	private static CameraRearCullingManager FindPendingManager(Node sceneRoot)
	{
		ulong sceneRootId = sceneRoot.GetInstanceId();
		if (!PendingManagers.TryGetValue(
			sceneRootId,
			out CameraRearCullingManager manager))
		{
			return null;
		}

		if (GodotObject.IsInstanceValid(manager))
			return manager;

		PendingManagers.Remove(sceneRootId);
		return null;
	}

	private void Register(Node3D target, float margin)
	{
		if (!TryGetLocalBounds(target, out Aabb localBounds))
			return;

		ulong instanceId = target.GetInstanceId();
		if (_entries.TryGetValue(instanceId, out CullEntry entry))
		{
			entry.LocalBounds = localBounds;
			entry.Margin = Mathf.Max(margin, 0.0f);
			_entriesChanged = true;
			SetProcess(true);
			return;
		}

		_entries.Add(instanceId, new CullEntry
		{
			Target = target,
			LocalBounds = localBounds,
			Margin = Mathf.Max(margin, 0.0f)
		});
		_entriesChanged = true;
		SetProcess(true);
	}

	private void Unregister(Node3D target)
	{
		ulong instanceId = target.GetInstanceId();
		if (!_entries.Remove(instanceId, out CullEntry entry))
			return;

		RestoreEntry(entry);
		if (_entries.Count == 0)
			SetProcess(false);
	}

	private static bool IsEntirelyBehindCamera(
		CullEntry entry,
		Vector3 cameraForward,
		Vector3 cameraPosition)
	{
		Transform3D targetTransform = entry.Target.GlobalTransform;
		Vector3 worldCenter = targetTransform * entry.LocalBounds.GetCenter();
		Vector3 halfSize = entry.LocalBounds.Size * 0.5f;
		Basis basis = targetTransform.Basis;
		float projectedExtent =
			Mathf.Abs(cameraForward.Dot(basis.X * halfSize.X)) +
			Mathf.Abs(cameraForward.Dot(basis.Y * halfSize.Y)) +
			Mathf.Abs(cameraForward.Dot(basis.Z * halfSize.Z));
		float furthestForwardPoint =
			cameraForward.Dot(worldCenter - cameraPosition) + projectedExtent;

		return furthestForwardPoint < -entry.Margin;
	}

	private static void ApplyCullState(CullEntry entry, bool shouldCull)
	{
		if (shouldCull)
		{
			if (!entry.IsCulled)
			{
				entry.VisibleBeforeCull = entry.Target.Visible;
				entry.IsCulled = true;
			}

			entry.Target.Visible = false;
			return;
		}

		RestoreEntry(entry);
	}

	private static void RestoreEntry(CullEntry entry)
	{
		if (!entry.IsCulled ||
			!GodotObject.IsInstanceValid(entry.Target))
		{
			return;
		}

		entry.Target.Visible = entry.VisibleBeforeCull;
		entry.IsCulled = false;
	}

	private void RestoreAllEntries()
	{
		foreach (CullEntry entry in _entries.Values)
			RestoreEntry(entry);
	}

	private static bool TryGetLocalBounds(Node3D target, out Aabb bounds)
	{
		bool hasBounds = false;
		Vector3 minimum = Vector3.Zero;
		Vector3 maximum = Vector3.Zero;
		Transform3D worldToTarget = target.GlobalTransform.AffineInverse();

		CollectGeometryBounds(
			target,
			worldToTarget,
			ref hasBounds,
			ref minimum,
			ref maximum);

		bounds = hasBounds
			? new Aabb(minimum, maximum - minimum)
			: default;
		return hasBounds;
	}

	private static void CollectGeometryBounds(
		Node node,
		Transform3D worldToTarget,
		ref bool hasBounds,
		ref Vector3 minimum,
		ref Vector3 maximum)
	{
		if (node is GeometryInstance3D geometry)
		{
			Aabb geometryBounds = geometry.GetAabb();
			Transform3D geometryToTarget =
				worldToTarget * geometry.GlobalTransform;
			Vector3 boundsEnd = geometryBounds.End;

			for (int x = 0; x < 2; x++)
			{
				for (int y = 0; y < 2; y++)
				{
					for (int z = 0; z < 2; z++)
					{
						Vector3 corner = geometryToTarget * new Vector3(
							x == 0 ? geometryBounds.Position.X : boundsEnd.X,
							y == 0 ? geometryBounds.Position.Y : boundsEnd.Y,
							z == 0 ? geometryBounds.Position.Z : boundsEnd.Z);
						ExpandBounds(
							corner,
							ref hasBounds,
							ref minimum,
							ref maximum);
					}
				}
			}
		}

		foreach (Node child in node.GetChildren())
		{
			CollectGeometryBounds(
				child,
				worldToTarget,
				ref hasBounds,
				ref minimum,
				ref maximum);
		}
	}

	private static void ExpandBounds(
		Vector3 point,
		ref bool hasBounds,
		ref Vector3 minimum,
		ref Vector3 maximum)
	{
		if (!hasBounds)
		{
			minimum = point;
			maximum = point;
			hasBounds = true;
			return;
		}

		minimum = new Vector3(
			Mathf.Min(minimum.X, point.X),
			Mathf.Min(minimum.Y, point.Y),
			Mathf.Min(minimum.Z, point.Z));
		maximum = new Vector3(
			Mathf.Max(maximum.X, point.X),
			Mathf.Max(maximum.Y, point.Y),
			Mathf.Max(maximum.Z, point.Z));
	}
}
