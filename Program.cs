using System.Diagnostics;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

public class TimestampService
{
    public static void ApplyTimestamp(string pdfFilePath, string tssJarPath, string tssAddress, int tssPort, string customerNo, string customerPassword, string hashType)
    {
        try
        {
            // PDF'e zaman damgası bilgisini ekleyelim
            AddTimestampTextToPdf(pdfFilePath, "1. zaman damgası uygulanmıştır", XStringFormats.BottomLeft);
            
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

    public static void ApplySecondTimestampAfterApproval(string originalPdfFilePath, string finalPdfFilePath, string tssJarPath, string tssAddress, int tssPort, string customerNo, string customerPassword, string hashType)
    {
        // PDF dosyasını kopyalama
        File.Copy(originalPdfFilePath, finalPdfFilePath, true);

        // Kopyalanan dosyaya ikinci zaman damgası metnini ekleyelim
        AddSecondTimestampTextToPdf(finalPdfFilePath, "2. zaman damgası uygulanmıştır", XStringFormats.BottomRight);

        // PDF'e ikinci zaman damgasını ekleyelim
        ApplyTimestamp(finalPdfFilePath, tssJarPath, tssAddress, tssPort, customerNo, customerPassword, hashType);
    }

    public static void AddTimestampTextToPdf(string pdfFilePath, string text, XStringFormat position)
    {
        PdfDocument document = PdfReader.Open(pdfFilePath, PdfDocumentOpenMode.Modify);
        PdfPage page = document.Pages[0];
        XGraphics gfx = XGraphics.FromPdfPage(page);
        XFont font = new XFont("Verdana", 10, XFontStyle.Bold);

        // Sol alt köşeye metni ekle
        gfx.DrawString(text, font, XBrushes.Black, new XPoint(50, page.Height - 50));

        document.Save(pdfFilePath);
    }

    public static void AddSecondTimestampTextToPdf(string pdfFilePath, string text, XStringFormat position)
    {
        PdfDocument document = PdfReader.Open(pdfFilePath, PdfDocumentOpenMode.Modify);
        PdfPage page = document.Pages[0];
        XGraphics gfx = XGraphics.FromPdfPage(page);
        XFont font = new XFont("Verdana", 10, XFontStyle.Bold);

        // Sağ alt köşeye metni ekle
        gfx.DrawString(text, font, XBrushes.Black, new XPoint(page.Width - 150, page.Height - 50));

        document.Save(pdfFilePath);
    }

    public static bool IsTimestampValid(string pdfFilePath, string tssJarPath)
    {
        try
        {
            // .zd uzantılı dosya adını oluştur
            string zdFilePath = $"{pdfFilePath}.zd";

            // Zaman damgası kontrol komutunu oluştur
            string command = $"-jar {tssJarPath} -c {pdfFilePath} {zdFilePath}";
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

                // Çıktıda zaman damgasının geçerli olup olmadığını kontrol et
                return output.Contains("Zaman Damgası geçerli, dosya değişmemiş.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hata: " + ex.Message);
            return false;
        }
    }
}

class Program
{
    static void Main()
    {
        string folderPath = @"C:\Users\hmztp\Desktop\bordrolar"; 
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

                // Kullanıcı onayladıktan sonra ikinci zaman damgasının eklenmesi
                string finalPdfFilePath = Path.Combine(Path.GetDirectoryName(pdfFilePath), Path.GetFileNameWithoutExtension(pdfFilePath) + "_final.pdf");
                TimestampService.ApplySecondTimestampAfterApproval(pdfFilePath, finalPdfFilePath, tssJarPath, tssAddress, tssPort, customerNo, customerPassword, hashType);
            }
            else
            {
                Console.WriteLine($"{pdfFilePath} dosyası zaten geçerli bir zaman damgası ile damgalanmış.");
            }
        }
    }
}
