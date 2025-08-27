using System.Text.Json.Serialization;
using MonopolyServer.Enums;

namespace MonopolyServer.Models;

public class Trade
{
    [JsonInclude]
    public uint NegotiateCount { get; set; }
    [JsonInclude]
    public Guid Id { get; init; }
    [JsonInclude]
    public Guid InitiatorId { get; private set; }
    [JsonInclude]
    public Guid RecipientId { get; private set; }

    [JsonInclude]
    public List<Guid> PropertyOffer { get; private set; }
    [JsonInclude]
    public List<Guid> PropertyCounterOffer { get; private set; }

    [JsonInclude]
    public int MoneyFromInitiator { get; private set; }
    [JsonInclude]
    public int MoneyFromRecipient { get; private set; }

    // Private helper method to set all trade details
    private void SetTradeDetails(Guid initiatorId, Guid recipientId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, int moneyFromInitiator, int moneyFromRecipient)
    {
        InitiatorId = initiatorId;
        RecipientId = recipientId;
        PropertyOffer = propertyOffer ?? new List<Guid>(); // Ensure lists are not null
        PropertyCounterOffer = propertyCounterOffer ?? new List<Guid>(); // Ensure lists are not null
        MoneyFromInitiator = moneyFromInitiator;
        MoneyFromRecipient = moneyFromRecipient;
    }

    public Trade(Guid initiatorId, Guid recipientId, List<Guid> propertyOffer, List<Guid> propertyCounterOffer, int moneyFromInitiator, int moneyFromRecipient)
    {
        Id = Guid.NewGuid();
        SetTradeDetails(initiatorId, recipientId, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);
        NegotiateCount = 0;
    }

    public void Negotiate(List<Guid> propertyOffer, List<Guid> propertyCounterOffer, int moneyFromInitiator, int moneyFromRecipient)
    {
        // For negotiation, swap the initiator and recipient
        SetTradeDetails(RecipientId, InitiatorId, propertyOffer, propertyCounterOffer, moneyFromInitiator, moneyFromRecipient);
        NegotiateCount++;
    }
}