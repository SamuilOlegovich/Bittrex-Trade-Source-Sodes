using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;

namespace CryptoTrade
{
    internal class StrategyManager
    {
        public Dictionary<string, Strategy> StrategiesList = new Dictionary<string, Strategy>(); //все стратегии
        public Strategy ActiveStrategy { get; private set; }

        private ActiveOrdersTableGrid AOrdersTable;
        private readonly Action<bool, bool> AStrategyChangeState;
        private readonly InvokePrint Print;

        public StrategyManager(InvokePrint print, Action<bool, bool> aStrategyChangeState)
        {
            Print = print;
            AStrategyChangeState = aStrategyChangeState;
        }

        public void SetStrategyActive(string StrategyName)
        {
            if (StrategiesList.ContainsKey(StrategyName))
            {
                ActiveStrategy = StrategiesList[StrategyName];
            }
            else
            {
                ActiveStrategy = null;
            }
        }

        public void AddNewStrategy(StrategyParam SParam)
        {
            if (SParam == null)
            {
                return;
            }
            Strategy nStrategy = StrategyTool.GetStrategyByName(SParam.StrategyType, "");
            if (nStrategy == null)
            {
                System.Media.SystemSounds.Beep.Play();
                Print("Ошибка: неправильный тип стратегии - " + SParam.StrategyType);
                return;
            }
            StrategyTool.ChangeBaseParam(ref nStrategy, SParam);
            StrategiesList.Add(nStrategy.Param.StrategyName, nStrategy);
            nStrategy.SaveData();
        }

        public void RemoveStrategy(string StrategyName)
        {
            if (!StrategiesList.ContainsKey(StrategyName))
            {
                return;
            }
            if (AOrdersTable != null && StrategiesList[StrategyName].State.IsStartegyRun)
            {
                UnsubscribeAOrdersTable(StrategiesList[StrategyName]);
            }
            if (ActiveStrategy.Param.StrategyName == StrategyName)
            {
                ActiveStrategy = null;
            }
            StrategiesList.Remove(StrategyName);
        }

        private void OnStartegyChangeState(bool FromRun, bool Result, string StrategyName)
        {
            if (ActiveStrategy != null && ActiveStrategy.Param.StrategyName == StrategyName)
            {
                AStrategyChangeState(FromRun, Result);
            }

            if (Result)
            {
                if (FromRun)
                {
                    if (AOrdersTable != null && StrategiesList.ContainsKey(StrategyName))
                    {
                        SubscribeAOrdersTable(StrategiesList[StrategyName]);
                    }
                }
                else
                {
                    if (AOrdersTable != null && StrategiesList.ContainsKey(StrategyName))
                    {
                        UnsubscribeAOrdersTable(StrategiesList[StrategyName]);
                    }
                }
            }
        }

        /// <summary>
        /// Проверить будет ли допустимым новое имя стратегии
        /// </summary>
        /// <param name="StrategyName">Новое имя</param>
        /// <param name="ForSave">True - изменение ActiveStrategy, False - добавление новой</param>
        /// <returns></returns>
        public bool CheckNameValid(string StrategyName, bool ForSave)
        {
            if (ForSave)
            {
                if (ActiveStrategy.Param.StrategyName == StrategyName)
                {
                    return true;
                }
                else
                {
                    return !StrategiesList.ContainsKey(StrategyName);
                }
            }
            else
            {
                return !StrategiesList.ContainsKey(StrategyName);
            }
        }

        public void RenameStrategy(string Oldname, string NewName)
        {
            if (!StrategiesList.ContainsKey(Oldname))
            {
                return;
            }
            Strategy tmpStrategy = StrategiesList[Oldname];
            StrategiesList.Remove(Oldname);
            StrategiesList.Add(NewName, tmpStrategy);

            if(AOrdersTable != null)
            {
                AOrdersTable.RenameStrategy(Oldname, NewName);
            }
        }

        /// <summary>
        /// Прочитать стратегии из ini файла
        /// </summary>
        public void ReadStrategies()
        {
            if (!File.Exists(IniTool.IniFnameStates))
            {
                FileIniDataParser tfileIniData = new FileIniDataParser();
                IniData newParsedData = new IniData();
                tfileIniData.WriteFile(IniTool.IniFnameStates, newParsedData);
            }

            FileIniDataParser fileIniData = new FileIniDataParser();
            IniData parsedDataParams = fileIniData.ReadFile(IniTool.IniFnameParams);
            IniData parsedDataStates = fileIniData.ReadFile(IniTool.IniFnameStates);
            IEnumerable<SectionData> StratSectParams = parsedDataParams.Sections.Where((x) => { return x.SectionName.Contains("Strategy_"); });
            foreach (SectionData sdata in StratSectParams)
            {
                try
                {
                    Dictionary<string, string> ResDictParam = new Dictionary<string, string>();
                    for (int i = 0; i < sdata.Keys.Count; i++)
                    {
                        ResDictParam.Add(sdata.Keys.ElementAt(i).KeyName, sdata.Keys.ElementAt(i).Value);
                    }
                    Strategy nStrategy = StrategyTool.GetStrategyByName(ResDictParam["StrategyType"], sdata.SectionName);
                    nStrategy.ChangeState += OnStartegyChangeState;
                    nStrategy.Param.LoadData(ResDictParam);

                    if (parsedDataStates.Sections.ContainsSection(sdata.SectionName))
                    {
                        SectionData SDataState = parsedDataStates.Sections.GetSectionData(sdata.SectionName);
                        Dictionary<string, string> ResDictState = new Dictionary<string, string>();
                        for (int i = 0; i < SDataState.Keys.Count; i++)
                        {
                            ResDictState.Add(SDataState.Keys.ElementAt(i).KeyName, SDataState.Keys.ElementAt(i).Value);
                        }
                        nStrategy.State.LoadData(ResDictState);
                    }
                    else
                    {
                        nStrategy.SaveData(false);
                    }

                    StrategiesList.Add(nStrategy.Param.StrategyName, nStrategy);

                    if (nStrategy.State.IsStartegyRun)
                    {
                        nStrategy.State.IsStartegyRun = false;
                        try
                        {
                            ThreadPool.QueueUserWorkItem((x) => nStrategy.Start(false));
                            Print(string.Format("Запущено стратегию: [{0}]", nStrategy.Param.StrategyName));
                        }
                        catch (Exception ex)
                        {
                            System.Media.SystemSounds.Beep.Play();
                            Print(string.Format("Ошибка при запуске стратегии [{0}]: {1}", nStrategy.Param.StrategyName, ex.Message));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Media.SystemSounds.Beep.Play();
                    Print("Ошибка при возобновлении стратегии из ini файла: " + ex.Message);
                }
            }
        }

        public void ShowOrdersTable(System.Windows.Forms.Form WinForm)
        {
            if (AOrdersTable != null)
            {
                return;
            }
            AOrdersTable = new ActiveOrdersTableGrid(GetPositionStates, ForcefullyStopStrategy, UnsubscribeAllAOrdersTable);
            foreach (Strategy CurStrategy in StrategiesList.Values)
            {
                if (CurStrategy.State.IsStartegyRun)
                {
                    SubscribeAOrdersTable(CurStrategy);
                }
            }
            AOrdersTable.Show(WinForm);
        }

        //For ActiveOrdersTable

        private void SubscribeAOrdersTable(Strategy CurStrategy)
        {
            CurStrategy.ChangeActiveOrders += AOrdersTable.OnUpdateActiveOrders;
            CurStrategy.ActiveOrdersInfo();
            AOrdersTable.AddStrategy(CurStrategy.Param.StrategyName);
        }

        private void UnsubscribeAOrdersTable(Strategy CurStrategy)
        {
            CurStrategy.ChangeActiveOrders -= AOrdersTable.OnUpdateActiveOrders;
            AOrdersTable.DeleteStrategy(CurStrategy.Param.StrategyName);
        }

        private void UnsubscribeAllAOrdersTable(List<string> StrategyNames)
        {
            foreach (Strategy CurStrategy in StrategiesList.Values)
            {
                if (StrategyNames.Contains(CurStrategy.Param.StrategyName))
                {
                    CurStrategy.ChangeActiveOrders -= AOrdersTable.OnUpdateActiveOrders;
                }
            }
            AOrdersTable = null;
        }

        public void ForcefullyStopStrategy(string StrategyName)
        {
            Strategy CurStrategy = StrategiesList.Values.First(x => x.Param.StrategyName == StrategyName);
            ThreadPool.QueueUserWorkItem((x) => CurStrategy.ForceStop());
        }

        public Dictionary<string, double> GetPositionStates(List<string> StrategyNames)
        {
            Dictionary<string, double> resultDict = new Dictionary<string, double>(StrategyNames.Count);
            foreach (Strategy CurStrategy in StrategiesList.Values)
            {
                if (StrategyNames.Contains(CurStrategy.Param.StrategyName))
                {
                    resultDict.Add(CurStrategy.Param.StrategyName, CurStrategy.GetPositionState());
                }
            }
            return resultDict;
        }
    }
}
