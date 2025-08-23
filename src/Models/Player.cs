using System.Text.Json.Serialization;

namespace MonopolyServer.Models;
public class Player
{
    const int MAX_POSITION = 40;
    const int JAIL_POSITION = 10;
    [JsonInclude]
    public Guid Id { get; init; } // Unique identifier for the player
    [JsonInclude]
    public string Name { get; init; }
    [JsonInclude]
    public int Money { get; private set; }
    [JsonInclude]
    public int CurrentPosition { get; private set; } // 0-39, representing board spaces
    [JsonInclude]
    public bool IsInJail { get; private set; }
    [JsonInclude]
    public int JailTurnsRemaining { get; private set; } // Max 3 turns
    [JsonInclude]
    public int GetOutOfJailFreeCards { get; private set; } // Count of cards
    [JsonInclude]
    public int ConsecutiveDoubles { get; private set; } // For tracking 3 doubles to jail
    [JsonInclude]
    public List<Guid> PropertiesOwned { get; private set; } // List of Property IDs owned by this player
    [JsonInclude]
    public bool IsBankrupt { get; private set; }
    [JsonInclude]
    public string HexColor { get; init; }

    public Player(string name, string hexColor, Guid id)
    {
        Id = id;
        Name = name;
        Money = 1500;
        CurrentPosition = 0;
        IsInJail = false;
        JailTurnsRemaining = 0;
        GetOutOfJailFreeCards = 0;
        ConsecutiveDoubles = 0;
        PropertiesOwned = new List<Guid>();
        IsBankrupt = false;
        HexColor = hexColor;
    }

    #region Money
    public void AddMoney(int amount) => Money += amount;
    public void DeductMoney(int amount)
    {
        Money -= amount;
    }
    public void setMoney(int amount)
    {
        Money = amount;
    }
    #endregion

    #region Movement

    // Return true if passing by start
    public bool MoveBy(int amount)
    {
        int newPotentialPosition = CurrentPosition + amount;
        CurrentPosition = newPotentialPosition % MAX_POSITION;

        if (CurrentPosition < 0)
        {
            CurrentPosition += MAX_POSITION;
        }

        return newPotentialPosition >= MAX_POSITION;
    }

    // Use this cautiously
    public void MoveTo(int position)
    {
        CurrentPosition = position;
    }
    #endregion

    public void GoToJail()
    {
        MoveTo(JAIL_POSITION);
        ResetConsecutiveDouble();
        IsInJail = true;
        JailTurnsRemaining = 3;
    }

    public void ReduceJailTurnRemaining()
    {
        if (!IsInJail || JailTurnsRemaining == 0) throw new Exception("Player is not in jail");
        JailTurnsRemaining--;
    }

    public void FreeFromJail()
    {
        IsInJail = false;
        JailTurnsRemaining = 0;
    }

    public bool UseGetOutOfJailFreeCard()
    {
        if (GetOutOfJailFreeCards <= 0)
        {
            throw new Exception("Player doesn't have any Get Out of Jail Free cards");
        }

        GetOutOfJailFreeCards--;
        return true;
    }

    public void AddGetOutOfJailFreeCard()
    {
        GetOutOfJailFreeCards++;
    }

    public void AddConsecutiveDouble()
    {
        ConsecutiveDoubles++;
    }

    public void ResetConsecutiveDouble()
    {
        ConsecutiveDoubles = 0;
    }
}
