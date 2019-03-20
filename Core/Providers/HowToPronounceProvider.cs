using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using SpeechMorphingDataGatherer.Core.Database;

namespace SpeechMorphingDataGatherer.Core.Providers
{
    public class HowToPronounceProvider : TrainingSetProvider
    {
        private WebClient client = new WebClient();

        public HowToPronounceProvider()
        {
            Title = "'How To Pronounce' Training Set Provider";
            ds = new TrainingDataset(@"D:\My Projects\Speech Morphing Data Gatherer\Data\");
        }

        protected override void Load()
        {
            List<String> urls = LoadURLs();
            List<ThreadStart> list = new List<ThreadStart>();

            for (int i = 0; i < urls.Count; i++)
            {
                String url = urls[i];
                ThreadStart t = new ThreadStart(() =>
                {
                    WaitPaused();
                    DownloadData(url);
                });

                list.Add(t);
            }

            ExecuteSubTasks(list, 20);

            ds.OnSaveProgress += OnSaveProgress;
            ds.Save();
        }

        private void OnSaveProgress(double obj)
        {
            base.RaiseOnLoadProgress(obj);
        }

        private int totalEntryCount = 0;
        private int totalAudioCount = 0;

        private void DownloadData(string word)
        {
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36");

            try
            {
                String url = $"https://www.howtopronounce.com/{word}/";
                String html = Encoding.UTF8.GetString(client.DownloadData(url));
                List<String> audioURL = GetAudioURL(html);

                if (audioURL.Count > 0)
                {
                    TrainingEntry entry = null;

                    lock (this)
                    {
                        entry = ds.GetEntry(word);
                    }

                    entry.Word = word;
                    entry.Phonetics = "";
                    lock (this)
                    {
                        totalEntryCount++;
                    }

                    String path = @"d:\" + Convert.ToBase64String(Encoding.ASCII.GetBytes(word)).Replace("=", "")
                                      .Replace("/", "").Replace("\\", "") + ".mp3";
                    foreach (String u in audioURL)
                    {
                        client.DownloadFile(u, path);
                        entry.AddAudio(path, "HowToPronounce@" + url);

                        lock (this)
                        {
                            totalAudioCount++;
                        }
                        File.Delete(path);                        
                    }
                    Console.WriteLine($"{totalEntryCount} -> {totalAudioCount}");
                }
            }
            catch (Exception ex)
            {
                int h = 0;
            }
        }

        private static List<String> GetAudioURL(String html)
        {
            List<String> result = new List<string>();
            String[] parts = html.Split(new String[] {"<div class=\"main_audio_block"},
                StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < parts.Length; i++)
            {
                String h = parts[i];
                if (h.Contains("data-id=\"https:"))
                {
                    h = h.Substring(h.IndexOf("data-id=\"https:") + 9);
                    h = h.Substring(0, h.IndexOf("\""));

                    if (h.EndsWith(".mp3") && h.Contains("-in-american"))
                    {
                        result.Add(h);
                    }
                }
            }

            return result;
        }

        private List<String> LoadURLs()
        {
            List<String> words = GetPreviousDataSetWords();
            List<String> result = new List<String>();

            foreach (String word in words)
            {
                result.Add(word);
            }

            //result = FilterCount(result, 2000);

            return result;
        }

        private List<String> GetPreviousDataSetWords()
        {
            Dictionary<String, int> wordsWithCount = new Dictionary<string, int>();

            AddToResult(wordsWithCount, @"C:\Users\Yektaie\Desktop\Data (Collins)\index.sqlite");
            AddToResult(wordsWithCount, @"C:\Users\Yektaie\Desktop\Data (Longman)\index.sqlite");
            AddToResult(wordsWithCount, @"C:\Users\Yektaie\Desktop\Data (Merriam-Webster)\index.sqlite");

            List<String> result = new List<String>();
            StringBuilder csvFile = new StringBuilder();

            foreach (KeyValuePair<String, int> pair in wordsWithCount)
            {
                result.Add(pair.Key);
                csvFile.Append($"{pair.Key},{pair.Value}\r\n");
            }

            //File.WriteAllText(@"d:\summary.csv", csvFile.ToString());
            return result;
        }

        private void AddToResult(Dictionary<string, int> wordsWithCount, string path)
        {
            SQLiteConnection connection = new SQLiteConnection($"Data Source=\"{path}\";Version=3;");
            connection.Open();

            SQLiteCommand command =
                new SQLiteCommand(
                    "select word from training_entries where not (word like '% %' or word like '%-%' or word like '%/%' or word like '%(%' or word like '%)%')",
                    connection);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                String word = reader["word"].ToString();
                if (wordsWithCount.ContainsKey(word))
                {
                    wordsWithCount[word]++;
                }
                else
                {
                    wordsWithCount.Add(word, 1);
                }
            }

            connection.Close();
        }
    }
}