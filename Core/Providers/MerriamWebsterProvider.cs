using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using SpeechMorphingDataGatherer.Core.Database;
using SpeechMorphingDataGatherer.Views;

namespace SpeechMorphingDataGatherer.Core.Providers
{
    public class MerriamWebsterProvider : TrainingSetProvider
    {
        private WebClient client = new WebClient();

        public MerriamWebsterProvider()
        {
            Title = "Merriam-Webster Training Set Provider";
            ds= new TrainingDataset(@"D:\My Projects\Speech Morphing Data Gatherer\Data\");
            
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

        private int totalCount = 0;
        private void DownloadData(string url)
        {
            WebClient client = new WebClient();
            try
            {
                String html = Encoding.UTF8.GetString(client.DownloadData(url));
                String audioURL = GetAudioURL(html);

                if (audioURL != null)
                {
                    String path = @"d:\" + Convert.ToBase64String(Encoding.ASCII.GetBytes(audioURL)).Replace("=", "").Replace("/", "").Replace("\\", "") + ".mp3";
                    client.DownloadFile(audioURL, path);
                    String word = GetWord(html);
                    String phonetics = GetPhonetics(html).Trim();

                    if (!phonetics.StartsWith("<html"))
                    {
                        TrainingEntry entry = null;

                        lock (this)
                        {
                            entry = ds.GetEntry(word);
                        }

                        entry.Word = word;
                        entry.Phonetics = phonetics;
                        entry.AddAudio(path, "Merriam-Webster@" + url);
                    }

                    lock(this)
                    {
                        totalCount++;
                    }
                    Console.WriteLine(totalCount);

                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                int h = 0;
            }
        }

        private static string GetAudioURL(String html)
        {
            String link = html.Substring(html.IndexOf("<a class=\"play-pron"));
            link = link.Substring(link.IndexOf("href") + 6);
            link = link.Substring(0, link.IndexOf("\"")).Replace("&amp;", "&");

            if (link == "javascript:void(0)")
            {
                return null;
            }
            String[] parts = link.Split(new string[] {"&"}, StringSplitOptions.RemoveEmptyEntries);
            
            return String.Format("https://media.merriam-webster.com/audio/prons/en/us/mp3/{0}/{1}.mp3", GetValue(parts, "dir"), GetValue(parts, "file"));
        }

        private static String GetValue(string[] parts, string name)
        {
            String result = null;

            foreach (String key in parts)
            {
                if (key.StartsWith(name))
                {
                    result = key.Substring(key.IndexOf("=") + 1);
                    break;
                }
            }

            return result;
        }

        private string GetPhonetics(string html)
        {
            String start = "<span class=\"pr\">";
            String result = html.Substring(html.IndexOf(start) + start.Length);
            result = result.Substring(0, result.IndexOf("</"));

            return result;
        }

        private string GetWord(string html)
        {
            String h1 = html.Substring(html.IndexOf("<h1"));
            h1 = h1.Substring(h1.IndexOf(">") + 1);
            h1 = h1.Substring(0, h1.IndexOf("</h1>"));

            return h1;
        }

        private List<String> LoadURLs()
        {
            List<String> result = new List<String>();

            for (int i = 0; i <= 51; i++)
            {
                String url = $"https://www.merriam-webster.com/sitemap-ssl/az-{i}.xml.gz";
                result.AddRange(GetURLsFrom(i));
            }

            return result;
        }

        private List<String> GetURLsFrom(int i)
        {
            String path = @"D:\My Projects\Speech Morphing Data Gatherer\Resources\az-" + i + ".xml";
            String xml = File.ReadAllText(path);
            List<String> result = new List<String>();

            String[] parts = xml.Split(new String[] {"<loc>"}, StringSplitOptions.RemoveEmptyEntries);
            for (int j = 1; j < parts.Length; j++)
            {
                String url = parts[j];
                url = url.Substring(0, url.IndexOf("</loc>"));
                result.Add(url);
            }

            return result;
        }
    }
}