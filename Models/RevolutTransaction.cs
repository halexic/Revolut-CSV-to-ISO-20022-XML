namespace RevolutToDh.Models;

public class RevolutTransaction
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal Balance { get; set; }
    public string? Reference { get; set; }
    public string? CounterPartyIBAN { get; set; }
    public string? CounterPartyBIC { get; set; }
    public string? CounterPartyName { get; set; }
    public string? CounterPartyAddress { get; set; }
    public string? PurposeCode { get; set; }
}
