using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor <c>et4_MTC_v1</c>.
/// The strategy exposes the same parameter structure and timing helpers while leaving the trading logic extensible.
/// </summary>
public class Et4MtcV1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _enableLogging;
	private readonly StrategyParam<TimeSpan> _tradeCooldown;
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset? _lastTradeTime;
	private DateTimeOffset? _previousCandleTime;
	private bool _isNewCandle;

	/// <summary>
	/// Initializes <see cref="Et4MtcV1Strategy"/> with parameters that match the original expert advisor.
	/// </summary>
	public Et4MtcV1Strategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 150m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Target profit in points", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10m, 500m, 10m);

		_lots = Param(nameof(Lots), -10m)
			.SetDisplay("Lots", "Fixed lot size or negative value for balance-based calculation", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(-20m, 20m, 1m);

		_stopLoss = Param(nameof(StopLoss), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Protective stop in points", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_enableLogging = Param(nameof(EnableLogging), false)
			.SetDisplay("Enable Logging", "Enable informational logging for template events", "General");

		_tradeCooldown = Param(nameof(TradeCooldown), TimeSpan.FromSeconds(30))
			.SetDisplay("Trade Cooldown", "Minimal delay between trades", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Market data source used for candle timing", "General");
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Order volume: positive value for fixed lots, negative value to enable balance-based sizing.
	/// </summary>
	public decimal Lots
	{
		get => _lots.Value;
		set => _lots.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Enables additional informational logging.
	/// </summary>
	public bool EnableLogging
	{
		get => _enableLogging.Value;
		set => _enableLogging.Value = value;
	}

	/// <summary>
	/// Minimal time span between two consecutive trades.
	/// </summary>
	public TimeSpan TradeCooldown
	{
		get => _tradeCooldown.Value;
		set => _tradeCooldown.Value = value;
	}

	/// <summary>
	/// Market data type used to emulate the MetaTrader current chart context.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Indicates whether the current candle is the first update after a new bar opened.
	/// </summary>
	protected bool IsNewCandle => _isNewCandle;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastTradeTime = null;
		_previousCandleTime = null;
		_isNewCandle = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = CalculateVolume();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var eventTime = candle.CloseTime ?? candle.OpenTime;

		CheckNewCandle(eventTime);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (IsCooldownActive(eventTime))
		{
			if (EnableLogging)
				this.LogInfo("Cooldown active until {0:O}.", _lastTradeTime!.Value + TradeCooldown);

			return;
		}

		if (OpenPosition(candle))
			return;

		if (ManagePosition(candle))
			return;

		ClosePosition(candle);
	}

	private void CheckNewCandle(DateTimeOffset eventTime)
	{
		_isNewCandle = _previousCandleTime != eventTime;
		_previousCandleTime = eventTime;
	}

	private bool IsCooldownActive(DateTimeOffset eventTime)
	{
		if (_lastTradeTime is null)
			return false;

		return eventTime - _lastTradeTime.Value < TradeCooldown;
	}

	private bool OpenPosition(ICandleMessage candle)
	{
		// Placeholder for entry conditions. Override or extend to implement custom logic.
		return false;
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		// Placeholder for trailing stop or breakeven management.
		return false;
	}

	private bool ClosePosition(ICandleMessage candle)
	{
		// Placeholder for exit logic, returns true when an action is taken.
		return false;
	}

	private decimal CalculateVolume()
	{
		var lots = Lots;

		if (lots >= 0m)
			return lots;

		var balance = Portfolio?.CurrentValue ?? 0m;
		if (balance <= 0m)
			return 0.1m;

		var dynamicLots = Math.Floor((balance / 1000m * -lots) / 10m) / 10m;

		if (dynamicLots < 0.1m)
			dynamicLots = 0.1m;

		return dynamicLots;
	}

	/// <inheritdoc />
	protected override void OnOrderRegistered(Order order)
	{
		base.OnOrderRegistered(order);

		UpdateLastTradeTime(order.Time);
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		UpdateLastTradeTime(order.Time);
	}

	/// <inheritdoc />
	protected override void OnOrderCanceled(Order order)
	{
		base.OnOrderCanceled(order);

		UpdateLastTradeTime(order.Time);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		UpdateLastTradeTime(trade.Trade.ServerTime);
	}

	private void UpdateLastTradeTime(DateTimeOffset time)
	{
		_lastTradeTime = time;
	}
}

