
using System.Text.Json.Serialization;
using MonopolyServer.Models;

namespace MonopolyServer.Utils;
public readonly struct RollResult
{
    // Inner struct for dice roll details
    [method: JsonConstructor]    // Inner struct for dice roll details
    public readonly struct DiceInfo(int roll1, int roll2, int totalRoll)
    {
        public int Roll1 { get; } = roll1;
        public int Roll2 { get; } = roll2;
        public int TotalRoll { get; } = totalRoll;
    }

    // Inner struct for player state after the roll
    [method: JsonConstructor]
    // Inner struct for player state after the roll
    public readonly struct PlayerStateInfo(bool isInJail, int newPlayerPosition, int newPlayerJailTurnsRemaining, decimal newPlayerMoney)
    {
        public bool IsInJail { get; } = isInJail;
        public int NewPlayerPosition { get; } = newPlayerPosition;
        public int NewPlayerJailTurnsRemaining { get; } = newPlayerJailTurnsRemaining;
        public decimal NewPlayerMoney { get; } = newPlayerMoney;
    }


    [JsonInclude]
    public DiceInfo Dice { get; }
    [JsonInclude]
    public PlayerStateInfo PlayerState { get; }
    [JsonInclude]
    public List<TransactionInfo> Transaction { get; }

    [JsonConstructor]
    public RollResult(DiceInfo dice, PlayerStateInfo playerState, List<TransactionInfo> transaction)
    {
        Dice = dice;
        PlayerState = playerState;
        Transaction = transaction;
    }
}