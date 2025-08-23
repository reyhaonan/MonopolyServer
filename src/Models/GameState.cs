using System.Text.Json.Serialization;
using MonopolyServer.Enums;
using MonopolyServer.Utils;

namespace MonopolyServer.Models;

using System.Text;
using Microsoft.Extensions.Logging;

public class GameState
{
    const int SALARY_AMOUNT = 200;
    #region Private property
    private readonly ILogger _logger;
    private static readonly Random _random = new Random();
    private int _diceRoll1 { get; set; } = 0;
    private int _diceRoll2 { get; set; } = 0;
    private int _totalDiceRoll { get; set; } = 0;
    public int CurrentPlayerIndex { get; private set; } = -1;
    #endregion

    #region Public property
    [JsonInclude]
    public GameConfig GameConfig;
    [JsonInclude]
    public Guid GameId { get; init; }
    // List of all active players(still playing)
    [JsonInclude]
    public List<Player> ActivePlayers { get; private set; } = [];
    [JsonInclude]
    public Board Board { get; private set; }
    [JsonInclude]
    public List<Trade> ActiveTrades { get; private set; } = [];

    // public List<string> ChanceDeck { get; set; } // Simplified for now, could be objects
    // public List<string> CommunityChestDeck { get; set; } // Simplified for now, could be objects
    [JsonInclude]
    public GamePhase CurrentPhase { get; private set; }

    [JsonInclude]
    public TransactionHistory TransactionsHistory { get; init; }
    #endregion

    /// <summary>
    /// Constructor for GameState. Initializes a new game with a unique ID,
    /// creates a new board, sets the initial game phase to WaitingForPlayers,
    /// and initializes the card decks.
    /// </summary>
    public GameState(ILogger<GameState> logger)
    {
        GameConfig = new GameConfig();
        _logger = logger;
        GameId = Guid.NewGuid();

        Board = new Board();

        CurrentPhase = GamePhase.WaitingForPlayers;

        TransactionsHistory = new TransactionHistory([]);

        InitializeDecks();
    }

    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Generates a random room code of a specified length.
    /// </summary>
    /// <param name="length">The desired length of the code.</param>
    /// <returns>A random string.</returns>
    public static string Generate(int length = 6)
    {
        var stringBuilder = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            stringBuilder.Append(Chars[_random.Next(Chars.Length)]);
        }
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Changes the current game phase to a new phase.
    /// </summary>
    /// <param name="newGamePhase">The new game phase to transition to</param>
    private void ChangeGamePhase(GamePhase newGamePhase)
    {
        _logger.LogInformation($"======Changing Game Phase to: {newGamePhase}======");
        CurrentPhase = newGamePhase;
    }

    #region Player Management
    /// <summary>
    /// Adds a new player to the game. The player is added to both the Players list
    /// (all players) and the ActivePlayers list (players still in the game).
    /// </summary>
    /// <param name="newPlayer">The new player to add to the game</param>
    public Player AddPlayer(string playerName, string hexColor, Guid newPlayerId)
    {
        if (ActivePlayers.Count >= GameConfig.MaxPlayers) throw new InvalidOperationException("Room is full");
        var newPlayer = new Player(playerName, hexColor, newPlayerId);

        ActivePlayers.Add(newPlayer);

        return newPlayer;
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
        _logger.LogInformation($"Invoked next player {CurrentPlayerIndex}, Count: {ActivePlayers.Count}");

        // Loop back to 0
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % ActivePlayers.Count;

        return CurrentPlayerIndex;
    }
    #endregion

    #region Jail Handling
    /// <summary>
    /// Allows a player to pay $50 to get out of jail immediately.
    /// </summary>
    public List<TransactionInfo> PayToGetOutOfJail()
    {

        if (CurrentPhase != GamePhase.PlayerTurnStart) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Player currentPlayer = GetCurrentPlayer();


        if (!currentPlayer.IsInJail)
        {
            throw new InvalidOperationException("Player is not in jail");
        }

        if (currentPlayer.Money < GameConfig.JailFine)
        {
            throw new InvalidOperationException("Not enough money to pay the jail fee");
        }

        TransactionsHistory.StartTransaction();
        TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.FreeFromJail, currentPlayer.Id, null, GameConfig.JailFine, true), (amount) =>
        {
            currentPlayer.DeductMoney(amount);
            currentPlayer.FreeFromJail();
        });

        ChangeGamePhase(GamePhase.PostLandingActions);

        return TransactionsHistory.CommitTransaction();
    }

    /// <summary>
    /// Allows a player to use a "Get Out of Jail Free" card if they have one.
    /// </summary>
    /// <returns>True if the card was used successfully and the player is out of jail, false otherwise</returns>
    /// <exception cref="Exception">Thrown if the player is not in jail or doesn't have a Get Out of Jail Free card</exception>
    public void UseGetOutOfJailCard()
    {
        if (CurrentPhase != GamePhase.PlayerTurnStart) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Player currentPlayer = GetCurrentPlayer();


        if (!currentPlayer.IsInJail)
        {
            throw new InvalidOperationException("Player is not in jail");
        }

        if (currentPlayer.GetOutOfJailFreeCards <= 0)
        {
            throw new InvalidOperationException("Player doesn't have any Get Out of Jail Free cards");
        }

        // Use the player's Get Out of Jail Free card
        currentPlayer.UseGetOutOfJailFreeCard();
        currentPlayer.FreeFromJail();

        
        ChangeGamePhase(GamePhase.PostLandingActions);
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
        
        if (ActivePlayers.Count < GameConfig.MinPlayers) throw new InvalidOperationException("Cannot start a game with only one player");
        CurrentPlayerIndex = 0;

        ActivePlayers = ActivePlayers.OrderBy(_ => _random.Next()).ToList();

        if (CurrentPhase == GamePhase.WaitingForPlayers) ChangeGamePhase(GamePhase.PlayerTurnStart);

        else throw new InvalidOperationException($"Game {GameId} already started");

        // Correct the starting money
        foreach (Player player in ActivePlayers)
        {
            player.setMoney(GameConfig.StartingMoney);
        }

        return ActivePlayers;
    }
    
    #region Dice rolling handling
    /// <summary>
    /// Simulates the physical rolling of two dice.
    /// </summary>
    private static (int, int) RollPhysicalDice()
    {
        // Gamba, might wanna look for better randomness?
        int dice1 = _random.Next(1, 7);
        int dice2 = _random.Next(1, 7);
        // int dice1 = 1;
        // int dice2 = 0;
        return (dice1, dice2);
    }

    /// <summary>
    /// Handles the game logic for doubles, such as getting out of jail or going to jail after three consecutive doubles.
    /// Also sets the total dice roll value for movement.
    /// </summary>
    private void HandleDiceRollConsequences(Player currentPlayer, int dice1, int dice2)
    {
        bool isDoubles = dice1 == dice2;

        if (isDoubles)
        {
            if (currentPlayer.IsInJail)
            {
                // Player rolled doubles, they get out of jail and can move
                currentPlayer.FreeFromJail();
                currentPlayer.ResetConsecutiveDouble();
                _totalDiceRoll = dice1 + dice2; // Enable movement
                _logger.LogInformation($"Player {currentPlayer.Name} rolled doubles and got out of jail!");
            }
            else
            {
                // Standard double roll
                currentPlayer.AddConsecutiveDouble();
                if (currentPlayer.ConsecutiveDoubles >= 3)
                {
                    currentPlayer.GoToJail();
                }
            }
        }
        else // Not a double roll
        {
            currentPlayer.ResetConsecutiveDouble();
            if (currentPlayer.IsInJail)
            {
                currentPlayer.ReduceJailTurnRemaining();
                _totalDiceRoll = 0; // Player does not move
                if (currentPlayer.JailTurnsRemaining == 0)
                {
                    // Free but no
                    // TODO: By game config, make em pay 50 bucks
                    currentPlayer.FreeFromJail();
                    return;
                }
            }
        }

        // If the player isn't in jail after all checks, set the total roll for movement
        if (!currentPlayer.IsInJail)
        {
            _totalDiceRoll = dice1 + dice2;
        }
    }

    /// <summary>
    /// Manages all events that occur when a player lands on a space.
    /// </summary>
    private void HandleLandingActions(Player currentPlayer, bool passedStart, int totalDiceRoll)
    {
        // Collect Salary if player passed Go
        if (passedStart)
        {
            TransactionsHistory.AddTransaction(
                new TransactionInfo(TransactionType.Salary, null, currentPlayer.Id, SALARY_AMOUNT, true),
                (amount) => currentPlayer.AddMoney(amount)
            );
        }

        var space = GetSpaceAtPosition(currentPlayer.CurrentPosition) ?? throw new InvalidOperationException("Invalid space");

        // Handle landing on different types of spaces
        if (space is SpecialSpace specialSpace)
        {
            ProcessSpecialSpaceLanding(currentPlayer, specialSpace);
        }
        else if (space is Property property)
        {
            ProcessPropertyLanding(currentPlayer, property, totalDiceRoll);
        }
        else
        {
            _logger.LogInformation($"{currentPlayer.Name} landed on an unknown space type.");
        }
    }

    /// <summary>
    /// Processes actions for landing on a SpecialSpace (e.g., Go To Jail, Tax).
    /// </summary>
    private void ProcessSpecialSpaceLanding(Player currentPlayer, SpecialSpace specialSpace)
    {
        _logger.LogInformation($"{currentPlayer.Name} landed on a special space: {specialSpace.Type}.");
        switch (specialSpace.Type)
        {
            case SpecialSpaceType.GoToJail:
                currentPlayer.GoToJail();
                break;
            case SpecialSpaceType.IncomeTax:
                TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Fine, currentPlayer.Id, null, 200, true), 
                    (amount) => currentPlayer.DeductMoney(amount));
                break;
            case SpecialSpaceType.LuxuryTax:
                TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Fine, currentPlayer.Id, null, 100, true), 
                    (amount) => currentPlayer.DeductMoney(amount));
                break;
            // Other cases (Chance, CommunityChest, etc.) would go here
            default:
                // No action needed for spaces like Free Parking, Jail (Just Visiting), etc.
                break;
        }
    }

    /// <summary>
    /// Processes actions for landing on a Property, primarily handling rent payment.
    /// </summary>
    private void ProcessPropertyLanding(Player currentPlayer, Property property, int totalDiceRoll)
    {
        if (!property.IsOwnedByOtherPlayer(currentPlayer.Id))
        {
            // Player landed on their own property or an unowned one. No action needed here.
            return;
        }

        var ownerId = property.OwnerId ?? throw new Exception("Property is owned but has no OwnerId.");
        Player owner = GetPlayerById(ownerId) ?? throw new Exception("Owner not found.");

        // TODO: Add check: Rent will not be collected when landing on properties whose owners are in prison.
        
        int rentValue = 0;
        if (property is CountryProperty countryProperty)
        {
            // TODO: Add logic for doubled rent on full sets.
            rentValue = countryProperty.CalculateRent(totalDiceRoll);
        }
        else if (property is UtilityProperty utilityProperty)
        {
            var utilityCount = Board.GetUtilityOwnedByPlayer(ownerId).Count;
            rentValue = utilityProperty.CalculateRent(totalDiceRoll, 0, utilityCount);
        }
        else if (property is RailroadProperty railroadProperty)
        {
            var railroadCount = Board.GetRailroadOwnedByPlayer(ownerId).Count;
            rentValue = railroadProperty.CalculateRent(0, railroadCount);
        }

        if (rentValue > 0)
        {
            _logger.LogInformation($"Deducting {rentValue} from {currentPlayer.Name} for rent to {owner.Name}.");
            TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Rent, currentPlayer.Id, ownerId, rentValue, false), (amount) =>
            {
                currentPlayer.DeductMoney(amount);
                owner.AddMoney(amount);
            });
        }
    }

    /// <summary>
    /// Rolls the dice for the current player's turn, handles movement and special cases like doubles.
    /// Changes game phases from PlayerTurnStart to RollingDice to MovingToken to LandingOnSpaceAction to PostLandingActions.
    /// </summary>
    /// <exception cref="Exception">Thrown if not in the PlayerTurnStart phase</exception>
    public RollResult RollDice()
    {
        if (CurrentPhase != GamePhase.PlayerTurnStart)
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        ChangeGamePhase(GamePhase.RollingDice);
        var currentPlayer = GetCurrentPlayer();

        // Roll the dice and handle immediate consequences (jail, doubles)
        _totalDiceRoll = 0; // Reset from previous turn
        (_diceRoll1, _diceRoll2) = RollPhysicalDice();
        HandleDiceRollConsequences(currentPlayer, _diceRoll1, _diceRoll2);

        // Move the player's token on the board
        bool passedStart = false;
        if (!currentPlayer.IsInJail)
        {
            ChangeGamePhase(GamePhase.MovingToken);
            passedStart = currentPlayer.MoveBy(_totalDiceRoll);
            _logger.LogInformation($"Player moved to position {currentPlayer.CurrentPosition}");
        }

        // Handle all actions related to landing on a new space
        ChangeGamePhase(GamePhase.LandingOnSpaceAction);
        TransactionsHistory.StartTransaction();
        HandleLandingActions(currentPlayer, passedStart, _totalDiceRoll);
        var transactionInfo = TransactionsHistory.CommitTransaction();

        // Finalize the dice rolling process and return the result
        if (currentPlayer.ConsecutiveDoubles > 0 && !currentPlayer.IsInJail) ChangeGamePhase(GamePhase.PlayerTurnStart);
        else ChangeGamePhase(GamePhase.PostLandingActions);
        var diceInfo = new RollResult.DiceInfo
        {
            Roll1 = _diceRoll1,
            Roll2 = _diceRoll2,
            TotalRoll = _totalDiceRoll
            
        };
        var playerStateInfo = new RollResult.PlayerStateInfo
        {
            IsInJail = currentPlayer.IsInJail,
            NewPlayerPosition = currentPlayer.CurrentPosition,
            NewPlayerJailTurnsRemaining = currentPlayer.JailTurnsRemaining,
            ConsecutiveDoubles = currentPlayer.ConsecutiveDoubles
        };

        return new RollResult
        {
            Dice = diceInfo,
            PlayerState = playerStateInfo,
            Transaction = transactionInfo,
            NewGamePhase = CurrentPhase
        };
    }

    #endregion
    /// <summary>
    /// Ends the current player's turn and advances to the next player.
    /// Changes the game phase from PostLandingActions to PlayerTurnStart.
    /// </summary>
    /// <returns>The index of the next player</returns>
    /// <exception cref="Exception">Thrown if not in the PostLandingActions phase</exception>
    public int EndTurn()
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions))
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Player currentPlayer = GetCurrentPlayer();
        if (currentPlayer.Money < 0) throw new InvalidOperationException("You're broke");

        ChangeGamePhase(GamePhase.PlayerTurnStart);

        // If the player rolled doubles and isn't in jail, they get another turn
        if (currentPlayer.ConsecutiveDoubles > 0 && !currentPlayer.IsInJail)
            return CurrentPlayerIndex;

        return NextPlayer();
    }

    public (int currentPlayerIndex, bool isGameOver ) DeclareBankcruptcy(Guid playerId)
    {
        Player bankcruptPlayer = GetPlayerById(playerId) ?? throw new InvalidOperationException("Player not found");

        foreach (Guid propertyId in bankcruptPlayer.PropertiesOwned)
        {
            Board.GetPropertyById(propertyId).ResetProperty();
        }

        bool isActivePlayer = GetCurrentPlayer().Id == bankcruptPlayer.Id;


        ActivePlayers.Remove(bankcruptPlayer);

        // Game over
        if (ActivePlayers.Count <= 1)
        {
            ChangeGamePhase(GamePhase.GameOver);
        }
        else if (isActivePlayer) ChangeGamePhase(GamePhase.PlayerTurnStart);
        CurrentPlayerIndex %= ActivePlayers.Count;
        return (CurrentPlayerIndex, ActivePlayers.Count <= 1);
    }

    #endregion
    #region Property 
    /// <summary>
    /// Allows the current player to buy the property they have landed on.
    /// Checks if the space is a property, if it's not already owned, and if the player has enough money.
    /// Deducts the purchase price from the player's money, sets the property's owner, and adds the property to the player's owned properties.
    /// Changes the game phase from LandingOnSpaceAction to PostLandingActions upon successful purchase.
    /// </summary>
    /// <returns>propertyId, and transaction info</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public (Guid, List<TransactionInfo>) BuyProperty()
    {
        Player currentPlayer = GetCurrentPlayer();
        // Action is only available on: [PostLandingActions, or on consecutive double and PlayerTurnStart]
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions)&&(!CurrentPhase.Equals(GamePhase.PlayerTurnStart) || currentPlayer.ConsecutiveDoubles <= 0))
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");
        

        var space = GetSpaceAtPosition(currentPlayer.CurrentPosition);

        if (space is Property property)
        {
            // Check ownership
            if (property.OwnerId != null) throw new InvalidOperationException("This property is already owned");

            // Check player money`
            if (currentPlayer.Money < property.PurchasePrice) throw new InvalidOperationException("Not enough money to buy this property");

            TransactionsHistory.StartTransaction();
            TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Buy, currentPlayer.Id, null, property.PurchasePrice, true), (amount) =>
            {
                currentPlayer.DeductMoney(amount);
                property.BuyProperty(currentPlayer.Id);
                currentPlayer.PropertiesOwned.Add(property.Id);
            });
            var transactionResult = TransactionsHistory.CommitTransaction();


            return (property.Id, transactionResult);
        }
        else
        {
            throw new InvalidOperationException("This space is not a property that can be purchased");
        }
    }

    /// <summary>
    /// Sellin
    /// </summary>
    /// <param name="propertyId"></param>
    /// <returns>transaction info</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<TransactionInfo> SellProperty(Guid propertyId)
    {
        // Action is only available on: [PostLandingActions, PlayerTurnStart]
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart)) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Property property = Board.GetPropertyById(propertyId);
        Player currentPlayer = GetCurrentPlayer();

        // Disallow selling property if its not owned by the player
        if (!property.IsOwnedByPlayer(currentPlayer.Id)) throw new InvalidOperationException($"{currentPlayer.Id} are not permitted to sell this property");

        if (property is CountryProperty countryProperty)
        {
            // DIsallow selling property if it has houses
            if (countryProperty.CurrentRentStage != RentStage.Unimproved) throw new InvalidOperationException("Can't sell property with house");


            bool groupIsOwnedByPlayer = Board.GroupIsOwnedByPlayer(countryProperty.Group, currentPlayer.Id);
            bool noHouseInGroup = Board.NoHouseInGroup(countryProperty.Group);

            // Disallow mortgage if player has a house in a group
            if (groupIsOwnedByPlayer && !noHouseInGroup) throw new InvalidOperationException("Cannot sell property if other property has house");
        }

        // You can sell mortgaged property, good luck getting any money though lol
        int sellValue = property.IsMortgaged ? 0 : property.MortgageValue;

        TransactionsHistory.StartTransaction();
        TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Sell, null, currentPlayer.Id, sellValue, true), (amount) =>
        {
            property.SellProperty();

            currentPlayer.PropertiesOwned.Remove(property.Id);

            currentPlayer.AddMoney(amount);
        });

        return TransactionsHistory.CommitTransaction();
    }
    /// <summary>
    /// Mortgagin a property
    /// </summary>
    /// <param name="propertyId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<TransactionInfo> MortgageProperty(Guid propertyId)
    {

        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart)) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Property property = Board.GetPropertyById(propertyId);
        Player currentPlayer = GetCurrentPlayer();


        // Disallow mortgage if property is now owned
        if (!property.IsOwnedByPlayer(currentPlayer.Id)) throw new InvalidOperationException("Property is not owned by this player");

        if (property is CountryProperty countryProperty)
        {
            // Disallow mortgage if property has houses
            if (!countryProperty.CurrentRentStage.Equals(RentStage.Unimproved)) throw new InvalidOperationException("Can't mortgage property with house");

            bool groupIsOwnedByPlayer = Board.GroupIsOwnedByPlayer(countryProperty.Group, currentPlayer.Id);
            bool noHouseInGroup = Board.NoHouseInGroup(countryProperty.Group);

            // Disallow mortgage if player has a house in a group
            if (groupIsOwnedByPlayer && !noHouseInGroup) throw new InvalidOperationException("Cannot mortgage if other property has house");
        }

        TransactionsHistory.StartTransaction();
        TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Mortgage, null, currentPlayer.Id, property.MortgageValue, true), (amount) =>
        {
            property.MortgageProperty();

            currentPlayer.AddMoney(amount);
        });

        return TransactionsHistory.CommitTransaction();
    }
    /// <summary>
    /// Unmortgage(pay money cuh)
    /// </summary>
    /// <param name="propertyId"></param>
    /// <returns>Transaction info</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<TransactionInfo> UnmortgageProperty(Guid propertyId)
    {

        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart)) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Property property = Board.GetPropertyById(propertyId);
        Player currentPlayer = GetCurrentPlayer();

        // Disallow unmortgage if no one own this property
        if (!property.IsOwnedByPlayer(currentPlayer.Id)) throw new InvalidOperationException("Property is not owned by this player");

        // Disallow unmortgage if its not mortgaged
        if (!property.IsMortgaged) throw new InvalidOperationException("Property is not mortgaged");

        // Broke player alert
        if (currentPlayer.Money < property.UnmortgageCost) throw new InvalidOperationException("Not enough money to unmortgage this property");

        TransactionsHistory.StartTransaction();
        TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Unmortgage, currentPlayer.Id, null, property.UnmortgageCost, true), (amount) =>
        {
            property.UnmortgageProperty();

            currentPlayer.DeductMoney(amount);
        });
        return TransactionsHistory.CommitTransaction();

    }


    /// <summary>
    /// Generic checking for countryProperty upgrade or downgrade
    /// </summary>
    /// <param name="countryProperty"></param>
    /// <param name="currentPlayer"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private void _checkUpgradeDowngradePermission(CountryProperty countryProperty, Player currentPlayer)
    {
        if (countryProperty.IsMortgaged) throw new InvalidOperationException("Cannot upgrade/downgrade mortgaged property");

        if (countryProperty.OwnerId != currentPlayer.Id) throw new InvalidOperationException("This player is not permitted to upgrade/downgrade the property");

        // Group ownership checks
        if (!Board.GroupIsOwnedByPlayer(countryProperty.Group, currentPlayer.Id)) throw new InvalidOperationException("Cannot perform upgrade/downgrade because the player didnt own this group");

        // TODO: Equal spread checks

        // Mortgaged property in group checks
        if (!Board.NoMortgagedPropertyInGroup(countryProperty.Group)) throw new InvalidOperationException("Cannot upgrade/downgrade because there is a mortgaged property in the group");

    }

    /// <summary>
    /// Upgrading property
    /// </summary>
    /// <param name="propertyId"></param>
    /// <returns>Return list of transactions(though its usually just one)</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<TransactionInfo> UpgradeProperty(Guid propertyId)
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart)) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");


        Player currentPlayer = GetCurrentPlayer();
        Property property = Board.GetPropertyById(propertyId);
        if (property is CountryProperty countryProperty)
        {
            // Generic checks for both upgrade/downgrade
            _checkUpgradeDowngradePermission(countryProperty, currentPlayer);

            // Maxxed out bruh wyd
            if (countryProperty.CurrentRentStage == RentStage.Hotel) throw new InvalidOperationException("Cannot upgrade more in this property");

            // Broke player alert
            if (currentPlayer.Money < countryProperty.HouseCost) throw new InvalidOperationException("Not enough money to upgrade this property");

            TransactionsHistory.StartTransaction();
            TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Upgrade, currentPlayer.Id, null, countryProperty.HouseCost, true), (amount) =>
            {
                countryProperty.UpgradeRentStage();
                currentPlayer.DeductMoney(amount);
            });
            return TransactionsHistory.CommitTransaction();

        }
        else throw new InvalidOperationException("Space is not a country");

    }
    /// <summary>
    /// Downgrading the property, player gains money yay
    /// </summary>
    /// <param name="propertyId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<TransactionInfo> DowngradeProperty(Guid propertyId)
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart)) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Player currentPlayer = GetCurrentPlayer();

        Property property = Board.GetPropertyById(propertyId);
        if (property is CountryProperty countryProperty)
        {
            // Generic checks for both upgrade/downgrade
            _checkUpgradeDowngradePermission(countryProperty, currentPlayer);

            if (countryProperty.CurrentRentStage == RentStage.Unimproved) throw new InvalidOperationException("Cannot downgrade more in this property");


            TransactionsHistory.StartTransaction();
            TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Downgrade, null, currentPlayer.Id, countryProperty.HouseSellValue, true), (amount) =>
            {
                countryProperty.DownGradeRentStage();
                currentPlayer.AddMoney(amount);
            });
            return TransactionsHistory.CommitTransaction();

        }
        else throw new InvalidOperationException("Space is not a country");
    }
    #endregion
    #region Trade
    private void _validateTrade(Player initiatorPlayer, Player recipientPlayer, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, int moneyFromInitiator, int moneyFromRecipient)
    {
        // Verify initiator money
        if (initiatorPlayer.Money < moneyFromInitiator) throw new InvalidOperationException("Initiator money is invalid");
        // Verify recipient money
        if (recipientPlayer.Money < moneyFromRecipient) throw new InvalidOperationException("Recipient money is invalid");

        // Verify if property offer is valid(owned by initiator)
        bool initiatorPropertyIsValid = propertyOffer.All(property => initiatorPlayer.PropertiesOwned.Contains(property));
        if (!initiatorPropertyIsValid) throw new InvalidOperationException("Property Offer is invalid");
        // Verify if property counter offer is valid(owned by recipient)
        bool recipientPropertyIsValid = propertyCounterOffer.All(property => recipientPlayer.PropertiesOwned.Contains(property));
        if (!recipientPropertyIsValid) throw new InvalidOperationException("Property Counter Offer is invalid");

        // Verify if the property is a country, this property or other in the same group doesnt have house
        foreach(Guid propertyId in propertyOffer) {
            var property = Board.GetPropertyById(propertyId);
            if (property is CountryProperty countryProperty)
            {
                bool groupIsOwnedByPlayer = Board.GroupIsOwnedByPlayer(countryProperty.Group, initiatorPlayer.Id);
                bool noHouseInGroup = Board.NoHouseInGroup(countryProperty.Group);

                // Disallow mortgage if player has a house in a group
                if (groupIsOwnedByPlayer && !noHouseInGroup) throw new InvalidOperationException("Cannot trade country property if this or other property has house");
            }
        }
        // Verify if the property is a country, this property or other in the same group doesnt have house
        foreach(Guid propertyId in propertyCounterOffer) {
            var property = Board.GetPropertyById(propertyId);
            if (property is CountryProperty countryProperty)
            {
                bool groupIsOwnedByPlayer = Board.GroupIsOwnedByPlayer(countryProperty.Group, recipientPlayer.Id);
                bool noHouseInGroup = Board.NoHouseInGroup(countryProperty.Group);

                // Disallow mortgage if player has a house in a group
                if (groupIsOwnedByPlayer && !noHouseInGroup) throw new InvalidOperationException("Cannot trade country property if this or other property has house");
            }
        }

    }
    public Trade InitiateTrade(Guid initiatorId, Guid recipientId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, int moneyFromInitiator, int moneyFromRecipient)
    {
        Player initiatorPlayer = GetPlayerById(initiatorId) ?? throw new Exception("Invalid initiator player");
        Player recipientPlayer = GetPlayerById(recipientId) ?? throw new Exception("Invalid recipipent player");

        _validateTrade(initiatorPlayer, recipientPlayer, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);
        // Start trade

        Trade newTrade = new Trade(initiatorId, recipientId, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);

        ActiveTrades.Add(newTrade);

        return newTrade;
    }

    public (List<TransactionInfo>, Trade) AcceptTrade(Guid tradeId, Guid recipientId)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeId) ?? throw new InvalidOperationException("Invalid trade");

        if (trade.RecipientId != recipientId) throw new InvalidOperationException("Player is not permitted to perform this action");

        Player initiatorPlayer = GetPlayerById(trade.InitiatorId) ?? throw new Exception("Initiator not found");
        Player recipientPlayer = GetPlayerById(trade.RecipientId) ?? throw new Exception("Recipient not found");

        _validateTrade(initiatorPlayer, recipientPlayer, trade.PropertyOffer, trade.PropertyCounterOffer,trade.MoneyFromInitiator, trade.MoneyFromRecipient);

        // Perform money transfer
        TransactionsHistory.StartTransaction();
        if (trade.MoneyFromInitiator > 0) TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Trade, initiatorPlayer.Id, recipientPlayer.Id, trade.MoneyFromInitiator, false),
            amount =>
            {
                recipientPlayer.AddMoney(amount);
                initiatorPlayer.DeductMoney(amount);
            }
        );
        if (trade.MoneyFromRecipient > 0) TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Trade, recipientPlayer.Id, initiatorPlayer.Id, trade.MoneyFromRecipient, false),
            amount =>
            {
                initiatorPlayer.AddMoney(amount);
                recipientPlayer.DeductMoney(amount);
            }
        );

        /// Perform property transfer

        // Remove intersected property offer from initiator
        initiatorPlayer.PropertiesOwned.RemoveAll(pr => trade.PropertyOffer.Contains(pr));
        // Add property counter offer to initiator
        initiatorPlayer.PropertiesOwned.AddRange(trade.PropertyCounterOffer);

        // Change each property counter offer OwnerId to initiator Id
        var PropertyCounterOffer = Board.GetPropertiesByGuidList(trade.PropertyCounterOffer);
        foreach (Property property in PropertyCounterOffer)
        {
            property.ChangeOwner(initiatorPlayer.Id);
        }


        // Remove intersected property counter offer from recipient
        recipientPlayer.PropertiesOwned.RemoveAll(pr => trade.PropertyCounterOffer.Contains(pr));
        // Add property offer to recipient
        recipientPlayer.PropertiesOwned.AddRange(trade.PropertyOffer);

        // Change each property offer OwnerId to recipient Id
        var PropertyOffer = Board.GetPropertiesByGuidList(trade.PropertyOffer);
        foreach (Property property in PropertyOffer)
        {
            property.ChangeOwner(recipientPlayer.Id);
        }
        
        ActiveTrades.Remove(trade);

        return (TransactionsHistory.CommitTransaction(), trade);
    }
    public void RejectTrade(Guid tradeId, Guid recipientId)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeId) ?? throw new InvalidOperationException("Invalid trade");

        if (trade.RecipientId != recipientId) throw new InvalidOperationException("Player is not permitted to perform this action");

        ActiveTrades.Remove(trade);
    }
    public void CancelTrade(Guid tradeId, Guid initiatorId)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeId) ?? throw new InvalidOperationException("Invalid trade");

        if (trade.InitiatorId != initiatorId) throw new InvalidOperationException("Player is not permitted to perform this action");

        ActiveTrades.Remove(trade);
    }
    public Trade NegotiateTrade(Guid negotiatorId, Guid tradeId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, int moneyFromInitiator, int moneyFromRecipient)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeId) ?? throw new InvalidOperationException("Invalid trade");

        if (trade.RecipientId != negotiatorId) throw new InvalidOperationException("Player is not permitted to perform this action");

        Player initiatorPlayer = GetPlayerById(trade.InitiatorId) ?? throw new Exception("Initiator not found");
        Player recipientPlayer = GetPlayerById(trade.RecipientId) ?? throw new Exception("Recipient not found");

        _validateTrade(initiatorPlayer, recipientPlayer, trade.PropertyOffer, trade.PropertyCounterOffer,trade.MoneyFromInitiator, trade.MoneyFromRecipient);

        trade.Negotiate(propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);

        return trade;
    }
    
    #endregion

}
