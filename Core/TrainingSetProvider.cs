using System;
using System.Threading;
using SpeechMorphingDataGatherer.Core.Database;

namespace SpeechMorphingDataGatherer.Core
{
    public abstract class TrainingSetProvider
    {
        public abstract event Action<double> OnLoadProgress;
        protected TrainingDataset ds = null;
        
        public String Title
        {
            get;
            set;
        }

        public bool Paused
        {
            get;
            set;
        }

        public void Start()
        {
            new Thread(new ThreadStart(Load)).Start();
        }

        protected abstract void Load();

        public void Pause()
        {
            Paused = true;
        }

        public void Resume()
        {
            Paused = false;
        }
        
        protected void WaitPaused()
        {
            while (Paused)
            {
                Thread.Sleep(1000);
            }
        }

    }
}