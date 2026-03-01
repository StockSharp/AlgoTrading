namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that opens a few sample trades and reports whether orders matching the configured filter remain active.
/// This is a utility/monitoring strategy.
/// </summary>
public class CheckOpenOrdersStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _mode;
	private readonly StrategyParam<DataType> _candleType;

	private string _modeDescription = string.Empty;
	private string _orderTypesDescription = string.Empty;
	private string _lastStatusMessage;

	/// <summary>
	/// Initializes a new instance of the <see cref="CheckOpenOrdersStrategy"/> class.
	/// </summary>
	public CheckOpenOrdersStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Lot size used for the demonstration orders.", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (points)", "Distance in broker points for the sample protective stop order.", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (points)", "Distance in broker points for the sample profit target.", "Risk");

		_mode = Param(nameof(Mode), 0)
			.SetDisplay("Order Filter", "Type of market positions monitored (0=All, 1=Buy, 2=Sell).", "Monitoring");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for periodic checks", "General");
	}

	/// <summary>
	/// Lot size for every sample order.
	/// </summary>
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }

	/// <summary>
	/// Stop-loss distance in broker points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Take-profit distance in broker points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Type of open orders checked by the status report (0=All, 1=Buy, 2=Sell).
	/// </summary>
	public int Mode { get => _mode.Value; set => _mode.Value = value; }

	/// <summary>
	/// Candle type for periodic processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_modeDescription = string.Empty;
		_orderTypesDescription = string.Empty;
		_lastStatusMessage = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		UpdateModeDescription();
		UpdateStatusMessage();

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateStatusMessage();
	}

	private void UpdateModeDescription()
	{
		_modeDescription = Mode switch
		{
			1 => "Checking for buy market open orders only",
			2 => "Checking sell market open orders only",
			_ => "Checking all market open orders"
		};

		_orderTypesDescription = Mode switch
		{
			1 => "buy",
			2 => "sell",
			_ => "buy and sell"
		};
	}

	private void UpdateStatusMessage()
	{
		if (_modeDescription.IsEmpty())
			return;

		var hasOpenOrders = HasOpenOrders();
		var message = $"Option chosen: {_modeDescription}. Are there any current {_orderTypesDescription} open orders? {(hasOpenOrders ? "Yes" : "No")}";

		if (message == _lastStatusMessage)
			return;

		_lastStatusMessage = message;
		LogInfo(message);
	}

	private bool HasOpenOrders()
	{
		return Mode switch
		{
			1 => Position > 0m,
			2 => Position < 0m,
			_ => Position != 0m,
		};
	}
}
