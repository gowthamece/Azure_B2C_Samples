using B2C_user_management.Models;
using ClosedXML.Excel;
using System.Text;

namespace B2C_user_management.Services;

/// <summary>
/// Service for handling Excel file operations
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Reads user records from an uploaded Excel file
    /// </summary>
    /// <param name="fileStream">The Excel file stream</param>
    /// <returns>List of user records</returns>
    Task<List<UserRecord>> ReadUsersFromExcelAsync(Stream fileStream);

    /// <summary>
    /// Generates an Excel report from user check results
    /// </summary>
    /// <param name="results">List of user check results</param>
    /// <returns>Excel file as byte array</returns>
    byte[] GenerateExcelReport(List<UserCheckResult> results);

    /// <summary>
    /// Generates a CSV report from user check results
    /// </summary>
    /// <param name="results">List of user check results</param>
    /// <returns>CSV file as byte array</returns>
    byte[] GenerateCsvReport(List<UserCheckResult> results);
}

public class ExcelService : IExcelService
{
    private readonly ILogger<ExcelService> _logger;

    public ExcelService(ILogger<ExcelService> logger)
    {
        _logger = logger;
    }

    public async Task<List<UserRecord>> ReadUsersFromExcelAsync(Stream fileStream)
    {
        var users = new List<UserRecord>();

        try
        {
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var workbook = new XLWorkbook(memoryStream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // Skip header row

            if (rows == null)
            {
                _logger.LogWarning("No data rows found in the Excel file");
                return users;
            }

            // Find column indexes (case-insensitive)
            var headerRow = worksheet.Row(1);
            var emailColIndex = FindColumnIndex(headerRow, "email", "emailaddress", "email address", "e-mail");
            var displayNameColIndex = FindColumnIndex(headerRow, "displayname", "display name", "name");
            var firstNameColIndex = FindColumnIndex(headerRow, "firstname", "first name", "givenname", "given name");
            var lastNameColIndex = FindColumnIndex(headerRow, "lastname", "last name", "surname", "family name");

            if (emailColIndex == -1)
            {
                throw new InvalidOperationException("Email column not found in the Excel file. Please ensure the file has a column named 'Email' or 'EmailAddress'.");
            }

            foreach (var row in rows)
            {
                var email = row.Cell(emailColIndex).GetString()?.Trim();
                
                if (string.IsNullOrWhiteSpace(email))
                    continue;

                var user = new UserRecord
                {
                    Email = email,
                    DisplayName = displayNameColIndex > 0 ? row.Cell(displayNameColIndex).GetString()?.Trim() : null,
                    FirstName = firstNameColIndex > 0 ? row.Cell(firstNameColIndex).GetString()?.Trim() : null,
                    LastName = lastNameColIndex > 0 ? row.Cell(lastNameColIndex).GetString()?.Trim() : null
                };

                users.Add(user);
            }

            _logger.LogInformation("Successfully read {Count} users from Excel file", users.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Excel file");
            throw;
        }

        return users;
    }

    public byte[] GenerateExcelReport(List<UserCheckResult> results)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("User Check Results");

        // Add headers
        var headers = new[] { "Email", "Display Name", "First Name", "Last Name", "IsExist", "B2C Object ID", "Error Message" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Add data rows
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var row = i + 2;

            worksheet.Cell(row, 1).Value = result.Email;
            worksheet.Cell(row, 2).Value = result.DisplayName ?? "";
            worksheet.Cell(row, 3).Value = result.FirstName ?? "";
            worksheet.Cell(row, 4).Value = result.LastName ?? "";
            
            var isExistCell = worksheet.Cell(row, 5);
            isExistCell.Value = result.IsExist;
            isExistCell.Style.Fill.BackgroundColor = result.IsExist == "Y" ? XLColor.LightGreen : XLColor.LightCoral;
            
            worksheet.Cell(row, 6).Value = result.B2CObjectId ?? "";
            worksheet.Cell(row, 7).Value = result.ErrorMessage ?? "";
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Add summary
        var summaryRow = results.Count + 4;
        worksheet.Cell(summaryRow, 1).Value = "Summary";
        worksheet.Cell(summaryRow, 1).Style.Font.Bold = true;
        worksheet.Cell(summaryRow + 1, 1).Value = "Total Users:";
        worksheet.Cell(summaryRow + 1, 2).Value = results.Count;
        worksheet.Cell(summaryRow + 2, 1).Value = "Users Found (Y):";
        worksheet.Cell(summaryRow + 2, 2).Value = results.Count(r => r.IsExist == "Y");
        worksheet.Cell(summaryRow + 3, 1).Value = "Users Not Found (N):";
        worksheet.Cell(summaryRow + 3, 2).Value = results.Count(r => r.IsExist == "N");

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    public byte[] GenerateCsvReport(List<UserCheckResult> results)
    {
        var sb = new StringBuilder();
        
        // Add header
        sb.AppendLine("Email,DisplayName,FirstName,LastName,IsExist,B2CObjectId,ErrorMessage");

        // Add data rows
        foreach (var result in results)
        {
            sb.AppendLine($"{EscapeCsvField(result.Email)},{EscapeCsvField(result.DisplayName)},{EscapeCsvField(result.FirstName)},{EscapeCsvField(result.LastName)},{result.IsExist},{EscapeCsvField(result.B2CObjectId)},{EscapeCsvField(result.ErrorMessage)}");
        }

        // Add summary
        sb.AppendLine();
        sb.AppendLine("Summary");
        sb.AppendLine($"Total Users,{results.Count}");
        sb.AppendLine($"Users Found (Y),{results.Count(r => r.IsExist == "Y")}");
        sb.AppendLine($"Users Not Found (N),{results.Count(r => r.IsExist == "N")}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static int FindColumnIndex(IXLRow headerRow, params string[] possibleNames)
    {
        var cells = headerRow.CellsUsed();
        foreach (var cell in cells)
        {
            var cellValue = cell.GetString()?.Trim()?.ToLowerInvariant();
            if (cellValue != null && possibleNames.Any(name => name.ToLowerInvariant() == cellValue))
            {
                return cell.Address.ColumnNumber;
            }
        }
        return -1;
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // If the field contains comma, quote, or newline, wrap it in quotes and escape existing quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
