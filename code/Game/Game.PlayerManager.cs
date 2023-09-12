namespace SUCH;

public partial class SUCHGame
{
	[ConVar.Client( "such.music", Help = "Toggles music, this doesn't affect round and game music at pre/post" )]
	public static bool PlayerMusic { get; set; } = true;

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
			SwapPawn( cl, AssignType.Pigmask );
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

	/// <summary>
	/// Plays music to the player when enabled
	/// </summary>
	/// <param name="music">The music track to play</param>
	[ClientRpc]
	public void PlayMusicToUser(string music)
	{
		if ( !PlayerMusic ) return;

		if ( playing.IsPlaying )
			playing.Stop();

		playing = Sound.FromScreen( music );
	}

	[ClientRpc]
	public void StopMusicToUser()
	{
		if ( playing.IsPlaying )
			playing.Stop();
	}

	/// <summary>
	/// Plays a sound to the client
	/// </summary>
	/// <param name="snd">The sound to play</param>
	[ClientRpc]
	public void PlaySoundToUser(string snd)
	{
		Sound.FromScreen( snd );
	}
}
