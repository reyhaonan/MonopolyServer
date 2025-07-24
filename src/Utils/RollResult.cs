
using System.Text.Json.Serialization;

namespace MonopolyServer.Utils;
public struct RollResult
{
    // Inner struct for dice roll details
    public struct DiceInfo
    {
        public int Roll1 { get; }
        public int Roll2 { get; }
        public int TotalRoll { get; }

        [JsonConstructor]
        public DiceInfo(int roll1, int roll2, int totalRoll)
        {
            Roll1 = roll1;
            Roll2 = roll2;
            TotalRoll = totalRoll;
        }
    }

    // Inner struct for player state after the roll
    public struct PlayerStateInfo
    {
        public bool isInJail { get; }
        public int NewPlayerPosition { get; }
        public int NewPlayerJailTurnsRemaining { get; }
        public decimal NewPlayerMoney { get; }

        [JsonConstructor]
        public PlayerStateInfo(bool isInJail, int newPlayerPosition, int newPlayerJailTurnsRemaining, decimal newPlayerMoney)
        {
            this.isInJail = isInJail;
            NewPlayerPosition = newPlayerPosition;
            NewPlayerMoney = newPlayerMoney;
            NewPlayerJailTurnsRemaining = newPlayerJailTurnsRemaining;
        }
    }

    public DiceInfo Dice { get; }
    public PlayerStateInfo PlayerState { get; }

    [JsonConstructor]
    public RollResult(DiceInfo dice, PlayerStateInfo playerState)
    {
        Dice = dice;
        PlayerState = playerState;
    }
}