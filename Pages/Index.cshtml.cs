using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.IO;
using RevolutToDh.Models;
using RevolutToDh.Services;

namespace RevolutToDh.Pages;

public class IndexModel : PageModel
{
    private readonly ICsvParserService _csvParserService;
    private readonly IXmlGeneratorService _xmlGeneratorService;

    public IndexModel(ICsvParserService csvParserService, IXmlGeneratorService xmlGeneratorService)
    {
        _csvParserService = csvParserService;
        _xmlGeneratorService = xmlGeneratorService;
    }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            ErrorMessage = "Please select a valid CSV file.";
            return Page();
        }

        try
        {
            using var stream = csvFile.OpenReadStream();
            var (transactions, info) = _csvParserService.Parse(stream);
            
            var xml = _xmlGeneratorService.GenerateXml(transactions, info);
            var bytes = Encoding.UTF8.GetBytes(xml);
            
            var fileName = Path.GetFileNameWithoutExtension(csvFile.FileName) + ".xml";
            return File(bytes, "application/xml", fileName);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
            return Page();
        }
    }
}
