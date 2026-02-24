using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy summing relative changes of open, close, high and low prices.
/// When the average sum exceeds a threshold, enters a position.
/// </summary>
public class FollowYourHeartStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bars;
	private readonly StrategyParam<decimal> _level;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private SimpleMovingAverage _openSma;
	private SimpleMovingAverage _closeSma;
	private SimpleMovingAverage _highSma;
	private SimpleMovingAverage _lowSma;

	private decimal _prevOpen;
	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _isFirst = true;
	private decimal _entryPrice;

	public FollowYourHeartStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_bars = Param(nameof(Bars), 6)
			.SetGreaterThanZero()
			.SetDisplay("Bars", "Number of bars to sum", "Parameters");

		_level = Param(nameof(Level), 0.01m)
			.SetDisplay("Level", "Threshold for changes", "Parameters");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("TP", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("SL", "Stop loss in price units", "Risk");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Bars { get => _bars.Value; set => _bars.Value = value; }
	public decimal Level { get => _level.Value; set => _level.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_openSma = new SimpleMovingAverage { Length = Bars };
		_closeSma = new SimpleMovingAverage { Length = Bars };
		_highSma = new SimpleMovingAverage { Length = Bars };
		_lowSma = new SimpleMovingAverage { Length = Bars };

		_isFirst = true;
		_entryPrice = 0;
		_prevOpen = 0;
		_prevClose = 0;
		_prevHigh = 0;
		_prevLow = 0;

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevOpen = candle.OpenPrice;
			_prevClose = candle.ClosePrice;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_isFirst = false;
			return;
		}

		// Calculate percentage change (relative change * 100)
		var deltaOpen = _prevOpen == 0m ? 0m : (candle.OpenPrice - _prevOpen) / _prevOpen * 100m;
		var deltaClose = _prevClose == 0m ? 0m : (candle.ClosePrice - _prevClose) / _prevClose * 100m;
		var deltaHigh = _prevHigh == 0m ? 0m : (candle.HighPrice - _prevHigh) / _prevHigh * 100m;
		var deltaLow = _prevLow == 0m ? 0m : (candle.LowPrice - _prevLow) / _prevLow * 100m;

		var t = candle.OpenTime;
		_openSma.Process(new DecimalIndicatorValue(_openSma, deltaOpen, t) { IsFinal = true });
		_closeSma.Process(new DecimalIndicatorValue(_closeSma, deltaClose, t) { IsFinal = true });
		_highSma.Process(new DecimalIndicatorValue(_highSma, deltaHigh, t) { IsFinal = true });
		_lowSma.Process(new DecimalIndicatorValue(_lowSma, deltaLow, t) { IsFinal = true });

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;

		if (!_openSma.IsFormed || !_closeSma.IsFormed || !_highSma.IsFormed || !_lowSma.IsFormed)
			return;

		var price = candle.ClosePrice;

		// Manage existing position
		if (Position > 0)
		{
			if (price - _entryPrice >= TakeProfit || _entryPrice - price >= StopLoss)
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= TakeProfit || price - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_entryPrice = 0;
				return;
			}
		}

		if (Position != 0)
			return;

		var o = _openSma.GetCurrentValue() * Bars;
		var c = _closeSma.GetCurrentValue() * Bars;
		var h = _highSma.GetCurrentValue() * Bars;
		var l = _lowSma.GetCurrentValue() * Bars;
		var sum = (o + c + h + l) / 4m;

		if (sum > Level && c > 0)
		{
			BuyMarket();
			_entryPrice = price;
		}
		else if (sum < -Level && c < 0)
		{
			SellMarket();
			_entryPrice = price;
		}
	}
}
