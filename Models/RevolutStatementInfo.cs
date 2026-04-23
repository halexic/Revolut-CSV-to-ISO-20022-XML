namespace RevolutToDh.Models;

public class RevolutStatementInfo
{
    public string IBAN { get; set; } = string.Empty;
    public string BIC { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerAddress { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
}
