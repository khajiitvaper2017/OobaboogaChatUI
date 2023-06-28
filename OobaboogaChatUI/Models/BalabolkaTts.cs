using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OobaboogaChatUI.Properties;

namespace OobaboogaChatUI.Models
{
    public static class BalabolkaTts
    {
        public static MD5 Md5Hash = MD5.Create();
        public static string CalculateHash(FileInfo fInfo)
        {
            using FileStream fileStream = fInfo.Open(FileMode.Open);
            string hash = "";
            try
            {
                fileStream.Position = 0;
                var bytes = Md5Hash.ComputeHash(fileStream);
                StringBuilder sBuilder = new StringBuilder();
                foreach (var b in bytes)
                {
                    sBuilder.Append(b.ToString("x2"));
                }
                hash = sBuilder.ToString();
            }
            catch (IOException e)
            {
                Debug.WriteLine($"I/O Exception: {e.Message}");
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.WriteLine($"Access Exception: {e.Message}");
            }
            return hash;
        }
        public static string GenerateAudio(string text)
        {
            var tempFolder = Path.Combine(Environment.CurrentDirectory, "Temp");
            var audioFolder = Path.Combine(Environment.CurrentDirectory, "Audio");
            Directory.CreateDirectory(audioFolder);
            Directory.CreateDirectory(tempFolder);

            var cmdPath = Settings.Default.BalabolkaExecutable ?? "";
            var textFile = Path.Combine(tempFolder, "text.txt");
            var audioFile = Path.Combine(tempFolder, "audio.wav");
            File.WriteAllText(textFile, text, Encoding.UTF8);


            var psi = new ProcessStartInfo(cmdPath, $"-f {textFile} -w {audioFile}")
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            var process = Process.Start(psi);

            process.WaitForExit();

            if (process.ExitCode != 0) return "";

            var file = new FileInfo(audioFile);
            var hash = CalculateHash(file);
            if (string.IsNullOrWhiteSpace(hash)) return "";
            var finalAudio = Path.Combine(audioFolder, hash + ".wav");
            try
            {
                file.MoveTo(finalAudio);
            }
            catch (Exception e)
            {
                return "";
            }
            return finalAudio;

        }
    }
}
