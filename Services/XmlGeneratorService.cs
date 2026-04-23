using System.Xml.Linq;
using System.Globalization;
using RevolutToDh.Models;

namespace RevolutToDh.Services;

public class XmlGeneratorService : IXmlGeneratorService
{
    public string GenerateXml(List<RevolutTransaction> transactions, RevolutStatementInfo info)
    {
        XNamespace ns = "urn:iso:std:iso:20022:tech:xsd:camt.053.001.02";
        
        // Calculate net amount for each transaction as it affects the balance
        // Revolut CSV: Balance = Previous Balance + Amount - Fee
        // So NetAmount = Amount - Fee
        var processedTransactions = transactions.Select(t => new {
            Tx = t,
            NetAmt = t.Amount - t.Fee
        }).ToList();

        var creditEntries = processedTransactions.Where(t => t.NetAmt >= 0).ToList();
        var debitEntries = processedTransactions.Where(t => t.NetAmt < 0).ToList();

        var firstDate = transactions.FirstOrDefault()?.Date ?? DateTime.Now;
        var lastDate = transactions.LastOrDefault()?.Date ?? firstDate;
        
        int month = firstDate.Month;
        var msgId = $"978-{firstDate:yyyy}-{month:D5}";

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "Document",
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XElement(ns + "BkToCstmrStmt",
                    new XElement(ns + "GrpHdr",
                        new XElement(ns + "MsgId", msgId),
                        new XElement(ns + "CreDtTm", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"))
                    ),
                    new XElement(ns + "Stmt",
                        new XElement(ns + "Id", msgId),
                        new XElement(ns + "LglSeqNb", month.ToString()),
                        new XElement(ns + "CreDtTm", firstDate.ToString("yyyy-MM-ddT00:00:00")),
                        new XElement(ns + "Acct",
                            new XElement(ns + "Id", new XElement(ns + "IBAN", info.IBAN)),
                            new XElement(ns + "Ownr", 
                                new XElement(ns + "Nm", info.OwnerName),
                                GenerateAddress(ns, info.OwnerAddress)
                            )
                        ),
                        // Opening Balance
                        new XElement(ns + "Bal",
                            new XElement(ns + "Tp", new XElement(ns + "CdOrPrtry", new XElement(ns + "Cd", "OPBD"))),
                            new XElement(ns + "Amt", new XAttribute("Ccy", "EUR"), info.OpeningBalance.ToString("F2", CultureInfo.InvariantCulture)),
                            new XElement(ns + "CdtDbtInd", "CRDT"),
                            new XElement(ns + "Dt", new XElement(ns + "Dt", firstDate.ToString("yyyy-MM-dd")))
                        ),
                        // Closing Balance
                        new XElement(ns + "Bal",
                            new XElement(ns + "Tp", new XElement(ns + "CdOrPrtry", new XElement(ns + "Cd", "CLBD"))),
                            new XElement(ns + "Amt", new XAttribute("Ccy", "EUR"), info.ClosingBalance.ToString("F2", CultureInfo.InvariantCulture)),
                            new XElement(ns + "CdtDbtInd", "CRDT"),
                            new XElement(ns + "Dt", new XElement(ns + "Dt", lastDate.ToString("yyyy-MM-dd")))
                        ),
                        // Summary block
                        new XElement(ns + "TxsSummry",
                            new XElement(ns + "TtlCdtNtries",
                                new XElement(ns + "NbOfNtries", creditEntries.Count.ToString()),
                                new XElement(ns + "Sum", creditEntries.Sum(t => t.NetAmt).ToString("F2", CultureInfo.InvariantCulture))
                            ),
                            new XElement(ns + "TtlDbtNtries",
                                new XElement(ns + "NbOfNtries", debitEntries.Count.ToString()),
                                new XElement(ns + "Sum", Math.Abs(debitEntries.Sum(t => t.NetAmt)).ToString("F2", CultureInfo.InvariantCulture))
                            )
                        ),
                        // Transactions
                        processedTransactions.Select((pt, idx) => {
                            var tx = pt.Tx;
                            bool isCredit = pt.NetAmt >= 0;
                            string counterPartyName = tx.CounterPartyName ?? "NOTPROVIDED";
                            string counterPartyIBAN = tx.CounterPartyIBAN ?? "NOTPROVIDED";
                            string counterPartyBIC = tx.CounterPartyBIC ?? "NOTPROVIDED";
                            string reference = tx.Reference ?? "NOTPROVIDED";
                            string txId = (282000000 + idx).ToString();

                            // Use a more "real" looking EndToEndId if reference is missing
                            string endToEndId = reference != "NOTPROVIDED" ? reference : $"REV-{tx.Date:yyyyMMdd}-{idx:D4}";

                            return new XElement(ns + "Ntry",
                                new XElement(ns + "Amt", new XAttribute("Ccy", "EUR"), Math.Abs(pt.NetAmt).ToString("F2", CultureInfo.InvariantCulture)),
                                new XElement(ns + "CdtDbtInd", isCredit ? "CRDT" : "DBIT"),
                                new XElement(ns + "RvslInd", "false"),
                                new XElement(ns + "Sts", "BOOK"),
                                new XElement(ns + "BookgDt", new XElement(ns + "Dt", tx.Date.ToString("yyyy-MM-dd"))),
                                new XElement(ns + "ValDt", new XElement(ns + "Dt", tx.Date.ToString("yyyy-MM-dd"))),
                                new XElement(ns + "AcctSvcrRef", txId),
                                new XElement(ns + "BkTxCd", 
                                    new XElement(ns + "Prtry", new XElement(ns + "Cd", "NOTPROVIDED"))
                                ),
                                new XElement(ns + "NtryDtls",
                                    new XElement(ns + "TxDtls",
                                        new XElement(ns + "Refs",
                                            new XElement(ns + "InstrId", endToEndId),
                                            new XElement(ns + "EndToEndId", endToEndId),
                                            new XElement(ns + "TxId", txId)
                                        ),
                                        new XElement(ns + "RltdPties",
                                            new XElement(ns + "Dbtr", 
                                                new XElement(ns + "Nm", isCredit ? counterPartyName : info.OwnerName),
                                                isCredit ? GenerateAddress(ns, tx.CounterPartyAddress) : GenerateAddress(ns, info.OwnerAddress)
                                            ),
                                            new XElement(ns + "DbtrAcct", new XElement(ns + "Id", new XElement(ns + "IBAN", isCredit ? counterPartyIBAN : info.IBAN))),
                                            new XElement(ns + "Cdtr", 
                                                new XElement(ns + "Nm", isCredit ? info.OwnerName : counterPartyName),
                                                isCredit ? GenerateAddress(ns, info.OwnerAddress) : GenerateAddress(ns, tx.CounterPartyAddress)
                                            ),
                                            new XElement(ns + "CdtrAcct", new XElement(ns + "Id", new XElement(ns + "IBAN", isCredit ? info.IBAN : counterPartyIBAN)))
                                        ),
                                        new XElement(ns + "RltdAgts",
                                            new XElement(ns + "DbtrAgt", new XElement(ns + "FinInstnId", new XElement(ns + "BIC", isCredit ? counterPartyBIC : info.BIC))),
                                            new XElement(ns + "CdtrAgt", new XElement(ns + "FinInstnId", new XElement(ns + "BIC", isCredit ? info.BIC : counterPartyBIC)))
                                        ),
                                        new XElement(ns + "Purp", new XElement(ns + "Cd", tx.PurposeCode ?? "OTHR")),
                                        new XElement(ns + "RmtInf",
                                            new XElement(ns + "Strd",
                                                new XElement(ns + "CdtrRefInf",
                                                    new XElement(ns + "Tp", new XElement(ns + "CdOrPrtry", new XElement(ns + "Cd", "SCOR"))),
                                                    new XElement(ns + "Ref", reference)
                                                ),
                                                new XElement(ns + "AddtlRmtInf", tx.Description + (tx.Fee > 0 ? $" (Fee: {tx.Fee:F2})" : ""))
                                            )
                                        )
                                    )
                                )
                            );
                        })
                    )
                )
            )
        );

        return doc.ToString();
    }

    private XElement GenerateAddress(XNamespace ns, string? address)
    {
        var element = new XElement(ns + "PstlAdr", new XElement(ns + "Ctry", "SI"));
        if (string.IsNullOrEmpty(address))
        {
            return element;
        }

        var lines = address.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            element.Add(new XElement(ns + "AdrLine", line.Trim().ToUpperInvariant()));
        }

        return element;
    }
}
