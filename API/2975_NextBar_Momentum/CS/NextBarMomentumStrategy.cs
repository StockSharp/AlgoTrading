using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum breakout strategy that compares the previous close with an older reference bar.
/// </summary>
public class NextBarMomentumStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _minDistancePips;
	private readonly StrategyParam<int> _lifetimeBars;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closeHistory = new();

	private decimal _pipSize = 1m;
	private decimal _entryPrice;
	private int _barsSinceEntry;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public decimal MinDistancePips
	{
		get => _minDistancePips.Value;
		set => _minDistancePips.Value = value;
	}

	public int LifetimeBars
	{
		get => _lifetimeBars.Value;
		set => _lifetimeBars.Value = value;
	}

	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NextBarMomentumStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for each market order", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 1m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Extra profit before trailing", "Risk");

		_signalBar = Param(nameof(SignalBar), 2)
			.SetGreaterOrEqual(2)
			.SetDisplay("Signal Bar", "Bars between reference closes", "Signal");

		_minDistancePips = Param(nameof(MinDistancePips), 15m)
			.SetNotNegative()
			.SetDisplay("Min Distance (pips)", "Minimum distance between closes", "Signal");

		_lifetimeBars = Param(nameof(LifetimeBars), 2)
			.SetNotNegative()
			.SetDisplay("Lifetime Bars", "Maximum bars to hold a trade", "Risk");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert long and short setups", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		Volume = OrderVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		HandleActivePosition(candle);

		if (_closeHistory.Count >= SignalBar && Position == 0)
		{
		var prevClose = _closeHistory[^1];
		var lagClose = _closeHistory[_closeHistory.Count - SignalBar];
		var distance = prevClose - lagClose;
		var minDistance = MinDistancePips * _pipSize;

		var direction = 0;

		if (distance > minDistance)
		{
		direction = 1;
		}
		else if (-distance > minDistance)
		{
		direction = -1;
		}

		if (ReverseSignals)
		direction = -direction;

		if (direction > 0)
		{
		EnterLong(candle);
		}
		else if (direction < 0)
		{
		EnterShort(candle);
		}
		}

		_closeHistory.Add(candle.ClosePrice);

		var maxHistory = Math.Max(SignalBar + 2, 10);
		if (_closeHistory.Count > maxHistory)
		{
		_closeHistory.RemoveRange(0, _closeHistory.Count - maxHistory);
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		if (Position != 0)
		return;

		Volume = OrderVolume;
		BuyMarket();

		_entryPrice = candle.ClosePrice;
		_barsSinceEntry = 0;

		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		_stopLossPrice = stopDistance > 0m ? _entryPrice - stopDistance : null;
		_takeProfitPrice = takeDistance > 0m ? _entryPrice + takeDistance : null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		if (Position != 0)
		return;

		Volume = OrderVolume;
		SellMarket();

		_entryPrice = candle.ClosePrice;
		_barsSinceEntry = 0;

		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		_stopLossPrice = stopDistance > 0m ? _entryPrice + stopDistance : null;
		_takeProfitPrice = takeDistance > 0m ? _entryPrice - takeDistance : null;
	}

	private void HandleActivePosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
		ResetPositionState();
		return;
		}

		var trailingStopDistance = TrailingStopPips * _pipSize;
		var trailingStepDistance = TrailingStepPips * _pipSize;

		if (trailingStopDistance > 0m)
		{
		if (Position > 0)
		{
		var profit = candle.ClosePrice - _entryPrice;
		if (profit > trailingStopDistance + trailingStepDistance)
		{
		var newStop = candle.ClosePrice - trailingStopDistance;
		if (!_stopLossPrice.HasValue || newStop >= _stopLossPrice.Value + (trailingStepDistance > 0m ? trailingStepDistance : 0m))
		{
		_stopLossPrice = newStop;
		}
		}
		}
		else if (Position < 0)
		{
		var profit = _entryPrice - candle.ClosePrice;
		if (profit > trailingStopDistance + trailingStepDistance)
		{
		var newStop = candle.ClosePrice + trailingStopDistance;
		if (!_stopLossPrice.HasValue || newStop <= _stopLossPrice.Value - (trailingStepDistance > 0m ? trailingStepDistance : 0m))
		{
		_stopLossPrice = newStop;
		}
		}
		}
		}

		var exitVolume = Math.Abs(Position);

		if (Position > 0)
		{
		if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
		{
		SellMarket(exitVolume);
		ResetPositionState();
		return;
		}

		if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
		{
		SellMarket(exitVolume);
		ResetPositionState();
		return;
		}
		}
		else if (Position < 0)
		{
		if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
		{
		BuyMarket(exitVolume);
		ResetPositionState();
		return;
		}

		if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
		{
		BuyMarket(exitVolume);
		ResetPositionState();
		return;
		}
		}

		_barsSinceEntry++;

		if (LifetimeBars > 0 && _barsSinceEntry >= LifetimeBars)
		{
		ClosePosition();
		ResetPositionState();
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
		{
		SellMarket(Position);
		}
		else if (Position < 0)
		{
		BuyMarket(Math.Abs(Position));
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_barsSinceEntry = 0;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
		return 1m;

		var decimals = GetDecimalPlaces(step);

		if (decimals == 3 || decimals == 5)
		return step * 10m;

		return step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var text = Math.Abs(value).ToString(CultureInfo.InvariantCulture);
		var separatorIndex = text.IndexOf('.');

		return separatorIndex < 0 ? 0 : text.Length - separatorIndex - 1;
	}
}
