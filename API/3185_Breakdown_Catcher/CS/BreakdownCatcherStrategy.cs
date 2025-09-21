using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mimics the "Breakdown catcher" MetaTrader Expert Advisor.
/// Places breakout orders around the previous candle and manages risk with pip-based distances.
/// </summary>
public class BreakdownCatcherStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _indentPips;
	private readonly StrategyParam<int> _allowedSpreadPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _pointSize;
	private decimal? _previousHigh;
	private decimal? _previousLow;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	/// <summary>
	/// Initializes a new instance of the <see cref="BreakdownCatcherStrategy"/> class.
	/// </summary>
	public BreakdownCatcherStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 30)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance for breakout positions", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 90)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take-profit distance for breakout positions", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance that protects gains", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Additional progress required before shifting the trailing stop", "Risk");

		_indentPips = Param(nameof(IndentPips), 0)
			.SetNotNegative()
			.SetDisplay("Indent (pips)", "Extra offset added to breakout levels", "Strategy");

		_allowedSpreadPoints = Param(nameof(AllowedSpreadPoints), 5)
			.SetNotNegative()
			.SetDisplay("Allowed Spread (points)", "Maximum bid-ask spread measured in points", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to detect breakouts", "General");
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal additional distance in pips that price must move before the trailing stop shifts.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Offset applied to the previous candle range to create breakout levels.
	/// </summary>
	public int IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in raw points.
	/// </summary>
	public int AllowedSpreadPoints
	{
		get => _allowedSpreadPoints.Value;
		set => _allowedSpreadPoints.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		_pointSize = Security?.PriceStep ?? 0m;

		if (TrailingStopPips > 0 && TrailingStepPips == 0)
		{
			LogError("Trailing is not possible when TrailingStepPips equals zero. Trailing will remain disabled.");
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with finished candles to mimic bar-by-bar logic from MetaTrader.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateTrailingStops(candle);

		if (TryHandleActivePosition(candle))
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		if (_previousHigh is null || _previousLow is null)
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		if (!IsSpreadAcceptable())
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		var indent = ToPipPrice(IndentPips);
		var buyLevel = _previousHigh.Value + indent;
		var sellLevel = _previousLow.Value - indent;

		var breakoutUp = candle.HighPrice >= buyLevel;
		var breakoutDown = candle.LowPrice <= sellLevel;

		if (breakoutUp && breakoutDown)
		{
			LogInfo($"Both breakout levels were reached within one candle (high={candle.HighPrice:F5}, low={candle.LowPrice:F5}). Entry skipped.");
		}
		else if (breakoutUp && Position <= 0)
		{
			EnterLong(buyLevel, candle);
		}
		else if (breakoutDown && Position >= 0)
		{
			EnterShort(sellLevel, candle);
		}

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}

	private bool TryHandleActivePosition(ICandleMessage candle)
	{
		if (Position > 0 && _longEntryPrice is decimal)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
				return false;

			if (_longStop > 0m && candle.LowPrice <= _longStop)
			{
				SellMarket(volume);
				LogInfo($"Long position closed by stop-loss at {_longStop:F5}.");
				ResetState();
				return true;
			}

			if (_longTake > 0m && candle.HighPrice >= _longTake)
			{
				SellMarket(volume);
				LogInfo($"Long position closed by take-profit at {_longTake:F5}.");
				ResetState();
				return true;
			}
		}
		else if (Position < 0 && _shortEntryPrice is decimal)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
				return false;

			if (_shortStop > 0m && candle.HighPrice >= _shortStop)
			{
				BuyMarket(volume);
				LogInfo($"Short position closed by stop-loss at {_shortStop:F5}.");
				ResetState();
				return true;
			}

			if (_shortTake > 0m && candle.LowPrice <= _shortTake)
			{
				BuyMarket(volume);
				LogInfo($"Short position closed by take-profit at {_shortTake:F5}.");
				ResetState();
				return true;
			}
		}

		return false;
	}

	private void EnterLong(decimal entryPrice, ICandleMessage candle)
	{
		var volume = Volume + Math.Abs(Position);
		if (volume <= 0m)
			return;

		_longEntryPrice = entryPrice;
		_shortEntryPrice = null;

		_longStop = StopLossPips > 0 ? entryPrice - ToPipPrice(StopLossPips) : 0m;
		_longTake = TakeProfitPips > 0 ? entryPrice + ToPipPrice(TakeProfitPips) : 0m;
		_shortStop = 0m;
		_shortTake = 0m;

		BuyMarket(volume);

		LogInfo($"Opened long position at ~{entryPrice:F5} with SL={_longStop:F5} TP={_longTake:F5}.");
	}

	private void EnterShort(decimal entryPrice, ICandleMessage candle)
	{
		var volume = Volume + Math.Abs(Position);
		if (volume <= 0m)
			return;

		_shortEntryPrice = entryPrice;
		_longEntryPrice = null;

		_shortStop = StopLossPips > 0 ? entryPrice + ToPipPrice(StopLossPips) : 0m;
		_shortTake = TakeProfitPips > 0 ? entryPrice - ToPipPrice(TakeProfitPips) : 0m;
		_longStop = 0m;
		_longTake = 0m;

		SellMarket(volume);

		LogInfo($"Opened short position at ~{entryPrice:F5} with SL={_shortStop:F5} TP={_shortTake:F5}.");
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0)
			return;

		var trailingDistance = ToPipPrice(TrailingStopPips);
		var trailingStep = ToPipPrice(TrailingStepPips);

		if (trailingDistance <= 0m || trailingStep < 0m)
			return;

		if (Position > 0 && _longEntryPrice is decimal entry)
		{
			var move = candle.ClosePrice - entry;
			if (move > trailingDistance + trailingStep)
			{
				var required = candle.ClosePrice - (trailingDistance + trailingStep);
				var newStop = candle.ClosePrice - trailingDistance;
				if (_longStop == 0m || _longStop < required)
				{
					_longStop = newStop;
					LogInfo($"Long trailing stop moved to {_longStop:F5} after price advanced to {candle.ClosePrice:F5}.");
				}
			}
		}
		else if (Position < 0 && _shortEntryPrice is decimal entry)
		{
			var move = entry - candle.ClosePrice;
			if (move > trailingDistance + trailingStep)
			{
				var required = candle.ClosePrice + trailingDistance + trailingStep;
				var newStop = candle.ClosePrice + trailingDistance;
				if (_shortStop == 0m || _shortStop > required)
				{
					_shortStop = newStop;
					LogInfo($"Short trailing stop moved to {_shortStop:F5} after price declined to {candle.ClosePrice:F5}.");
				}
			}
		}
	}

	private bool IsSpreadAcceptable()
	{
		if (_pointSize <= 0m || AllowedSpreadPoints <= 0)
			return true;

		var bestBid = Security?.BestBid?.Price ?? 0m;
		var bestAsk = Security?.BestAsk?.Price ?? 0m;
		if (bestBid <= 0m || bestAsk <= 0m)
			return true;

		var spread = bestAsk - bestBid;
		var maxSpread = AllowedSpreadPoints * _pointSize;
		return spread <= maxSpread;
	}

	private decimal ToPipPrice(int pips)
	{
		if (pips <= 0 || _pipSize <= 0m)
			return 0m;

		return pips * _pipSize;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);
		var decimals = 0;
		var scaled = value;

		while (scaled != Math.Floor(scaled) && decimals < 10)
		{
			scaled *= 10m;
			decimals++;
		}

		return decimals;
	}

	private void ResetState()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
	}
}
