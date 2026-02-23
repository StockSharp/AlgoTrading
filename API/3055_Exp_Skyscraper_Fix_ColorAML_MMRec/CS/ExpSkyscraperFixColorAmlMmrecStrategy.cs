namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

using StockSharp.Algo.Candles;

/// <summary>
/// Simplified Skyscraper Fix + Color AML strategy using ATR-based channel and fractal dimension smoothing.
/// </summary>
public class ExpSkyscraperFixColorAmlMmrecStrategy : Strategy
{
	// Skyscraper Fix channel
	private readonly List<decimal> _trueRanges = new();
	private decimal _upperBand;
	private decimal _lowerBand;
	private int _trend;
	private int _prevTrend;

	// Color AML
	private readonly List<ICandleMessage> _candles = new();
	private readonly List<decimal> _smoothHistory = new();
	private decimal? _previousAml;
	private int? _previousColor;
	private int _amlColor;

	private decimal? _entryPrice;
	private int _barCount;

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;

		// ---- Skyscraper Fix (ATR channel) ----
		var tr = candle.HighPrice - candle.LowPrice;
		_trueRanges.Add(tr);
		while (_trueRanges.Count > 10)
			_trueRanges.RemoveAt(0);

		if (_trueRanges.Count >= 10)
		{
			decimal atrSum = 0;
			foreach (var r in _trueRanges) atrSum += r;
			var atr = atrSum / _trueRanges.Count;
			var step = atr * 0.9m;

			var mid = (candle.HighPrice + candle.LowPrice) / 2m;
			var newUpper = mid + step;
			var newLower = mid - step;

			if (_barCount > 10)
			{
				if (newUpper < _upperBand || candle.ClosePrice > _upperBand)
					_upperBand = newUpper;
				if (newLower > _lowerBand || candle.ClosePrice < _lowerBand)
					_lowerBand = newLower;
			}
			else
			{
				_upperBand = newUpper;
				_lowerBand = newLower;
			}

			_prevTrend = _trend;
			if (candle.ClosePrice > _upperBand)
				_trend = 1;
			else if (candle.ClosePrice < _lowerBand)
				_trend = -1;
		}

		// ---- Color AML (fractal dimension adaptive MA) ----
		_candles.Add(candle);
		while (_candles.Count > 64)
			_candles.RemoveAt(0);

		if (_candles.Count >= 12)
		{
			var fractal = 6;
			var lag = 7;
			var count = _candles.Count;

			var range1 = GetRange(count - fractal, fractal);
			var range2 = GetRange(count - 2 * fractal, fractal);
			var range3 = GetRange(count - 2 * fractal, 2 * fractal);

			var dim = 0d;
			if (range1 + range2 > 0m && range3 > 0m)
				dim = (Math.Log((double)(range1 + range2)) - Math.Log((double)range3)) * 1.44269504088896d;

			var alpha = Math.Exp(-lag * (dim - 1d));
			if (alpha > 1d) alpha = 1d;
			if (alpha < 0.01d) alpha = 0.01d;

			var price = (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m;
			var prevSmooth = _smoothHistory.Count > 0 ? _smoothHistory[^1] : price;
			var smooth = (decimal)alpha * price + (1m - (decimal)alpha) * prevSmooth;

			_smoothHistory.Add(smooth);
			while (_smoothHistory.Count > lag + 2)
				_smoothHistory.RemoveAt(0);

			if (_smoothHistory.Count > lag)
			{
				var lagIdx = _smoothHistory.Count - 1 - lag;
				var smoothLag = _smoothHistory[lagIdx];
				var pStep = Security?.PriceStep ?? 1m;
				var threshold = lag * lag * pStep;

				var aml = Math.Abs(smooth - smoothLag) >= threshold
					? smooth
					: _previousAml ?? smooth;

				if (_previousAml.HasValue)
				{
					if (aml > _previousAml) _amlColor = 2;
					else if (aml < _previousAml) _amlColor = 0;
				}

				_previousAml = aml;
				_previousColor = _amlColor;
			}
		}

		// ---- Risk management ----
		if (_entryPrice.HasValue && Position != 0)
		{
			var pStep = Security?.PriceStep ?? 1m;
			if (pStep <= 0) pStep = 1m;
			var stopDist = 1000 * pStep;
			var takeDist = 2000 * pStep;

			if (Position > 0)
			{
				if (candle.ClosePrice <= _entryPrice.Value - stopDist || candle.ClosePrice >= _entryPrice.Value + takeDist)
				{
					SellMarket();
					_entryPrice = null;
					return;
				}
			}
			else if (Position < 0)
			{
				if (candle.ClosePrice >= _entryPrice.Value + stopDist || candle.ClosePrice <= _entryPrice.Value - takeDist)
				{
					BuyMarket();
					_entryPrice = null;
					return;
				}
			}
		}

		if (_barCount < 15)
			return;

		// ---- Combined signals ----
		var skyBuy = _trend > 0 && _prevTrend <= 0;
		var skySell = _trend < 0 && _prevTrend >= 0;
		var amlBuy = _amlColor == 2;
		var amlSell = _amlColor == 0;

		// Exit
		if (Position > 0 && (skySell || amlSell))
		{
			SellMarket();
			_entryPrice = null;
		}
		else if (Position < 0 && (skyBuy || amlBuy))
		{
			BuyMarket();
			_entryPrice = null;
		}

		// Entry
		if (Position == 0)
		{
			if (skyBuy || amlBuy)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (skySell || amlSell)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
	}

	private decimal GetRange(int start, int length)
	{
		if (start < 0) start = 0;
		var end = Math.Min(start + length, _candles.Count);
		var max = decimal.MinValue;
		var min = decimal.MaxValue;

		for (var i = start; i < end; i++)
		{
			if (_candles[i].HighPrice > max) max = _candles[i].HighPrice;
			if (_candles[i].LowPrice < min) min = _candles[i].LowPrice;
		}

		if (max == decimal.MinValue || min == decimal.MaxValue) return 0m;
		return max - min;
	}
}
