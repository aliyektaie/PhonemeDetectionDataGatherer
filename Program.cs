using System;
using System.Windows.Forms;
using SpeechMorphingDataGatherer.Core.Database;
using SpeechMorphingDataGatherer.Core.Providers;
using SpeechMorphingDataGatherer.Views;
using System.Net;

namespace SpeechMorphingDataGatherer
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 40;

            Application.EnableVisualStyles();
            FrmDownloadTrainingData frm = new FrmDownloadTrainingData();

            frm.Provider = new HowToPronounceProvider();
            frm.ShowDialog();
        }
    }
}