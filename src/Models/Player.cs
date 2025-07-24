using System.Text.Json.Serialization;

public class Player
{
    const int MAX_POSITION = 40;
    const int JAIL_POSITION = 10;
    [JsonInclude]
    public Guid Id { get; init; } // Unique identifier for the player
    [JsonInclude]
    public string Name { get; private set; }
    [JsonInclude]
    public decimal Money { get; private set; }
    [JsonInclude]
    public int CurrentPosition { get; private set; } // 0-39, representing board spaces
    [JsonInclude]
    public bool IsInJail { get; private set; }
    [JsonInclude]
    public int JailTurnsRemaining { get; private set; } // Max 3 turns
    [JsonInclude]
    public int GetOutOfJailFreeCards { get; private set; } // Count of cards
    public int ConsecutiveDoubles { get; set; } // For tracking 3 doubles to jail
    [JsonInclude]
    public List<Guid> PropertiesOwned { get; private set; } // List of Property IDs owned by this player
    [JsonInclude]
    public bool IsBankrupt { get; private set; }

    public Player(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        Money = 1500;
        CurrentPosition = 0;
        IsInJail = false;
        JailTurnsRemaining = 0;
        GetOutOfJailFreeCards = 0;
        ConsecutiveDoubles = 0;
        PropertiesOwned = new List<Guid>();
        IsBankrupt = false;
    }

    #region Money
    public void AddMoney(decimal amount) => Money += amount;
    public void DeductMoney(decimal amount)
    {
        Money -= amount;
    }
    #endregion

    #region Movement
    public void MoveBy(int amount)
    {
        int newPotentialPosition = CurrentPosition + amount;
        CurrentPosition =  newPotentialPosition % MAX_POSITION;
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
        IsInJail = true;
        JailTurnsRemaining = 3;
    }
}
