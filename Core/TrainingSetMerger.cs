using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects.DataClasses;
using System.IO;
using System.Text;
using System.Threading;
using SpeechMorphingDataGatherer.Core.Database;

namespace SpeechMorphingDataGatherer.Core
{
    public class TrainingSetMerger
    {
        public TrainingSetMerger()
        {
            TrainingSetFolders = new List<string>();
        }
        
        public List<String> TrainingSetFolders
        {
            get;
            set;
        }

        public String TargetFolder
        {
            get;
            set;
        }

        public int SamplingRate
        {
            get;
            set;
        }

        public int Bitrate { get; set; }

        public void Merge()
        {
            Dictionary<String, TrainingEntry> entries = new Dictionary<string, TrainingEntry>();

            foreach (string folder in TrainingSetFolders)
            {
                Console.WriteLine("Merging Dataset: " + folder);
                TrainingDataset ds = new TrainingDataset(folder);
                MergeDataset(ds, entries);
            }
        }

        private void MergeDataset(TrainingDataset ds, Dictionary<string, TrainingEntry> entries)
        {
            var dsEntries = ds.GetEntries();
            double lastProgress = -1;
            
            for (int i = 0; i < dsEntries.Count; i++)
            {
                dsEntries[i].Word = dsEntries[i].Word.ToLower().Trim(); 
                double progress = Math.Round(i * 100.0 / dsEntries.Count, 2);
                if (progress != lastProgress)
                {
                    lastProgress = progress;
                    Console.Write($"\r   -> %{progress}");
                }
                
                if (IsValidEntry(dsEntries[i]))
                {
                    TrainingEntry entry = GetEntry(dsEntries[i], entries);
                    entry.Phonetics = PreparePhonetics(dsEntries[i].Phonetics);

                    ConvertAudios(dsEntries[i], entry);
                }
            }

            SaveTrainingIndex(entries);
            Console.WriteLine($"\r   -> Done!      ");
        }

        private void SaveTrainingIndex(Dictionary<string,TrainingEntry> entries)
        {
            StringBuilder result = new StringBuilder();

            foreach (KeyValuePair<string,TrainingEntry> pair in entries)
            {
                result.Append(pair.Value.ToString()).Append("\r\n");
            }
            
            File.WriteAllText(TargetFolder + "index.txt", result.ToString());
        }

        private void ConvertAudios(TrainingEntry dsEntry, TrainingEntry entry1)
        {
            String targetFolder = TargetFolder + "Audio Files\\" +
                                  dsEntry.Word.Substring(0, Math.Min(2, dsEntry.Word.Length)) + "\\";
            if (entry1.AudioFileCount == 0)
            {
                Directory.CreateDirectory(targetFolder);
            }
            
            foreach (TrainingAudioFile audio in dsEntry.AudioFiles)
            {
                String input = audio.FilePath;
                String output = targetFolder + entry1.Word + "_" + entry1.AudioFileCount + ".wav";
                FFMPEG.ConvertAudioToWave(input, output, SamplingRate);

                entry1.AudioFileCount++;
            }
        }

        private string PreparePhonetics(string phonetics)
        {
            StringBuilder result = new StringBuilder();
            bool isInTag = false;

            for (int i = 0; i < phonetics.Length; i++)
            {
                char c = phonetics[i];

                if (c == '(')
                {
                    isInTag = true;
                }
                else if (c == ')')
                {
                    isInTag = false;
                }
                else if (!isInTag)
                {
                    result.Append(c);
                }
            }

            phonetics = result.ToString();

            phonetics = phonetics.Replace("-", "");
//            phonetics = phonetics.Replace("ˈ", "");
            return phonetics;
        }

        private TrainingEntry GetEntry(TrainingEntry entry, Dictionary<string,TrainingEntry> entries)
        {
            if (entries.ContainsKey(entry.Word))
            {
                return entries[entry.Word];
            }
            
            TrainingEntry result = new TrainingEntry();
            result.Word = entry.Word;

            entries[entry.Word] = result;

            return result;
        }

        private bool IsValidEntry(TrainingEntry entry)
        {
            bool result = true;

            result = entry.Word.Length > 0;
            result = result && !entry.Word.Contains(" ");
            result = result && !entry.Word.Contains("\r");
            result = result && !entry.Word.Contains("\n");
            result = result && !entry.Word.Contains("\t");
            result = result && !entry.Word.Contains("(");
            result = result && !entry.Word.Contains(")");
            result = result && !entry.Word.Contains("/");
            result = result && !entry.Word.StartsWith("-");
            result = result && !entry.Word.EndsWith("-");
            result = result && !entry.Word.EndsWith("_");
            
            result = result && !entry.Phonetics.Contains("\r");
            result = result && !entry.Phonetics.Contains("\n");
            result = result && !entry.Phonetics.Contains("\t");

            
            return result;
        }
    }
}