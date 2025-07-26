using System.Text.Json.Serialization;
using MonopolyServer.Utils;

public class GameState
{
    const decimal SALARY_AMOUNT = 200;
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
        // TODO: [GameConfig] Adjust how much money players start the game with
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

    #region Jail Handling
    /// <summary>
    /// Handles a player's turn while they are in jail.
    /// If they roll doubles, they get out of jail and can move.
    /// Otherwise, their jail turn count is reduced, and they stay in jail.
    /// If they've been in jail for 3 turns, they are automatically released.
    /// </summary>
    /// <param name="player">The player who is currently in jail</param>
    private void HandleJailTurn(Player player)
    {
        if (_diceRoll1 == _diceRoll2)
        {
            // Player rolled doubles, they get out of jail and can move
            player.FreeFromJail();

            // This line specifically enable the movement
            _totalDiceRoll = _diceRoll1 + _diceRoll2;
            Console.WriteLine($"Player {player.Name} rolled doubles and got out of jail!");
        }
        else
        {
            // Player didn't roll doubles, reduce their jail turn count
            player.ReduceJailTurnRemaining();

            _totalDiceRoll = 0; // No movement while in jail

        }
    }

    /// <summary>
    /// Allows a player to pay $50 to get out of jail immediately.
    /// </summary>
    /// <param name="playerId">The ID of the player who wants to pay to get out of jail</param>
    /// <returns>True if the payment was successful and the player is out of jail, false otherwise</returns>
    /// <exception cref="Exception">Thrown if the player is not in jail or doesn't have enough money</exception>
    public bool PayToGetOutOfJail(Guid playerId)
    {

        if (CurrentPhase != GamePhase.PlayerTurnStart) throw new Exception("Not the appropriate game phase for this action");

        Player? player = GetPlayerById(playerId);

        if (player == null)
        {
            throw new Exception("Player not found");
        }

        if (!player.IsInJail)
        {
            throw new Exception("Player is not in jail");
        }

        const decimal JAIL_FEE = 50;

        if (player.Money < JAIL_FEE)
        {
            throw new Exception("Not enough money to pay the jail fee");
        }

        player.DeductMoney(JAIL_FEE);
        player.FreeFromJail();

        return true;
    }

    /// <summary>
    /// Allows a player to use a "Get Out of Jail Free" card if they have one.
    /// </summary>
    /// <param name="playerId">The ID of the player who wants to use a Get Out of Jail Free card</param>
    /// <returns>True if the card was used successfully and the player is out of jail, false otherwise</returns>
    /// <exception cref="Exception">Thrown if the player is not in jail or doesn't have a Get Out of Jail Free card</exception>
    public bool UseGetOutOfJailFreeCard(Guid playerId)
    {
        if (CurrentPhase != GamePhase.PlayerTurnStart) throw new Exception("Not the appropriate game phase for this action");

        Player? player = GetPlayerById(playerId);

        if (player == null)
        {
            throw new Exception("Player not found");
        }

        if (!player.IsInJail)
        {
            throw new Exception("Player is not in jail");
        }

        if (player.GetOutOfJailFreeCards <= 0)
        {
            throw new Exception("Player doesn't have any Get Out of Jail Free cards");
        }

        // Use the player's Get Out of Jail Free card
        player.UseGetOutOfJailFreeCard();
        player.FreeFromJail();

        return true;
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
    /// Changes game phases from PlayerTurnStart to RollingDice to MovingToken to LandingOnSpaceAction to PostLandingActions.
    /// </summary>
    /// <exception cref="Exception">Thrown if not in the PlayerTurnStart phase</exception>
    public RollResult RollDice()
    {
        if (CurrentPhase != GamePhase.PlayerTurnStart) throw new Exception("Not the appropriate game phase for this action");

        ChangeGamePhase(GamePhase.RollingDice);

        // Gotta reset lil bro
        _totalDiceRoll = 0;

        // Gamba, might wanna look for better randomness?
        _diceRoll1 = _random.Next(1, 7);
        _diceRoll2 = _random.Next(1, 7);

        Player currentPlayer = GetCurrentPlayer();

        // Handle jail status before movement
        if (currentPlayer.IsInJail) HandleJailTurn(currentPlayer);
        else _totalDiceRoll = _diceRoll1 + _diceRoll2;


        // Handle rolling doubles
        if (_diceRoll1 == _diceRoll2)
        {
            currentPlayer.AddConsecutiveDouble();
            if (currentPlayer.ConsecutiveDoubles >= 3)
            {
                currentPlayer.GoToJail();
            }
        }
        else currentPlayer.ResetConsecutiveDouble();

        bool passedStart = false;
        // Only move if the player is not in jail or just got out of jail
        if (!currentPlayer.IsInJail)
        {
            passedStart = currentPlayer.MoveBy(_totalDiceRoll);
            ChangeGamePhase(GamePhase.MovingToken);
            Console.WriteLine($"Player moved to position {currentPlayer.CurrentPosition}");
        }

        ChangeGamePhase(GamePhase.LandingOnSpaceAction);

        // Salary🥳
        if (passedStart)
        {
            currentPlayer.AddMoney(SALARY_AMOUNT);
        }

        // Handle landing on spaces
        var space = GetSpaceAtPosition(currentPlayer.CurrentPosition) ?? throw new Exception("Invalid space");

        // Landed on Go To Jail👮‍♂️
        if (space is SpecialSpace { Type: PropertyType.GoToJail })
        {
            currentPlayer.GoToJail();
            Console.WriteLine($"{currentPlayer.Name} landed on Go To Jail!");
        }

        // Landed on special(ntar)
        else if (space is SpecialSpace specialSpace)
        {
            // TODO: other special space action
            Console.WriteLine($"{currentPlayer.Name} landed on another special space of type {specialSpace.Type}.");
            // TODO: [GameConfig] If a player lands on Vacation, all collected money from taxes and bank payments will be earned
        }

        // Landed on a property
        else if (space is Property property)
        {
            // PROPERTY OWNED BY OTHER PLAYER
            if (property.IsOwnedByOtherPlayer(currentPlayer.Id))
            {
                var rentValue = property.CalculateRent();

                // TODO: [GameConfig] If a player owns a full property set, the base rent payment will be doubled
                // TODO: [GameConfig] Rent will not be collected when landing on properties whose owners are in prison

                Console.WriteLine($"Deducting {currentPlayer.Name}'s money from rent: {rentValue}");
                currentPlayer.DeductMoney(rentValue);
            }
        }
        else
        {
            Console.WriteLine($"{currentPlayer.Name} landed on an unknown space type.");
        }

        var diceInfo = new RollResult.DiceInfo(_diceRoll1, _diceRoll2, _totalDiceRoll);
        var playerStateInfo = new RollResult.PlayerStateInfo(currentPlayer.IsInJail, currentPlayer.CurrentPosition, currentPlayer.JailTurnsRemaining, currentPlayer.Money);

        
        ChangeGamePhase(GamePhase.PostLandingActions);

        return new RollResult(diceInfo, playerStateInfo);
    }

    /// <summary>
    /// Ends the current player's turn and advances to the next player.
    /// Changes the game phase from PostLandingActions or LandingOnSpaceAction to PlayerTurnStart.
    /// </summary>
    /// <returns>The index of the next player</returns>
    /// <exception cref="Exception">Thrown if not in the PostLandingActions or LandingOnSpaceAction phase</exception>
    public int EndTurn()
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions))
            throw new Exception($"{CurrentPhase} is not the appropriate game phase for this action");

        Player currentPlayer = GetCurrentPlayer();
        if (currentPlayer.Money < 0) throw new Exception("You're broke");

        ChangeGamePhase(GamePhase.PlayerTurnStart);

        // If the player rolled doubles and isn't in jail, they get another turn
        if (currentPlayer.ConsecutiveDoubles > 0 && !currentPlayer.IsInJail)
            return CurrentPlayerIndex;

        return NextPlayer();
    }

    #endregion
    #region Property 
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
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions)) throw new Exception($"{CurrentPhase} is not the appropriate game phase for this action");

        Player currentPlayer = GetCurrentPlayer();

        var space = GetSpaceAtPosition(currentPlayer.CurrentPosition);

        if (space is Property property)
        {
            if (property.OwnerId == null)
            {
                if (currentPlayer.Money >= property.PurchasePrice)
                {
                    currentPlayer.DeductMoney(property.PurchasePrice);

                    property.BuyProperty(currentPlayer.Id);

                    currentPlayer.PropertiesOwned.Add(property.Id);

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

    private (bool result, CountryProperty countryProperty) CheckUpgradeDowngradePermission(Guid propertyGuid)
    {
        Property property = Board.GetPropertyById(propertyGuid);
        Player currentPlayer = GetCurrentPlayer();
        // Group ownership checks
        if (property is CountryProperty countryProperty)
        {
            return (Board.GroupIsOwnedByPlayer(countryProperty.Group, currentPlayer.Id), countryProperty);
        }
        // TODO: [GameConfig] House spread checks
        else throw new Exception("Space is not a country");
    }

    public void UpgradeProperty(Guid propertyGuid)
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions)) throw new Exception($"{CurrentPhase} is not the appropriate game phase for this action");
        
        var (playerCanUpgrade, countryProperty) = CheckUpgradeDowngradePermission(propertyGuid);

        if (!playerCanUpgrade) throw new Exception("Cannot perform upgrade for this property");
        countryProperty.UpgradeRentStage();

    }
    public void DowngradeProperty(Guid propertyGuid)
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions)) throw new Exception($"{CurrentPhase} is not the appropriate game phase for this action");

        var (playerCanUpgrade, countryProperty) = CheckUpgradeDowngradePermission(propertyGuid);

        if (!playerCanUpgrade) throw new Exception("Cannot perform downgrade for this property");
        countryProperty.DownGradeRentStage();
    }
    public void MortgageProperty(Guid propertyGuid) {
        
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions)) throw new Exception($"{CurrentPhase} is not the appropriate game phase for this action");

        Property property = Board.GetPropertyById(propertyGuid);
        Player currentPlayer = GetCurrentPlayer();
        if (property.IsOwnedByPlayer(currentPlayer.Id))
        {
            property.MortgageProperty();
        }else throw new Exception("Property is not owned by this player");
    }
    
   
    #endregion
}
