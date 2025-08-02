using System.Text.Json.Serialization;
using MonopolyServer.Enums;

namespace MonopolyServer.Models;

public class Trade
{
    [JsonInclude]
    public Guid Id { get; init; }
    [JsonInclude]
    public TradeStatus Status { get; private set; }
    [JsonInclude]
    public Guid InitiatorGuid{ get; init; }
    [JsonInclude]
    public Guid RecipientGuid{ get; init; }

    [JsonInclude]
    public List<Guid> PropertyOffer{ get; private set; }
    [JsonInclude]
    public List<Guid> PropertyCounterOffer{ get; private set; }

    [JsonInclude]
    public decimal MoneyFromInitiator{ get; private set; }
    [JsonInclude]
    public decimal MoneyFromRecipient{ get; private set; }

    public Trade(Guid initiatorGuid, Guid recipientGuid, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, decimal moneyFromInitiator, decimal moneyFromRecipient)
    {
        Id = Guid.NewGuid();

        InitiatorGuid = initiatorGuid;

        RecipientGuid = recipientGuid;

        PropertyOffer = propertyOffer;

        PropertyCounterOffer = propertyCounterOffer;

        MoneyFromInitiator = moneyFromInitiator;

        MoneyFromRecipient = moneyFromRecipient;

        Status = TradeStatus.WaitForApproval;
    }
}