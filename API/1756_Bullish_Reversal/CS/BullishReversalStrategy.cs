using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy looking for several bullish reversal candlestick patterns.
/// </summary>
public class BullishReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _trailingStop;

	private ICandleMessage _prev1;
	private ICandleMessage _prev2;
	private ICandleMessage _prev3;
	private decimal _stopPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	public BullishReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetDisplay("MA Period", "SMA length", "Parameters")
			.SetGreaterThanZero();

		_trailingStop = Param(nameof(TrailingStop), 300m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
			.SetGreaterThanZero();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SMA { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prev1 is null || _prev2 is null || _prev3 is null)
		{
			_prev3 = _prev2;
			_prev2 = _prev1;
			_prev1 = candle;
			return;
		}

		var open1 = _prev1.OpenPrice;
		var close1 = _prev1.ClosePrice;
		var low1 = _prev1.LowPrice;
		var open2 = _prev2.OpenPrice;
		var close2 = _prev2.ClosePrice;
		var low2 = _prev2.LowPrice;
		var open3 = _prev3.OpenPrice;
		var close3 = _prev3.ClosePrice;
		var low3 = _prev3.LowPrice;

		// Bullish patterns
		var abandonedBaby = open3 > close3 && open2 > close2 && low2 < low3 &&
			open1 < close1 && low1 >= low2 && close1 > open3;

		var morningDojiStar = open3 > close3 && open2 <= close2 &&
			open1 < close3 && close1 < open3;

		var threeInsideUp = open3 > close3 &&
			Math.Abs(close2 - open2) <= 0.6m * Math.Abs(open3 - close3) &&
			close2 > open2 && close1 > open1 && close1 > open3;

		var threeOutsideUp = open3 > close3 &&
			1.1m * Math.Abs(open3 - close3) < Math.Abs(open2 - close2) &&
			open2 < close2 && open1 < close1;

		var threeWhiteSoldiers = open3 < close3 && open2 < close2 && open1 < close1 &&
			close3 < close2 && close2 < close1;

		// Bearish counterparts for short entries
		var threeBlackCrows = open3 > close3 && open2 > close2 && open1 > close1 &&
			close3 > close2 && close2 > close1;

		var threeInsideDown = open3 < close3 &&
			Math.Abs(close2 - open2) <= 0.6m * Math.Abs(open3 - close3) &&
			close2 < open2 && close1 < open1 && close1 < open3;

		var bullSignal = abandonedBaby || morningDojiStar || threeInsideUp || threeOutsideUp || threeWhiteSoldiers;
		var bearSignal = threeBlackCrows || threeInsideDown;

		if (bullSignal && candle.ClosePrice < ma && Position <= 0)
		{
			BuyMarket();
			_stopPrice = candle.ClosePrice - TrailingStop;
		}
		else if (bearSignal && candle.ClosePrice > ma && Position >= 0)
		{
			SellMarket();
			_stopPrice = candle.ClosePrice + TrailingStop;
		}

		// Trailing stop management
		if (Position > 0)
		{
			var newStop = candle.ClosePrice - TrailingStop;
			if (newStop > _stopPrice)
				_stopPrice = newStop;

			if (candle.ClosePrice <= _stopPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + TrailingStop;
			if (newStop < _stopPrice)
				_stopPrice = newStop;

			if (candle.ClosePrice >= _stopPrice)
				BuyMarket();
		}

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = candle;
	}
}
