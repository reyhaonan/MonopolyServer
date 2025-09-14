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
    private List<ChanceCard> _chanceCards { get; init; }
    private Dictionary<int, ChanceCard> _chanceCardsDrawn { get; set; } = new Dictionary<int, ChanceCard> ();
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

    [JsonInclude]
    public GamePhase CurrentPhase { get; private set; }

    [JsonInclude]
    public TransactionHistory TransactionsHistory { get; init; }

    #endregion

    public Game(ILogger<Game> logger)
    {
        GameConfig = new GameConfig();
        _logger = logger;
        GameId = Guid.NewGuid();
        Board = new Board();
        CurrentPhase = GamePhase.WaitingForPlayers;
        TransactionsHistory = new TransactionHistory([]);
        _chanceCards = new List<ChanceCard>{
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.AdvanceToGo,
                FlavorText = "Advance to Go"
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.AdvanceToProperty,
                FlavorText = "Advance to Indonesia",
                PropertyDestination = 1
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.AdvanceToNearestRailroad,
                FlavorText = "Advance to the nearest Railroad. If unowned, you may buy it from the Bank. If owned, pay owner twice the rent to which they are otherwise entitled.",
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.AdvanceToNearestUtility,
                FlavorText = "Advance token to the nearest Utility. If unowned, you may buy it from the Bank. If owned, pay owner a total 10 times the amount thrown by last dice.",
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.ReceiveX,
                FlavorText = "Bank error in your favor. Collect $200.",
                MonetaryAmount = 200
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.ReceiveX,
                FlavorText = "From sale of stock you get $50.",
                MonetaryAmount = 50
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.ReceiveX,
                FlavorText = "Bank pays you dividend of $50",
                MonetaryAmount = 50
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.GetOutOfJailFreeCard,
                FlavorText = "Get out of Jail Free. This card may be kept until needed, or traded/sold.",
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.GoBackXSpace,
                FlavorText = "Go Back three spaces.",
                MoveAdded = -3
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.GoToJail,
                FlavorText = "Go to Jail.",
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.PayForEachHouse,
                FlavorText = "For each house pay $25, For each hotel pay $100.",
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.ReceiveX,
                FlavorText = "Your building and loan matures. Receive Collect $150.",
                MonetaryAmount = 150
            },
            new ChanceCard
            {
                ChanceOutcome = ChanceOutcome.PayEachPlayer,
                FlavorText = "You have been elected Chairman of the Board. Pay each player $50.",
                MonetaryAmount = 50
            }
        };
    }

    private void ChangeGamePhase(GamePhase newGamePhase)
    {
        _logger.LogInformation($"======Changing Game Phase to: {newGamePhase}======");
        CurrentPhase = newGamePhase;
    }

    #region Player Management
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

    public Player GetCurrentPlayer()
    {
        if (CurrentPlayerIndex < 0 || CurrentPlayerIndex >= ActivePlayers.Count)
        {
            throw new InvalidOperationException("No current player or invalid index.");
        }
        return ActivePlayers[CurrentPlayerIndex];
    }

    public Player? GetPlayerById(Guid playerId)
    {
        return ActivePlayers.FirstOrDefault(p => p.Id == playerId);
    }

    public bool PlayerIsInGame(Guid playerId)
    {
        return ActivePlayers.Any(p => p.Id == playerId);
    }
    #endregion

    public Space? GetSpaceAtPosition(int position)
    {
        if (position < 0 || position >= Board.Spaces.Count)
        {
            return null;
        }
        return Board.Spaces[position];
    }

    #region Turn Management
    private int NextPlayer()
    {
        _logger.LogInformation($"Invoked next player {CurrentPlayerIndex}, Count: {ActivePlayers.Count}");

        // Use the modulo operator to loop back to 0
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % ActivePlayers.Count;

        return CurrentPlayerIndex;
    }
    #endregion

    #region Jail Handling
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
    private static (int, int) RollPhysicalDice()
    {
        // Corrected to roll a random number between 1 and 6 for each die.
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
            if(!player.JustFreedFromJail)player.AddConsecutiveDouble();
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

    private void GivePlayerSalary(Player player)
    {
        TransactionsHistory.AddTransaction(
                new TransactionInfo(TransactionType.Salary, null, player.Id, SALARY_AMOUNT, true),
                (amount) => player.AddMoney(amount)
            );
    }

    private void HandleLandingActions(Player currentPlayer, bool passedStart, int totalDiceRoll, bool doubleRailroadRent = false, bool tenTimesUtilityRent = false)
    {
        // Collect Salary if player passed Go
        if (passedStart)
        {
            GivePlayerSalary(currentPlayer);
        }

        var space = GetSpaceAtPosition(currentPlayer.CurrentPosition) ?? throw new InvalidOperationException("Invalid space.");

        // Handle landing on different types of spaces
        if (space is SpecialSpace specialSpace)
        {
            ProcessSpecialSpaceLanding(currentPlayer, specialSpace, totalDiceRoll);
        }
        else if (space is Property property)
        {
            ProcessPropertyLanding(currentPlayer, property, totalDiceRoll, doubleRailroadRent, tenTimesUtilityRent);
        }
        else
        {
            _logger.LogInformation($"{currentPlayer.Name} landed on an unknown space type.");
        }
    }

    private void ProcessSpecialSpaceLanding(Player currentPlayer, SpecialSpace specialSpace, int totalDiceRoll)
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
            case SpecialSpaceType.Chance:
                var card = _chanceCards[_random.Next(0,_chanceCards.Count())];
                _logger.LogInformation($"Drawed chance card: {card.FlavorText}");
                var initialPosition = specialSpace.BoardPosition;
                _chanceCardsDrawn.Add(initialPosition, card);
                switch (card.ChanceOutcome)
                {
                    case ChanceOutcome.AdvanceToGo:
                        const int GO_POSITION = 0;
                        currentPlayer.MoveTo(GO_POSITION);
                        HandleLandingActions(currentPlayer, true, totalDiceRoll);
                        break;
                    case ChanceOutcome.AdvanceToProperty:
                        currentPlayer.MoveTo(card.PropertyDestination);
                        HandleLandingActions(currentPlayer, initialPosition > card.PropertyDestination, totalDiceRoll);
                        break;
                    case ChanceOutcome.AdvanceToNearestRailroad:
                        var nearestRailroad = Board.GetNearestRailroad(initialPosition);
                        currentPlayer.MoveTo(nearestRailroad.BoardPosition);
                        HandleLandingActions(currentPlayer, initialPosition > nearestRailroad.BoardPosition, totalDiceRoll, doubleRailroadRent:true);
                        break;
                    case ChanceOutcome.AdvanceToNearestUtility:
                        var nearestUtility = Board.GetNearestUtility(initialPosition);
                        currentPlayer.MoveTo(nearestUtility.BoardPosition);
                        HandleLandingActions(currentPlayer, initialPosition > nearestUtility.BoardPosition, totalDiceRoll, tenTimesUtilityRent: true);
                        break;
                    case ChanceOutcome.ReceiveX:
                        TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Reward, null, currentPlayer.Id, card.MonetaryAmount, true),
                        (amount) =>
                        {
                            currentPlayer.AddMoney(amount);
                        });
                        break;
                    case ChanceOutcome.GetOutOfJailFreeCard:
                        currentPlayer.AddGetOutOfJailFreeCard(1);
                        break;
                    case ChanceOutcome.GoBackXSpace:
                        currentPlayer.MoveBy(card.MoveAdded);
                        HandleLandingActions(currentPlayer, false, totalDiceRoll);
                        break;
                    case ChanceOutcome.GoToJail:
                        currentPlayer.GoToJail();
                        break;
                    case ChanceOutcome.PayForEachHouse:
                        var houseCount = Board.GetHouseCountOwnedByPlayer(currentPlayer);
                        var hotelCount = Board.GetHotelCountOwnedByPlayer(currentPlayer);

                        const int HOUSE_FINE = 25;
                        const int HOTEL_FINE = 100;

                        TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Fine, currentPlayer.Id, null, HOUSE_FINE * houseCount + HOTEL_FINE * hotelCount, true), amount =>
                        {
                            currentPlayer.DeductMoney(amount);
                        });
                        break;
                    case ChanceOutcome.PayEachPlayer:
                        foreach (var otherPlayer in ActivePlayers.Where(p => p.Id != currentPlayer.Id))
                        {
                            TransactionsHistory.AddTransaction(new TransactionInfo(TransactionType.Fine, currentPlayer.Id, otherPlayer.Id, card.MonetaryAmount, false), amount =>
                            {
                                otherPlayer.AddMoney(amount);
                                currentPlayer.DeductMoney(amount);
                            });
                        }

                        break;
                    default:
                        throw new InvalidOperationException("Invalid chance card received");
                        
                    

                }
                break;
            // Other cases (Chance, CommunityChest, etc.) would go here
            default:
                // No action needed for spaces like Just Visiting, Go, etc.
                break;
        }
    }

    private void ProcessPropertyLanding(Player currentPlayer, Property property, int totalDiceRoll, bool doubleRailroadRent = false, bool tenTimesUtilityRent = false)
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
            // Force the rentCalc to 10 if param override
            rentValue = utilityProperty.CalculateRent(diceRoll: totalDiceRoll, ownerUtilities: tenTimesUtilityRent?2:utilityCount);
        }
        else if (property is RailroadProperty railroadProperty)
        {
            var railroadCount = Board.GetRailroadOwnedByPlayer(ownerId).Count;
            rentValue = railroadProperty.CalculateRent(ownerRailroads: railroadCount);
            rentValue = doubleRailroadRent ? rentValue * 2 : rentValue;
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

    public RollResult RollDice()
    {
        if (CurrentPhase != GamePhase.PlayerTurnStart)
        {
            throw new InvalidOperationException($"{CurrentPhase} is not the appropriate game phase for this action.");
        }

        ChangeGamePhase(GamePhase.RollingDice);
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer.Money < 0) throw new InvalidOperationException("Player is in debt");

        _chanceCardsDrawn.Clear();

        // Reset total dice roll for the current turn.
        _totalDiceRoll = 0;
        (_diceRoll1, _diceRoll2) = RollPhysicalDice();

        TransactionsHistory.StartTransaction();
        HandleDiceRollConsequences(currentPlayer, _diceRoll1, _diceRoll2);
        currentPlayer.JustFreedFromJail = false;

        // Only move if the player is not going to jail or is not in jail after the roll.
        if (!currentPlayer.IsInJail)
        {
            ChangeGamePhase(GamePhase.MovingToken);
            bool passedStart = currentPlayer.MoveBy(_totalDiceRoll);
            _logger.LogInformation($"Player moved to position {currentPlayer.CurrentPosition}");
            // Handle all actions related to landing on a new space, ignore if player is in jail
            ChangeGamePhase(GamePhase.LandingOnSpaceAction);
            HandleLandingActions(currentPlayer, passedStart, _totalDiceRoll);
        }

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
            NewGamePhase = CurrentPhase,
            ChanceCardsDrawn = _chanceCardsDrawn
        };
    }

    #endregion

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

    public Trade InitiateTrade(Guid initiatorId, Guid recipientId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, int moneyFromInitiator, int moneyFromRecipient, int getOutOfJailCardFromInitiator, int getOutOfJailCardFromRecipient)
    {
        Player initiatorPlayer = GetPlayerById(initiatorId) ?? throw new InvalidOperationException("Invalid initiator player.");
        Player recipientPlayer = GetPlayerById(recipientId) ?? throw new InvalidOperationException("Invalid recipient player.");

        _validateTrade(initiatorPlayer, recipientPlayer, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient, getOutOfJailCardFromInitiator, getOutOfJailCardFromRecipient);

        Trade newTrade = new Trade(initiatorId, recipientId, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient, getOutOfJailCardFromInitiator, getOutOfJailCardFromRecipient);
        ActiveTrades.Add(newTrade);
        return newTrade;
    }

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

    public void RejectTrade(Guid tradeId, Guid recipientId)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeId) ?? throw new InvalidOperationException("Invalid trade.");
        if (trade.RecipientId != recipientId) throw new InvalidOperationException("Player is not permitted to perform this action.");
        ActiveTrades.Remove(trade);
    }

    public void CancelTrade(Guid tradeId, Guid initiatorId)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeId) ?? throw new InvalidOperationException("Invalid trade.");
        if (trade.InitiatorId != initiatorId) throw new InvalidOperationException("Player is not permitted to perform this action.");
        ActiveTrades.Remove(trade);
    }
    public Trade NegotiateTrade(Guid negotiatorId, Guid tradeId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, int moneyFromInitiator, int moneyFromRecipient, int getOutOfJailCardFromInitiator, int getOutOfJailCardFromRecipient)
    {
        Trade trade = ActiveTrades.First(tr => tr.Id == tradeId) ?? throw new InvalidOperationException("Invalid trade.");

        // Only recipient can negotiate
        if (trade.RecipientId != negotiatorId) throw new InvalidOperationException("Player is not permitted to perform this action.");

        // Last initiator become recipient
        Player recipientPlayer = GetPlayerById(trade.InitiatorId) ?? throw new InvalidOperationException("Initiator not found.");
        // Last recipient become new initiator
        Player negotiatorPlayer = GetPlayerById(trade.RecipientId) ?? throw new InvalidOperationException("Recipient not found.");

        _validateTrade(negotiatorPlayer, recipientPlayer, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient, getOutOfJailCardFromInitiator, getOutOfJailCardFromRecipient);

        trade.Negotiate(propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient, getOutOfJailCardFromInitiator, getOutOfJailCardFromRecipient);
        return trade;
    }
    #endregion

    ~Game()
    {
        _logger.LogWarning($"Destroying game: {GameId}");
    }
}
