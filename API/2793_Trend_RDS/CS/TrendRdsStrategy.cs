using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend recognition strategy based on three consecutive candles.
/// </summary>
public class TrendRdsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<bool> _reverse;

	private decimal _prevHigh1;
	private decimal _prevHigh2;
	private decimal _prevHigh3;
	private decimal _prevLow1;
	private decimal _prevLow2;
	private decimal _prevLow3;
	private int _historyCount;

	private decimal _entryPrice;
	private decimal _stopLossPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// Type of candles to analyze.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance measured in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step measured in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Percent of account equity to risk per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Trading session start time (inclusive).
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading session end time (exclusive).
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Reverse the trade direction when enabled.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrendRdsStrategy"/> class.
	/// </summary>
	public TrendRdsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPips = Param(nameof(StopLossPips), 30)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 65)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Trailing step increment", "Risk")
			.SetCanOptimize(true);

		_riskPercent = Param(nameof(RiskPercent), 3m)
			.SetDisplay("Risk %", "Percent of equity to risk", "Risk")
			.SetRange(0m, 100m);

		_startTime = Param(nameof(StartTime), new TimeSpan(9, 0, 0))
			.SetDisplay("Session Start", "Trading session start time", "Session");

		_endTime = Param(nameof(EndTime), new TimeSpan(12, 0, 0))
			.SetDisplay("Session End", "Trading session end time", "Session");

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse", "Trade in the opposite direction", "General");
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

		_prevHigh1 = 0m;
		_prevHigh2 = 0m;
		_prevHigh3 = 0m;
		_prevLow1 = 0m;
		_prevLow2 = 0m;
		_prevLow3 = 0m;
		_historyCount = 0;
		_entryPrice = 0m;
		_stopLossPrice = 0m;
		_takeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
		{
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");
		}

		if (StartTime >= EndTime)
		{
			throw new InvalidOperationException("Session start time must be earlier than end time.");
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
		// Skip unfinished candles to work on closed bars only.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure that the trading environment is ready.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var pip = GetPipSize();

		// Handle protective exits even outside of the session window.
		if (HandleActivePositionExits(candle))
		{
			UpdateHistory(candle);
			return;
		}

		var candleTime = candle.OpenTime.TimeOfDay;
		var inSession = candleTime >= StartTime && candleTime < EndTime;

		if (inSession && _historyCount >= 3)
		{
			var higherLows = _prevLow1 > _prevLow2 && _prevLow2 > _prevLow3;
			var lowerHighs = _prevHigh1 < _prevHigh2 && _prevHigh2 < _prevHigh3;
			var conflict = higherLows && lowerHighs;

			var longSignal = false;
			var shortSignal = false;

			if (!conflict)
			{
				if (higherLows)
				{
					if (Reverse)
						shortSignal = true;
					else
						longSignal = true;
				}

				if (lowerHighs)
				{
					if (Reverse)
						longSignal = true;
					else
						shortSignal = true;
				}
			}

			if (longSignal && Position <= 0)
			{
				EnterLong(candle, pip);
			}
			else if (shortSignal && Position >= 0)
			{
				EnterShort(candle, pip);
			}

			// Update trailing logic after potential entries.
			ApplyTrailing(candle, pip);
		}

		UpdateHistory(candle);
	}

	private void EnterLong(ICandleMessage candle, decimal pip)
	{
		var stopOffset = StopLossPips > 0 ? StopLossPips * pip : 0m;
		var takeOffset = TakeProfitPips > 0 ? TakeProfitPips * pip : 0m;

		var volume = CalculateVolume(stopOffset);
		if (Position < 0)
		{
			volume += Math.Abs(Position);
		}

		BuyMarket(volume);

		_entryPrice = candle.ClosePrice;
		_stopLossPrice = stopOffset > 0m ? _entryPrice - stopOffset : 0m;
		_takeProfitPrice = takeOffset > 0m ? _entryPrice + takeOffset : 0m;
	}

	private void EnterShort(ICandleMessage candle, decimal pip)
	{
		var stopOffset = StopLossPips > 0 ? StopLossPips * pip : 0m;
		var takeOffset = TakeProfitPips > 0 ? TakeProfitPips * pip : 0m;

		var volume = CalculateVolume(stopOffset);
		if (Position > 0)
		{
			volume += Math.Abs(Position);
		}

		SellMarket(volume);

		_entryPrice = candle.ClosePrice;
		_stopLossPrice = stopOffset > 0m ? _entryPrice + stopOffset : 0m;
		_takeProfitPrice = takeOffset > 0m ? _entryPrice - takeOffset : 0m;
	}

	private void ApplyTrailing(ICandleMessage candle, decimal pip)
	{
		if (TrailingStopPips <= 0)
			return;

		var trailingStop = TrailingStopPips * pip;
		var trailingStep = TrailingStepPips * pip;

		if (trailingStop <= 0m || trailingStep <= 0m || _entryPrice == 0m)
			return;

		var price = candle.ClosePrice;

		if (Position > 0)
		{
			var profit = price - _entryPrice;
			if (profit > trailingStop + trailingStep)
			{
				var threshold = price - (trailingStop + trailingStep);
				if (_stopLossPrice == 0m || _stopLossPrice < threshold)
				{
					_stopLossPrice = price - trailingStop;
				}
			}
		}
		else if (Position < 0)
		{
			var profit = _entryPrice - price;
			if (profit > trailingStop + trailingStep)
			{
				var threshold = price + (trailingStop + trailingStep);
				if (_stopLossPrice == 0m || _stopLossPrice > threshold)
				{
					_stopLossPrice = price + trailingStop;
				}
			}
		}
	}

	private bool HandleActivePositionExits(ICandleMessage candle)
	{
		var positionVolume = Math.Abs(Position);
		if (positionVolume == 0m)
			return false;

		if (Position > 0)
		{
			if (_stopLossPrice > 0m && candle.LowPrice <= _stopLossPrice)
			{
				SellMarket(positionVolume);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice > 0m && candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket(positionVolume);
				ResetPositionState();
				return true;
			}
		}
		else
		{
			if (_stopLossPrice > 0m && candle.HighPrice >= _stopLossPrice)
			{
				BuyMarket(positionVolume);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice > 0m && candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket(positionVolume);
				ResetPositionState();
				return true;
			}
		}

		return false;
	}

	private decimal CalculateVolume(decimal stopOffset)
	{
		var baseVolume = Volume > 0m ? Volume : 1m;
		var equity = Portfolio?.CurrentValue ?? 0m;

		if (stopOffset <= 0m || equity <= 0m)
			return baseVolume;

		var step = Security?.PriceStep ?? Security?.Step ?? 0m;
		var stepPrice = Security?.StepPrice ?? 1m;

		if (step <= 0m)
			return baseVolume;

		var stepsToStop = stopOffset / step;
		if (stepsToStop <= 0m)
			return baseVolume;

		var riskAmount = equity * RiskPercent / 100m;
		if (riskAmount <= 0m)
			return baseVolume;

		var riskPerUnit = stepsToStop * stepPrice;
		if (riskPerUnit <= 0m)
			return baseVolume;

		var quantity = riskAmount / riskPerUnit;
		if (quantity <= 0m)
			return baseVolume;

		return Math.Max(quantity, baseVolume);
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_prevHigh3 = _prevHigh2;
		_prevHigh2 = _prevHigh1;
		_prevHigh1 = candle.HighPrice;

		_prevLow3 = _prevLow2;
		_prevLow2 = _prevLow1;
		_prevLow1 = candle.LowPrice;

		if (_historyCount < 3)
		{
			_historyCount++;
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_stopLossPrice = 0m;
		_takeProfitPrice = 0m;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? Security?.Step ?? 0m;
		if (step <= 0m)
			return 1m;

		var pip = step;
		var decimals = GetScale(step);

		if (decimals == 3 || decimals == 5)
		{
			pip *= 10m;
		}

		return pip;
	}

	private static int GetScale(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}
