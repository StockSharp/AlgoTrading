using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "OsMA Four Colors Arrow" MetaTrader expert.
/// Uses MACD histogram (OsMA) zero-crossing as entry signal.
/// Buys when histogram crosses above zero, sells when it crosses below.
/// </summary>
public class OsMaFourColorsArrowStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;

	private MovingAverageConvergenceDivergence _macd;
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

	public OsMaFourColorsArrowStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signal generation", "General");

		_fastPeriod = Param(nameof(FastPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length for MACD", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length for MACD", "Indicators");

		_signalPeriod = Param(nameof(SignalPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal line smoothing period", "Indicators");
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

		// Compute signal line as SMA of MACD values
		_macdHistory.Enqueue(macdValue);
		if (_macdHistory.Count > SignalPeriod)
			_macdHistory.Dequeue();

		if (_macdHistory.Count < SignalPeriod)
		{
			_prevHistogram = null;
			return;
		}

		decimal sum = 0;
		var history = _macdHistory.ToArray();
		foreach (var v in history)
			sum += v;
		var signal = sum / history.Length;

		// OsMA = MACD - Signal (histogram)
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

		if (crossUp)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		else if (crossDown)
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
		_prevHistogram = null;
		_macdHistory.Clear();

		base.OnReseted();
	}
}
