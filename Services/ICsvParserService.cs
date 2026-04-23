using RevolutToDh.Models;

namespace RevolutToDh.Services;

public interface ICsvParserService
{
    (List<RevolutTransaction> Transactions, RevolutStatementInfo Info) Parse(Stream csvStream);
}
