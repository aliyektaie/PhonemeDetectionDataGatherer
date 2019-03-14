using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SpeechMorphingDataGatherer.Core;
using SpeechMorphingDataGatherer.Core.Database;
using SpeechMorphingDataGatherer.Core.Providers;

namespace SpeechMorphingDataGatherer.Views
{
    public partial class FrmDownloadTrainingData : Form
    {
        public FrmDownloadTrainingData()
        {
            InitializeComponent();
            Load += OnLoaded;

            progressBar.Maximum = 10000;
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            lblTitle.Text = Provider.Title;
            Provider.OnLoadProgress += OnProviderProgress;
            
            Provider.Start();
        }

        private void OnProviderProgress(double progress)
        {
            Invoke(new Action(() =>
            {
                lblProgress.Text = "%" + Math.Round(progress, 2);
                progressBar.Value = (int) (progress * 100);
            }));
        }

        public TrainingSetProvider Provider { get; set; }

        private void cmdPause_Click(object sender, EventArgs e)
        {
            if (Provider.Paused)
            {
                Provider.Resume();
                cmdPause.Text = "Pause";
            } else
            {
                Provider.Pause();
                cmdPause.Text = "Resume";
            }
        }
    }
}
