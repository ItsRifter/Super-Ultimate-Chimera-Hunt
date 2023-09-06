namespace SUCH;

public partial class SUCHGame
{
	public static IDictionary<IClient, PigmaskPawn.PigRankEnum> PigRanks { get; set; }

	public static void UpdatePigRank(IClient cl, PigmaskPawn.PigRankEnum newRank)
	{
		PigRanks[cl] = newRank;
	}

	public static PigmaskPawn.PigRankEnum GetPigRank( IClient cl )
	{
		if ( Game.IsClient )
			return PigmaskPawn.PigRankEnum.Ensign;

		return PigRanks[cl];
	}

	public static void ResetPigRank( IClient cl )
	{
		PigRanks[cl] = PigmaskPawn.PigRankEnum.Ensign;
	}

	public static void PigRankUp( IClient cl )
	{
		if ( PigRanks[cl] == PigmaskPawn.PigRankEnum.Colonel ) return;

		PigRanks[cl] += 1;
	}
}
