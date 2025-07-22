public abstract class Space
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int BoardPosition { get; set; } // 0 to 39

    protected Space(string name, int boardPosition)
    {
        Id = Guid.NewGuid();
        Name = name;
        BoardPosition = boardPosition;
    }
}
