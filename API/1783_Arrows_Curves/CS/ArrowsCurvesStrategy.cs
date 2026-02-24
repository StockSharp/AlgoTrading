using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Arrows and Curves channel indicator.
/// Trades breakouts of a Highest/Lowest channel.
/// </summary>
public class ArrowsCurvesStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _prevAbove;
	private bool _prevBelow;
	private bool _initialized;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ArrowsCurvesStrategy()
	{
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Period for channel", "Parameters");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_prevAbove = false;
		_prevBelow = false;
		_initialized = false;

		var highest = new Highest { Length = Period };
		var lowest = new Lowest { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var mid = (high + low) / 2m;

		var above = close > mid;
		var below = close < mid;

		// Manage exits
		if (Position > 0)
		{
			if (close - _entryPrice >= TakeProfit || _entryPrice - close >= StopLoss)
			{
				SellMarket();
				_entryPrice = 0;
				_prevAbove = above;
				_prevBelow = below;
				_initialized = true;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - close >= TakeProfit || close - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_entryPrice = 0;
				_prevAbove = above;
				_prevBelow = below;
				_initialized = true;
				return;
			}
		}

		// Entry on channel midpoint crossing
		if (Position == 0 && _initialized)
		{
			if (above && !_prevAbove)
			{
				BuyMarket();
				_entryPrice = close;
			}
			else if (below && !_prevBelow)
			{
				SellMarket();
				_entryPrice = close;
			}
		}

		_prevAbove = above;
		_prevBelow = below;
		_initialized = true;
	}
}
