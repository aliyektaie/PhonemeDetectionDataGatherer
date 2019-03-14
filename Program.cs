using System;
using System.Windows.Forms;
using SpeechMorphingDataGatherer.Core.Database;
using SpeechMorphingDataGatherer.Core.Providers;
using SpeechMorphingDataGatherer.Views;

namespace SpeechMorphingDataGatherer
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            FrmDownloadTrainingData frm = new FrmDownloadTrainingData();

            frm.Provider = new MerriamWebsterProvider();
            frm.ShowDialog();
        }
    }
}