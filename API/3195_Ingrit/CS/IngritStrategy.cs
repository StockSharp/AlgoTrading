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
/// Ingrit breakout strategy converted from the MetaTrader 5 expert advisor.
/// The strategy monitors five-minute candles and looks for momentum bursts between two swing points.
/// </summary>
public class IngritStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _stepPips;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOppositePositions;

	private readonly List<ICandleMessage> _history = new();

	private decimal _pipSize;
	private decimal _stepDistance;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;

	private decimal _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="IngritStrategy"/> class.
	/// </summary>
	public IngritStrategy()
	{
		Volume = 1m;

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used to evaluate the breakout logic", "General");

		_stopLossPips = Param(nameof(StopLossPips), 80m)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true)
		.SetOptimize(20m, 150m, 10m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 70m)
		.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true)
		.SetOptimize(20m, 150m, 10m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true)
		.SetOptimize(0m, 40m, 5m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetDisplay("Trailing Step (pips)", "Extra profit in pips required before the trailing stop moves", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true)
		.SetOptimize(1m, 20m, 1m);

		_stepPips = Param(nameof(StepPips), 25m)
		.SetDisplay("Breakout Step (pips)", "Minimum distance between the swing points that activates a trade", "Signals")
		.SetNotNegative()
		.SetCanOptimize(true)
		.SetOptimize(10m, 80m, 5m);

		_reverseSignals = Param(nameof(ReverseSignals), false)
		.SetDisplay("Reverse Signals", "Flip buy and sell conditions", "Signals");

		_closeOppositePositions = Param(nameof(CloseOppositePositions), false)
		.SetDisplay("Close Opposite", "Close opposite exposure before entering a new position", "Risk");
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step distance in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Minimum swing distance expressed in pips.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Invert buy and sell signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Close opposite exposure before entering a new trade.
	/// </summary>
	public bool CloseOppositePositions
	{
		get => _closeOppositePositions.Value;
		set => _closeOppositePositions.Value = value;
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

		_history.Clear();
		_entryPrice = 0m;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		UpdatePipSettings();

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		UpdatePipSettings();

		_history.Add(candle);

		if (_history.Count > 40)
		_history.RemoveAt(0);

		HandleActivePosition(candle);

		if (_history.Count < 14)
		return;

		var latest = _history[^1];
		var reference = _history[^14];

		var bearishSetup = latest.OpenPrice > latest.ClosePrice && reference.HighPrice - latest.LowPrice > _stepDistance;
		var bullishSetup = latest.ClosePrice > latest.OpenPrice && latest.HighPrice - reference.LowPrice > _stepDistance;

		var openLong = bearishSetup;
		var openShort = bullishSetup;

		if (ReverseSignals)
		{
			(openLong, openShort) = (openShort, openLong);
		}

		if (openLong)
		{
			TryEnterLong(latest);
		}
		else if (openShort)
		{
			TryEnterShort(latest);
		}
	}

	private void HandleActivePosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			ResetPositionState();
			return;
		}

		var trailingDistance = _trailingStopDistance;
		var trailingStep = _trailingStepDistance;

		if (trailingDistance > 0m)
		{
			if (Position > 0)
			{
				var profit = candle.ClosePrice - _entryPrice;
				if (profit > trailingDistance + trailingStep)
				{
					var desiredStop = candle.ClosePrice - trailingDistance;

					if (!_stopLossPrice.HasValue || desiredStop > _stopLossPrice.Value)
					{
						_stopLossPrice = desiredStop;
					}
				}
			}
			else if (Position < 0)
			{
				var profit = _entryPrice - candle.ClosePrice;
				if (profit > trailingDistance + trailingStep)
				{
					var desiredStop = candle.ClosePrice + trailingDistance;

					if (!_stopLossPrice.HasValue || desiredStop < _stopLossPrice.Value)
					{
						_stopLossPrice = desiredStop;
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
			}
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (Position > 0)
		return;

		if (Position < 0 && !CloseOppositePositions)
		return;

		var volume = Volume;

		if (Position < 0 && CloseOppositePositions)
		volume += Math.Abs(Position);

		BuyMarket(volume);

		_entryPrice = candle.ClosePrice;

		_stopLossPrice = _stopLossDistance > 0m ? _entryPrice - _stopLossDistance : null;
		_takeProfitPrice = _takeProfitDistance > 0m ? _entryPrice + _takeProfitDistance : null;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (Position < 0)
		return;

		if (Position > 0 && !CloseOppositePositions)
		return;

		var volume = Volume;

		if (Position > 0 && CloseOppositePositions)
		volume += Math.Abs(Position);

		SellMarket(volume);

		_entryPrice = candle.ClosePrice;

		_stopLossPrice = _stopLossDistance > 0m ? _entryPrice + _stopLossDistance : null;
		_takeProfitPrice = _takeProfitDistance > 0m ? _entryPrice - _takeProfitDistance : null;
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private void UpdatePipSettings()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
		{
			step = 1m;
		}

		var decimals = Security?.Decimals ?? 0;

		_pipSize = step;

		if (decimals == 3 || decimals == 5)
		{
			_pipSize *= 10m;
		}

		_stepDistance = StepPips * _pipSize;
		_stopLossDistance = StopLossPips * _pipSize;
		_takeProfitDistance = TakeProfitPips * _pipSize;
		_trailingStopDistance = TrailingStopPips * _pipSize;
		_trailingStepDistance = TrailingStepPips * _pipSize;
	}
}

