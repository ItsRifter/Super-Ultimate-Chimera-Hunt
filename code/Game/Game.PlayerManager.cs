namespace SUCH;

public partial class SUCHGame
{
	public IClient LastChimera;

	public void ResetPlayers()
	{
		foreach ( IClient cl in Game.Clients.ToArray() )
		{
			cl.Pawn?.Delete();
		}
	}

	public void AssignChimera()
	{
		var clients = GetClients().ToArray();

		var chosen = clients.Where( x => x != LastChimera ).OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		LastChimera = chosen;

		var pawn = new ChimeraPawn();
		chosen.Pawn = pawn;
		PlaySoundToUser( To.Single( chosen ), "chimera_spawn" );
	}

	public void AssignPigmasks()
	{
		var clients = GetClients().Where(x => x != LastChimera).ToArray();

		foreach ( var cl in clients )
		{
			var pawn = new PigmaskPawn();
			cl.Pawn = pawn;
			pawn.AssignPigRank( cl );

			switch(GetPigRank(cl))
			{
				case PigmaskPawn.PigRankEnum.Ensign:
					PlaySoundToUser( To.Single( cl ), "ensign_spawn");
					break;
				case PigmaskPawn.PigRankEnum.Captain:
					PlaySoundToUser( To.Single( cl ), "captain_spawn");
					break;
				case PigmaskPawn.PigRankEnum.Major:
					PlaySoundToUser( To.Single( cl ), "major_spawn");
					break;
				case PigmaskPawn.PigRankEnum.Colonel:
					PlaySoundToUser( To.Single( cl ), "colonel_spawn");
					break;
			}
		}
	}

	public List<IClient> GetClients()
	{
		List<IClient> players = new List<IClient>();

		foreach ( IClient cl in Game.Clients )
		{
			players.Add(cl);
		}

		return players;
	}

	public List<BasePawn> GetPlayers(Type pawnType)
	{
		List<BasePawn> players = new List<BasePawn>();

		foreach ( IClient cl in Game.Clients )
		{
			if ( cl.Pawn is BasePawn player && player.GetType() == pawnType )
				players.Add( player );
		}

		return players;
	}

	Sound playing;

	[ClientRpc]
	public void PlayMusicToUser(string sndPath)
	{
		if ( playing.IsPlaying )
			playing.Stop();

		playing = Sound.FromScreen( sndPath );
	}

	[ClientRpc]
	public void StopMusicToUser()
	{
		if ( playing.IsPlaying )
			playing.Stop();
	}

	[ClientRpc]
	public void PlaySoundToUser(string sndPath)
	{
		Sound.FromScreen( sndPath );
	}
}
