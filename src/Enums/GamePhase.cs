
public enum GamePhase
{
    WaitingForPlayers,
    PlayerTurnStart,
    RollingDice,
    MovingToken,
    LandingOnSpaceAction,
    PostLandingActions, // Auctions, debt resolution
    TurnEnd,
    GameOver
}