using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "EA_MACD_FixedPSAR" MetaTrader expert.
/// Combines MACD histogram crossover with EMA trend filter.
/// Buys when MACD histogram goes positive and price above EMA.
/// Sells when MACD histogram goes negative and price below EMA.
/// </summary>
public class MacdFixedPsarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _trendPeriod;

	private MovingAverageConvergenceDivergence _macd;
	private readonly Queue<decimal> _macdHistory = new();
	private readonly Queue<decimal> _closeHistory = new();
	private decimal? _prevHistogram;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	public MacdFixedPsarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for MACD calculations", "General");

		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period for MACD", "Indicators");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal line smoothing period", "Indicators");

		_trendPeriod = Param(nameof(TrendPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA", "Trend filter EMA period (computed as SMA)", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHistogram = null;
		_macdHistory.Clear();
		_closeHistory.Clear();

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = FastPeriod },
			LongMa = { Length = SlowPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed)
			return;

		var close = candle.ClosePrice;

		// Track close prices for trend EMA (SMA proxy)
		_closeHistory.Enqueue(close);
		if (_closeHistory.Count > TrendPeriod)
			_closeHistory.Dequeue();

		// Compute signal line manually
		_macdHistory.Enqueue(macdValue);
		if (_macdHistory.Count > SignalPeriod)
			_macdHistory.Dequeue();

		if (_macdHistory.Count < SignalPeriod || _closeHistory.Count < TrendPeriod)
			return;

		decimal signalSum = 0;
		foreach (var v in _macdHistory)
			signalSum += v;
		var signal = signalSum / SignalPeriod;

		decimal trendSum = 0;
		foreach (var v in _closeHistory)
			trendSum += v;
		var trendEma = trendSum / TrendPeriod;

		var histogram = macdValue - signal;

		if (_prevHistogram is null)
		{
			_prevHistogram = histogram;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		var crossUp = _prevHistogram.Value <= 0 && histogram > 0;
		var crossDown = _prevHistogram.Value >= 0 && histogram < 0;

		if (crossUp && close > trendEma)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossDown && close < trendEma)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevHistogram = histogram;
	}
}
