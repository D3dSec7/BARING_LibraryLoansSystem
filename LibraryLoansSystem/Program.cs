using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

class LoanRecord
{
    public int RecordId { get; set; }
    public string BorrowerName { get; set; }
    public string BookTitle { get; set; }
    public string BorrowDate { get; set; }
    public string ReturnDate { get; set; }
    public string CreatedAt { get; set; }
    public string UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public string Checksum { get; set; }
}

class Program
{
    static string dataFolder = "Data";
    static string loansFile = Path.Combine(dataFolder, "loans.txt");
    static string auditFile = Path.Combine(dataFolder, "audit.txt");

    static void Main()
    {
        InitializeStorage();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== LIBRARY LOANS SYSTEM ===");
            Console.WriteLine("1. Add Record");
            Console.WriteLine("2. View Records");
            Console.WriteLine("3. Search Records");
            Console.WriteLine("4. Update Record");
            Console.WriteLine("5. Soft Delete");
            Console.WriteLine("6. Generate Report");
            Console.WriteLine("7. Exit");
            Console.Write("Select an option: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    AddRecord();
                    break;

                case "2":
                    ViewRecords();
                    break;

                case "3":
                    SearchRecords();
                    break;

                case "4":
                    UpdateRecord();
                    break;

                case "5":
                    SoftDelete();
                    break;

                case "6":
                    GenerateReport();
                    break;

                case "7":
                    return;

                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }

            Console.WriteLine("\nPress any key...");
            Console.ReadKey();
        }
    }

    static void InitializeStorage()
    {
        if (!Directory.Exists(dataFolder))
            Directory.CreateDirectory(dataFolder);

        if (!File.Exists(loansFile))
            File.Create(loansFile).Close();

        if (!File.Exists(auditFile))
            File.Create(auditFile).Close();
    }

    static void AddRecord()
    {
        try
        {
            List<LoanRecord> records = LoadRecords();

            LoanRecord record = new LoanRecord();

            record.RecordId = records.Count > 0
                ? records.Max(r => r.RecordId) + 1
                : 1;

            Console.Write("Borrower Name: ");
            record.BorrowerName = Console.ReadLine();

            Console.Write("Book Title: ");
            record.BookTitle = Console.ReadLine();

            Console.Write("Borrow Date: ");
            record.BorrowDate = Console.ReadLine();

            Console.Write("Return Date: ");
            record.ReturnDate = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(record.BorrowerName) ||
                string.IsNullOrWhiteSpace(record.BookTitle))
            {
                Console.WriteLine("Validation failed.");
                return;
            }

            record.CreatedAt = DateTime.Now.ToString();
            record.UpdatedAt = DateTime.Now.ToString();
            record.IsActive = true;

            record.Checksum = GenerateChecksum(record);

            SaveRecord(record);

            LogAction("ADD", $"Added Record ID {record.RecordId}");

            Console.WriteLine("Record added successfully.");
        }
        catch (Exception ex)
        {
            LogAction("ERROR", ex.Message);
            Console.WriteLine("Error adding record.");
        }
    }

    static void ViewRecords()
    {
        List<LoanRecord> records = LoadRecords();

        Console.WriteLine("\n=== ACTIVE RECORDS ===");

        foreach (var r in records.Where(x => x.IsActive))
        {
            Console.WriteLine($"ID: {r.RecordId}");
            Console.WriteLine($"Borrower: {r.BorrowerName}");
            Console.WriteLine($"Book: {r.BookTitle}");
            Console.WriteLine($"Borrow Date: {r.BorrowDate}");
            Console.WriteLine($"Return Date: {r.ReturnDate}");
            Console.WriteLine("-------------------------");
        }

        LogAction("READ", "Viewed records");
    }

    static void SearchRecords()
    {
        Console.Write("Enter borrower name: ");
        string keyword = Console.ReadLine().ToLower();

        List<LoanRecord> records = LoadRecords();

        var results = records.Where(r =>
            r.BorrowerName.ToLower().Contains(keyword) &&
            r.IsActive);

        foreach (var r in results)
        {
            Console.WriteLine($"{r.RecordId} - {r.BorrowerName} - {r.BookTitle}");
        }

        LogAction("READ", $"Searched {keyword}");
    }

    static void UpdateRecord()
    {
        try
        {
            List<LoanRecord> records = LoadRecords();

            Console.Write("Enter Record ID: ");
            int id = int.Parse(Console.ReadLine());

            LoanRecord record = records.FirstOrDefault(r => r.RecordId == id);

            if (record == null)
            {
                Console.WriteLine("Record not found.");
                return;
            }

            Console.Write("New Borrower Name: ");
            record.BorrowerName = Console.ReadLine();

            Console.Write("New Book Title: ");
            record.BookTitle = Console.ReadLine();

            record.UpdatedAt = DateTime.Now.ToString();

            record.Checksum = GenerateChecksum(record);

            RewriteFile(records);

            LogAction("UPDATE", $"Updated Record ID {id}");

            Console.WriteLine("Record updated.");
        }
        catch (Exception ex)
        {
            LogAction("ERROR", ex.Message);
            Console.WriteLine("Update failed.");
        }
    }

    static void SoftDelete()
    {
        try
        {
            List<LoanRecord> records = LoadRecords();

            Console.Write("Enter Record ID: ");
            int id = int.Parse(Console.ReadLine());

            LoanRecord record = records.FirstOrDefault(r => r.RecordId == id);

            if (record == null)
            {
                Console.WriteLine("Record not found.");
                return;
            }

            record.IsActive = false;
            record.UpdatedAt = DateTime.Now.ToString();

            RewriteFile(records);

            LogAction("DELETE", $"Soft deleted Record ID {id}");

            Console.WriteLine("Record deleted.");
        }
        catch (Exception ex)
        {
            LogAction("ERROR", ex.Message);
            Console.WriteLine("Delete failed.");
        }
    }

    static void GenerateReport()
    {
        List<LoanRecord> records = LoadRecords();

        int activeCount = records.Count(r => r.IsActive);

        Console.WriteLine("\n=== REPORT ===");
        Console.WriteLine($"Total Active Loans: {activeCount}");

        LogAction("REPORT", "Generated report");
    }

    static void SaveRecord(LoanRecord r)
    {
        string line =
            $"{r.RecordId}|{r.BorrowerName}|{r.BookTitle}|{r.BorrowDate}|{r.ReturnDate}|{r.CreatedAt}|{r.UpdatedAt}|{r.IsActive}|{r.Checksum}";

        File.AppendAllText(loansFile, line + Environment.NewLine);
    }

    static List<LoanRecord> LoadRecords()
    {
        List<LoanRecord> records = new List<LoanRecord>();

        string[] lines = File.ReadAllLines(loansFile);

        foreach (string line in lines)
        {
            try
            {
                string[] parts = line.Split('|');

                LoanRecord r = new LoanRecord
                {
                    RecordId = int.Parse(parts[0]),
                    BorrowerName = parts[1],
                    BookTitle = parts[2],
                    BorrowDate = parts[3],
                    ReturnDate = parts[4],
                    CreatedAt = parts[5],
                    UpdatedAt = parts[6],
                    IsActive = bool.Parse(parts[7]),
                    Checksum = parts[8]
                };

                records.Add(r);
            }
            catch
            {
                LogAction("ERROR", "Malformed record skipped.");
            }
        }

        return records;
    }

    static void RewriteFile(List<LoanRecord> records)
    {
        List<string> lines = new List<string>();

        foreach (var r in records)
        {
            lines.Add(
                $"{r.RecordId}|{r.BorrowerName}|{r.BookTitle}|{r.BorrowDate}|{r.ReturnDate}|{r.CreatedAt}|{r.UpdatedAt}|{r.IsActive}|{r.Checksum}"
            );
        }

        File.WriteAllLines(loansFile, lines);
    }

    static string GenerateChecksum(LoanRecord r)
    {
        string raw =
            $"{r.RecordId}{r.BorrowerName}{r.BookTitle}{r.BorrowDate}";

        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));

            return Convert.ToBase64String(bytes);
        }
    }

    static void LogAction(string action, string details)
    {
        string log =
            $"{DateTime.Now} | {action} | {details}";

        File.AppendAllText(auditFile, log + Environment.NewLine);
    }
}