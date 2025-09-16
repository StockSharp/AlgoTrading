using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time based opening strategy with optional trailing stop logic.
/// </summary>
public class OpenTimeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useCloseTime;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<int> _closeMinute;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _tradeMinute;
	private readonly StrategyParam<int> _durationSeconds;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;

	private decimal _pipSize;
	private decimal _stopOffset;
	private decimal _takeOffset;
	private decimal _trailOffset;
	private decimal _trailStep;

	private decimal? _longEntry;
	private decimal? _shortEntry;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenTimeStrategy"/> class.
	/// </summary>
	public OpenTimeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle subscription type", "General");
		_useCloseTime = Param(nameof(UseCloseTime), true)
			.SetDisplay("Use Close Window", "Enable automatic closing window", "Trading");
		_closeHour = Param(nameof(CloseHour), 20)
			.SetDisplay("Close Hour", "Hour for the closing window", "Trading");
		_closeMinute = Param(nameof(CloseMinute), 50)
			.SetDisplay("Close Minute", "Minute for the closing window", "Trading");
		_enableTrailing = Param(nameof(EnableTrailing), false)
			.SetDisplay("Enable Trailing", "Use trailing stop logic", "Risk");
		_trailingStopPips = Param(nameof(TrailingStopPips), 30)
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");
		_trailingStepPips = Param(nameof(TrailingStepPips), 3)
			.SetDisplay("Trailing Step", "Additional move required to shift the trail", "Risk");
		_tradeHour = Param(nameof(TradeHour), 18)
			.SetDisplay("Trade Hour", "Hour to start opening positions", "Trading");
		_tradeMinute = Param(nameof(TradeMinute), 50)
			.SetDisplay("Trade Minute", "Minute to start opening positions", "Trading");
		_durationSeconds = Param(nameof(DurationSeconds), 300)
			.SetDisplay("Duration", "Window length in seconds", "Trading");
		_enableSell = Param(nameof(EnableSell), true)
			.SetDisplay("Enable Sell", "Allow short entries", "Trading");
		_enableBuy = Param(nameof(EnableBuy), false)
			.SetDisplay("Enable Buy", "Allow long entries", "Trading");
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");
		_stopLossPips = Param(nameof(StopLossPips), 0)
			.SetDisplay("Stop Loss", "Initial stop loss in pips", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 0)
			.SetDisplay("Take Profit", "Initial take profit in pips", "Risk");
	}

	/// <summary>
	/// Candle subscription type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Indicates whether automatic closing window is enabled.
	/// </summary>
	public bool UseCloseTime
	{
		get => _useCloseTime.Value;
		set => _useCloseTime.Value = value;
	}

	/// <summary>
	/// Hour of the closing window (0-24).
	/// </summary>
	public int CloseHour
	{
		get => _closeHour.Value;
		set => _closeHour.Value = value;
	}

	/// <summary>
	/// Minute of the closing window (0-59).
	/// </summary>
	public int CloseMinute
	{
		get => _closeMinute.Value;
		set => _closeMinute.Value = value;
	}

	/// <summary>
	/// Enables trailing stop logic.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional movement required to move the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Hour when the strategy can open positions.
	/// </summary>
	public int TradeHour
	{
		get => _tradeHour.Value;
		set => _tradeHour.Value = value;
	}

	/// <summary>
	/// Minute when the strategy can open positions.
	/// </summary>
	public int TradeMinute
	{
		get => _tradeMinute.Value;
		set => _tradeMinute.Value = value;
	}

	/// <summary>
	/// Duration of the trading window in seconds.
	/// </summary>
	public int DurationSeconds
	{
		get => _durationSeconds.Value;
		set => _durationSeconds.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Trading volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initial stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Initial take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_stopOffset = 0m;
		_takeOffset = 0m;
		_trailOffset = 0m;
		_trailStep = 0m;

		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Convert pip-based inputs to absolute price offsets.
		_pipSize = CalculatePipSize();
		_stopOffset = StopLossPips * _pipSize;
		_takeOffset = TakeProfitPips * _pipSize;
		_trailOffset = TrailingStopPips * _pipSize;
		_trailStep = TrailingStepPips * _pipSize;

		// Subscribe to candle data used for time-based processing.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		// Enable built-in position protection against leftover exposure.
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with finished candles to avoid premature actions.
		if (candle.State != CandleStates.Finished)
			return;

		var now = candle.CloseTime;

		// Force-close any position during the configured closing window.
		if (UseCloseTime && IsWithinWindow(now, CloseHour, CloseMinute, DurationSeconds))
		{
			CloseActivePositions();
			return;
		}

		// Update trailing stops and exit if risk limits were exceeded.
		UpdateTrailingStops(candle);
		CheckRiskManagement(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Skip entries outside the trading window.
		if (!IsWithinWindow(now, TradeHour, TradeMinute, DurationSeconds))
			return;

		// Open or reverse long positions when buying is enabled.
		if (EnableBuy && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			if (volume > 0)
			{
				if (Position < 0)
					ResetShortState();

				BuyMarket(volume);
				InitializeLongState(candle.ClosePrice);
			}
		}
		else if (EnableSell && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			if (volume > 0)
			{
				if (Position > 0)
					ResetLongState();

				SellMarket(volume);
				InitializeShortState(candle.ClosePrice);
			}
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (!EnableTrailing || _trailOffset <= 0m)
			return;

		// Move the trailing stop for long positions once the minimal step is reached.
		if (Position > 0 && _longEntry.HasValue)
		{
			var distance = candle.ClosePrice - _longEntry.Value;
			if (distance > _trailOffset + _trailStep)
			{
				var triggerLevel = candle.ClosePrice - (_trailOffset + _trailStep);
				if (!_longStop.HasValue || _longStop.Value < triggerLevel)
					_longStop = candle.ClosePrice - _trailOffset;
			}
		}
		// Move the trailing stop for short positions in a symmetrical way.
		else if (Position < 0 && _shortEntry.HasValue)
		{
			var distance = _shortEntry.Value - candle.ClosePrice;
			if (distance > _trailOffset + _trailStep)
			{
				var triggerLevel = candle.ClosePrice + (_trailOffset + _trailStep);
				if (!_shortStop.HasValue || _shortStop.Value > triggerLevel)
					_shortStop = candle.ClosePrice + _trailOffset;
			}
		}
	}

	private void CheckRiskManagement(ICandleMessage candle)
	{
		if (Position > 0)
		{
			// Close long positions when price touches stop loss.
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}

			// Close long positions when the take profit target is reached.
			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Position);
				ResetLongState();
			}
		}
		else if (Position < 0)
		{
			var absPos = -Position;
			// Close short positions when price hits the stop level.
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(absPos);
				ResetShortState();
				return;
			}

			// Close short positions when the take profit target is reached.
			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(absPos);
				ResetShortState();
			}
		}
	}

	private void CloseActivePositions()
	{
		// Flatten the portfolio and clear cached levels.
		if (Position > 0)
		{
			SellMarket(Position);
			ResetLongState();
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
			ResetShortState();
		}
	}

	private void InitializeLongState(decimal price)
	{
		// Remember entry price and derived risk levels for long trades.
		_longEntry = price;
		_longStop = StopLossPips > 0 ? price - _stopOffset : null;
		_longTake = TakeProfitPips > 0 ? price + _takeOffset : null;
	}

	private void InitializeShortState(decimal price)
	{
		// Remember entry price and derived risk levels for short trades.
		_shortEntry = price;
		_shortStop = StopLossPips > 0 ? price + _stopOffset : null;
		_shortTake = TakeProfitPips > 0 ? price - _takeOffset : null;
	}

	private void ResetLongState()
	{
		_longEntry = null;
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortState()
	{
		_shortEntry = null;
		_shortStop = null;
		_shortTake = null;
	}

	private decimal CalculatePipSize()
	{
		// Convert MT5-style pip values into absolute price units.
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			return 1m;

		var decimals = CountDecimals(step);
		var multiplier = decimals == 3 || decimals == 5
			? 10m
			: 1m;
		return step * multiplier;
	}

	private static int CountDecimals(decimal value)
	{
		// Count decimal places by repeatedly shifting the decimal point.
		value = Math.Abs(value);
		var decimals = 0;
		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}
		return decimals;
	}

	private static bool IsWithinWindow(DateTimeOffset time, int hour, int minute, int durationSeconds)
	{
		if (durationSeconds <= 0)
			return false;

		var start = BuildReferenceTime(time, hour, minute);
		var end = start.AddSeconds(durationSeconds);
		return time >= start && time < end;
	}

	private static DateTimeOffset BuildReferenceTime(DateTimeOffset reference, int hour, int minute)
	{
		// Align the target time with the current trading day, allowing hour values above 23.
		var normalizedHour = hour;
		var day = new DateTimeOffset(reference.Year, reference.Month, reference.Day, 0, 0, 0, reference.Offset);

		while (normalizedHour >= 24)
		{
			normalizedHour -= 24;
			day = day.AddDays(1);
		}

		if (minute < 0)
			minute = 0;
		else if (minute > 59)
			minute = 59;

		return day.AddHours(normalizedHour).AddMinutes(minute);
	}
}
