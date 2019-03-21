using System;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Text;
using SpeechMorphingDataGatherer.Core.Database;

namespace SpeechMorphingDataGatherer.Core
{
    public class FFMPEG
    {
        public static void ConvertAudioToWave(String input, String output, int freq)
        {
            String[] args = new String[]
            {
                "-i", input, "-acodec", "pcm_s16le", "-ac", "1", "-ar", freq.ToString(), output
            };

            RunFFMPEGWithParameters(args);
        }
        
        public static FFMPEGFileInfo GetFileInfo(string path)
        {
            String output = RunFFMPEGWithParameters(new String[] {"-i", path});
            String line = output.Substring(output.IndexOf("Stream #0"));
            line = line.Substring(0, line.IndexOf("\n")).Trim();
            
            FFMPEGFileInfo info = new FFMPEGFileInfo();

            info.Format = GetFormat(line);
            info.SamplingRate = GetSamplingRate(line);
            info.BitRate = GetBitRate(line);
            
            return info;
        }

        private static int GetSamplingRate(String line)
        {
            line = line.Substring(0, line.IndexOf("Hz")).Trim();
            line = line.Substring(line.LastIndexOf(" ")).Trim();

            return int.Parse(line);
        }

        private static int GetBitRate(String line)
        {
            line = line.Substring(0, line.IndexOf("kb/s")).Trim();
            line = line.Substring(line.LastIndexOf(" ")).Trim();

            return int.Parse(line);
        }

        private static AudioFormat GetFormat(String line)
        {
            String format = line.Substring(line.IndexOf("Audio:") + 6).Trim();
            format = format.Substring(0, format.IndexOf(","));

            if (format == "mp3")
            {
                return AudioFormat.MP3;
            }
            
            throw new NotSupportedException();
        }

        private static String RunFFMPEGWithParameters(String[] args)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = Constants.FFMPEG_EXE_PATH;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardError = true;
            info.RedirectStandardOutput = true;
            info.Arguments = CreateArgument(args);

            Process process = Process.Start(info);

            StringBuilder output = new StringBuilder();
            while (!process.StandardError.EndOfStream)
            {
                string line = process.StandardError.ReadLine();
                output.Append(line).Append("\r\n");
            }

            return output.ToString();
        }

        private static String CreateArgument(String[] args)
        {
            StringBuilder result = new StringBuilder();

            foreach (String arg in args)
            {
                result.Append(" ");
                if (arg.Contains(" "))
                {
                    result.Append("\"" + arg + "\"");
                }
                else
                {
                    result.Append(arg);
                }
            }

            return result.ToString().Trim();
        }
    }

    public class FFMPEGFileInfo
    {
        public double BitRate
        {
            get;
            set;
        }

        public int SamplingRate
        {
            get;
            set;
        }

        public AudioFormat Format { get; set; }
    }
}