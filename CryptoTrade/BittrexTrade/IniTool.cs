using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using IniParser;
using IniParser.Model;

namespace CryptoTrade
{
    internal static class IniTool
    {
        private static readonly InvokePrint Print = Form1.Print;
        private static readonly object LockWriteParam = new object();
        public static string IniFnameParams = "Settings.ini";

        private static readonly object LockWriteState = new object();
        private static ConcurrentDictionary<string, Dictionary<string, string>> StatesData =
            new ConcurrentDictionary<string, Dictionary<string, string>>();
        private static Timer timerStateUpdate;
        private static bool StateHaveData = false;
        public static string IniFnameStates = "States.ini";

        private static string GetMd5Hash(MD5 md5Hash, string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        public static string ConvertName(string Name)
        {
            string SectName = "";
            using (var md5Hash = MD5.Create())
            {
                SectName = "Strategy_" + GetMd5Hash(md5Hash, Name);
            }
            return SectName;
        }

        /// <summary>
        /// Изменение переданных параметров в файле Settings.ini
        /// </summary>
        /// <param name="data"></param>
        public static void ChangeValueParam(string SectionName, Dictionary<string, string> data)
        {
            lock (LockWriteParam)
            {
                var fileIniData = new FileIniDataParser();
                var parsedData = fileIniData.ReadFile(IniFnameParams);
                ChangeSectionData(ref parsedData, SectionName, data);
                fileIniData.WriteFile(IniFnameParams, parsedData);
            }
        }

        public static void ChangeValueState(string Section, Dictionary<string, string> data)
        {
            if (timerStateUpdate == null)
            {
                timerStateUpdate = new Timer(TStateUpdate);
                timerStateUpdate.Change(1000, 2000);
            }
            StatesData.AddOrUpdate(Section, data, (x, y) => data);
            StateHaveData = true;
        }

        private static void TStateUpdate(object state)
        {
            if (!StateHaveData)
            {
                return;
            }

            lock (LockWriteState)
            {
                var fileIniData = new FileIniDataParser();
                var parsedData = fileIniData.ReadFile(IniFnameStates);
                lock (StatesData)
                {
                    foreach (var SData in StatesData)
                    {
                        ChangeSectionData(ref parsedData, SData.Key, SData.Value);
                    }
                    StatesData.Clear();
                    StateHaveData = false;
                }
                fileIniData.WriteFile(IniFnameStates, parsedData);
            }
        }

        private static void ChangeSectionData(ref IniData parsedData, string SectionName, Dictionary<string, string> data)
        {
            if (!parsedData.Sections.ContainsSection(SectionName))
            {
                parsedData.Sections.AddSection(SectionName);
            }
            var section = parsedData.Sections.GetSectionData(SectionName);

            foreach (var t in data)
            {
                try
                {
                    if (section.Keys.ContainsKey(t.Key))
                    {
                        section.Keys.GetKeyData(t.Key).Value = t.Value;
                    }
                    else
                    {
                        section.Keys.AddKey(t.Key, t.Value);
                    }
                }
                catch (Exception ex)
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print(String.Format("Ошибка изменения ключа {0} в секции {1}: {2}", t.Key, section, ex.Message), true);
                }
            }
        }

        /// <summary>
        /// Создает новый файл с переданной информацией
        /// </summary>
        /// <param name="IniFname"></param>
        /// <param name="Section"></param>
        /// <param name="data"></param>
        //static public void CreateIniFile(string IniFname, string Section, Dictionary<string, string> data)
        //{
        //    FileIniDataParser fileIniData = new FileIniDataParser();
        //    IniData newParsedData = new IniData();
        //    newParsedData.Sections.AddSection(Section);
        //    foreach (var t in data)
        //    {
        //        newParsedData.Sections.GetSectionData(Section).Keys.AddKey(t.Key, t.Value);
        //    }
        //    fileIniData.WriteFile(IniFname, newParsedData);
        //}

        /// <summary>
        /// Считывает настройки с ini файла по указанной Section
        /// </summary>
        public static Dictionary<string, string> ReadSectionParams(string Section)
        {
            IniData parsedData = null;
            var fileIniData = new FileIniDataParser();
            lock (LockWriteParam)
            {
                parsedData = fileIniData.ReadFile(IniFnameParams);
            }

            return DataToDictionary(parsedData, Section);
        }

        public static Dictionary<string, string> ReadSectionStates(string Section)
        {
            IniData parsedData = null;
            var fileIniData = new FileIniDataParser();
            lock (LockWriteState)
            {
                parsedData = fileIniData.ReadFile(IniFnameStates);
            }

            return DataToDictionary(parsedData, Section);
        }

        private static Dictionary<string, string> DataToDictionary(IniData parsedData, string Section)
        {
            if (!parsedData.Sections.ContainsSection(Section))
            {
                return null;
            }
            var sdata = parsedData.Sections.GetSectionData(Section);
            var ResDict = new Dictionary<string, string>();
            for (int i = 0; i < sdata.Keys.Count; i++)
            {
                ResDict.Add(sdata.Keys.ElementAt(i).KeyName, sdata.Keys.ElementAt(i).Value);
            }
            return ResDict;
        }

        public static void RemoveFromParams(string SectionName)
        {
            var fileIniData = new FileIniDataParser();
            lock (LockWriteParam)
            {
                var parsedData = fileIniData.ReadFile(IniFnameParams);
                if (parsedData.Sections.ContainsSection(SectionName))
                {
                    parsedData.Sections.RemoveSection(SectionName);
                    fileIniData.WriteFile(IniFnameParams, parsedData);
                }
            }
        }

        public static void RemoveFromStates(string SectionName)
        {
            lock (StatesData)
            {
                var tmp = new Dictionary<string, string>();
                if (StatesData.ContainsKey(SectionName))
                {
                    StatesData.TryRemove(SectionName, out tmp);
                }
            }

            var fileIniData = new FileIniDataParser();
            lock (LockWriteState)
            {
                var parsedData = fileIniData.ReadFile(IniFnameStates);
                if (parsedData.Sections.ContainsSection(SectionName))
                {
                    parsedData.Sections.RemoveSection(SectionName);
                    fileIniData.WriteFile(IniFnameStates, parsedData);
                }
            }
        }
    }
}
