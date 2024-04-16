using System.Windows;
using System.IO;

using Ecng.Serialization;
using Ecng.Configuration;

using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Xaml;

namespace Connect_and_get_security
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private readonly Connector _connector = new Connector();
		private const string _connectorFile = "ConnectorFile.json";

		public MainWindow()
		{
			InitializeComponent();

			// registering all connectors
			ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(_connector.Adapter.InnerAdapters));

			if (File.Exists(_connectorFile))
			{
				_connector.Load(_connectorFile.Deserialize<SettingsStorage>());
			}
		}

		private void Setting_Click(object sender, RoutedEventArgs e)
		{
			if (_connector.Configure(this))
			{
				_connector.Save().Serialize(_connectorFile);
			}
		}

		private void Connect_Click(object sender, RoutedEventArgs e)
		{
			SecurityPicker.SecurityProvider = _connector;
			SecurityPicker.MarketDataProvider = _connector;
			_connector.Connected += Connector_Connected;
			_connector.Connect();
		}

		private void Connector_Connected()
		{
			// for Interactive Brokers Trader Workstation
			_connector.LookupSecurities(new Security() { Code = "BTC" });
		}

		private void SecurityPicker_SecuritySelected(Security security)
		{
			if (security == null) return;
			//_connector.RegisterSecurity(security); // - out of date
			_connector.SubscribeLevel1(security);
		}
	}
}
