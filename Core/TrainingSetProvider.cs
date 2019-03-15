using System;
using System.Threading;
using SpeechMorphingDataGatherer.Core.Database;
using System.Collections.Generic;

namespace SpeechMorphingDataGatherer.Core
{
    public abstract class TrainingSetProvider
    {
        public event Action<double> OnLoadProgress;
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

        protected void ExecuteSubTasks(List<ThreadStart> tasks, int threadCount)
        {
            List<List<ThreadStart>> splittedTasks = new List<List<ThreadStart>>();

            for (int i = 0; i < threadCount; i++)
            {
                splittedTasks.Add(new List<ThreadStart>());
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                splittedTasks[i % threadCount].Add(tasks[i]);
            }

            bool[] completions = new bool[threadCount];
            SubTaskParam[] parameters = new SubTaskParam[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                Thread t = new Thread(new ParameterizedThreadStart(SubTaskMain));
                parameters[i] = new SubTaskParam();
                parameters[i].index = i;
                parameters[i].tasks = splittedTasks[i];
                parameters[i].complete = completions;

                t.Start(parameters[i]);
            }

            while (!AllFinished(completions))
            {
                Thread.Sleep(1000);

                OnLoadProgress?.Invoke(GetProgress(parameters));
            }
        }

        protected void RaiseOnLoadProgress(double obj)
        {
            OnLoadProgress(obj);
        }

        private double GetProgress(SubTaskParam[] parameters)
        {
            double result = 0;

            for (int i = 0; i < parameters.Length; i++)
            {
                result += parameters[i].progress;
            }

            return result / parameters.Length;
        }

        private bool AllFinished(bool[] completions)
        {
            bool result = true;

            lock (SubTaskParam.LOCK)
            {
                for (int i = 0; i < completions.Length; i++)
                {
                    result = result && completions[i];
                }
            }

            return result;
        }

        private void SubTaskMain(object obj)
        {
            SubTaskParam param = obj as SubTaskParam;

            int i = 0;
            foreach (ThreadStart runnable in param.tasks)
            {
                runnable();
                i++;
                param.progress = i * 100.0 / param.tasks.Count;
            }

            lock (SubTaskParam.LOCK)
            {
                param.complete[param.index] = true;
            }
        }
    }

    class SubTaskParam
    {
        public static String LOCK = "kdnlcwnlnw";
        public int index = 0;
        public bool[] complete = null;
        public double progress = 0;

        public List<ThreadStart> tasks = null;
    }
}