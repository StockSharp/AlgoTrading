namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

using StockSharp.Algo.Candles;

/// <summary>
/// Simplified MACD 1 Min Scalper - uses WMA crossover + MACD + Momentum confirmation.
/// </summary>
public class Macd1MinScalperStrategy : Strategy
{
	private WeightedMovingAverage _fastMa;
	private WeightedMovingAverage _slowMa;
	private MovingAverageConvergenceDivergence _macd;
	private Momentum _momentum;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private decimal? _entryPrice;

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new WeightedMovingAverage { Length = 3 };
		_slowMa = new WeightedMovingAverage { Length = 5 };
		_macd = new MovingAverageConvergenceDivergence();
		_momentum = new Momentum { Length = 14 };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		var openTime = candle.OpenTime;

		var fastResult = _fastMa.Process(new DecimalIndicatorValue(_fastMa, price, openTime) { IsFinal = true });
		var slowResult = _slowMa.Process(new DecimalIndicatorValue(_slowMa, price, openTime) { IsFinal = true });
		var macdResult = _macd.Process(new DecimalIndicatorValue(_macd, price, openTime) { IsFinal = true });
		var momResult = _momentum.Process(new DecimalIndicatorValue(_momentum, price, openTime) { IsFinal = true });

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_macd.IsFormed || !_momentum.IsFormed)
			return;

		if (fastResult.IsEmpty || slowResult.IsEmpty || macdResult.IsEmpty || momResult.IsEmpty)
			return;

		var fast = fastResult.ToDecimal();
		var slow = slowResult.ToDecimal();
		var macdVal = macdResult.ToDecimal();
		var momVal = momResult.ToDecimal();

		// Risk management
		if (_entryPrice.HasValue && Position != 0)
		{
			var step = Security?.PriceStep ?? 1m;
			if (step <= 0) step = 1m;
			var stopDist = 100 * step;
			var takeDist = 100 * step;

			if (Position > 0 && (candle.ClosePrice <= _entryPrice.Value - stopDist || candle.ClosePrice >= _entryPrice.Value + takeDist))
			{
				SellMarket();
				_entryPrice = null;
				_hasPrev = false;
				_prevFast = fast; _prevSlow = slow;
				return;
			}
			if (Position < 0 && (candle.ClosePrice >= _entryPrice.Value + stopDist || candle.ClosePrice <= _entryPrice.Value - takeDist))
			{
				BuyMarket();
				_entryPrice = null;
				_hasPrev = false;
				_prevFast = fast; _prevSlow = slow;
				return;
			}
		}

		if (_hasPrev)
		{
			// Buy: fast crosses above slow + MACD positive + momentum above 100
			var buySignal = fast > slow && _prevFast <= _prevSlow && macdVal > 0 && momVal > 100;
			// Sell: fast crosses below slow + MACD negative + momentum below 100
			var sellSignal = fast < slow && _prevFast >= _prevSlow && macdVal < 0 && momVal < 100;

			if (buySignal && Position <= 0)
			{
				if (Position < 0)
				{
					BuyMarket();
					_entryPrice = null;
				}
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (sellSignal && Position >= 0)
			{
				if (Position > 0)
				{
					SellMarket();
					_entryPrice = null;
				}
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_hasPrev = true;
	}
}
