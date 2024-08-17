using System;
using System.Diagnostics;
using System.IO;

public class TimestampService
{
    public static void ApplyTimestamp(string pdfFilePath, string tssJarPath, string tssAddress, int tssPort, string customerNo, string customerPassword, string hashType)
    {
        try
        {
            string command = $"-jar {tssJarPath} -z {pdfFilePath} {tssAddress} {tssPort} {customerNo} {customerPassword} {hashType}";
            ProcessStartInfo processStartInfo = new ProcessStartInfo("java", command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (Process process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Zaman damgası başarıyla uygulandı: {pdfFilePath}");
                    Console.WriteLine(output);
                }
                else
                {
                    Console.WriteLine($"Zaman damgası uygulama hatası: {pdfFilePath}");
                    Console.WriteLine(error);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hata: " + ex.Message);
        }
    }

    public static bool IsTimestampValid(string pdfFilePath, string tssJarPath)
    {
        try
        {
            string command = $"-c {pdfFilePath} {pdfFilePath}.zd";
            ProcessStartInfo processStartInfo = new ProcessStartInfo("java", command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (Process process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Contains("Zaman Damgası geçerli, dosya değişmemiş.");
            }
        }
        catch
        {
            return false;
        }
    }
}

class Program
{
    static void Main()
    {
        string folderPath = @"C:\Users\hmztp\Desktop\bordrolar"; // ToDo - payroll docs logics, Second timestamp adding and qr code adding
        string tssJarPath = @"C:\Users\hmztp\Desktop\tss-client-console-3.1.19.jar";
        string tssAddress = "http://zd.kamusm.gov.tr";
        int tssPort = 80;
        string customerNo = "106572";
        string customerPassword = "83%71sU8";
        string hashType = "sha-512";

        var pdfFiles = Directory.GetFiles(folderPath, "*.pdf");

        foreach (var pdfFilePath in pdfFiles)
        {
            if (!TimestampService.IsTimestampValid(pdfFilePath, tssJarPath))
            {
                Console.WriteLine($"{pdfFilePath} dosyası için zaman damgası uygulanıyor...");
                TimestampService.ApplyTimestamp(pdfFilePath, tssJarPath, tssAddress, tssPort, customerNo, customerPassword, hashType);
            }
            else
            {
                Console.WriteLine($"{pdfFilePath} dosyası zaten geçerli bir zaman damgası ile damgalanmış.");
            }
        }
    }
}
