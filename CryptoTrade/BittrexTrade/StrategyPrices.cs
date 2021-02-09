using CryptoTrade.BaseTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoTrade
{
    public class StrategyPrices
    {
        private Strategy MainStrategy;
        private IStrategyParam Param => MainStrategy.Param;
        private IStock Stock => MainStrategy.Param.Stock;
        private readonly InvokePrint Print;

        public List<Candle> Candles { get; private set; } = new List<Candle>();
        public DateTime LastCandleTime { get; private set; }
        public Action<List<Candle>> OnUpdateCandles;

        private Timer timerCandlesUpdate; //обновление свечей
        private Timer timerLastCUpdate; //обновление Close последней свечи
        private bool LockCandlesUpdate; //блок на обновление свечей и проверки последней цены

        private Timer timerTickerUpdate; //получение последней цены
        private bool LockTickerUpdate;
        public Action<Ticker> OnUpdateTicker;
        public bool WebSocketUpdateTicker = true;
        private DateTime LastWebSocketUpdate = DateTime.MinValue;
        private Timer timerCheckWebSocket;

        private readonly int MaxContractErrors = 5;
        private int CurrentContractErrors = 0;

        public StrategyPrices(Strategy strategy, InvokePrint Print)
        {
            MainStrategy = strategy;
            this.Print = Print;
            LastCandleTime = DateTime.MinValue;
        }

        public async Task<Ticker> GetMarketPrice()
        {
            return await Stock.GetMarketPrice(Param.Market, Print);
        }

        public bool StartUpdateCandles()
        {
            try
            {
                TCandlesUpdate(null);
                if (timerCandlesUpdate != null)
                {
                    timerCandlesUpdate.Dispose();
                }
                timerCandlesUpdate = new Timer(TCandlesUpdate);
                timerCandlesUpdate.Change(StrategyTool.NextTimerTick(Param.Interval, Print), StrategyTool.TimerPeriod(Param.Interval));

                if (timerLastCUpdate != null)
                {
                    timerLastCUpdate.Dispose();
                }
                timerLastCUpdate = new Timer(TLastCUpdate);
                timerLastCUpdate.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(Param.FreqUpdate));
                return true;
            }
            catch (Exception ex)
            {
                Print(string.Format("Ошибка StartUpdates: {0}", ex.Message), true);
                return false;
            }
        }

        public bool StartUpdateTicker()
        {
            try
            {
                if (WebSocketUpdateTicker)
                {
                    Stock.ListenPrice(Param.Market, HWebSocket, Print);
                    LastWebSocketUpdate = DateTime.Now;
                    if (timerCheckWebSocket != null)
                    {
                        timerCheckWebSocket.Dispose();
                    }
                    timerCheckWebSocket = new Timer(TCheckWebSocket);
                    timerCheckWebSocket.Change(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
                    return true;
                }
                else
                {
                    TTickerUpdate(null);
                    if (timerTickerUpdate != null)
                    {
                        timerTickerUpdate.Dispose();
                    }
                    timerTickerUpdate = new Timer(TTickerUpdate);
                    timerTickerUpdate.Change(1000, Param.FreqUpdate);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                Print(string.Format("Ошибка StartUpdateTicker: {0}", ex.Message), true);
                return false;
            }
        }

        private void HWebSocket(Ticker ticker)
        {
            if (LockTickerUpdate)
            {
                return;
            }

            LockTickerUpdate = true;
            LastWebSocketUpdate = DateTime.Now;

            OnUpdateTicker(ticker);
            LockTickerUpdate = false;
        }

        private void TCheckWebSocket(object state)
        {
            if(DateTime.Now - LastWebSocketUpdate > TimeSpan.FromHours(1))
            {
                LastWebSocketUpdate = DateTime.Now;
                System.Media.SystemSounds.Beep.Play();
                Print(String.Format("На паре {0} нету обновлений цены 1 час!", Param.Market.MarketName));
                Stock.RestartWebSocket();
            }
        }

        public void StopUpdates()
        {
            if (timerCandlesUpdate != null)
            {
                timerCandlesUpdate.Dispose();
            }
            if (timerLastCUpdate != null)
            {
                timerLastCUpdate.Dispose();
            }
            if (timerTickerUpdate != null)
            {
                timerTickerUpdate.Dispose();
            }

            if (timerCheckWebSocket != null)
            {
                timerCheckWebSocket.Dispose();
            }
            if (WebSocketUpdateTicker)
            {
                Stock.CloseListenPrice(Param.Market, HWebSocket, Print);
            }
        }

        private void TTickerUpdate(object state)
        {
            if (LockTickerUpdate)
            {
                return;
            }
            LockTickerUpdate = true;

            Ticker ticker = Stock.GetMarketPrice(Param.Market, Print).Result;
            if (ticker == null)
            {
                LockTickerUpdate = false;
                CurrentContractErrors++;
                if (CurrentContractErrors > MaxContractErrors)
                {
                    Print("Остановка стратеги из-за повторяющихся ошибок.");
                    MainStrategy.Stop();
                }
                return;
            }

            OnUpdateTicker(ticker);
            CurrentContractErrors = 0;
            LockTickerUpdate = false;
        }

        private void TLastCUpdate(object state)
        {
            if (LockCandlesUpdate)
            {
                return;
            }

            if (Candles.Count == 0)
            {
                return;
            }

            LockCandlesUpdate = true;

            Ticker tmp = Stock.GetMarketPrice(Param.Market, Print).Result;
            if (tmp == null)
            {
                LockCandlesUpdate = false;
                return;
            }
            decimal price = (tmp.Ask + tmp.Bid) / 2;
            Candles.Last().Close = price;

            OnUpdateCandles(Candles);
            LockCandlesUpdate = false;
        }

        private void TCandlesUpdate(object state)
        {
            while (LockCandlesUpdate)
            {
                Thread.Sleep(50);
            }
            LockCandlesUpdate = true;
            UpdateCandles().Wait();
            OnUpdateCandles(Candles);
            LockCandlesUpdate = false;
        }

        private async Task UpdateCandles()
        {
            Candles = await Stock.GetCandles(Param.Market, Param.Interval, 50, Print);
            if (Candles != null)
            {
                LastCandleTime = Candles.Last().Time;
                Print(string.Format("Загружено {0} свечей.", Candles.Count), true);
            }
        }
    }
}
