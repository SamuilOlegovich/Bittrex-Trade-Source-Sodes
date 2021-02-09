using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace CryptoTrade
{
    public class TelegramBot
    {
        private readonly TelegramBotClient _telegramBotClient;
        private readonly long _chatId;

        private static ConcurrentQueue<(long, string)> _messages = new ConcurrentQueue<(long, string)>();
        private static AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        private static Thread _messagesThread = null;

        private static TelegramBot instance;

        public static void SetParam(string token, string schatId)
        {
            if (String.IsNullOrEmpty(token) || String.IsNullOrEmpty(schatId))
            {
                instance = null;
                return;
            }

            long chatId = Convert.ToInt64(schatId);
            instance = new TelegramBot(token, chatId);
        }

        public static void Send(string message)
        {
            if (instance == null)
            {
                return;
            }
            instance.SendTextMessage(message);
        }

        private TelegramBot(string token, long chatId)
        {
            _telegramBotClient = new TelegramBotClient(token);
            _chatId = chatId;

            if (_messagesThread == null)
            {
                _messagesThread = new Thread(MessagesHandler);
                _messagesThread.Start();
            }
        }

        private void MessagesHandler()
        {
            while (true)
            {
                if (_messages.IsEmpty)
                {
                    _autoResetEvent.WaitOne();
                }

                if (_messages.TryDequeue(out var message))
                {
                    if (message.Item2 == "dispose!")
                    {
                        break;
                    }

                    int tries = 1;
                    Task task = null;
                    while (true)
                    {
                        try
                        {
                            task = _telegramBotClient.SendTextMessageAsync(message.Item1, message.Item2);
                            task.Wait();
                            break;
                        }
                        catch (Exception)
                        {
                            if (tries >= 2)
                            {
                                string tex = (task.Exception.InnerException ?? task.Exception).Message;
                                Form1.Print(String.Format("Failed to send a Telegram message. Channel: {0}, Message: {1}, ex: {2}",
                                    message.Item1, message.Item2, tex));
                                break;
                            }
                            Thread.Sleep(3000);
                        }
                        tries++;
                    }
                }
            }
        }

        private void SendTextMessage(string message)
        {
            _messages.Enqueue((_chatId, message));
            _autoResetEvent.Set();
        }

        public static void DisposeResources()
        {
            if (instance != null)
            {
                instance.SendTextMessage("dispose!");
                _autoResetEvent.Close();
                _autoResetEvent.Dispose();
            }
        }
    }
}
