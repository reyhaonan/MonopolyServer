using System.Text.Json.Serialization;
using MonopolyServer.Utils;

public class GameState
{
    #region Private property
    private static readonly Random _random = new Random();
    private int _diceRoll1 { get; set; } = 0;
    private int _diceRoll2 { get; set; } = 0;
    public int _totalDiceRoll { get; set; } = 0;
    // private GameConfig _gameConfig;
    public int CurrentPlayerIndex { get; private set; } = -1;
    #endregion

    #region Public property
    [JsonInclude]
    public Guid GameId { get; init; }
    // List of all players
    [JsonInclude]
    public List<Player> Players { get; private set; } = [];
    // List of all active players(still playing)
    [JsonInclude]
    public List<Player> ActivePlayers { get; private set; } = [];
    [JsonInclude]
    public Board Board { get; set; }

    // public List<string> ChanceDeck { get; set; } // Simplified for now, could be objects
    // public List<string> CommunityChestDeck { get; set; } // Simplified for now, could be objects
    [JsonInclude]
    public GamePhase CurrentPhase { get; private set; }
    #endregion

    /// <summary>
    /// Constructor for GameState. Initializes a new game with a unique ID,
    /// creates a new board, sets the initial game phase to WaitingForPlayers,
    /// and initializes the card decks.
    /// </summary>
    public GameState()
    {
        GameId = Guid.NewGuid();

        Board = new Board();

        CurrentPhase = GamePhase.WaitingForPlayers;

        InitializeDecks();
    }

    /// <summary>
    /// Changes the current game phase to a new phase.
    /// </summary>
    /// <param name="newGamePhase">The new game phase to transition to</param>
    private void ChangeGamePhase(GamePhase newGamePhase)
    {
        CurrentPhase = newGamePhase;
    }


    #region Player Management
    /// <summary>
    /// Adds a new player to the game. The player is added to both the Players list
    /// (all players) and the ActivePlayers list (players still in the game).
    /// </summary>
    /// <param name="newPlayer">The new player to add to the game</param>
    public void AddPlayer(Player newPlayer)
    {
        Players.Add(newPlayer);

        ActivePlayers.Add(newPlayer);
    }

    /// <summary>
    /// Gets the current player whose turn it is.
    /// </summary>
    /// <returns>The current player object</returns>
    public Player GetCurrentPlayer()
    {
        return ActivePlayers[CurrentPlayerIndex];
    }

    /// <summary>
    /// Gets a player by their unique ID.
    /// </summary>
    /// <param name="playerId">The unique ID of the player to find</param>
    /// <returns>The player with the specified ID, or null if not found</returns>
    public Player? GetPlayerById(Guid playerId)
    {
        return ActivePlayers.FirstOrDefault(p => p.Id == playerId);
    }
    #endregion

    /// <summary>
    /// Initializes the Chance and Community Chest card decks.
    /// Currently commented out as the implementation is simplified.
    /// </summary>
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

    /// <summary>
    /// Gets the space at a specific position on the board.
    /// </summary>
    /// <param name="position">The position on the board (0-39)</param>
    /// <returns>The space at the specified position, or null if the position is invalid</returns>
    public Space? GetSpaceAtPosition(int position)
    {
        if (position >= 0 && position < Board.Spaces.Count)
        {
            return Board.Spaces[position];
        }
        return null;
    }

    #region Turn Management
    /// <summary>
    /// Advances to the next player in turn order.
    /// Cycles back to the first player after the last player.
    /// </summary>
    /// <returns>The index of the next player</returns>
    private int NextPlayer()
    {
        Console.WriteLine($"Invoked next player {CurrentPlayerIndex}, Count: {ActivePlayers.Count}");

        // Loop back to 0
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % ActivePlayers.Count;

        return CurrentPlayerIndex;
    }
    #endregion

    #region Game flow
    /// <summary>
    /// Starts the game by setting the first player, randomizing player order,
    /// and changing the game phase from WaitingForPlayers to PlayerTurnStart.
    /// </summary>
    /// <returns>The list of active players in their randomized order</returns>
    /// <exception cref="Exception">Thrown if the game has already started</exception>
    public List<Player> StartGame()
    {
        CurrentPlayerIndex = 0;

        ActivePlayers = ActivePlayers.OrderBy(_ => _random.Next()).ToList();

        if (CurrentPhase == GamePhase.WaitingForPlayers) ChangeGamePhase(GamePhase.PlayerTurnStart);

        else throw new Exception($"Game {GameId} already started");

        return ActivePlayers;
    }

    /// <summary>
    /// Rolls the dice for the current player's turn, handles movement and special cases like doubles.
    /// Changes game phases from PlayerTurnStart to RollingDice to MovingToken to LandingOnSpaceAction.
    /// </summary>
    /// <exception cref="Exception">Thrown if not in the PlayerTurnStart phase</exception>
    public RollResult RollDice()
    {
        if (CurrentPhase != GamePhase.PlayerTurnStart) throw new Exception("Not the appropriate game phase for this action");

        _totalDiceRoll = 0;

        bool wasJailed = false;

        ChangeGamePhase(GamePhase.RollingDice);

        _diceRoll1 = _random.Next(1, 7);

        _diceRoll2 = _random.Next(1, 7);

        Player currentPlayer = GetCurrentPlayer();

        if (_diceRoll1 == _diceRoll2)
        {
            if (currentPlayer.ConsecutiveDoubles >= 3)
            {
                wasJailed = true;
            }
            currentPlayer.ConsecutiveDoubles++;
        }
        else currentPlayer.ConsecutiveDoubles = 0;
        _totalDiceRoll = _diceRoll1 + _diceRoll2;

        ChangeGamePhase(GamePhase.MovingToken);

        currentPlayer.MoveBy(_totalDiceRoll);

        ChangeGamePhase(GamePhase.LandingOnSpaceAction);
        var space = GetSpaceAtPosition(currentPlayer.CurrentPosition);
        if (space is SpecialSpace specialSpace)
        {
            if (specialSpace.Type.Equals(PropertyType.GoToJail)) wasJailed = true;
            // TODO: other special space action
        }
        else if (space is Property property)
        {
            // TODO: mortgage logic later after game config is done
            if (property.IsOwnedByOtherPlayer(currentPlayer.Id))
            {
                var rentValue = property.CalculateRent();

                Console.WriteLine($"Deducting player money from rent {rentValue}");

                currentPlayer.DeductMoney(rentValue);
            }
        }

        if (wasJailed) currentPlayer.GoToJail();

        var diceInfo = new RollResult.DiceInfo(_diceRoll1, _diceRoll2, _totalDiceRoll);

        var playerStateInfo = new RollResult.PlayerStateInfo(wasJailed, currentPlayer.CurrentPosition, currentPlayer.Money);
        
        return new RollResult(diceInfo, playerStateInfo);
    }

    /// <summary>
    /// Allows the current player to buy the property they have landed on.
    /// Checks if the space is a property, if it's not already owned, and if the player has enough money.
    /// Deducts the purchase price from the player's money, sets the property's owner, and adds the property to the player's owned properties.
    /// Changes the game phase from LandingOnSpaceAction to PostLandingActions upon successful purchase.
    /// </summary>
    /// <exception cref="Exception">
    /// Thrown if not in the LandingOnSpaceAction phase, if the space is not a property,
    /// if the property is already owned, or if the player doesn't have enough money.
    /// </exception>
    public (Guid propertyGuid, decimal playerRemainingMoney) BuyProperty()
    {
        if (!CurrentPhase.Equals(GamePhase.LandingOnSpaceAction)) throw new Exception($"{CurrentPhase} is not the appropriate game phase for this action");

        Player currentPlayer = GetCurrentPlayer();

        var space = GetSpaceAtPosition(currentPlayer.CurrentPosition);

        if (space is Property property)
        {
            if (property.OwnerId == null)
            {
                if (currentPlayer.Money >= property.PurchasePrice)
                {
                    currentPlayer.DeductMoney(property.PurchasePrice);

                    property.OwnerId = currentPlayer.Id;

                    currentPlayer.PropertiesOwned.Add(property.Id);

                    ChangeGamePhase(GamePhase.PostLandingActions);
                    return (property.Id, currentPlayer.Money);
                }
                else
                {
                    throw new Exception("Not enough money to buy this property");
                }
            }
            else
            {
                throw new Exception("This property is already owned");
            }
        }
        else
        {
            throw new Exception("This space is not a property that can be purchased");
        }



    }
    /// <summary>
    /// Ends the current player's turn and advances to the next player.
    /// Changes the game phase from PostLandingActions or LandingOnSpaceAction to PlayerTurnStart.
    /// </summary>
    /// <returns>The index of the next player</returns>
    /// <exception cref="Exception">Thrown if not in the PostLandingActions or LandingOnSpaceAction phase</exception>
    public int EndTurn()
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.LandingOnSpaceAction)) throw new Exception($"{CurrentPhase} is not the appropriate game phase for this action");

        ChangeGamePhase(GamePhase.PlayerTurnStart);

        if (GetCurrentPlayer().ConsecutiveDoubles > 0) return CurrentPlayerIndex;
        return NextPlayer();

    }
    #endregion

}
