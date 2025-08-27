
using System.Text.Json.Serialization;
using MonopolyServer.Enums;

namespace MonopolyServer.Models;

[method: JsonConstructor]
public struct TransactionInfo(TransactionType transactionType,Guid? senderId, Guid? receiverId, int amount, bool isTransactionWithBank)
{
    [JsonInclude]
    public TransactionType TransactionType = transactionType;
    [JsonInclude]
    public Guid? SenderId = senderId;
    [JsonInclude]
    public Guid? ReceiverId = receiverId;
    [JsonInclude]
    public int Amount = amount;
    [JsonInclude]
    public bool IsTransactionWithBank = isTransactionWithBank;
}