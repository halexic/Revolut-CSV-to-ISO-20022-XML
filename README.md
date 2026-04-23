# Revolut to Delavska Hranilnica (DH) XML Converter

A specialized .NET 10 Razor Pages web application designed to convert Revolut monthly CSV statements into the ISO 20022 XML format (camt.053) required by the Slovenian bank **Delavska hranilnica d.d.**

## 🚀 Purpose

Many business users in Slovenia use Revolut for their operations but need to import their transaction data into local accounting software that specifically requires the XML format used by Delavska hranilnica. This tool bridges that gap by:
- Parsing Revolut's standard CSV export.
- Mapping transactions to the exact ISO 20022 specification used by DH.
- Producing a single, downloadable `.xml` file ready for import.

## ✨ Key Features

- **Direct File Conversion:** Upload a `.csv` and instantly download the matching `.xml`.
- **ISO 20022 Compliance:** Generates `camt.053.001.02` messages.
- **Smart Transaction Mapping:**
  - Automatic detection of Credits (CRDT) and Debits (DBIT).
  - Extraction of references (SI00, etc.) and counterparty details.
  - Calculation of opening and closing balances based on transaction history.
- **Configurable Metadata:** Sensitive data (IBAN, BIC, Owner Name) is managed via `appsettings.json`, allowing the tool to be used for different business entities.
- **Logging:** Comprehensive file-based logging using Serilog.
- **Clean Architecture:** Modern C# project structure with separated Models, Services, and Interfaces.

## 🛠 Technology Stack

- **Backend:** .NET 10 (ASP.NET Core Razor Pages)
- **CSV Parsing:** [CsvHelper](https://joshclose.github.io/CsvHelper/)
- **XML Generation:** System.Xml.Linq (XDocument)
- **Logging:** [Serilog](https://serilog.net/) (File Sink)
- **Frontend:** Bootstrap 5 (Vanilla CSS for custom styling)

## 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## ⚙️ Configuration

Before running the application, configure your company details in `appsettings.json`:

```json
{
  "CompanySettings": {
    "IBAN": "YOUR_COMPANY_BANK_ACCOUNT_IBAN",
    "BIC": "YOUR_COMPANY_BANK_BIC",
    "OwnerName": "ENTER_YOUR_COMPANY_NAME",
    "OwnerAddress": "ENTER_YOUR_COMPANY_ADDRESS",
    "RevolutBankName": "Revolut Bank UAB",
    "RevolutBankIBAN": "LT673250045712503752",
    "RevolutBankBIC": "REVOLT21",
    "KnownCounterParties": [
      {
        "MatchName": "SOME_NAME_TO_MATCH",
        "IBAN": "COUNTERPARTY_IBAN",
        "BIC": "COUNTERPARTY_BIC"
      }
    ]
  }
}
```

- **KnownCounterParties:** Allows you to define specific IBAN/BIC for counterparties that Revolut might not include in the CSV.

## 🚀 How to Use

1. **Clone the repository.**
2. **Configure `appsettings.json`** with your data.
3. **Run the application:**
   ```bash
   dotnet run
   ```
4. **Open your browser** and navigate to `https://localhost:5001`.
5. **Select your Revolut CSV file** (e.g., `202603-revolut.csv`).
6. **Click "Convert and Download XML"**.
7. The resulting file will be named `202603-revolut.xml`.

## 📁 Project Structure

- `Pages/`: Razor Pages for the UI.
- `Models/`: Data transfer objects (DTOs) and configuration classes.
- `Services/`: Core business logic for parsing and XML generation.
- `Logs/`: Directory where Serilog stores daily logs.

## ⚖️ License

Copyright © 2026 - [Kvadrati](https://www.kvadrati.com)
