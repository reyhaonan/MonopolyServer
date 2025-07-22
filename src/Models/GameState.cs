public class GameState
{
    #region Private property
    private static readonly Random _random = new Random();
    private int _diceRoll1 { get; set; } = 0;
    private int _diceRoll2 { get; set; } = 0;
    // private GameConfig _gameConfig;
    public int CurrentPlayerIndex { get; private set; } = 0;
    #endregion

    #region Public property
    public Guid GameId { get; set; }
    // List of all players
    public List<Player> Players { get; private set; } = [];
    // List of all active players(still playing)
    public List<Player> ActivePlayers { get; private set; } = [];
    public Board Board { get; set; }
    public Guid CurrentPlayerId { get; set; } 
    public int TotalDiceRoll { get; private set; } = 0;

    // public List<string> ChanceDeck { get; set; } // Simplified for now, could be objects
    // public List<string> CommunityChestDeck { get; set; } // Simplified for now, could be objects

    public GamePhase CurrentPhase { get; private set; }
    #endregion

    public GameState()
    {
        GameId = Guid.NewGuid();
        Board = new Board();
        CurrentPhase = GamePhase.WaitingForPlayers;
        InitializeDecks();
    }

    private void ChangeGamePhase(GamePhase newGamePhase)
    {
        CurrentPhase = newGamePhase;
    }


    #region Player Management
    public void AddPlayer(Player newPlayer)
    {
        Players.Add(newPlayer);
        ActivePlayers.Add(newPlayer);
    }

    public Player GetCurrentPlayer()
    {
        return ActivePlayers[CurrentPlayerIndex];
    }

    public Player? GetPlayerById(Guid playerId)
    {
        return ActivePlayers.FirstOrDefault(p => p.Id == playerId);
    }
    #endregion

    private void InitializeDecks()
    {
        // Populate with example cards (will need full card logic later)
        // ChanceDeck =
        // [
        //     "Advance to GO!",
        //     "Go to Jail",
        //     "Bank pays you dividend of $50",
        //     // ... more Chance cards
        // ];
        // CommunityChestDeck =
        // [
        //     "Bank error in your favor – Collect $200",
        //     "Doctor's fee – Pay $50",
        //     "Go to Jail",
        //     // ... more Community Chest cards
        // ];
    }

    public Space? GetSpaceAtPosition(int position)
    {
        if (position >= 0 && position < Board.Spaces.Count)
        {
            return Board.Spaces[position];
        }
        return null;
    }

    #region Game flow
    public void StartGame()
    {
        CurrentPlayerIndex = _random.Next(0, ActivePlayers.Count);
        CurrentPlayerId = GetCurrentPlayer().Id;
        if (CurrentPhase == GamePhase.WaitingForPlayers) ChangeGamePhase(GamePhase.PlayerTurnStart);
        else throw new Exception($"Game {GameId} already started");
    }

    public (int roll1, int roll2, int totalRoll, bool wasJailed, Player? player) RollDice()
    {
        TotalDiceRoll = 0;
        bool wasJailed = false;

        ChangeGamePhase(GamePhase.RollingDice);

        _diceRoll1 = _random.Next(1, 7);
        _diceRoll2 = _random.Next(1, 7);
        Player currentPlayer = GetCurrentPlayer();
        if (_diceRoll1 == _diceRoll2)
        {
            if (currentPlayer.ConsecutiveDoubles >= 3)
            {
                // TODO: Jail
                wasJailed = true;
            }
            currentPlayer.ConsecutiveDoubles++;
        }
        TotalDiceRoll = _diceRoll1 + _diceRoll2;

        ChangeGamePhase(GamePhase.MovingToken);

        currentPlayer.MoveBy(TotalDiceRoll);

        ChangeGamePhase(GamePhase.LandingOnSpaceAction);

        return (_diceRoll1, _diceRoll2, TotalDiceRoll, wasJailed, wasJailed ? currentPlayer : null);
    }
    #endregion

}