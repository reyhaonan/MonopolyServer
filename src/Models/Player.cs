public class Player
{
    const int MAX_POSITION = 40;
    const int JAIL_POSITION = 10;
    public Guid Id { get; set; } // Unique identifier for the player
    public string Name { get; set; }
    public decimal Money { get; set; }
    public int CurrentPosition { get; set; } // 0-39, representing board spaces
    public bool IsInJail { get; set; }
    public int JailTurnsRemaining { get; set; } // Max 3 turns
    public int GetOutOfJailFreeCards { get; set; } // Count of cards
    public int ConsecutiveDoubles { get; set; } // For tracking 3 doubles to jail
    public List<Guid> PropertiesOwned { get; set; } // List of Property IDs owned by this player
    public bool IsBankrupt { get; set; }

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
        CurrentPosition = CurrentPosition = newPotentialPosition % MAX_POSITION;
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
