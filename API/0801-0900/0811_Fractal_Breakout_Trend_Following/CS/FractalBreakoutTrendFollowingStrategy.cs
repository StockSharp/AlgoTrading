using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fractal Breakout Trend Following strategy.
/// An up fractal (a high above the two highs on each side) that lies above the Alligator teeth (SMMA 8 of the median price, shifted
/// 5 bars) arms a buy stop at its high. While flat, inside the TradeStart..TradeStop window and with the AtrPeriod average of the
/// ATR percentile rank over the last 100 bars below AtrThreshold, a candle trading through that level goes long. The stop is the
/// higher of the StopLossPercent (a fraction) stop below the entry and the latest down fractal that lies below the teeth.
/// </summary>
public class FractalBreakoutTrendFollowingStrategy : Strategy
{
	private const int _teethLength = 8;
	private const int _teethShift = 5;
	private const int _percentileLookback = 100;

	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _atrThreshold;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _tradeStart;
	private readonly StrategyParam<DateTimeOffset> _tradeStop;

	private readonly List<decimal> _highs = [];
	private readonly List<decimal> _lows = [];
	private readonly List<decimal> _teethSource = [];
	private readonly List<decimal> _atrValues = [];
	private readonly List<decimal> _percentiles = [];
	private SmoothedMovingAverage _smma;
	private decimal? _buyLevel;
	private decimal? _downFractal;
	private decimal _entryPrice;

	/// <summary>
	/// Stop loss distance as a fraction of the entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Maximum averaged ATR percentile for entries.
	/// </summary>
	public decimal AtrThreshold
	{
		get => _atrThreshold.Value;
		set => _atrThreshold.Value = value;
	}

	/// <summary>
	/// ATR period, also the averaging period of its percentile.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Start of the trading window.
	/// </summary>
	public DateTimeOffset TradeStart
	{
		get => _tradeStart.Value;
		set => _tradeStart.Value = value;
	}

	/// <summary>
	/// End of the trading window.
	/// </summary>
	public DateTimeOffset TradeStop
	{
		get => _tradeStop.Value;
		set => _tradeStop.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FractalBreakoutTrendFollowingStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 0.03m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance as a fraction of the entry price", "Risk");

		_atrThreshold = Param(nameof(AtrThreshold), 50m)
			.SetDisplay("ATR Threshold", "Maximum averaged ATR percentile for entries", "Volatility");

		_atrPeriod = Param(nameof(AtrPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period, also the averaging period of its percentile", "Volatility");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_tradeStart = Param(nameof(TradeStart), new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Trade Start", "Start of the trading window", "Time");

		_tradeStop = Param(nameof(TradeStop), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Trade Stop", "End of the trading window", "Time");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
		_smma = null;
	}

	private void ResetState()
	{
		_highs.Clear();
		_lows.Clear();
		_teethSource.Clear();
		_atrValues.Clear();
		_percentiles.Clear();
		_buyLevel = null;
		_downFractal = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();
		_smma = new SmoothedMovingAverage { Length = _teethLength };

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var smma = _smma.Process(median, candle.OpenTime, true);

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		_teethSource.Add(_smma.IsFormed ? smma.ToDecimal() : 0m);
		Trim(_highs, 5);
		Trim(_lows, 5);
		Trim(_teethSource, _teethShift + 3);

		var avgPercentile = UpdatePercentile(atr);

		UpdateFractals();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			var stop = _entryPrice * (1 - StopLossPercent);
			if (_downFractal is decimal fractalStop && fractalStop > stop)
				stop = fractalStop;

			if (candle.LowPrice <= stop)
				SellMarket(Position);

			return;
		}

		if (Position < 0 || _buyLevel is not decimal level || avgPercentile is not decimal percentile)
			return;

		var time = candle.OpenTime;
		if (time < TradeStart.UtcDateTime || time >= TradeStop.UtcDateTime)
			return;

		if (percentile < AtrThreshold && candle.HighPrice >= level)
		{
			_entryPrice = Math.Max(level, candle.OpenPrice);
			_buyLevel = null;
			BuyMarket(Volume);
		}
	}

	private static void Trim(List<decimal> list, int size)
	{
		while (list.Count > size)
			list.RemoveAt(0);
	}

	private decimal? UpdatePercentile(decimal atr)
	{
		var rank = 0m;
		var hasHistory = _atrValues.Count >= _percentileLookback;

		if (hasHistory)
		{
			var below = 0;
			foreach (var value in _atrValues)
			{
				if (value < atr)
					below++;
			}

			rank = 100m * below / _atrValues.Count;
		}

		_atrValues.Add(atr);
		Trim(_atrValues, _percentileLookback);

		if (!hasHistory)
			return null;

		_percentiles.Add(rank);
		Trim(_percentiles, AtrPeriod);

		if (_percentiles.Count < AtrPeriod)
			return null;

		var sum = 0m;
		foreach (var value in _percentiles)
			sum += value;

		return sum / _percentiles.Count;
	}

	private void UpdateFractals()
	{
		if (_highs.Count < 5 || _teethSource.Count < _teethShift + 3)
			return;

		// The middle of the last five bars is the fractal bar; the teeth on it are the SMMA from 5 bars earlier.
		var teeth = _teethSource[_teethSource.Count - 3 - _teethShift];
		if (teeth <= 0)
			return;

		var high = _highs[2];
		if (high > _highs[0] && high > _highs[1] && high > _highs[3] && high > _highs[4] && high > teeth)
			_buyLevel = high;

		var low = _lows[2];
		if (low < _lows[0] && low < _lows[1] && low < _lows[3] && low < _lows[4] && low < teeth)
			_downFractal = low;
	}
}
