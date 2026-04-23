namespace RevolutToDh.Models;

public class CompanySettings
{
    public string IBAN { get; set; } = string.Empty;
    public string BIC { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerAddress { get; set; } = string.Empty;
    
    public string RevolutBankName { get; set; } = string.Empty;
    public string RevolutBankIBAN { get; set; } = string.Empty;
    public string RevolutBankBIC { get; set; } = string.Empty;

    public List<CounterPartyInfo> KnownCounterParties { get; set; } = new();
}

public class CounterPartyInfo
{
    public string MatchName { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string BIC { get; set; } = string.Empty;
}
