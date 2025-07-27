using System.Text.Json.Serialization;

namespace MonopolyServer.Models;


public delegate void TransactionCallback(decimal amount);

public class TransactionHistory
{
    private List<TransactionInfo> _transactionDiff = [];
    private bool _hasActiveTransaction = false;
    [JsonInclude]
    public List<TransactionInfo> History { get; private set; }

    public TransactionHistory(List<TransactionInfo> history)
    {
        History = history;
    }

    public void StartTransaction()
    {
        _transactionDiff.Clear();
        _hasActiveTransaction = true;
    }
    public void AddTransaction(TransactionInfo transaction, TransactionCallback cb)
    {
        if (!_hasActiveTransaction) throw new Exception("No active transaction");
        _transactionDiff.Add(transaction);
        History.Add(transaction);
        cb.Invoke(transaction.Amount);
    }
    public List<TransactionInfo> CommitTransaction()
    {
        _hasActiveTransaction = false;
        return _transactionDiff;
    }
}