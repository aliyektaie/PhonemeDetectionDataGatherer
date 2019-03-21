using System;
using System.Windows.Forms;
using SpeechMorphingDataGatherer.Core.Database;
using SpeechMorphingDataGatherer.Core.Providers;
using SpeechMorphingDataGatherer.Views;
using System.Net;
using SpeechMorphingDataGatherer.Core;

namespace SpeechMorphingDataGatherer
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
//            DownloadNewDataSet(new HowToPronounceProvider());

            TrainingSetMerger merger = new TrainingSetMerger();
            merger.TrainingSetFolders.Add(@"C:\Users\Yektaie\Desktop\Data (HowToPronounce.com)\");
            merger.TrainingSetFolders.Add(@"C:\Users\Yektaie\Desktop\Data (Merriam-Webster)\");
            merger.TrainingSetFolders.Add(@"C:\Users\Yektaie\Desktop\Data (Longman)\");
            merger.TrainingSetFolders.Add(@"C:\Users\Yektaie\Desktop\Data (Collins)\");

            merger.TargetFolder = @"D:\My Projects\Speech Morphing Data Gatherer\Data\";
            merger.SamplingRate = 16000;
            merger.Bitrate = 48;

            merger.Merge();
        }

        private static void DownloadNewDataSet(TrainingSetProvider provider)
        {
            ServicePointManager.DefaultConnectionLimit = 40;

            Application.EnableVisualStyles();
            FrmDownloadTrainingData frm = new FrmDownloadTrainingData();

            frm.Provider = provider;
            frm.ShowDialog();
        }
    }
}