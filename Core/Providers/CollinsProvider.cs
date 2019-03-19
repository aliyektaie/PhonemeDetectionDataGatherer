using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using SpeechMorphingDataGatherer.Core.Database;

namespace SpeechMorphingDataGatherer.Core.Providers
{
    public class CollinsProvider : TrainingSetProvider
    {
        private WebClient client = new WebClient();

        public CollinsProvider()
        {
            Title = "Collins Training Set Provider";
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
                    String path = @"d:\" + Convert.ToBase64String(Encoding.ASCII.GetBytes(audioURL)).Replace("=", "")
                                      .Replace("/", "").Replace("\\", "") + ".mp3";
                    String word = GetWord(html);
                    String phonetics = GetPhonetics(html);

                    if (phonetics != null && word != null)
                    {
                        client.DownloadFile(audioURL, path);
                        TrainingEntry entry = null;

                        lock (this)
                        {
                            entry = ds.GetEntry(word);
                        }

                        entry.Word = word;
                        entry.Phonetics = phonetics;
                        entry.AddAudio(path, "Collins@" + url);

                        lock (this)
                        {
                            totalCount++;
                        }

                        Console.WriteLine(totalCount);
                        File.Delete(path);
                    }
                }
            }
            catch (Exception ex)
            {
                int h = 0;
            }
        }

        private static string GetAudioURL(String html)
        {
            if (!html.Contains("data-src-mp3"))
            {
                return null;
            }

            String result = html.Substring(html.IndexOf("data-src-mp3"));
            result = result.Substring(result.IndexOf("\"") + 1);
            result = result.Substring(0, result.IndexOf("\""));

            return result;
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
            String start = "<span class=\"pron type";
            String result = null;

            if (html.Contains(start))
            {
                result = html.Substring(html.IndexOf(start));
                result = result.Substring(0, result.IndexOf("<span class=\"span\">)"));
                StringBuilder f = new StringBuilder();
                bool isInTag = false;

                for (int i = 0; i < result.Length; i++)
                {
                    char c = result[i];

                    if (c == '<')
                    {
                        isInTag = true;
                    }
                    else if (c == '>')
                    {
                        isInTag = false;
                    }
                    else if (!isInTag)
                    {
                        f.Append(c);
                    }
                }

                result = f.ToString().Trim();
            }
            else
            {
                result = null;
            }

            if (result == "")
            {
                result = null;
            }

            return result;
        }

        private string GetWord(string html)
        {
            if (!html.Contains("<h1 class=\"entry_title\">"))
            {
                return null;
            }

            String title = html.Substring(html.IndexOf("<h1 class=\"entry_title\">"));
            title = title.Substring(title.IndexOf("'") + 1);
            title = title.Substring(0, title.IndexOf("'"));

            if (title.Contains("<"))
            {
                return null;
            }

            return title.Trim();
        }

        private List<String> LoadURLs()
        {
            List<String> result = new List<String>();

            for (int i = 1; i <= 7; i++)
            {
                String url = $"https://www.collinsdictionary.com/sitemap/english/sitemap{i}.xml";
                result.AddRange(GetURLsFrom(url));
            }

            // result = FilterCount(result, 2000);

            return result;
        }

        private List<String> GetURLsFrom(String siteMapURL)
        {
            Console.WriteLine("Loading: " + siteMapURL);
            String xml = client.DownloadString(siteMapURL);
            Console.WriteLine("   -> Done");
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