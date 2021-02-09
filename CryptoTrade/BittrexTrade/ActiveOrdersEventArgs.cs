using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoTrade
{
    public class ActiveOrdersGridEventArgs : EventArgs
    {
        public string StrategyType { get; set; }
        public string StrategyName { get; set; }
        public List<ActiveOrders> ActiveOrdersList { get; set; }

        public ActiveOrdersGridEventArgs()
        {
            ActiveOrdersList = new List<ActiveOrders>();
        }
    }

    public class ActiveOrders
    {
        public string Direction { get; set; }
        public string OrderType { get; set; }
        public double Amount { get; set; }
        public double Price { get; set; }
        public string Comment { get; set; }
    }
}
