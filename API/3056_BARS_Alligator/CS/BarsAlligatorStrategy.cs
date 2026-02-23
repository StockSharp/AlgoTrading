namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

using StockSharp.Algo.Candles;

/// <summary>
/// Bill Williams Alligator strategy: trades on lips/jaw crossover, exits on lips/teeth crossover.
/// </summary>
public class BarsAlligatorStrategy : Strategy
{
	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;

	private decimal _prevJaw;
	private decimal _prevTeeth;
	private decimal _prevLips;
	private bool _hasPrev;
	private decimal? _entryPrice;

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_jaw = new SmoothedMovingAverage { Length = 13 };
		_teeth = new SmoothedMovingAverage { Length = 8 };
		_lips = new SmoothedMovingAverage { Length = 5 };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawVal = _jaw.Process(new DecimalIndicatorValue(_jaw, price, candle.OpenTime) { IsFinal = true });
		var teethVal = _teeth.Process(new DecimalIndicatorValue(_teeth, price, candle.OpenTime) { IsFinal = true });
		var lipsVal = _lips.Process(new DecimalIndicatorValue(_lips, price, candle.OpenTime) { IsFinal = true });

		if (!_jaw.IsFormed || !_teeth.IsFormed || !_lips.IsFormed)
			return;

		var jaw = jawVal.IsEmpty ? 0m : jawVal.ToDecimal();
		var teeth = teethVal.IsEmpty ? 0m : teethVal.ToDecimal();
		var lips = lipsVal.IsEmpty ? 0m : lipsVal.ToDecimal();

		if (jaw == 0 || teeth == 0 || lips == 0)
			return;

		// Risk management
		if (_entryPrice.HasValue && Position != 0)
		{
			var step = Security?.PriceStep ?? 1m;
			if (step <= 0) step = 1m;
			var stopDist = 150 * step;
			var takeDist = 150 * step;

			if (Position > 0 && (candle.LowPrice <= _entryPrice.Value - stopDist || candle.HighPrice >= _entryPrice.Value + takeDist))
			{
				SellMarket();
				_entryPrice = null;
				_hasPrev = false;
				_prevJaw = jaw; _prevTeeth = teeth; _prevLips = lips;
				return;
			}
			if (Position < 0 && (candle.HighPrice >= _entryPrice.Value + stopDist || candle.LowPrice <= _entryPrice.Value - takeDist))
			{
				BuyMarket();
				_entryPrice = null;
				_hasPrev = false;
				_prevJaw = jaw; _prevTeeth = teeth; _prevLips = lips;
				return;
			}
		}

		if (_hasPrev)
		{
			// Entry: lips crosses jaw
			var buySignal = lips > jaw && _prevLips <= _prevJaw;
			var sellSignal = lips < jaw && _prevLips >= _prevJaw;

			// Exit: lips crosses teeth
			var closeLong = lips < teeth && _prevLips >= _prevTeeth;
			var closeShort = lips > teeth && _prevLips <= _prevTeeth;

			if (Position > 0 && closeLong)
			{
				SellMarket();
				_entryPrice = null;
			}
			else if (Position < 0 && closeShort)
			{
				BuyMarket();
				_entryPrice = null;
			}

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

		_prevJaw = jaw;
		_prevTeeth = teeth;
		_prevLips = lips;
		_hasPrev = true;
	}
}
