using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ClipboardKeeper
{
    internal sealed class HistoryStore
    {
        private const int MaxItems = 100;
        private const string AppFolderName = "JiraAceClipboardManager";
        private const string HistoryFileName = "history.xml";
        private const string ImagesFolderName = "images";

        private readonly ClipboardHistory history;
        private readonly string historyPath;

        private HistoryStore(string rootDirectory, ClipboardHistory history)
        {
            RootDirectory = rootDirectory;
            ImagesDirectory = Path.Combine(rootDirectory, ImagesFolderName);
            historyPath = Path.Combine(rootDirectory, HistoryFileName);
            this.history = history ?? new ClipboardHistory();
            Directory.CreateDirectory(RootDirectory);
            Directory.CreateDirectory(ImagesDirectory);
        }

        public string RootDirectory { get; private set; }

        public string ImagesDirectory { get; private set; }

        public IList<ClipboardRecord> Records
        {
            get { return history.Records; }
        }

        public int Count
        {
            get { return history.Records.Count; }
        }

        public static HistoryStore LoadDefault()
        {
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppFolderName);

            string historyPath = Path.Combine(root, HistoryFileName);
            ClipboardHistory loaded = null;

            if (File.Exists(historyPath))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(ClipboardHistory));
                    using (var stream = File.OpenRead(historyPath))
                    {
                        loaded = serializer.Deserialize(stream) as ClipboardHistory;
                    }
                }
                catch
                {
                    string brokenPath = historyPath + ".broken-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    try
                    {
                        File.Move(historyPath, brokenPath);
                    }
                    catch
                    {
                    }
                }
            }

            return new HistoryStore(root, loaded ?? new ClipboardHistory());
        }

        public void Upsert(ClipboardRecord record)
        {
            ClipboardRecord existing = Find(record.Id);
            if (existing != null)
            {
                record.Name = existing.Name;
                record.StarredAt = existing.StarredAt;
                history.Records.Remove(existing);
            }

            history.Records.Insert(0, record);
            Trim();
            Save();
        }

        public ClipboardRecord Find(string id)
        {
            return history.Records.FirstOrDefault(item => item.Id == id);
        }

        public void UpdateName(string id, string name)
        {
            ClipboardRecord record = Find(id);
            if (record == null)
            {
                return;
            }

            record.Name = name ?? string.Empty;
            Save();
        }

        public bool ToggleStar(string id)
        {
            ClipboardRecord record = Find(id);
            if (record == null)
            {
                return false;
            }

            if (record.IsStarred)
            {
                record.StarredAt = DateTime.MinValue;
                Save();
                return false;
            }

            record.StarredAt = DateTime.Now;
            Save();
            return true;
        }

        public void Delete(string id)
        {
            ClipboardRecord record = Find(id);
            if (record == null)
            {
                return;
            }

            history.Records.Remove(record);
            DeleteImageFile(record);
            Save();
        }

        public int DeleteMany(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                return 0;
            }

            var idSet = new HashSet<string>(ids.Where(id => !string.IsNullOrEmpty(id)));
            if (idSet.Count == 0)
            {
                return 0;
            }

            List<ClipboardRecord> recordsToDelete = history.Records
                .Where(record => idSet.Contains(record.Id))
                .ToList();

            foreach (ClipboardRecord record in recordsToDelete)
            {
                history.Records.Remove(record);
                DeleteImageFile(record);
            }

            if (recordsToDelete.Count > 0)
            {
                Save();
            }

            return recordsToDelete.Count;
        }

        public void Clear()
        {
            foreach (ClipboardRecord record in history.Records.ToList())
            {
                DeleteImageFile(record);
            }

            history.Records.Clear();
            Save();
        }

        public string GetImagePath(ClipboardRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.ImageFile))
            {
                return null;
            }

            return Path.Combine(ImagesDirectory, record.ImageFile);
        }

        private void Trim()
        {
            while (history.Records.Count > MaxItems)
            {
                ClipboardRecord last = history.Records[history.Records.Count - 1];
                history.Records.RemoveAt(history.Records.Count - 1);
                DeleteImageFile(last);
            }
        }

        private void Save()
        {
            Directory.CreateDirectory(RootDirectory);
            Directory.CreateDirectory(ImagesDirectory);

            string tempPath = historyPath + ".tmp";
            var serializer = new XmlSerializer(typeof(ClipboardHistory));
            using (var stream = File.Create(tempPath))
            {
                serializer.Serialize(stream, history);
            }

            if (File.Exists(historyPath))
            {
                File.Delete(historyPath);
            }

            File.Move(tempPath, historyPath);
        }

        private void DeleteImageFile(ClipboardRecord record)
        {
            if (record == null || !record.IsImage || string.IsNullOrEmpty(record.ImageFile))
            {
                return;
            }

            string path = GetImagePath(record);
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }
}
