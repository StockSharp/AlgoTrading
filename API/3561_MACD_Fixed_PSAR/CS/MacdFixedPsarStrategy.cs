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
	private ExponentialMovingAverage _trendEma;
	private readonly Queue<decimal> _macdHistory = new();
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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for MACD calculations", "General");

		_fastPeriod = Param(nameof(FastPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period for MACD", "Indicators");

		_signalPeriod = Param(nameof(SignalPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal line smoothing period", "Indicators");

		_trendPeriod = Param(nameof(TrendPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA", "Trend filter EMA period (computed as SMA)", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHistogram = null;
		_macdHistory.Clear();

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = FastPeriod },
			LongMa = { Length = SlowPeriod },
		};
		_trendEma = new ExponentialMovingAverage { Length = TrendPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, _trendEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _trendEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal trendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed || !_trendEma.IsFormed)
			return;

		var close = candle.ClosePrice;

		// Compute signal line manually
		_macdHistory.Enqueue(macdValue);
		if (_macdHistory.Count > SignalPeriod)
			_macdHistory.Dequeue();

		if (_macdHistory.Count < SignalPeriod)
			return;

		decimal signalSum = 0;
		var history = _macdHistory.ToArray();
		foreach (var v in history)
			signalSum += v;
		var signal = signalSum / history.Length;

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

		if (crossUp && close > trendValue)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		else if (crossDown && close < trendValue)
		{
			if (Position >= 0)
				SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
		}

		_prevHistogram = histogram;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_macd = null;
		_trendEma = null;
		_prevHistogram = null;
		_macdHistory.Clear();

		base.OnReseted();
	}
}
