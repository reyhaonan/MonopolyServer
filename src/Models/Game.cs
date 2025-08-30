using System.Text.Json.Serialization;
using MonopolyServer.Enums;
using MonopolyServer.Utils;

namespace MonopolyServer.Models;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class Game
{
    const int SALARY_AMOUNT = 200;

    #region Private property
    private readonly ILogger _logger;
    private const int MaxConsecutiveDoubles = 3;
    private static readonly Random _random = new Random();
    private int _diceRoll1 = 0;
    private int _diceRoll2 = 0;
    private int _totalDiceRoll = 0;
    private int _freeParkingPot = 0;
    #endregion

    #region Public property
    public int CurrentPlayerIndex { get; private set; } = -1;
    [JsonInclude]
    public GameConfig GameConfig;
    [JsonInclude]
    public Guid GameId { get; init; }

    // List of all active players (still playing)
    [JsonInclude]
    public List<Player> ActivePlayers { get; private set; } = [];
    [JsonInclude]
    public Board Board { get; private set; }
    [JsonInclude]
    public List<Trade> ActiveTrades { get; private set; } = [];

    // Card decks are simplified for now, could be objects
    [JsonInclude]
    public GamePhase CurrentPhase { get; private set; }

    [JsonInclude]
    public TransactionHistory TransactionsHistory { get; init; }
    #endregion

    /// <summary>
    /// Constructor for Game. Initializes a new game with a unique ID,
    /// creates a new board, sets the initial game phase to WaitingForPlayers,
    /// and initializes the card decks.
    /// </summary>
    /// <param name="logger">The logger instance provided by dependency injection.</param>
    public Game(ILogger<Game> logger)
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
    /// Adds a new player to the game.
    /// </summary>
    /// <param name="playerName">The name of the new player.</param>
    /// <param name="hexColor">The hexadecimal color string for the player's token.</param>
    /// <param name="newPlayerId">The unique ID for the new player.</param>
    /// <returns>The newly created Player object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the room is full.</exception>
    public Player AddPlayer(string playerName, string hexColor, Guid newPlayerId)
    {
        if (ActivePlayers.Count >= GameConfig.MaxPlayers)
        {
            throw new InvalidOperationException("Room is full");
        }

        var newPlayer = new Player(playerName, hexColor, newPlayerId);
        ActivePlayers.Add(newPlayer);
        return newPlayer;
    }

    /// <summary>
    /// Gets the current player whose turn it is.
    /// </summary>
    /// <returns>The current player object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if there is no active player or the index is invalid.</exception>
    public Player GetCurrentPlayer()
    {
        if (CurrentPlayerIndex < 0 || CurrentPlayerIndex >= ActivePlayers.Count)
        {
            throw new InvalidOperationException("No current player or invalid index.");
        }
        return ActivePlayers[CurrentPlayerIndex];
    }

    /// <summary>
    /// Gets a player by their unique ID.
    /// </summary>
    /// <param name="playerId">The unique ID of the player to find.</param>
    /// <returns>The player with the specified ID, or null if not found.</returns>
    public Player? GetPlayerById(Guid playerId)
    {
        return ActivePlayers.FirstOrDefault(p => p.Id == playerId);
    }

    /// <summary>
    /// Checks if a player with the given ID is currently in the game.
    /// </summary>
    /// <param name="playerId">The unique ID of the player to check.</param>
    /// <returns>True if the player is in the game, false otherwise.</returns>
    public bool PlayerIsInGame(Guid playerId)
    {
        return ActivePlayers.Any(p => p.Id == playerId);
    }
    #endregion

    /// <summary>
    /// Initializes the Chance and Community Chest card decks.
    /// Currently a placeholder as the implementation is simplified.
    /// </summary>
    private void InitializeDecks()
    {
        // Populate with example cards (will need full card logic later)
    }

    /// <summary>
    /// Gets the space at a specific position on the board.
    /// </summary>
    /// <param name="position">The position on the board (0-39).</param>
    /// <returns>The space at the specified position, or null if the position is invalid.</returns>
    public Space? GetSpaceAtPosition(int position)
    {
        if (position < 0 || position >= Board.Spaces.Count)
        {
            return null;
        }
        return Board.Spaces[position];
    }

    #region Turn Management
    /// <summary>
    /// Advances to the next player in turn order.
    /// Cycles back to the first player after the last player.
    /// </summary>
    /// <returns>The index of the next player.</returns>
    private int NextPlayer()
    {
        _logger.LogInformation($"Invoked next player {CurrentPlayerIndex}, Count: {ActivePlayers.Count}");

        // Use the modulo operator to loop back to 0
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % ActivePlayers.Count;

        return CurrentPlayerIndex;
    }
    #endregion

    #region Jail Handling
    /// <summary>
    /// Allows a player to pay $50 to get out of jail immediately.
    /// </summary>
    /// <returns>A list of transaction information for the jail payment.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the current phase is not PlayerTurnStart or the player is not in jail or doesn't have enough money.</exception>
    public List<TransactionInfo> PayToGetOutOfJail()
    {
        if (CurrentPhase != GamePhase.PlayerTurnStart)
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");
        }

        Player currentPlayer = GetCurrentPlayer();

        if (!currentPlayer.IsInJail)
        {
            throw new InvalidOperationException("Player is not in jail.");
        }

        if (currentPlayer.Money < GameConfig.JailFine)
        {
            throw new InvalidOperationException("Not enough money to pay the jail fee.");
        }

        TransactionsHistory.StartTransaction();
        TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.FreeFromJail, currentPlayer.Id, null, GameConfig.JailFine, true), (amount) =>
        {
            currentPlayer.DeductMoney(amount);
            currentPlayer.FreeFromJail();
        });

        return TransactionsHistory.CommitTransaction();
    }

    /// <summary>
    /// Allows a player to use a "Get Out of Jail Free" card if they have one.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the player is not in jail or doesn't have a Get Out of Jail Free card.</exception>
    public void UseGetOutOfJailCard()
    {
        if (CurrentPhase != GamePhase.PlayerTurnStart)
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");
        }

        Player currentPlayer = GetCurrentPlayer();

        if (!currentPlayer.IsInJail)
        {
            throw new InvalidOperationException("Player is not in jail.");
        }

        if (currentPlayer.GetOutOfJailFreeCards <= 0)
        {
            throw new InvalidOperationException("Player doesn't have any Get Out of Jail Free cards.");
        }

        // Use the player's Get Out of Jail Free card
        currentPlayer.UseGetOutOfJailFreeCard();
        currentPlayer.FreeFromJail();
    }
    #endregion

    #region Game flow
    /// <summary>
    /// Starts the game by randomizing player order, setting the first player,
    /// and changing the game phase from WaitingForPlayers to PlayerTurnStart.
    /// </summary>
    /// <returns>The list of active players in their randomized order.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the game has already started or not enough players are present.</exception>
    public List<Player> StartGame()
    {
        if (ActivePlayers.Count < GameConfig.MinPlayers)
        {
            throw new InvalidOperationException("Cannot start a game with fewer than the minimum number of players.");
        }

        if (CurrentPhase != GamePhase.WaitingForPlayers)
        {
            throw new InvalidOperationException($"Game {GameId} has already started.");
        }

        ActivePlayers = ActivePlayers.OrderBy(_ => _random.Next()).ToList();
        CurrentPlayerIndex = 0;
        ChangeGamePhase(GamePhase.PlayerTurnStart);

        // Correct the starting money
        foreach (Player player in ActivePlayers)
        {
            player.setMoney(GameConfig.StartingMoney);
        }

        return ActivePlayers;
    }

    /// <summary>
    /// Updates the game configuration settings.
    /// </summary>
    /// <param name="newGameConfig">The new GameConfig object with updated settings.</param>
    /// <exception cref="InvalidOperationException">Thrown if the game is not in the WaitingForPlayers phase.</exception>
    public void UpdateGameConfig(GameConfig newGameConfig)
    {
        if (CurrentPhase != GamePhase.WaitingForPlayers)
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action.");
        }

        GameConfig.FreeParkingPot = newGameConfig.FreeParkingPot;
        GameConfig.DoubleBaseRentOnFullColorSet = newGameConfig.DoubleBaseRentOnFullColorSet;
        GameConfig.AllowCollectRentOnJail = newGameConfig.AllowCollectRentOnJail;
        GameConfig.AllowMortgagingProperties = newGameConfig.AllowMortgagingProperties;
        GameConfig.BalancedHousePurchase = newGameConfig.BalancedHousePurchase;

        // Clamp the starting money to a reasonable range
        GameConfig.StartingMoney = Math.Clamp(newGameConfig.StartingMoney, 500, 3000);
    }

    #region Dice rolling handling
    /// <summary>
    /// Simulates the physical rolling of two dice.
    /// </summary>
    /// <returns>A tuple containing the result of each dice roll.</returns>
    private static (int, int) RollPhysicalDice()
    {
        // Corrected to roll a random number between 1 and 6 for each die.
        // int dice1 = 1;
        // int dice2 = 1;
        int dice1 = _random.Next(1, 7);
        int dice2 = _random.Next(1, 7);
        return (dice1, dice2);
    }

    private void HandleDiceRollConsequences(Player currentPlayer, int dice1, int dice2)
    {
        bool isDoubles = dice1 == dice2;
        int totalDiceRoll = dice1 + dice2;

        if (currentPlayer.IsInJail)
        {
            HandleInJailRoll(currentPlayer, totalDiceRoll, isDoubles);
        }
        else
        {
            HandleRegularRoll(currentPlayer, totalDiceRoll, isDoubles);
        }
    }

    private void HandleInJailRoll(Player player, int totalDiceRoll, bool isDoubles)
    {
        if (isDoubles)
        {
            // Player rolled doubles, they get out of jail and move.
            _logger.LogInformation($"Player {player.Name} rolled doubles and got out of jail!");
            player.FreeFromJail();
            player.ResetConsecutiveDouble();
            _totalDiceRoll = totalDiceRoll;
        }
        else
        {
            // Did not roll doubles. Reduce remaining time in jail.
            player.ReduceJailTurnRemaining();
            if (player.JailTurnsRemaining == 0)
            {
                // 3rd failed attempt. Player must pay the fine and then moves.
                _logger.LogInformation($"Player {player.Name} must pay the fine to get out of jail.");
                TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.FreeFromJail, player.Id, null, GameConfig.JailFine, true), (amount) =>
                {
                    player.DeductMoney(amount);
                    player.FreeFromJail();
                });
                _totalDiceRoll = totalDiceRoll;
            }
            else
            {
                // 1st or 2nd failed attempt. Turn ends, no movement.
                _logger.LogInformation($"Player {player.Name} did not roll doubles and remains in jail.");
                _totalDiceRoll = 0;
            }
        }
    }

    private void HandleRegularRoll(Player player, int totalDiceRoll, bool isDoubles)
    {
        if (isDoubles)
        {
            player.AddConsecutiveDouble();
            if (player.ConsecutiveDoubles >= MaxConsecutiveDoubles)
            {
                // Rolled 3 consecutive doubles. Go to jail, no movement.
                _logger.LogInformation($"Player {player.Name} rolled three consecutive doubles and is sent to jail!");
                player.GoToJail();
                _totalDiceRoll = 0;
            }
            else
            {
                // Standard double roll. Player will move and roll again.
                _totalDiceRoll = totalDiceRoll;
            }
        }
        else
        {
            // Not a double roll. Reset the counter and move.
            player.ResetConsecutiveDouble();
            _totalDiceRoll = totalDiceRoll;
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

        var space = GetSpaceAtPosition(currentPlayer.CurrentPosition) ?? throw new InvalidOperationException("Invalid space.");

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
                TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Fine, currentPlayer.Id, null, GameConfig.IncomeTax, true),
                    (amount) =>
                    {
                        _freeParkingPot += amount;
                        currentPlayer.DeductMoney(amount);
                    });
                break;
            case SpecialSpaceType.LuxuryTax:
                TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Fine, currentPlayer.Id, null, GameConfig.LuxuryTax, true),
                    (amount) =>
                    {
                        _freeParkingPot += amount;
                        currentPlayer.DeductMoney(amount);
                    });
                break;
            case SpecialSpaceType.FreeParking:
                // Check if the game config allows collecting from the Free Parking pot.
                if (GameConfig.FreeParkingPot)
                {
                    TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Reward, null, currentPlayer.Id, _freeParkingPot, true),
                        (amount) =>
                        {
                            currentPlayer.AddMoney(amount);
                            _freeParkingPot = 0;
                        });
                }
                break;
            // Other cases (Chance, CommunityChest, etc.) would go here
            default:
                // No action needed for spaces like Just Visiting, Go, etc.
                break;
        }
    }

    /// <summary>
    /// Processes actions for landing on a Property, primarily handling rent payment.
    /// </summary>
    private void ProcessPropertyLanding(Player currentPlayer, Property property, int totalDiceRoll)
    {
        // No action if landed on your own property or an unowned one.
        if (!property.IsOwnedByOtherPlayer(currentPlayer.Id))
        {
            return;
        }

        var ownerId = property.OwnerId ?? throw new InvalidOperationException("Property is owned but has no OwnerId.");
        Player owner = GetPlayerById(ownerId) ?? throw new InvalidOperationException("Owner not found.");

        // Check if the owner is in jail and if the game config allows rent collection.
        if (owner.IsInJail && !GameConfig.AllowCollectRentOnJail)
        {
            return;
        }

        int rentValue = 0;

        // Use polymorphism to handle different property types.
        if (property is CountryProperty countryProperty)
        {
            var groupIsOwnedByPlayer = Board.GroupIsOwnedByPlayer(countryProperty.Group, owner.Id);
            rentValue = countryProperty.CalculateRent(doubleBaseRent: groupIsOwnedByPlayer && GameConfig.DoubleBaseRentOnFullColorSet);
        }
        else if (property is UtilityProperty utilityProperty)
        {
            var utilityCount = Board.GetUtilityOwnedByPlayer(ownerId).Count;
            rentValue = utilityProperty.CalculateRent(diceRoll: totalDiceRoll, ownerUtilities: utilityCount);
        }
        else if (property is RailroadProperty railroadProperty)
        {
            var railroadCount = Board.GetRailroadOwnedByPlayer(ownerId).Count;
            rentValue = railroadProperty.CalculateRent(ownerRailroads: railroadCount);
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
    /// Rolls the dice for the current player's turn, handles movement and special cases.
    /// </summary>
    /// <returns>A RollResult object containing the dice and player state information.</returns>
    /// <exception cref="InvalidOperationException">Thrown if not in the PlayerTurnStart phase.</exception>
    public RollResult RollDice()
    {
        if (CurrentPhase != GamePhase.PlayerTurnStart)
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action.");
        }

        ChangeGamePhase(GamePhase.RollingDice);
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer.Money < 0) throw new InvalidOperationException("Player is in debt");
        // Reset total dice roll for the current turn.
        _totalDiceRoll = 0;
        (_diceRoll1, _diceRoll2) = RollPhysicalDice();
        TransactionsHistory.StartTransaction();
        HandleDiceRollConsequences(currentPlayer, _diceRoll1, _diceRoll2);

        // Only move if the player is not going to jail or is not in jail after the roll.
        bool passedStart = false;
        if (!currentPlayer.IsInJail)
        {
            ChangeGamePhase(GamePhase.MovingToken);
            passedStart = currentPlayer.MoveBy(_totalDiceRoll);
            _logger.LogInformation($"Player moved to position {currentPlayer.CurrentPosition}");
        }

        // Handle all actions related to landing on a new space
        ChangeGamePhase(GamePhase.LandingOnSpaceAction);
        HandleLandingActions(currentPlayer, passedStart, _totalDiceRoll);
        var transactionInfo = TransactionsHistory.CommitTransaction();

        // Finalize the dice rolling process and return the result
        if (currentPlayer.ConsecutiveDoubles > 0 && !currentPlayer.IsInJail)
        {
            ChangeGamePhase(GamePhase.PlayerTurnStart); // Player gets another turn
        }
        else
        {
            ChangeGamePhase(GamePhase.PostLandingActions); // Awaiting player actions
        }

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
    /// </summary>
    /// <returns>The index of the next player.</returns>
    /// <exception cref="InvalidOperationException">Thrown if not in the PostLandingActions phase or if the player has negative money.</exception>
    public int EndTurn()
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions))
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action.");
        }

        Player currentPlayer = GetCurrentPlayer();
        if (currentPlayer.Money < 0)
        {
            throw new InvalidOperationException("You are broke. Declare bankruptcy to proceed.");
        }

        ChangeGamePhase(GamePhase.PlayerTurnStart);

        // If the player rolled doubles and isn't in jail, they get another turn.
        if (currentPlayer.ConsecutiveDoubles > 0 && !currentPlayer.IsInJail)
        {
            return CurrentPlayerIndex;
        }

        return NextPlayer();
    }

    /// <summary>
    /// Handles a player declaring bankruptcy, removing them from the game.
    /// </summary>
    /// <param name="playerId">The ID of the player declaring bankruptcy.</param>
    /// <returns>A tuple containing the new current player index and a boolean indicating if the game is over.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the player is not found.</exception>
    public (int currentPlayerIndex, bool isGameOver) DeclareBankcruptcy(Guid playerId)
    {
        Player bankruptPlayer = GetPlayerById(playerId) ?? throw new InvalidOperationException("Player not found.");

        foreach (Guid propertyId in bankruptPlayer.PropertiesOwned)
        {
            Board.GetPropertyById(propertyId).ResetProperty();
        }

        bool isActivePlayer = GetCurrentPlayer().Id == bankruptPlayer.Id;

        ActivePlayers.Remove(bankruptPlayer);

        // Game over
        if (ActivePlayers.Count <= 1)
        {
            ChangeGamePhase(GamePhase.GameOver);
        }
        else if (isActivePlayer)
        {
            ChangeGamePhase(GamePhase.PlayerTurnStart);
        }

        // Adjust the current player index if a player before them was removed.
        CurrentPlayerIndex %= ActivePlayers.Count;
        return (CurrentPlayerIndex, ActivePlayers.Count <= 1);
    }

    #endregion

    #region Property Management
    /// <summary>
    /// Allows the current player to buy the property they have landed on.
    /// </summary>
    /// <returns>A tuple containing the ID of the purchased property and the transaction details.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the game phase is inappropriate, the property is already owned, the player has insufficient funds, or the space is not a purchasable property.</exception>
    public (Guid, List<TransactionInfo>) BuyProperty()
    {
        Player currentPlayer = GetCurrentPlayer();
        // Action is only available on: [PostLandingActions, or on consecutive double and PlayerTurnStart]
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && (!CurrentPhase.Equals(GamePhase.PlayerTurnStart) || currentPlayer.ConsecutiveDoubles <= 0))
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action.");
        }

        var space = GetSpaceAtPosition(currentPlayer.CurrentPosition);

        if (space is Property property)
        {
            if (property.OwnerId != null)
            {
                throw new InvalidOperationException("This property is already owned.");
            }

            if (currentPlayer.Money < property.PurchasePrice)
            {
                throw new InvalidOperationException("Not enough money to buy this property.");
            }

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
            throw new InvalidOperationException("This space is not a property that can be purchased.");
        }
    }

    /// <summary>
    /// Sells a property back to the bank for half its original purchase price.
    /// </summary>
    /// <param name="propertyId">The ID of the property to sell.</param>
    /// <returns>A list of transaction information.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the game phase is inappropriate, the property is not owned by the player, or it has houses on it.</exception>
    public List<TransactionInfo> SellProperty(Guid propertyId)
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart))
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action.");
        }

        Property property = Board.GetPropertyById(propertyId);
        Player currentPlayer = GetCurrentPlayer();

        if (!property.IsOwnedByPlayer(currentPlayer.Id))
        {
            throw new InvalidOperationException($"{currentPlayer.Name} is not permitted to sell this property.");
        }

        if (property is CountryProperty countryProperty)
        {
            // Disallow selling property if it has houses
            if (countryProperty.CurrentRentStage != RentStage.Unimproved)
            {
                throw new InvalidOperationException("Cannot sell property with houses or a hotel.");
            }

            // Disallow selling if other properties in the same group have houses.
            bool groupIsOwnedByPlayer = Board.GroupIsOwnedByPlayer(countryProperty.Group, currentPlayer.Id);
            bool noHouseInGroup = Board.NoHouseInGroup(countryProperty.Group);

            if (groupIsOwnedByPlayer && !noHouseInGroup)
            {
                throw new InvalidOperationException("Cannot sell property if other properties in the same color group have houses.");
            }
        }

        // You can sell a mortgaged property, but its value is 0.
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
    /// Mortgages a property, giving the player its mortgage value.
    /// </summary>
    /// <param name="propertyId">The ID of the property to mortgage.</param>
    /// <returns>A list of transaction information.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the game phase is inappropriate, mortgaging is not allowed, or the property cannot be mortgaged.</exception>
    public List<TransactionInfo> MortgageProperty(Guid propertyId)
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart))
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action.");
        }

        if (!GameConfig.AllowMortgagingProperties)
        {
            throw new InvalidOperationException("Current game does not allow mortgaging properties.");
        }

        Property property = Board.GetPropertyById(propertyId);
        Player currentPlayer = GetCurrentPlayer();

        if (!property.IsOwnedByPlayer(currentPlayer.Id))
        {
            throw new InvalidOperationException("Property is not owned by this player.");
        }

        if (property is CountryProperty countryProperty)
        {
            if (!countryProperty.CurrentRentStage.Equals(RentStage.Unimproved))
            {
                throw new InvalidOperationException("Can't mortgage a property with houses.");
            }

            bool groupIsOwnedByPlayer = Board.GroupIsOwnedByPlayer(countryProperty.Group, currentPlayer.Id);
            bool noHouseInGroup = Board.NoHouseInGroup(countryProperty.Group);

            if (groupIsOwnedByPlayer && !noHouseInGroup)
            {
                throw new InvalidOperationException("Cannot mortgage if other properties in the same group have houses.");
            }
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
    /// Unmortgages a property by paying the unmortgage cost.
    /// </summary>
    /// <param name="propertyId">The ID of the property to unmortgage.</param>
    /// <returns>A list of transaction information.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the game phase is inappropriate, unmortgaging is not allowed, the property is not owned, or the player has insufficient funds.</exception>
    public List<TransactionInfo> UnmortgageProperty(Guid propertyId)
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart))
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action.");
        }

        if (!GameConfig.AllowMortgagingProperties)
        {
            throw new InvalidOperationException("Current game does not allow mortgaging.");
        }

        Property property = Board.GetPropertyById(propertyId);
        Player currentPlayer = GetCurrentPlayer();

        if (!property.IsOwnedByPlayer(currentPlayer.Id))
        {
            throw new InvalidOperationException("Property is not owned by this player.");
        }

        if (!property.IsMortgaged)
        {
            throw new InvalidOperationException("Property is not mortgaged.");
        }

        if (currentPlayer.Money < property.UnmortgageCost)
        {
            throw new InvalidOperationException("Not enough money to unmortgage this property.");
        }

        TransactionsHistory.StartTransaction();
        TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Unmortgage, currentPlayer.Id, null, property.UnmortgageCost, true), (amount) =>
        {
            property.UnmortgageProperty();
            currentPlayer.DeductMoney(amount);
        });
        return TransactionsHistory.CommitTransaction();
    }

    /// <summary>
    /// Generic checking for countryProperty upgrade or downgrade.
    /// </summary>
    /// <param name="countryProperty">The property to check.</param>
    /// <param name="currentPlayer">The player attempting the action.</param>
    /// <exception cref="InvalidOperationException">Thrown if the property is mortgaged, not owned by the player, or the player doesn't own the full color group.</exception>
    private void _checkUpgradeDowngradePermission(CountryProperty countryProperty, Player currentPlayer)
    {
        if (countryProperty.IsMortgaged)
        {
            throw new InvalidOperationException("Cannot upgrade/downgrade mortgaged property.");
        }

        if (countryProperty.OwnerId != currentPlayer.Id)
        {
            throw new InvalidOperationException("This player is not permitted to upgrade/downgrade the property.");
        }

        if (!Board.GroupIsOwnedByPlayer(countryProperty.Group, currentPlayer.Id))
        {
            throw new InvalidOperationException("Cannot perform upgrade/downgrade because the player does not own this entire group.");
        }

        if (!Board.NoMortgagedPropertyInGroup(countryProperty.Group))
        {
            throw new InvalidOperationException("Cannot upgrade/downgrade because there is a mortgaged property in the group.");
        }
    }

    /// <summary>
    /// Upgrades a country property by building a house or hotel.
    /// </summary>
    /// <param name="propertyId">The ID of the property to upgrade.</param>
    /// <returns>A list of transactions for the upgrade.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the game phase is inappropriate or the property cannot be upgraded.</exception>
    public List<TransactionInfo> UpgradeProperty(Guid propertyId)
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart))
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action.");
        }

        Player currentPlayer = GetCurrentPlayer();
        Property property = Board.GetPropertyById(propertyId);

        if (property is CountryProperty countryProperty)
        {
            if (currentPlayer.Money < countryProperty.HouseCost)
            {
                throw new InvalidOperationException("Not enough money to upgrade this property.");
            }

            _checkUpgradeDowngradePermission(countryProperty, currentPlayer);

            if (countryProperty.CurrentRentStage == RentStage.Hotel)
            {
                throw new InvalidOperationException("Cannot upgrade this property further.");
            }

            if (GameConfig.BalancedHousePurchase && Board.LowestRentStateInGroup(countryProperty.Group) != countryProperty.CurrentRentStage)
            {
                throw new InvalidOperationException("Cannot purchase an unbalanced house. You must build evenly across the color group.");
            }

            TransactionsHistory.StartTransaction();
            TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Upgrade, currentPlayer.Id, null, countryProperty.HouseCost, true), (amount) =>
            {
                countryProperty.UpgradeRentStage();
                currentPlayer.DeductMoney(amount);
            });
            return TransactionsHistory.CommitTransaction();
        }
        else
        {
            throw new InvalidOperationException("This space is not a country property that can be upgraded.");
        }
    }

    /// <summary>
    /// Downgrades a country property, selling a house or hotel back to the bank for money.
    /// </summary>
    /// <param name="propertyId">The ID of the property to downgrade.</param>
    /// <returns>A list of transactions for the downgrade.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the game phase is inappropriate or the property cannot be downgraded.</exception>
    public List<TransactionInfo> DowngradeProperty(Guid propertyId)
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart))
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action.");
        }

        Player currentPlayer = GetCurrentPlayer();
        Property property = Board.GetPropertyById(propertyId);

        if (property is CountryProperty countryProperty)
        {
            _checkUpgradeDowngradePermission(countryProperty, currentPlayer);

            if (countryProperty.CurrentRentStage == RentStage.Unimproved)
            {
                throw new InvalidOperationException("Cannot downgrade this property further; it has no houses or hotels.");
            }

            if (GameConfig.BalancedHousePurchase && Board.HighestRentStateInGroup(countryProperty.Group) != countryProperty.CurrentRentStage)
            {
                throw new InvalidOperationException("Cannot sell an unbalanced house. You must sell evenly across the color group.");
            }

            TransactionsHistory.StartTransaction();
            TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Downgrade, null, currentPlayer.Id, countryProperty.HouseSellValue, true), (amount) =>
            {
                countryProperty.DownGradeRentStage();
                currentPlayer.AddMoney(amount);
            });
            return TransactionsHistory.CommitTransaction();
        }
        else
        {
            throw new InvalidOperationException("This space is not a country property that can be downgraded.");
        }
    }
    #endregion

    #region Trade
    /// <summary>
    /// Validates a trade proposal to ensure all properties and money are valid.
    /// </summary>
    /// <param name="initiatorPlayer">The player initiating the trade.</param>
    /// <param name="recipientPlayer">The player receiving the trade offer.</param>
    /// <param name="propertyOffer">The properties offered by the initiator.</param>
    /// <param name="propertyCounterOffer">The properties offered by the recipient.</param>
    /// <param name="moneyFromInitiator">The money offered by the initiator.</param>
    /// <param name="moneyFromRecipient">The money offered by the recipient.</param>
    /// <exception cref="InvalidOperationException">Thrown if the trade is invalid (e.g., insufficient funds, properties not owned, or properties with houses).</exception>
    private void _validateTrade(Player initiatorPlayer, Player recipientPlayer, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, int moneyFromInitiator, int moneyFromRecipient, int getOutOfJailCardFromInitiator, int getOutOfJailCardFromRecipient)
    {
        // Verify get out of free card
        if (initiatorPlayer.GetOutOfJailFreeCards < getOutOfJailCardFromInitiator) throw new InvalidOperationException("Initiator doesn't have enough get out of jail card");
        if (recipientPlayer.GetOutOfJailFreeCards < getOutOfJailCardFromRecipient) throw new InvalidOperationException("Recipient doesn't have enough get out of jail card");

        // Verify money
        if (initiatorPlayer.Money > 0 && initiatorPlayer.Money < moneyFromInitiator) throw new InvalidOperationException("Initiator's money is invalid.");
        if (recipientPlayer.Money > 0 && recipientPlayer.Money < moneyFromRecipient) throw new InvalidOperationException("Recipient's money is invalid.");

        // Verify property ownership
        bool initiatorPropertyIsValid = propertyOffer.All(property => initiatorPlayer.PropertiesOwned.Contains(property));
        if (!initiatorPropertyIsValid) throw new InvalidOperationException("One or more properties in the initiator's offer are not owned by them.");

        bool recipientPropertyIsValid = propertyCounterOffer.All(property => recipientPlayer.PropertiesOwned.Contains(property));
        if (!recipientPropertyIsValid) throw new InvalidOperationException("One or more properties in the recipient's counter-offer are not owned by them.");

        // Verify properties do not have houses
        foreach (Guid propertyId in propertyOffer.Concat(propertyCounterOffer))
        {
            var property = Board.GetPropertyById(propertyId);
            if (property is CountryProperty countryProperty)
            {
                // Disallow trading properties within a group that has houses.
                bool groupIsOwnedByPlayer = Board.GroupIsOwnedByPlayer(countryProperty.Group, property.OwnerId ?? Guid.Empty);
                bool noHouseInGroup = Board.NoHouseInGroup(countryProperty.Group);
                if (groupIsOwnedByPlayer && !noHouseInGroup)
                {
                    throw new InvalidOperationException("Cannot trade country property if it or another property in its group has a house.");
                }
            }
        }
    }

    /// <summary>
    /// Initiates a new trade proposal between two players.
    /// </summary>
    /// <param name="initiatorId">The ID of the player initiating the trade.</param>
    /// <param name="recipientId">The ID of the player the trade is offered to.</param>
    /// <param name="propertyOffer">A list of properties offered by the initiator.</param>
    /// <param name="propertyCounterOffer">A list of properties offered by the recipient.</param>
    /// <param name="moneyFromInitiator">The amount of money offered by the initiator.</param>
    /// <param name="moneyFromRecipient">The amount of money offered by the recipient.</param>
    /// <returns>The newly created Trade object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the players or trade details are invalid.</exception>
    public Trade InitiateTrade(Guid initiatorId, Guid recipientId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, int moneyFromInitiator, int moneyFromRecipient, int getOutOfJailCardFromInitiator, int getOutOfJailCardFromRecipient)
    {
        Player initiatorPlayer = GetPlayerById(initiatorId) ?? throw new InvalidOperationException("Invalid initiator player.");
        Player recipientPlayer = GetPlayerById(recipientId) ?? throw new InvalidOperationException("Invalid recipient player.");

        _validateTrade(initiatorPlayer, recipientPlayer, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient, getOutOfJailCardFromInitiator, getOutOfJailCardFromRecipient);

        Trade newTrade = new Trade(initiatorId, recipientId, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient, getOutOfJailCardFromInitiator, getOutOfJailCardFromRecipient);
        ActiveTrades.Add(newTrade);
        return newTrade;
    }

    /// <summary>
    /// Accepts a trade proposal and executes the property and money transfers.
    /// </summary>
    /// <param name="tradeId">The ID of the trade to accept.</param>
    /// <param name="recipientId">The ID of the player accepting the trade.</param>
    /// <returns>A tuple containing the transaction details and the accepted trade object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the trade or players are invalid, or the player is not authorized to accept.</exception>
    public (List<TransactionInfo>, Trade) AcceptTrade(Guid tradeId, Guid recipientId)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeId) ?? throw new InvalidOperationException("Invalid trade.");

        if (trade.RecipientId != recipientId) throw new InvalidOperationException("Player is not permitted to perform this action.");

        Player initiatorPlayer = GetPlayerById(trade.InitiatorId) ?? throw new InvalidOperationException("Initiator not found.");
        Player recipientPlayer = GetPlayerById(trade.RecipientId) ?? throw new InvalidOperationException("Recipient not found.");

        _validateTrade(initiatorPlayer, recipientPlayer, trade.PropertyOffer, trade.PropertyCounterOffer, trade.MoneyFromInitiator, trade.MoneyFromRecipient, trade.GetOutOfJailCardFromInitiator, trade.GetOutOfJailCardFromRecipient);

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

        // Perform property transfer
        initiatorPlayer.PropertiesOwned.RemoveAll(pr => trade.PropertyOffer.Contains(pr));
        initiatorPlayer.PropertiesOwned.AddRange(trade.PropertyCounterOffer);

        var propertyCounterOffer = Board.GetPropertiesByGuidList(trade.PropertyCounterOffer);
        foreach (Property property in propertyCounterOffer)
        {
            property.ChangeOwner(initiatorPlayer.Id);
        }

        recipientPlayer.PropertiesOwned.RemoveAll(pr => trade.PropertyCounterOffer.Contains(pr));
        recipientPlayer.PropertiesOwned.AddRange(trade.PropertyOffer);

        var propertyOffer = Board.GetPropertiesByGuidList(trade.PropertyOffer);
        foreach (Property property in propertyOffer)
        {
            property.ChangeOwner(recipientPlayer.Id);
        }
        Console.WriteLine("TTrade", JsonSerializer.Serialize<Trade>(trade));
        // Perform get out of jail card transfer
        if (trade.GetOutOfJailCardFromInitiator > 0)
        {
            initiatorPlayer.AddGetOutOfJailFreeCard(trade.GetOutOfJailCardFromInitiator * -1);
            recipientPlayer.AddGetOutOfJailFreeCard(trade.GetOutOfJailCardFromInitiator);
        }
        if (trade.GetOutOfJailCardFromRecipient > 0)
        {
            recipientPlayer.AddGetOutOfJailFreeCard(trade.GetOutOfJailCardFromRecipient * -1);
            initiatorPlayer.AddGetOutOfJailFreeCard(trade.GetOutOfJailCardFromRecipient);
        }

        ActiveTrades.Remove(trade);

        return (TransactionsHistory.CommitTransaction(), trade);
    }

    /// <summary>
    /// Rejects a trade proposal.
    /// </summary>
    /// <param name="tradeId">The ID of the trade to reject.</param>
    /// <param name="recipientId">The ID of the player rejecting the trade.</param>
    /// <exception cref="InvalidOperationException">Thrown if the trade or player is invalid.</exception>
    public void RejectTrade(Guid tradeId, Guid recipientId)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeId) ?? throw new InvalidOperationException("Invalid trade.");
        if (trade.RecipientId != recipientId) throw new InvalidOperationException("Player is not permitted to perform this action.");
        ActiveTrades.Remove(trade);
    }

    /// <summary>
    /// Cancels a trade initiated by the current player.
    /// </summary>
    /// <param name="tradeId">The ID of the trade to cancel.</param>
    /// <param name="initiatorId">The ID of the player cancelling the trade.</param>
    /// <exception cref="InvalidOperationException">Thrown if the trade or player is invalid.</exception>
    public void CancelTrade(Guid tradeId, Guid initiatorId)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeId) ?? throw new InvalidOperationException("Invalid trade.");
        if (trade.InitiatorId != initiatorId) throw new InvalidOperationException("Player is not permitted to perform this action.");
        ActiveTrades.Remove(trade);
    }

    /// <summary>
    /// Negotiates a new trade proposal based on an existing one.
    /// </summary>
    /// <param name="negotiatorId">The ID of the player negotiating.</param>
    /// <param name="tradeId">The ID of the trade being negotiated.</param>
    /// <param name="propertyOffer">The new list of properties from the initiator.</param>
    /// <param name="propertyCounterOffer">The new list of properties from the recipient.</param>
    /// <param name="moneyFromInitiator">The new amount of money from the initiator.</param>
    /// <param name="moneyFromRecipient">The new amount of money from the recipient.</param>
    /// <returns>The updated Trade object.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the trade or player is invalid.</exception>
    public Trade NegotiateTrade(Guid negotiatorId, Guid tradeId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, int moneyFromInitiator, int moneyFromRecipient, int getOutOfJailCardFromInitiator, int getOutOfJailCardFromRecipient)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeId) ?? throw new InvalidOperationException("Invalid trade.");

        // Only recipient can negotiate
        if (trade.RecipientId != negotiatorId) throw new InvalidOperationException("Player is not permitted to perform this action.");

        // Last initiator become recipient
        Player recipientPlayer = GetPlayerById(trade.InitiatorId) ?? throw new InvalidOperationException("Initiator not found.");
        // Last recipient become new initiator
        Player negotiatorPlayer = GetPlayerById(trade.RecipientId) ?? throw new InvalidOperationException("Recipient not found.");

        _validateTrade(negotiatorPlayer, recipientPlayer, trade.PropertyOffer, trade.PropertyCounterOffer, trade.MoneyFromInitiator, trade.MoneyFromRecipient, getOutOfJailCardFromInitiator, getOutOfJailCardFromRecipient);

        trade.Negotiate(propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient, getOutOfJailCardFromInitiator, getOutOfJailCardFromRecipient);
        return trade;
    }
    #endregion

    ~Game()
    {
        _logger.LogWarning($"Destroying game: {GameId}");
    }
}
