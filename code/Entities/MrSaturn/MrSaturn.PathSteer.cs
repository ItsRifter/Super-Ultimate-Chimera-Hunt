public class NPCPathSteer
{
	protected NPCNavigation Path { get; private set; }

	public bool CanNavigate { get; set; } = true;

	public NPCPathSteer()
	{
		Path = new NPCNavigation();
	}

	public virtual void Tick( Vector3 position )
	{
		if ( Target.IsNearlyZero() || Target.IsNaN || !CanNavigate )
			return;

		Path.Update( position, Target );

		Output.Direction = Path.GetDirection( position );

		if ( Path.IsEmpty )
		{
			FindNewTarget( position );
		}
	}

	public virtual bool FindNewTarget( Vector3 center )
	{
		var t = NavMesh.GetPointWithinRadius( center, 100, 250 );
		if ( t.HasValue )
		{
			Target = t.Value;
		}

		return t.HasValue;
	}


	public Vector3 Target { get; set; }

	public NavSteerOutput Output;

	public struct NavSteerOutput
	{
		public bool Finished;
		public Vector3 Direction;
	}
}
