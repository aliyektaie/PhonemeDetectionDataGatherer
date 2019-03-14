using SpeechMorphingDataGatherer.Core.Database;

namespace SpeechMorphingDataGatherer
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            TrainingDataset ds = new TrainingDataset(@"D:\My Projects\Speech Morphing Data Gatherer\Data\");
            TrainingEntry entry = ds.GetEntry("test");

            entry.Phonetics = "/test/";
            entry.AddAudio(@"d:\test0001.mp3", "Merriam-Webster");
            
            ds.Save();

        }
    }
}