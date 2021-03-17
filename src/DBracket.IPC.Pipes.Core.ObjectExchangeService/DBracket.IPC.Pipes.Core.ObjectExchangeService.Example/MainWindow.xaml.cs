using DBracket.IPC.Pipes.Core.PairedObjectExchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DBracket.IPC.Pipes.Core.ObjectExchangeService.Example
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObjectExchangeHandler _exchangeHandler;
        private DataContainer _dataToExchange;


        public MainWindow()
        {
            InitializeComponent();

            _dataToExchange = new DataContainer();
            _exchangeHandler = new ObjectExchangeHandler(_dataToExchange);
            _exchangeHandler.ObjectChanged += DataChanged;
        }


        private void DataChanged(IExchangeObject changedObject)
        {
            var tmp = (DataContainer)changedObject;
            Application.Current.Dispatcher.Invoke(() => txtData.Text = tmp.TestDataString);
        }

        private void btnCntMaster_Click(object sender, RoutedEventArgs e)
        {
            _exchangeHandler.Start("Channel", Points.PointA);
        }

        private void btnDisCntMaster_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnCntSlave_Click(object sender, RoutedEventArgs e)
        {
            _exchangeHandler.Start("Channel", Points.PointB);
        }

        private void btnDisCntSlave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void txtTestDataMaster_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _dataToExchange.TestDataString = txtTestDataMaster.Text;
                txtData.Text = _dataToExchange.TestDataString;
            }
        }

        private void txtTestDataSlave_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _dataToExchange.TestDataString = txtTestDataSlave.Text;
                txtData.Text = _dataToExchange.TestDataString;
            }
        }
    }
}
