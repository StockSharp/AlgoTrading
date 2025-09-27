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
/// Russian20 Momentum MA strategy converted from MetaTrader 5 (Russian20-hp1.mq5).
/// Combines a simple moving average filter with a momentum confirmation signal.
/// </summary>
public class Russian20MomentumMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _movingAverageLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _stopLossBuyPips;
	private readonly StrategyParam<decimal> _takeProfitBuyPips;
	private readonly StrategyParam<decimal> _stopLossSellPips;
	private readonly StrategyParam<decimal> _takeProfitSellPips;

	private SimpleMovingAverage _movingAverage;
	private Momentum _momentum;
	private decimal? _previousClose;
	private decimal? _entryPrice;
	private decimal _pipSize;
	private decimal _buyStopOffset;
	private decimal _buyTakeOffset;
	private decimal _sellStopOffset;
	private decimal _sellTakeOffset;

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the simple moving average filter.
	/// </summary>
	public int MovingAverageLength
	{
		get => _movingAverageLength.Value;
		set => _movingAverageLength.Value = value;
	}

	/// <summary>
	/// Lookback period for the momentum indicator.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss distance for long trades expressed in pips.
	/// </summary>
	public decimal StopLossBuyPips
	{
		get => _stopLossBuyPips.Value;
		set => _stopLossBuyPips.Value = value;
	}

	/// <summary>
	/// Take profit distance for long trades expressed in pips.
	/// </summary>
	public decimal TakeProfitBuyPips
	{
		get => _takeProfitBuyPips.Value;
		set => _takeProfitBuyPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance for short trades expressed in pips.
	/// </summary>
	public decimal StopLossSellPips
	{
		get => _stopLossSellPips.Value;
		set => _stopLossSellPips.Value = value;
	}

	/// <summary>
	/// Take profit distance for short trades expressed in pips.
	/// </summary>
	public decimal TakeProfitSellPips
	{
		get => _takeProfitSellPips.Value;
		set => _takeProfitSellPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy with default parameters.
	/// </summary>
	public Russian20MomentumMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
		.SetDisplay("Candle Type", "Candle type used for analysis", "General");

		_movingAverageLength = Param(nameof(MovingAverageLength), 20)
		.SetDisplay("Moving Average Period", "Simple moving average filter length", "Indicators")
		.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 5)
		.SetDisplay("Momentum Period", "Length for the Momentum indicator", "Indicators")
		.SetCanOptimize(true);

		_stopLossBuyPips = Param(nameof(StopLossBuyPips), 50m)
		.SetDisplay("Stop Loss Buy (pips)", "Stop loss distance for long positions", "Risk")
		.SetNotNegative();

		_takeProfitBuyPips = Param(nameof(TakeProfitBuyPips), 50m)
		.SetDisplay("Take Profit Buy (pips)", "Take profit distance for long positions", "Risk")
		.SetNotNegative();

		_stopLossSellPips = Param(nameof(StopLossSellPips), 50m)
		.SetDisplay("Stop Loss Sell (pips)", "Stop loss distance for short positions", "Risk")
		.SetNotNegative();

		_takeProfitSellPips = Param(nameof(TakeProfitSellPips), 50m)
		.SetDisplay("Take Profit Sell (pips)", "Take profit distance for short positions", "Risk")
		.SetNotNegative();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousClose = null;
		_entryPrice = null;
		_pipSize = 0m;
		_buyStopOffset = 0m;
		_buyTakeOffset = 0m;
		_sellStopOffset = 0m;
		_sellTakeOffset = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateRiskOffsets();

		_movingAverage = new SimpleMovingAverage
		{
			Length = MovingAverageLength,
		};

		_momentum = new Momentum
		{
			Length = MomentumPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_movingAverage, _momentum, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _movingAverage);
			DrawIndicator(area, _momentum);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal momentumValue)
	{
		// Skip unfinished candles to operate strictly on closed bars.
		if (candle.State != CandleStates.Finished)
		return;

		// Ensure that the strategy is allowed to trade and indicators are ready.
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_movingAverage.IsFormed || !_momentum.IsFormed)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		if (_pipSize == 0m)
		UpdateRiskOffsets();

		var closePrice = candle.ClosePrice;

		if (_previousClose is null)
		{
			_previousClose = closePrice;
			return;
		}

		var entryPrice = _entryPrice;

		if (Position == 0)
		{
			// Reset entry tracking when no position is open.
			_entryPrice = null;

			var bullishSignal = closePrice > maValue && momentumValue > 100m && closePrice > _previousClose.Value;
			var bearishSignal = closePrice < maValue && momentumValue < 100m && closePrice < _previousClose.Value;

			if (bullishSignal)
			{
				// Price closed above MA with positive momentum and rising close -> buy.
				BuyMarket();
				_entryPrice = closePrice;
			}
			else if (bearishSignal)
			{
				// Price closed below MA with negative momentum and falling close -> sell.
				SellMarket();
				_entryPrice = closePrice;
			}
		}
		else if (Position > 0)
		{
			// Exit long when momentum weakens or protective levels are touched.
			var exitByMomentum = momentumValue < 100m;
			var stopLossHit = entryPrice.HasValue && _buyStopOffset > 0m && closePrice <= entryPrice.Value - _buyStopOffset;
			var takeProfitHit = entryPrice.HasValue && _buyTakeOffset > 0m && closePrice >= entryPrice.Value + _buyTakeOffset;

			if (exitByMomentum || stopLossHit || takeProfitHit)
			{
				ClosePosition();
				_entryPrice = null;
			}
		}
		else
		{
			// Exit short when momentum turns positive or protective levels are touched.
			var exitByMomentum = momentumValue > 100m;
			var stopLossHit = entryPrice.HasValue && _sellStopOffset > 0m && closePrice >= entryPrice.Value + _sellStopOffset;
			var takeProfitHit = entryPrice.HasValue && _sellTakeOffset > 0m && closePrice <= entryPrice.Value - _sellTakeOffset;

			if (exitByMomentum || stopLossHit || takeProfitHit)
			{
				ClosePosition();
				_entryPrice = null;
			}
		}

		// Store the close price for next candle comparisons.
		_previousClose = closePrice;
	}

	private void UpdateRiskOffsets()
	{
		// Determine pip size similar to the MetaTrader logic (adjust for fractional pricing).
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
		{
			_pipSize = 1m;
		}
		else
		{
			var decimals = GetDecimalPlaces(step);
			var multiplier = decimals == 3 || decimals == 5 ? 10m : 1m;
			_pipSize = step * multiplier;
		}

		_buyStopOffset = StopLossBuyPips > 0m ? StopLossBuyPips * _pipSize : 0m;
		_buyTakeOffset = TakeProfitBuyPips > 0m ? TakeProfitBuyPips * _pipSize : 0m;
		_sellStopOffset = StopLossSellPips > 0m ? StopLossSellPips * _pipSize : 0m;
		_sellTakeOffset = TakeProfitSellPips > 0m ? TakeProfitSellPips * _pipSize : 0m;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		// Extract scale information from the decimal representation.
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}