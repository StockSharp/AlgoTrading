using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "VIDYA N Bars Borders Martingale" MetaTrader expert.
/// Uses EMA as adaptive MA proxy and a range-based channel from recent N bars.
/// Buys when price closes below lower band, sells when above upper band.
/// Includes simple martingale volume increase on losing trades.
/// </summary>
public class VidyaNBarsBordersMartingaleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<decimal> _martingaleMultiplier;

	private ExponentialMovingAverage _ema;
	private readonly Queue<decimal> _highHistory = new();
	private readonly Queue<decimal> _lowHistory = new();
	private decimal _currentVolume;
	private decimal _entryPrice;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int RangePeriod
	{
		get => _rangePeriod.Value;
		set => _rangePeriod.Value = value;
	}

	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	public VidyaNBarsBordersMartingaleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Trading candle type", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Smoothing period for adaptive MA proxy", "Indicators");

		_rangePeriod = Param(nameof(RangePeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Range Period", "Number of bars for high/low range channel", "Indicators");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 1.5m)
			.SetDisplay("Martingale Multiplier", "Volume multiplier after losing trade", "Risk");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_highHistory.Clear();
		_lowHistory.Clear();
		_currentVolume = Volume > 0 ? Volume : 1;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track high/low for range calculation
		_highHistory.Enqueue(candle.HighPrice);
		_lowHistory.Enqueue(candle.LowPrice);

		if (_highHistory.Count > RangePeriod)
		{
			_highHistory.Dequeue();
			_lowHistory.Dequeue();
		}

		if (!_ema.IsFormed || _highHistory.Count < RangePeriod)
			return;

		// Compute range from recent bars
		decimal highest = decimal.MinValue;
		decimal lowest = decimal.MaxValue;
		foreach (var h in _highHistory)
			if (h > highest) highest = h;
		foreach (var l in _lowHistory)
			if (l < lowest) lowest = l;

		var range = (highest - lowest) * 0.5m;
		if (range <= 0)
			return;

		var upper = emaValue + range;
		var lower = emaValue - range;
		var close = candle.ClosePrice;

		var vol = _currentVolume;

		if (close < lower)
		{
			// Price below lower band -> buy signal
			if (Position < 0)
			{
				var wasLoss = close > _entryPrice;
				BuyMarket(Math.Abs(Position));
				if (wasLoss)
					_currentVolume = Math.Min(_currentVolume * MartingaleMultiplier, 100);
				else
					_currentVolume = Volume > 0 ? Volume : 1;
			}

			if (Position <= 0)
			{
				BuyMarket(vol);
				_entryPrice = close;
			}
		}
		else if (close > upper)
		{
			// Price above upper band -> sell signal
			if (Position > 0)
			{
				var wasLoss = close < _entryPrice;
				SellMarket(Position);
				if (wasLoss)
					_currentVolume = Math.Min(_currentVolume * MartingaleMultiplier, 100);
				else
					_currentVolume = Volume > 0 ? Volume : 1;
			}

			if (Position >= 0)
			{
				SellMarket(vol);
				_entryPrice = close;
			}
		}
	}
}
