using System.Text.Json.Serialization;
using MonopolyServer.Enums;
using MonopolyServer.Utils;

namespace MonopolyServer.Models;

using Microsoft.Extensions.Logging;

public class GameState
{
    const decimal SALARY_AMOUNT = 200;
    #region Private property
    private readonly ILogger _logger;
    private static readonly Random _random = new Random();
    private int _diceRoll1 { get; set; } = 0;
    private int _diceRoll2 { get; set; } = 0;
    private int _totalDiceRoll { get; set; } = 0;
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
        _logger = logger;
        GameId = Guid.NewGuid();

        Board = new Board();

        CurrentPhase = GamePhase.WaitingForPlayers;

        TransactionsHistory = new TransactionHistory([]);

        InitializeDecks();
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
        _logger.LogInformation($"Invoked next player {CurrentPlayerIndex}, Count: {ActivePlayers.Count}");

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
            _logger.LogInformation($"Player {player.Name} rolled doubles and got out of jail!");
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
    public List<TransactionInfo> PayToGetOutOfJail()
    {

        if (CurrentPhase != GamePhase.PlayerTurnStart) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Player currentPlayer = GetCurrentPlayer();


        if (!currentPlayer.IsInJail)
        {
            throw new InvalidOperationException("Player is not in jail");
        }

        const decimal JAIL_FEE = 50;

        if (currentPlayer.Money < JAIL_FEE)
        {
            throw new InvalidOperationException("Not enough money to pay the jail fee");
        }

        TransactionsHistory.StartTransaction();
        TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.FreeFromJail, currentPlayer.Id, null, JAIL_FEE, true), (amount) =>
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
        CurrentPlayerIndex = 0;

        ActivePlayers = ActivePlayers.OrderBy(_ => _random.Next()).ToList();

        if (CurrentPhase == GamePhase.WaitingForPlayers) ChangeGamePhase(GamePhase.PlayerTurnStart);

        else throw new InvalidOperationException($"Game {GameId} already started");

        return ActivePlayers;
    }

    /// <summary>
    /// Rolls the dice for the current player's turn, handles movement and special cases like doubles.
    /// Changes game phases from PlayerTurnStart to RollingDice to MovingToken to LandingOnSpaceAction to PostLandingActions.
    /// </summary>
    /// <exception cref="Exception">Thrown if not in the PlayerTurnStart phase</exception>
    public RollResult RollDice()
    {
        if (CurrentPhase != GamePhase.PlayerTurnStart) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        ChangeGamePhase(GamePhase.RollingDice);


        // Gotta reset lil bro
        _totalDiceRoll = 0;

        // Gamba, might wanna look for better randomness?
        _diceRoll1 = _random.Next(1, 7);
        _diceRoll2 = _random.Next(1, 7);
        // _diceRoll1 = 2;
        // _diceRoll2 = 0;

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
            _logger.LogInformation($"Player moved to position {currentPlayer.CurrentPosition}");
        }

        ChangeGamePhase(GamePhase.LandingOnSpaceAction);

        TransactionsHistory.StartTransaction();
        // Salary🥳
        if (passedStart)
        {
            TransactionsHistory.AddTransaction(
                new TransactionInfo(TransactionType.Salary, null, currentPlayer.Id, SALARY_AMOUNT, true),
                (amount) => currentPlayer.AddMoney(amount)
            );
        }

        // Handle landing on spaces
        var space = GetSpaceAtPosition(currentPlayer.CurrentPosition) ?? throw new InvalidOperationException("Invalid space");

        // Landed on Go To Jail👮‍♂️


        // Landed on special(ntar)
        if (space is SpecialSpace specialSpace)
        {
            _logger.LogInformation($"{currentPlayer.Name} landed on another special space of type {specialSpace.Type}.");
            switch (specialSpace.Type)
            {
                case SpecialSpaceType.GoToJail:
                    currentPlayer.GoToJail();
                    break;
                case SpecialSpaceType.IncomeTax:
                    TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Fine, currentPlayer.Id, null, 200, true), (amount) =>
                    {
                        currentPlayer.DeductMoney(amount);
                    });
                    break;
                case SpecialSpaceType.LuxuryTax:
                    TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Fine, currentPlayer.Id, null, 100, true), (amount) =>
                    {
                        currentPlayer.DeductMoney(amount);
                    });
                    break;
                case SpecialSpaceType.Chance:
                    // TODO: this
                    break;
                case SpecialSpaceType.CommunityChest:
                    // TODO: this
                    break;
                default:
                    throw new Exception("How did you land on this space??");
            }
            // TODO: [GameConfig] If a player lands on Vacation, all collected money from taxes and bank payments will be earned
        }

        // Landed on a property
        else if (space is Property property)
        {
            // PROPERTY OWNED BY OTHER PLAYER
            if (property.IsOwnedByOtherPlayer(currentPlayer.Id))
            {
                var ownerId = property.OwnerId ?? throw new Exception("[Impossible] Landed on a tile owned by other player but no id?");

                decimal rentValue;
                if (property is CountryProperty countryProperty)
                {
                    rentValue = countryProperty.CalculateRent(_totalDiceRoll);
                }
                else if (property is UtilityProperty utilityProperty)
                {
                    var utilityOwnedByRentOwner = Board.GetUtilityOwnedByPlayer(ownerId);
                    rentValue = utilityProperty.CalculateRent(_totalDiceRoll, 0, utilityOwnedByRentOwner.Count);
                }
                else if (property is RailroadProperty railroadProperty)
                {
                    var railroadOwnedByRentOwner = Board.GetRailroadOwnedByPlayer(ownerId);
                    rentValue = railroadProperty.CalculateRent(0, railroadOwnedByRentOwner.Count);
                }
                else throw new Exception("[Impossible] Unhandled property type");


                // TODO: [GameConfig] If a player owns a full property set, the base rent payment will be doubled
                // TODO: [GameConfig] Rent will not be collected when landing on properties whose owners are in prison

                _logger.LogInformation($"Deducting {currentPlayer.Name}'s money from rent: {rentValue}");

                Player Owner = GetPlayerById(ownerId) ?? throw new Exception("[Impossible] Owner not found on active player list");

                if (rentValue != 0)
                {
                    TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Rent, currentPlayer.Id, ownerId, rentValue, false), (amount) =>
                    {
                        currentPlayer.DeductMoney(amount);
                        Owner.AddMoney(amount);
                    });
                }
            }
        }
        else
        {
            _logger.LogInformation($"{currentPlayer.Name} landed on an unknown space type.");
        }

        var diceInfo = new RollResult.DiceInfo(_diceRoll1, _diceRoll2, _totalDiceRoll);
        var playerStateInfo = new RollResult.PlayerStateInfo(currentPlayer.IsInJail, currentPlayer.CurrentPosition, currentPlayer.JailTurnsRemaining);

        ChangeGamePhase(GamePhase.PostLandingActions);

        var transactionInfo = TransactionsHistory.CommitTransaction();
        return new RollResult(diceInfo, playerStateInfo, transactionInfo);
    }

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

    public int DeclareBankcruptcy(Guid playerGuid)
    {
        Player bankcruptPlayer = GetPlayerById(playerGuid) ?? throw new InvalidOperationException("Player not found");

        foreach (Guid propertyId in bankcruptPlayer.PropertiesOwned)
        {
            Board.GetPropertyById(propertyId).ResetProperty();
        }

        bool isActivePlayer = GetCurrentPlayer().Id == bankcruptPlayer.Id;


        ActivePlayers.Remove(bankcruptPlayer);

        if (isActivePlayer) ChangeGamePhase(GamePhase.PlayerTurnStart);
        CurrentPlayerIndex %= ActivePlayers.Count;
        return CurrentPlayerIndex;
    }

    #endregion
    #region Property 
    /// <summary>
    /// Allows the current player to buy the property they have landed on.
    /// Checks if the space is a property, if it's not already owned, and if the player has enough money.
    /// Deducts the purchase price from the player's money, sets the property's owner, and adds the property to the player's owned properties.
    /// Changes the game phase from LandingOnSpaceAction to PostLandingActions upon successful purchase.
    /// </summary>
    /// <returns>propertyGuid, and transaction info</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public (Guid, List<TransactionInfo>) BuyProperty()
    {
        // Action is only available on: [PostLandingActions]
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions)) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Player currentPlayer = GetCurrentPlayer();

        var space = GetSpaceAtPosition(currentPlayer.CurrentPosition);

        if (space is Property property)
        {
            // Check ownership
            if (property.OwnerId != null) throw new InvalidOperationException("This property is already owned");

            // Check player money
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
    /// <param name="propertyGuid"></param>
    /// <returns>transaction info</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<TransactionInfo> SellProperty(Guid propertyGuid)
    {
        // Action is only available on: [PostLandingActions, PlayerTurnStart]
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart)) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Property property = Board.GetPropertyById(propertyGuid);
        Player currentPlayer = GetCurrentPlayer();

        // Disallow selling property if its not owned by the player
        if (!property.IsOwnedByPlayer(currentPlayer.Id)) throw new InvalidOperationException($"{currentPlayer.Id} are not permitted to sell this property");

        if (property is CountryProperty countryProperty)
        {
            // DIsallow selling property if it has houses
            if (countryProperty.CurrentRentStage != RentStage.Unimproved) throw new InvalidOperationException("Can't sell property with house");


            bool groupIsOwnedByPlayer = Board.GroupIsOwnedByPlayer(countryProperty.Group, currentPlayer.Id);
            bool noHouseInGroup = Board.GetPropertiesInGroup(countryProperty.Group).All(property => property.CurrentRentStage == RentStage.Unimproved);

            // Disallow mortgage if player has a house in a group
            if (groupIsOwnedByPlayer && !noHouseInGroup) throw new InvalidOperationException("Cannot sell property if other property has house");
        }

        // You can sell mortgaged property, good luck getting any money though lol
        decimal sellValue = property.IsMortgaged ? 0 : property.MortgageValue;

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
    /// <param name="propertyGuid"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<TransactionInfo> MortgageProperty(Guid propertyGuid)
    {

        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart)) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Property property = Board.GetPropertyById(propertyGuid);
        Player currentPlayer = GetCurrentPlayer();


        // Disallow mortgage if property is now owned
        if (!property.IsOwnedByPlayer(currentPlayer.Id)) throw new InvalidOperationException("Property is not owned by this player");

        if (property is CountryProperty countryProperty)
        {
            // Disallow mortgage if property has houses
            if (!countryProperty.CurrentRentStage.Equals(RentStage.Unimproved)) throw new InvalidOperationException("Can't mortgage property with house");

            bool groupIsOwnedByPlayer = Board.GroupIsOwnedByPlayer(countryProperty.Group, currentPlayer.Id);
            bool noHouseInGroup = Board.GetPropertiesInGroup(countryProperty.Group).All(property => property.CurrentRentStage == RentStage.Unimproved);

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
    /// <param name="propertyGuid"></param>
    /// <returns>Transaction info</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<TransactionInfo> UnmortgageProperty(Guid propertyGuid)
    {

        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart)) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Property property = Board.GetPropertyById(propertyGuid);
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

        // Mortgaged property in group checks
        if (!Board.NoMortgagedPropertyInGroup(countryProperty.Group)) throw new InvalidOperationException("Cannot upgrade/downgrade because there is a mortgaged property in the group");

    }

    /// <summary>
    /// Upgrading property
    /// </summary>
    /// <param name="propertyGuid"></param>
    /// <returns>Return list of transactions(though its usually just one)</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<TransactionInfo> UpgradeProperty(Guid propertyGuid)
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart)) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");


        Player currentPlayer = GetCurrentPlayer();
        Property property = Board.GetPropertyById(propertyGuid);
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
    /// <param name="propertyGuid"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<TransactionInfo> DowngradeProperty(Guid propertyGuid)
    {
        if (!CurrentPhase.Equals(GamePhase.PostLandingActions) && !CurrentPhase.Equals(GamePhase.PlayerTurnStart)) throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action");

        Player currentPlayer = GetCurrentPlayer();

        Property property = Board.GetPropertyById(propertyGuid);
        if (property is CountryProperty countryProperty)
        {
            // Generic checks for both upgrade/downgrade
            _checkUpgradeDowngradePermission(countryProperty, currentPlayer);

            if (countryProperty.CurrentRentStage == RentStage.Unimproved) throw new InvalidOperationException("Cannot downgrade more in this property");


            TransactionsHistory.StartTransaction();
            TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Upgrade, currentPlayer.Id, null, countryProperty.HouseSellValue, true), (amount) =>
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
    public Trade InitiateTrade(Guid initiatorGuid, Guid recipientGuid, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, decimal moneyFromInitiator, decimal moneyFromRecipient)
    {
        Player initiatorPlayer = GetPlayerById(initiatorGuid) ?? throw new Exception("Invalid initiator player");
        Player recipientPlayer = GetPlayerById(recipientGuid) ?? throw new Exception("Invalid recipipent player");

        // Verify if property offer is valid(owned by initiator)
        bool initiatorPropertyIsValid = propertyOffer.All(property => initiatorPlayer.PropertiesOwned.Contains(property));
        if (!initiatorPropertyIsValid) throw new InvalidOperationException("Property Offer is invalid");
        // Verify initiator money
        if (initiatorPlayer.Money < moneyFromInitiator) throw new InvalidOperationException("Initiator money is invalid");

        // Verify if property counter offer is valid(owned by recipient)
        bool recipientPropertyIsValid = propertyOffer.All(property => recipientPlayer.PropertiesOwned.Contains(property));
        if (!recipientPropertyIsValid) throw new InvalidOperationException("Property Counter Offer is invalid");

        // Verify recipient money
        if (recipientPlayer.Money < moneyFromRecipient) throw new InvalidOperationException("Recipient money is invalid");

        // Start trade

        Trade newTrade = new Trade(initiatorGuid, recipientGuid, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);

        ActiveTrades.Add(newTrade);

        return newTrade;
    }

    public List<TransactionInfo> AcceptTrade(Guid tradeGuid, Guid approvalId)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeGuid) ?? throw new InvalidOperationException("Invalid trade");

        if (trade.ApprovalId != approvalId) throw new InvalidOperationException("Player is not permitted to perform this action");

        Player initiatorPlayer = GetPlayerById(trade.InitiatorGuid) ?? throw new Exception("Initiator not found");
        Player recipientPlayer = GetPlayerById(trade.RecipientGuid) ?? throw new Exception("Recipient not found");

        // Verify if property offer is valid(owned by initiator)
        bool initiatorPropertyIsValid = trade.PropertyOffer.All(property => initiatorPlayer.PropertiesOwned.Contains(property));
        if (!initiatorPropertyIsValid) throw new InvalidOperationException("Property Offer is invalid");
        // Verify initiator money
        if (initiatorPlayer.Money < trade.MoneyFromInitiator) throw new InvalidOperationException("Initiator money is invalid");

        // Verify if property counter offer is valid(owned by recipient)
        bool recipientPropertyIsValid = trade.PropertyOffer.All(property => recipientPlayer.PropertiesOwned.Contains(property));
        if (!recipientPropertyIsValid) throw new InvalidOperationException("Property Counter Offer is invalid");

        // Verify recipient money
        if (recipientPlayer.Money < trade.MoneyFromRecipient) throw new InvalidOperationException("Recipient money is invalid");

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

        return TransactionsHistory.CommitTransaction();
    }
    public void RejectTrade(Guid tradeGuid, Guid approvalId)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeGuid) ?? throw new InvalidOperationException("Invalid trade");

        if (trade.ApprovalId != approvalId) throw new InvalidOperationException("Player is not permitted to perform this action");

        ActiveTrades.Remove(trade);
    }
    public void NegotiateTrade(){}
    
    #endregion

}
