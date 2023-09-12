namespace SUCH.Player;

public partial class PigmaskPawn
{
	public enum PigRanks
	{
		Ensign,
		Captain,
		Major,
		Colonel
	}

	[Net] public PigRanks PigRank { get; set; }

	public void SetRank( PigRanks newRank ) => PigRank = newRank;

	public void IncrementRank()
	{
		if ( PigRank == PigRanks.Colonel ) return;

		PigRank++;
	}

	public void ResetRank() => PigRank = PigRanks.Ensign;

	[ClientRpc]
	public void DoRankSpawnSound()
	{
		GameTask.DelaySeconds( 0.1f );

		string sndName = "";

		switch ( PigRank )
		{
			case PigRanks.Ensign: sndName = "ensign_spawn"; break;
			case PigRanks.Captain: sndName = "captain_spawn"; break;
			case PigRanks.Major: sndName = "major_spawn"; break;
			case PigRanks.Colonel: sndName = "colonel_spawn"; break;
		}

		Sound.FromScreen( sndName );
	}
}
