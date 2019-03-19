using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using SpeechMorphingDataGatherer.Core.Database;

namespace SpeechMorphingDataGatherer.Core.Providers
{
    public class LongmanProvider : TrainingSetProvider
    {
        private WebClient client = new WebClient();

        public LongmanProvider()
        {
            Title = "Longman Training Set Provider";
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
                    client.DownloadFile(audioURL, path);
                    String word = GetWord(html);
                    String phonetics = GetPhonetics(html).Trim();

                    if (phonetics != null)
                    {
                        TrainingEntry entry = null;

                        lock (this)
                        {
                            entry = ds.GetEntry(word);
                        }

                        entry.Word = word;
                        entry.Phonetics = phonetics;
                        entry.AddAudio(path, "Longman@" + url);

                        lock (this)
                        {
                            totalCount++;
                        }

                        Console.WriteLine(totalCount);
                    }

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
            if (!html.Contains("Play American pronunciation") && !html.Contains("data-src-mp3"))
            {
                return null;
            }

            int index = html.IndexOf("Play American pronunciation");
            String result = html.Substring(html.LastIndexOf("data-src-mp3", index));
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
            String start = "<span class=\"neutral span\">";
            String result = null;

            if (html.Contains(start))
            {
                result = html.Substring(html.IndexOf(start) + start.Length);
                if (result.Contains(start))
                {
                    result = result.Substring(0, result.IndexOf(start));
                    StringBuilder f = new StringBuilder();
                    bool isInTag = false;

                    for (int i = 0; i < result.Length; i++)
                    {
                        char c = result[i];

                        if (c == '<')
                        {
                            isInTag = true;
                        } else if (c == '>')
                        {
                            isInTag = false;
                        }
                        else if (!isInTag)
                        {
                            f.Append(c);
                        }
                    }

                    result = f.ToString().Trim();
                    if (!result.StartsWith("/"))
                    {
                        result = null;
                    }
                    else
                    {
                        result = result.Substring(1);
                    }
                }
                else
                {
                    result = null;
                }
            }

            if (result == "")
            {
                result = null;
            }

            return result;
        }

        private string GetWord(string html)
        {
            String title = html.Substring(html.IndexOf("<title"));
            title = title.Substring(title.IndexOf(">") + 1);
            title = title.Substring(0, title.IndexOf("|"));

            return title.Trim();
        }

        private List<String> LoadURLs()
        {
            List<String> result = new List<String>();

            for (int i = 1; i <= 3; i++)
            {
                String url = $"https://www.ldoceonline.com/sitemap/english/sitemap{i}.xml";
                result.AddRange(GetURLsFrom(url));
            }

//            result = FilterCount(result, 2000);

            return result;
        }
        
        private List<String> GetURLsFrom(String siteMapURL)
        {
            String xml = client.DownloadString(siteMapURL);
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