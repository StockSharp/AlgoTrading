using System;
using System.Windows;
using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Logging;
using StockSharp.Xaml;

namespace MarketRule;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private HistoryEmulationConnector _connector;

	private Security _security;
	private Portfolio _portfolio;
	private readonly LogManager _logManager;
	private Strategy _strategy;
	private readonly string _pathHistory = Paths.HistoryDataPath;

	public MainWindow()
	{
		InitializeComponent();

		DatePickerBegin.SelectedDate = Paths.HistoryBeginDate;
		DatePickerEnd.SelectedDate = Paths.HistoryEndDate;

		_logManager = new LogManager();
		_logManager.Listeners.Add(new GuiLogListener(Monitor));
	}

	private void Start_Click(object sender, RoutedEventArgs e)
	{
		_security = new Security
		{
			Id = "SBER@TQBR",
			Code = "SBER",
			PriceStep = 0.01m,
			Board = ExchangeBoard.Micex
		};
		_portfolio = new Portfolio { Name = "test account", BeginValue = 1000000 };
		var storageRegistry = new StorageRegistry
		{
			DefaultDrive = new LocalMarketDataDrive(_pathHistory),
		};

		_connector = new HistoryEmulationConnector(new[] { _security }, new[] { _portfolio })
		{
			HistoryMessageAdapter =
			{
				StorageRegistry = storageRegistry,
				StorageFormat = StorageFormats.Binary,
				StartDate = DatePickerBegin.SelectedDate.Value.ChangeKind(DateTimeKind.Utc),
				StopDate = DatePickerEnd.SelectedDate.Value.ChangeKind(DateTimeKind.Utc),
			},
			LogLevel = LogLevels.Info,
		};

		_strategy = new SimpleCandleRules
		//_strategy = new SimpleOrderRules
		//_strategy = new SimpleRules
		//_strategy = new SimpleRulesUntil
		//_strategy = new SimpleTradeRules
		{
			Security = _security,
			Connector = _connector,
			Portfolio = _portfolio,
			LogLevel = LogLevels.Debug
		};
		//_logManager.Sources.Add(_connector);
		_logManager.Sources.Add(_strategy);

		_connector.Connected += Connector_Connected;
		_connector.Connect();
	}

	private void Connector_Connected()
	{
		_strategy.Start();
		_connector.Start();
	}
}

