using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy using dynamic Highest/Lowest channel levels.
/// Opens positions when price breaks through channel and uses trailing stop.
/// </summary>
public class BreakThroughStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingDist;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _trailingStop;
	private decimal _prevHigh;
	private decimal _prevLow;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TrailingDist { get => _trailingDist.Value; set => _trailingDist.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BreakThroughStrategy()
	{
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Lookback period for channel", "Parameters");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss", "Risk");

		_trailingDist = Param(nameof(TrailingDist), 200m)
			.SetDisplay("Trailing Distance", "Trailing stop distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_trailingStop = 0;
		_prevHigh = 0;
		_prevLow = 0;

		var highest = new Highest { Length = Period };
		var lowest = new Lowest { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Manage exits with trailing stop
		if (Position > 0)
		{
			var newTrail = price - TrailingDist;
			if (newTrail > _trailingStop)
				_trailingStop = newTrail;

			if (price - _entryPrice >= TakeProfit || price <= _trailingStop || _entryPrice - price >= StopLoss)
			{
				SellMarket();
				_entryPrice = 0;
				_trailingStop = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			var newTrail = price + TrailingDist;
			if (_trailingStop == 0 || newTrail < _trailingStop)
				_trailingStop = newTrail;

			if (_entryPrice - price >= TakeProfit || price >= _trailingStop || price - _entryPrice >= StopLoss)
			{
				BuyMarket();
				_entryPrice = 0;
				_trailingStop = 0;
				return;
			}
		}

		// Breakout entry using previous bar's channel values
		if (Position == 0 && _prevHigh > 0)
		{
			if (price > _prevHigh)
			{
				BuyMarket();
				_entryPrice = price;
				_trailingStop = price - TrailingDist;
			}
			else if (price < _prevLow)
			{
				SellMarket();
				_entryPrice = price;
				_trailingStop = price + TrailingDist;
			}
		}

		_prevHigh = high;
		_prevLow = low;
	}
}
