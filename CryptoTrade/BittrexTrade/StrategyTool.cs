using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoTrade.Strategies;

namespace CryptoTrade
{
    public enum Candle_Interval
    {
        oneMin, fiveMin, thirtyMin, hour, day
    }

    public static class StrategyTool
    {
        private static Random rand = new Random();

        /// <summary>
        /// Количество секунд до первого запуска таймера Update
        /// </summary>
        /// <returns></returns>
        public static TimeSpan NextTimerTick(Candle_Interval Interval, InvokePrint Print)
        {
            int TimeShift = rand.Next(1, 10);
            DateTime NextTickTime = DateTime.Now; //ближайшее подходящее время для запуска
            TimeSpan resspan = new TimeSpan(); //осталось до NextTickTime
            switch (Interval)
            {
                case Candle_Interval.oneMin:
                    {
                        TimeSpan tspan = new TimeSpan(NextTickTime.Hour, NextTickTime.Minute, TimeShift);
                        tspan = tspan.Add(TimeSpan.FromMinutes(1));
                        NextTickTime = NextTickTime.Date + tspan;
                        resspan = NextTickTime - DateTime.Now;
                    }
                    break;
                case Candle_Interval.fiveMin:
                    {
                        TimeSpan tspan = new TimeSpan(NextTickTime.Hour,
                            5 * (int)Math.Truncate((double)NextTickTime.Minute / 5), TimeShift);
                        tspan = tspan.Add(TimeSpan.FromMinutes(5));
                        NextTickTime = NextTickTime.Date + tspan;
                        resspan = NextTickTime - DateTime.Now;
                    }
                    break;
                case Candle_Interval.thirtyMin:
                    {
                        TimeSpan tspan = new TimeSpan(NextTickTime.Hour,
                            30 * (int)Math.Truncate((double)NextTickTime.Minute / 30), TimeShift);
                        tspan = tspan.Add(TimeSpan.FromMinutes(30));
                        NextTickTime = NextTickTime.Date + tspan;
                        resspan = NextTickTime - DateTime.Now;
                    }
                    break;
                case Candle_Interval.hour:
                    {
                        TimeSpan tspan = new TimeSpan(NextTickTime.Hour, 0, TimeShift);
                        tspan = tspan.Add(TimeSpan.FromHours(1));
                        NextTickTime = NextTickTime.Date + tspan;
                        resspan = NextTickTime - DateTime.Now;
                    }
                    break;
                case Candle_Interval.day:
                    {
                        TimeSpan tspan = new TimeSpan(0, 0, TimeShift);
                        tspan = tspan.Add(TimeSpan.FromDays(1));
                        NextTickTime = NextTickTime.Date + tspan;
                        resspan = NextTickTime - DateTime.Now;
                    }
                    break;
                default: return TimeSpan.FromMinutes(1);
            }
            if (resspan < TimeSpan.FromSeconds(5))
                resspan.Add(TimerPeriod(Interval)); //уже здесь, вызов на следующий раз
            Print("Следущее обновление через: " + resspan.ToString(@"hh\:mm\:ss"));
            return resspan;
        }

        /// <summary>
        /// Период через которое вызывать таймер Update
        /// </summary>
        /// <returns></returns>
        public static TimeSpan TimerPeriod(Candle_Interval Interval)
        {
            TimeSpan timeSpan = TimeSpan.FromMinutes(1);
            switch (Interval)
            {
                case Candle_Interval.oneMin:
                    timeSpan = TimeSpan.FromMinutes(1);
                    break;
                case Candle_Interval.fiveMin:
                    timeSpan = TimeSpan.FromMinutes(5);
                    break;
                case Candle_Interval.thirtyMin:
                    timeSpan = TimeSpan.FromMinutes(30);
                    break;
                case Candle_Interval.hour:
                    timeSpan = TimeSpan.FromHours(1);
                    break;
                case Candle_Interval.day:
                    timeSpan = TimeSpan.FromDays(1);
                    break;
            }
            return timeSpan;
        }

        static public DateTime AddPeriod(DateTime Time, Candle_Interval Interval)
        {
            DateTime result = Time;
            switch (Interval)
            {
                case Candle_Interval.oneMin:
                    result = result.AddMinutes(1);
                    break;
                case Candle_Interval.fiveMin:
                    result = result.AddMinutes(5);
                    break;
                case Candle_Interval.thirtyMin:
                    result = result.AddMinutes(30);
                    break;
                case Candle_Interval.hour:
                    result = result.AddHours(1);
                    break;
                case Candle_Interval.day:
                    result = result.AddDays(1);
                    break;
            }
            return result;
        }

        public static Strategy GetStrategyByName(string SName, string uniqueID)
        {
            Strategy nStrategy = null;
            switch (SName)
            {
                case "CrossMA": nStrategy = new StrategyCrossMA(uniqueID);
                    break;
                case "Grid": nStrategy = new StrategyGrid(uniqueID);
                    break;
                case "Limit-Limit":
                    nStrategy = new StrategyLimit_Limit(uniqueID);
                    break;
            }
            
            return nStrategy;
        }

        //Замена только параметров, которые меняются с формы
        public static void ChangeBaseParam(ref Strategy pStrategy, StrategyParam ReadParam)
        {
            //StrategyType, FreqUpdate
            pStrategy.Param.StrategyName = ReadParam.StrategyName;
            pStrategy.Param.Market = ReadParam.Market;
            pStrategy.Param.WriteToFile = ReadParam.WriteToFile;

            pStrategy.Param.BalanceRestBuy.IsPercentSize = ReadParam.BalanceRestBuy.IsPercentSize;
            pStrategy.Param.BalanceRestBuy.Values[0] = ReadParam.BalanceRestBuy.Values[0];

            pStrategy.Param.BalanceRestSell.IsPercentSize = ReadParam.BalanceRestSell.IsPercentSize;
            pStrategy.Param.BalanceRestSell.Values[0] = ReadParam.BalanceRestSell.Values[0];

            pStrategy.Param.Interval = ReadParam.Interval;
            pStrategy.Param.SellOnlyBought = ReadParam.SellOnlyBought;
            pStrategy.Param.Stock = ReadParam.Stock;
        }
    }
}
