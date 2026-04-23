using RevolutToDh.Models;

namespace RevolutToDh.Services;

public interface IXmlGeneratorService
{
    string GenerateXml(List<RevolutTransaction> transactions, RevolutStatementInfo info);
}
