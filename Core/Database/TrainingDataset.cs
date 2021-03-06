using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Net;

namespace SpeechMorphingDataGatherer.Core.Database
{
    public class TrainingDataset : IDisposable
    {
        public Action<double> OnSaveProgress = null;

        private const String INDEX_FILE_NAME = "index.sqlite";
        private const String AUDIO_FILE_FOLDER_NAME = "Audio Files";
        private List<TrainingEntry> _content = new List<TrainingEntry>();
        private SQLiteConnection _connection = null;

        private Dictionary<int, TrainingEntry> _entriesIDIndex = new Dictionary<int, TrainingEntry>();
        private Dictionary<String, TrainingEntry> _entriesWordIndex = new Dictionary<String, TrainingEntry>();

        public TrainingDataset(String path)
        {
            Path = path;
            LoadTrainingSet();
        }

        public String Path { get; set; }

        private void LoadTrainingSet()
        {
            if (File.Exists(Path + INDEX_FILE_NAME))
            {
                OpenConnection();

                LoadTrainingEntries();
                LoadAudioFileInfo();
            }
            else
            {
                InitializeTrainingSet();
            }
        }

        private void LoadAudioFileInfo()
        {
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM audio_files", _connection);
            SQLiteDataReader reader = command.ExecuteReader();
            String baseAudioDirectory = GetAudioDirectory();

            while (reader.Read())
            {
                TrainingAudioFile entry = new TrainingAudioFile();

                entry.ID = Convert.ToInt32(reader["id"]);
                entry.FilePath = baseAudioDirectory + Convert.ToString(reader["file_name"]);
                entry.AudioFormat = (AudioFormat) Convert.ToInt32(reader["format"]);
                entry.AudioBitrateInKB = Convert.ToInt32(reader["bit_rate"]);
                entry.AudioSamplingRate = Convert.ToInt32(reader["sampling_rate"]);

                int entryID = Convert.ToInt32(reader["entry_id"]);

                _entriesIDIndex[entryID].AudioFiles.Add(entry);
            }

            reader.Close();
        }

        private string GetAudioDirectory()
        {
            return Path + AUDIO_FILE_FOLDER_NAME + @"\";
        }

        private void LoadTrainingEntries()
        {
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM training_entries", _connection);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                TrainingEntry entry = new TrainingEntry();

                entry.ID = Convert.ToInt32(reader["id"]);
                entry.Word = Convert.ToString(reader["word"]);
                entry.Phonetics = Convert.ToString(reader["phonetics"]);

                _content.Add(entry);
                _entriesWordIndex.Add(entry.Word, entry);
                _entriesIDIndex.Add(entry.ID, entry);
            }

            reader.Close();
        }

        private void OpenConnection()
        {
            _connection = new SQLiteConnection($"Data Source=\"{Path + INDEX_FILE_NAME}\";Version=3;");
            _connection.Open();
        }

        private void InitializeTrainingSet()
        {
            Directory.CreateDirectory(Path);
            Directory.CreateDirectory(Path + AUDIO_FILE_FOLDER_NAME);
            
            File.WriteAllBytes(Path + INDEX_FILE_NAME, File.ReadAllBytes("Resources\\index.sqlite"));

            OpenConnection();

            ExecuteCommand(
                "CREATE TABLE training_entries(id INTEGER PRIMARY KEY, word nlp NOT NULL, phonetics nlp NOT NULL)");
            ExecuteCommand(
                "CREATE TABLE audio_files(id INTEGER PRIMARY KEY, file_name nlp NOT NULL, format INTEGER, bit_rate INTEGER, sampling_rate INTEGER, entry_id INTEGER NOT NULL, provider nlp NOT NULL)");
        }

        private void ExecuteCommand(String sql)
        {
            SQLiteCommand command = new SQLiteCommand(sql, _connection);
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _connection.Close();
            }
        }

        public void Save()
        {
            int i = 0;
            foreach (TrainingEntry entry in _content)
            {
                if (entry.ID == 0)
                {
                    SaveNewTrainingEntry(entry);
                }
                else
                {
                    // Nothing to update, this should not happen.
                }

                int audioIndex = 0;
                foreach (TrainingAudioFile file in entry.AudioFiles)
                {
                    if (file.ID == 0)
                    {
                        SaveNewAudioFile(file, entry, audioIndex);
                    }

                    audioIndex++;
                }

                i++;
                if (OnSaveProgress != null)
                {
                    double p = i * 100.0 / _content.Count;
                    OnSaveProgress(p);
                }
            }

            Console.WriteLine("Finished");
        }

        private void SaveNewAudioFile(TrainingAudioFile file, TrainingEntry entry, int index)
        {
            SQLiteCommand command = new SQLiteCommand();
            command.CommandText = $"INSERT INTO audio_files(file_name, format, sampling_rate, bit_rate, entry_id, provider) VALUES(@file_name, {(int)file.AudioFormat}, {file.AudioSamplingRate}, {file.AudioBitrateInKB}, {entry.ID}, @provider)";
            command.Connection = _connection;

            String fileName = $"{entry.ID}_{index}.{GetExtension(file.AudioFormat)}";
            command.Parameters.AddWithValue("@file_name", fileName);
            command.Parameters.AddWithValue("@provider", file.Provider);

            command.ExecuteNonQuery();
            
            File.WriteAllBytes(GetAudioDirectory() + fileName, file.RawContent);

            file.ID = -1;
        }

        private String GetExtension(AudioFormat format)
        {
            switch (format)
            {
                case AudioFormat.MP3:
                    return "mp3";
                case AudioFormat.Wave:
                    return "wav";
                case AudioFormat.Ogg:
                    return "ogg";
            }
            
            throw new NotImplementedException();
        }

        private void SaveNewTrainingEntry(TrainingEntry entry)
        {
            SQLiteCommand command = new SQLiteCommand();
            command.CommandText = "INSERT INTO training_entries(word, phonetics) VALUES(@word, @phonetics)";
            command.Connection = _connection;

            command.Parameters.AddWithValue("@word", entry.Word);
            command.Parameters.AddWithValue("@phonetics", entry.Phonetics);

            command.ExecuteNonQuery();

            entry.ID = GetMaxID("training_entries");
        }

        private int GetMaxID(string table)
        {
            String sql = $"SELECT MAX(id) FROM {table}";
            SQLiteCommand command = new SQLiteCommand(sql, _connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public TrainingEntry GetEntry(String word)
        {
            if (_entriesWordIndex.ContainsKey(word))
            {
                return _entriesWordIndex[word];
            }
            
            TrainingEntry entry = new TrainingEntry();

            entry.Word = word;
            _entriesWordIndex.Add(word, entry);
            _content.Add(entry);

            return entry;
        }

        public List<TrainingEntry> GetEntries()
        {
            return _content;
        }
    }

    public class TrainingEntry
    {
        public TrainingEntry()
        {
            AudioFiles = new List<TrainingAudioFile>();
        }
        
        public int ID { get; set; }

        public String Word { get; set; }
        
        public int AudioFileCount { get; set; }

        public String Phonetics { get; set; }

        public List<TrainingAudioFile> AudioFiles { get; set; }

        public void AddAudio(String path, String provider)
        {
            TrainingAudioFile file = new TrainingAudioFile();

            file.RawContent = File.ReadAllBytes(path);
            file.Provider = provider;

            FFMPEGFileInfo info = FFMPEG.GetFileInfo(path);
            file.AudioFormat = info.Format;
            file.AudioSamplingRate = info.SamplingRate;
            file.AudioBitrateInKB = (int) info.BitRate;

            AudioFiles.Add(file);
        }

        public override string ToString()
        {
            return $"{Word}\t{Phonetics}\t{AudioFileCount}";
        }
    }

    public class TrainingAudioFile
    {
        private byte[] _content = null;

        public int ID { get; set; }

        public String FilePath { get; set; }

        public byte[] RawContent
        {
            get
            {
                if (_content == null)
                {
                    _content = File.ReadAllBytes(FilePath);
                }

                return _content;
            }
            set { _content = value; }
        }

        public AudioFormat AudioFormat { get; set; }

        public int AudioBitrateInKB { get; set; }

        public int AudioSamplingRate { get; set; }

        public String Provider { get; set; }
    }

    public enum AudioFormat : int
    {
        MP3 = 1,
        Wave = 2,
        Ogg = 3,
    }

    public enum Language : int
    {
        English = 1,
        Farsi = 2,
    }
}