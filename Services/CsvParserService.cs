using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.RegularExpressions;
using RevolutToDh.Models;

namespace RevolutToDh.Services;

public class CsvParserService : ICsvParserService
{
    private readonly CompanySettings _companySettings;

    public CsvParserService(IOptions<CompanySettings> companySettings)
    {
        _companySettings = companySettings.Value;
    }

    private static readonly Dictionary<string, string> BicMapping = new()
    {
        { "SI5661", "HDELSI22" }, // Delavska Hranilnica
        { "SI5601", "BSLJSI2X" }, // Banka Slovenije
        { "SI5602", "NLBSSI2X" }, // NLB
        { "SI5603", "SKBASK2X" }, // SKB
        { "SI5605", "BAKXSI2X" }, // Sparkasse
        { "SI5612", "GORESI2X" }, // Gorenjska banka
        { "SI5619", "DBSISI2X" }, // Deželna banka
        { "SI5630", "UNICSI2X" }, // UniCredit
        { "SI5634", "BKSISI22" }, // BKS Bank
        { "SI5633", "INTESI2X" }, // Intesa Sanpaolo
        { "LT6732", "REVOLT21" }, // Revolut
    };

    public (List<RevolutTransaction> Transactions, RevolutStatementInfo Info) Parse(Stream csvStream)
    {
        var transactions = new List<RevolutTransaction>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);

        var records = csv.GetRecords<dynamic>().ToList();

        foreach (var record in records)
        {
            var dict = (IDictionary<string, object>)record;
            
            var tx = new RevolutTransaction
            {
                Date = DateTime.ParseExact(dict["Completed Date"].ToString()!, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                Description = dict["Description"].ToString()!,
                Amount = decimal.Parse(dict["Amount"].ToString()!, CultureInfo.InvariantCulture),
                Fee = decimal.Parse(dict["Fee"].ToString()!, CultureInfo.InvariantCulture),
                Balance = decimal.Parse(dict["Balance"].ToString()!, CultureInfo.InvariantCulture),
                PurposeCode = "OTHR" // Default
            };

            string desc = tx.Description;
            string type = dict["Type"].ToString()!;

            if (type == "TRANSFER" || desc.StartsWith("To "))
            {
                ParseTransfer(tx, desc);
            }
            else if (type == "CASHBACK" || desc.Contains("Cashback"))
            {
                tx.CounterPartyName = _companySettings.RevolutBankName;
                tx.CounterPartyIBAN = _companySettings.RevolutBankIBAN;
                tx.CounterPartyBIC = _companySettings.RevolutBankBIC;
                tx.PurposeCode = "OTHR";
            }
            else if (desc.Contains("Payment from"))
            {
                tx.CounterPartyName = desc.Replace("Payment from", "").Trim();
                tx.PurposeCode = "MDCS";
                
                var knownCp = _companySettings.KnownCounterParties
                    .FirstOrDefault(cp => tx.CounterPartyName.Contains(cp.MatchName, StringComparison.OrdinalIgnoreCase));
                
                if (knownCp != null)
                {
                    tx.CounterPartyIBAN = knownCp.IBAN;
                    tx.CounterPartyBIC = knownCp.BIC;
                }
            }
            else if (type == "CARD_PAYMENT" || type == "Card Payment")
            {
                tx.CounterPartyName = desc.Split(',')[0].Trim();
                tx.PurposeCode = "OTHR";
            }
            else
            {
                tx.CounterPartyName = desc;
            }

            // Reference extraction: look for SI... or something that looks like a reference at the end
            var refMatch = Regex.Match(desc, @"(SI\d{2}\s*[\d-]+)");
            if (refMatch.Success)
            {
                tx.Reference = refMatch.Groups[1].Value.Replace(" ", "");
            }

            // Infer BIC if IBAN is present and BIC is not
            if (!string.IsNullOrEmpty(tx.CounterPartyIBAN) && string.IsNullOrEmpty(tx.CounterPartyBIC))
            {
                tx.CounterPartyBIC = InferBic(tx.CounterPartyIBAN);
            }

            transactions.Add(tx);
        }

        transactions = transactions.OrderBy(t => t.Date).ToList();

        var info = new RevolutStatementInfo
        {
            IBAN = _companySettings.IBAN,
            BIC = _companySettings.BIC,
            OwnerName = _companySettings.OwnerName,
            OwnerAddress = _companySettings.OwnerAddress,
            OpeningBalance = transactions.Count > 0 ? transactions[0].Balance - transactions[0].Amount : 0,
            ClosingBalance = transactions.Count > 0 ? transactions.Last().Balance : 0
        };

        return (transactions, info);
    }

    private void ParseTransfer(RevolutTransaction tx, string desc)
    {
        // Example: "To RS PDP, Miklošičeva cesta 24, 1000 Ljubljana, Slovenija - Prispevek za ZZ"
        // Example: "To Unifakt d.o.o, Smarska cesta 7a, 6000 Koper, Slovenija"
        
        string workingDesc = desc;
        if (workingDesc.StartsWith("To ")) workingDesc = workingDesc.Substring(3);

        var parts = workingDesc.Split(new[] { " - " }, StringSplitOptions.None);
        string partyAndAddr = parts[0];
        if (parts.Length > 1) tx.Reference = parts[1].Trim();

        var commaParts = partyAndAddr.Split(',');
        tx.CounterPartyName = commaParts[0].Trim();
        if (commaParts.Length > 1)
        {
            tx.CounterPartyAddress = string.Join(",", commaParts.Skip(1)).Trim();
        }

        // Try to find IBAN in the whole description
        var ibanMatch = Regex.Match(desc, @"([A-Z]{2}\d{2}[A-Z0-9]{10,30})");
        if (ibanMatch.Success)
        {
            tx.CounterPartyIBAN = ibanMatch.Groups[1].Value;
        }
    }

    private string? InferBic(string iban)
    {
        if (string.IsNullOrEmpty(iban) || iban.Length < 6) return null;
        
        string prefix = iban.Substring(0, 6);
        if (BicMapping.TryGetValue(prefix, out var bic)) return bic;
        
        // Try 4 chars prefix
        prefix = iban.Substring(0, 4);
        if (BicMapping.TryGetValue(prefix, out bic)) return bic;

        return null;
    }
}
