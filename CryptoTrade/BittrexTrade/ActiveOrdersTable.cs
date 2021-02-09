using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace CryptoTrade
{
    public partial class ActiveOrdersTableGrid : Form
    {
        private object LockObjectGrid1 = new object();
        private object LockObjectGrid2 = new object();

        private List<string> AllStrategyNames = new List<string>();
        private System.Threading.Timer timerGrid2Update;
        private bool InGrid2Update = false;

        private Func<List<string>, Dictionary<string, double>> GetPositionStates;
        private Action<string> ForcefullyStopStrategy;
        private Action<List<string>> UnsubscribeAllAOrdersTable;

        public ActiveOrdersTableGrid(Func<List<string>, Dictionary<string, double>> getPositionStates,
            Action<string> forcefullyStopStrategy, Action<List<string>> unsubscribeAllAOrdersTable)
        {
            InitializeComponent();
            FormatTable();

            GetPositionStates = getPositionStates;
            ForcefullyStopStrategy = forcefullyStopStrategy;
            UnsubscribeAllAOrdersTable = unsubscribeAllAOrdersTable;

            timerGrid2Update = new System.Threading.Timer(TGrid2Update);
            timerGrid2Update.Change(500, 2000);
        }

        private void FormatTable()
        {
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();

            DataGridViewTextBoxColumn colName1 = new DataGridViewTextBoxColumn()
            {
                CellTemplate = cell,
                Name = "StrategyName",
                HeaderText = "Стратегия",
                //DataPropertyName = "Name"
            };
            dataGridView1.Columns.Add(colName1);

            DataGridViewTextBoxColumn colName2 = new DataGridViewTextBoxColumn()
            {
                CellTemplate = cell,
                Name = "OrderType",
                HeaderText = "Тип",
            };
            dataGridView1.Columns.Add(colName2);

            DataGridViewTextBoxColumn colName3 = new DataGridViewTextBoxColumn()
            {
                CellTemplate = cell,
                Name = "Direction",
                HeaderText = "Направление",
            };
            dataGridView1.Columns.Add(colName3);

            DataGridViewTextBoxColumn colName4 = new DataGridViewTextBoxColumn()
            {
                CellTemplate = cell,
                Name = "Amount",
                HeaderText = "Сума",
            };
            dataGridView1.Columns.Add(colName4);

            DataGridViewTextBoxColumn colName5 = new DataGridViewTextBoxColumn()
            {
                CellTemplate = cell,
                Name = "Price",
                HeaderText = "Цена",
            };
            dataGridView1.Columns.Add(colName5);

            DataGridViewTextBoxColumn colName6 = new DataGridViewTextBoxColumn()
            {
                CellTemplate = cell,
                Name = "Comment",
                HeaderText = "Комментарий",
            };
            dataGridView1.Columns.Add(colName6);
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.EditMode = DataGridViewEditMode.EditProgrammatically;
            dataGridView1.RowHeadersVisible = false;

            //grid 2
            DataGridViewTextBoxColumn g2colName1 = new DataGridViewTextBoxColumn()
            {
                CellTemplate = cell,
                Name = "StrategyName",
                HeaderText = "Стратегия"
            };
            dataGridView2.Columns.Add(g2colName1);

            DataGridViewTextBoxColumn g2colName2 = new DataGridViewTextBoxColumn()
            {
                CellTemplate = cell,
                Name = "StrategyPL",
                HeaderText = "P/L %"
            };
            dataGridView2.Columns.Add(g2colName2);

            DataGridViewButtonCell cellButton = new DataGridViewButtonCell();
            DataGridViewButtonColumn g2colName3 = new DataGridViewButtonColumn()
            {
                CellTemplate = cellButton,
                Name = "ButtonStop",
                HeaderText = ""
            };
            dataGridView2.Columns.Add(g2colName3);
            //dataGridView2.ColumnHeadersVisible = false;

            dataGridView2.CellContentClick += dataGridView2_CellContentClick;
            dataGridView2.AllowUserToAddRows = false;
            dataGridView2.EditMode = DataGridViewEditMode.EditProgrammatically;
            dataGridView2.RowHeadersVisible = false;
        }

        private void UpdateTableGrid1(ActiveOrdersGridEventArgs e)
        {
            List<int> indexes = new List<int>();
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                string SName = dataGridView1.Rows[i].Cells[0].Value?.ToString();
                if (SName == e.StrategyName)
                {
                    indexes.Add(i);
                }
            }
            int shift = 0;
            foreach (int index in indexes)
            {
                dataGridView1.Rows.RemoveAt(index - shift);
                shift++;
            }

            foreach (ActiveOrders order in e.ActiveOrdersList)
            {
                dataGridView1.Rows.Add(e.StrategyName, order.OrderType, order.Direction,
                    order.Amount, order.Price, order.Comment);
            }
            dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Descending);
            dataGridView1.Refresh();
        }

        public void OnUpdateActiveOrders(object sender, ActiveOrdersGridEventArgs e)
        {
            lock (LockObjectGrid1)
            {
                if (e.StrategyType != "Grid")
                {
                    return;
                }

                if (dataGridView1.InvokeRequired)
                {
                    dataGridView1.Invoke(new Action<ActiveOrdersGridEventArgs>((arg1) =>
                    {
                        //ThreadPool.QueueUserWorkItem((x) => UpdateTable());
                        UpdateTableGrid1(arg1);
                    }), e);
                }
                else
                {
                    UpdateTableGrid1(e);
                }
            }
        }

        //For Grid2

        public void AddStrategy(string StrategyName)
        {
            lock (LockObjectGrid2)
            {
                try
                {
                    AllStrategyNames.Add(StrategyName);
                    if (dataGridView2.InvokeRequired)
                    {
                        dataGridView2.Invoke(new Action<string>((arg1) =>
                        {
                            dataGridView2.Rows.Add(arg1, 0, "Закрыть");
                        }), StrategyName);
                    }
                    else
                    {
                        dataGridView2.Rows.Add(StrategyName, 0, "Закрыть");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public void DeleteStrategy(string StrategyName)
        {
            lock (LockObjectGrid2)
            {
                try
                {
                    AllStrategyNames.Remove(StrategyName);
                    if (dataGridView2.InvokeRequired)
                    {
                        dataGridView2.Invoke(new Action<string>((arg1) =>
                        {
                            DeleteRowGrid2(arg1);
                        }), StrategyName);
                    }
                    else
                    {
                        DeleteRowGrid2(StrategyName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void DeleteRowGrid2(string StrategyName)
        {
            int index = -1;
            for (int i = 0; i < dataGridView2.Rows.Count; i++)
            {
                string SName = dataGridView2.Rows[i].Cells[0].Value?.ToString();
                if (SName == StrategyName)
                {
                    index = i;
                    break;
                }
            }
            if (index >= 0)
            {
                dataGridView2.Rows.RemoveAt(index);
            }
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView senderGrid = (DataGridView)sender;

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
                e.RowIndex >= 0)
            {
                //MessageBox.Show("Close strategy " + senderGrid.Rows[e.RowIndex].Cells[0].Value + " !");
                ForcefullyStopStrategy(senderGrid.Rows[e.RowIndex].Cells[0].Value.ToString());
            }
        }

        private void TGrid2Update(object state)
        {
            if (InGrid2Update)
            {
                return;
            }
            InGrid2Update = true;

            Dictionary<string, double> PosDict = GetPositionStates(AllStrategyNames);
            if (dataGridView2.InvokeRequired)
            {
                dataGridView2.Invoke(new Action<Dictionary<string, double>>((arg1) =>
                {
                    //ThreadPool.QueueUserWorkItem((x) => UpdateTable());
                    UpdateTableGrid2(arg1);
                }), PosDict);
            }
            else
            {
                UpdateTableGrid2(PosDict);
            }
            InGrid2Update = false;
        }

        private void UpdateTableGrid2(Dictionary<string, double> PositionsDict)
        {
            lock (LockObjectGrid2)
            {
                for (int i = 0; i < dataGridView2.Rows.Count; i++)
                {
                    string SName = dataGridView2.Rows[i].Cells[0].Value?.ToString();
                    if (PositionsDict.TryGetValue(SName, out double PosValue))
                    {
                        dataGridView2.Rows[i].Cells[1].Value = PosValue;
                    }
                }

                dataGridView2.Sort(dataGridView2.Columns[1], ListSortDirection.Ascending);
                dataGridView2.Refresh();
            }
        }

        public void RenameStrategy(string Oldname, string NewName)
        {
            lock (LockObjectGrid2)
            {
                AllStrategyNames.Remove(Oldname);
                AllStrategyNames.Add(NewName);

                if (FindAndDeleteRow(dataGridView2, Oldname, 0))
                {
                    dataGridView2.Rows.Add(NewName, 0, "Закрыть");
                    dataGridView2.Refresh();
                }
            }

            lock (LockObjectGrid1)
            {
                FindAndDeleteRow(dataGridView1, Oldname, 0);
                dataGridView1.Refresh();
            }
        }

        private bool FindAndDeleteRow(DataGridView dataGridView, string Value, int indexCell)
        {
            DataGridViewRow tmpRow = null;
            for (int i = 0; i < dataGridView2.Rows.Count; i++)
            {
                string tmpstr = dataGridView2.Rows[i].Cells[indexCell].Value.ToString();
                if (tmpstr == Value)
                {
                    tmpRow = dataGridView2.Rows[i];
                    break;
                }
            }
            if (tmpRow != null)
            {
                dataGridView2.Rows.Remove(tmpRow);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ActiveOrdersTable_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (timerGrid2Update != null)
            {
                timerGrid2Update.Dispose();
            }
            UnsubscribeAllAOrdersTable(AllStrategyNames);
            //Dispose();
        }

        private void ActiveOrdersTableGrid_Load(object sender, EventArgs e)
        {

        }
    }
}
