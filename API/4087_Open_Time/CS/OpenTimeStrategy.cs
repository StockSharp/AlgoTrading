using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time-of-day strategy converted from the MetaTrader expert "OpenTime".
/// Places market orders inside a trading window and optionally closes them during a separate exit window.
/// </summary>
public class OpenTimeStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableCloseWindow;
	private readonly StrategyParam<TimeSpan> _closeTime;
	private readonly StrategyParam<TimeSpan> _tradeTime;
	private readonly StrategyParam<TimeSpan> _windowLength;
	private readonly StrategyParam<bool> _allowSellEntries;
	private readonly StrategyParam<bool> _allowBuyEntries;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<DataType> _candleType;

	private bool _buyRequested;
	private bool _sellRequested;
	private bool _closeRequested;
	private decimal _pointValue;

	/// <summary>
	/// Initializes a new instance of <see cref="OpenTimeStrategy"/>.
	/// </summary>
	public OpenTimeStrategy()
	{
		_enableCloseWindow = Param(nameof(EnableCloseWindow), true)
			.SetDisplay("Enable Close Window", "Close positions during the exit window", "Schedule");

		_closeTime = Param(nameof(CloseTime), new TimeSpan(20, 50, 0))
			.SetDisplay("Close Position Time", "Time of day when the exit window starts", "Schedule");

		_tradeTime = Param(nameof(TradeTime), new TimeSpan(18, 50, 0))
			.SetDisplay("Trading Time", "Time of day when new trades are allowed", "Schedule");

		_windowLength = Param(nameof(WindowLength), TimeSpan.FromMinutes(5))
			.SetDisplay("Window Length", "Duration of the trading and closing windows", "Schedule");

		_allowSellEntries = Param(nameof(AllowSellEntries), true)
			.SetDisplay("Allow Sell Entries", "Enable short entries", "Trading");

		_allowBuyEntries = Param(nameof(AllowBuyEntries), false)
			.SetDisplay("Allow Buy Entries", "Enable long entries", "Trading");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Target volume for each trade", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Stop-Loss Points", "Protective stop distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Take-Profit Points", "Take-profit distance in points", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Activate the trailing stop helper", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 300m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop Points", "Trailing stop distance in points", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 3m)
			.SetNotNegative()
			.SetDisplay("Trailing Step Points", "Additional distance required before trailing advances", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for time-of-day checks", "General");
	}

	/// <summary>
	/// Enables the exit window that closes open positions.
	/// </summary>
	public bool EnableCloseWindow
	{
		get => _enableCloseWindow.Value;
		set => _enableCloseWindow.Value = value;
	}

	/// <summary>
	/// Daily time when the closing window starts.
	/// </summary>
	public TimeSpan CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	/// <summary>
	/// Daily time when the trading window starts.
	/// </summary>
	public TimeSpan TradeTime
	{
		get => _tradeTime.Value;
		set => _tradeTime.Value = value;
	}

	/// <summary>
	/// Duration of both trading and closing windows.
	/// </summary>
	public TimeSpan WindowLength
	{
		get => _windowLength.Value;
		set => _windowLength.Value = value;
	}

	/// <summary>
	/// Enables opening short positions inside the trading window.
	/// </summary>
	public bool AllowSellEntries
	{
		get => _allowSellEntries.Value;
		set => _allowSellEntries.Value = value;
	}

	/// <summary>
	/// Enables opening long positions inside the trading window.
	/// </summary>
	public bool AllowBuyEntries
	{
		get => _allowBuyEntries.Value;
		set => _allowBuyEntries.Value = value;
	}

	/// <summary>
	/// Target net volume for new entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables trailing stop management.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Base trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Additional progress required before the trailing stop moves.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for time checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyRequested = false;
		_sellRequested = false;
		_closeRequested = false;
		_pointValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		_pointValue = CalculatePointValue();

		var takeProfit = ToAbsoluteUnit(TakeProfitPoints);
		var stopLoss = ToAbsoluteUnit(StopLossPoints);

		Unit? trailingStop = null;
		Unit? trailingStep = null;

		if (UseTrailingStop)
		{
			trailingStop = ToAbsoluteUnit(TrailingStopPoints);
			trailingStep = ToAbsoluteUnit(TrailingStepPoints);

			if (stopLoss == null && trailingStop != null)
			{
				// Provide an initial protective distance when only the trailing stop is configured.
				stopLoss = trailingStop;
			}
		}

		if (takeProfit != null || stopLoss != null || trailingStop != null)
		{
			StartProtection(
				takeProfit: takeProfit,
				stopLoss: stopLoss,
				trailingStop: trailingStop,
				trailingStep: trailingStep,
				useMarketOrders: true);
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private Unit? ToAbsoluteUnit(decimal points)
	{
		if (points <= 0m || _pointValue <= 0m)
			return null;

		return new Unit(points * _pointValue, UnitTypes.Absolute);
	}

	private decimal CalculatePointValue()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var decimals = security.Decimals;
		var adjust = decimals is 3 or 5 ? 10m : 1m;
		return step * adjust;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var timeOfDay = candle.OpenTime.TimeOfDay;

		if (EnableCloseWindow && Position != 0m && !_closeRequested && IsWithinWindow(timeOfDay, CloseTime, WindowLength))
		{
			// ClosePosition sends a market order that closes the current net exposure.
			ClosePosition();
			_closeRequested = true;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinWindow(timeOfDay, TradeTime, WindowLength))
			return;

		if (AllowBuyEntries && !_buyRequested && Position <= 0m)
		{
			var volume = Volume;
			if (Position < 0m)
				volume += Math.Abs(Position);

			if (volume > 0m)
			{
				BuyMarket(volume);
				_buyRequested = true;
				_closeRequested = false;
			}
		}

		if (AllowSellEntries && !_sellRequested && Position >= 0m)
		{
			var volume = Volume;
			if (Position > 0m)
				volume += Math.Abs(Position);

			if (volume > 0m)
			{
				SellMarket(volume);
				_sellRequested = true;
				_closeRequested = false;
			}
		}
	}

	private static bool IsWithinWindow(TimeSpan current, TimeSpan start, TimeSpan length)
	{
		if (length <= TimeSpan.Zero)
			return current == start;

		var end = start + length;

		if (end < TimeSpan.FromDays(1))
			return current >= start && current < end;

		var wrappedEnd = end - TimeSpan.FromDays(1);
		return current >= start || current < wrappedEnd;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (delta > 0m)
			_buyRequested = false;
		else if (delta < 0m)
			_sellRequested = false;

		if (Position == 0m)
		{
			_buyRequested = false;
			_sellRequested = false;
			_closeRequested = false;
		}
	}
}
