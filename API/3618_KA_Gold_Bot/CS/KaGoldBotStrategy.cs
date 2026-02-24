using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "KA Gold Bot" MetaTrader expert.
/// Uses Keltner channel (EMA + ATR-based bands) with EMA crossover for entries.
/// Buys when price breaks above upper Keltner + short EMA crosses above long EMA.
/// Sells on reverse conditions.
/// </summary>
public class KaGoldBotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _keltnerPeriod;
	private readonly StrategyParam<int> _emaShortPeriod;
	private readonly StrategyParam<int> _emaLongPeriod;

	private ExponentialMovingAverage _emaKeltner;
	private ExponentialMovingAverage _emaShort;
	private ExponentialMovingAverage _emaLong;

	// Manual ATR-like range average
	private readonly Queue<decimal> _rangeQueue = new();
	private decimal _rangeSum;

	private decimal? _prevClose;
	private decimal? _prevEmaKeltner;
	private decimal? _prevRangeAvg;
	private decimal? _prevEmaShort;
	private decimal? _prevEmaLong;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int KeltnerPeriod
	{
		get => _keltnerPeriod.Value;
		set => _keltnerPeriod.Value = value;
	}

	public int EmaShortPeriod
	{
		get => _emaShortPeriod.Value;
		set => _emaShortPeriod.Value = value;
	}

	public int EmaLongPeriod
	{
		get => _emaLongPeriod.Value;
		set => _emaLongPeriod.Value = value;
	}

	public KaGoldBotStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_keltnerPeriod = Param(nameof(KeltnerPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Period", "EMA period for Keltner channel center", "Indicators");

		_emaShortPeriod = Param(nameof(EmaShortPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Short Period", "Short EMA for crossover signal", "Indicators");

		_emaLongPeriod = Param(nameof(EmaLongPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long Period", "Long EMA for crossover signal", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_emaKeltner = new ExponentialMovingAverage { Length = KeltnerPeriod };
		_emaShort = new ExponentialMovingAverage { Length = EmaShortPeriod };
		_emaLong = new ExponentialMovingAverage { Length = EmaLongPeriod };

		_rangeQueue.Clear();
		_rangeSum = 0;

		_prevClose = null;
		_prevEmaKeltner = null;
		_prevRangeAvg = null;
		_prevEmaShort = null;
		_prevEmaLong = null;

		// Use Bind with short EMA, process others manually
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaShort, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaShort);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaShortValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Process Keltner EMA manually
		var keltnerInput = new DecimalIndicatorValue(_emaKeltner, close, candle.OpenTime);
		var keltnerResult = _emaKeltner.Process(keltnerInput);
		var emaKeltnerValue = keltnerResult.IsEmpty ? 0m : keltnerResult.GetValue<decimal>();

		// Process long EMA manually
		var longInput = new DecimalIndicatorValue(_emaLong, close, candle.OpenTime);
		var longResult = _emaLong.Process(longInput);
		var emaLongValue = longResult.IsEmpty ? 0m : longResult.GetValue<decimal>();

		// Calculate range average (manual SMA of high-low)
		var range = candle.HighPrice - candle.LowPrice;
		_rangeQueue.Enqueue(range);
		_rangeSum += range;
		while (_rangeQueue.Count > KeltnerPeriod)
			_rangeSum -= _rangeQueue.Dequeue();

		var rangeAvg = _rangeQueue.Count > 0 ? _rangeSum / _rangeQueue.Count : 0;

		if (!_emaShort.IsFormed || !_emaKeltner.IsFormed || !_emaLong.IsFormed || _rangeQueue.Count < KeltnerPeriod)
		{
			_prevClose = close;
			_prevEmaKeltner = emaKeltnerValue;
			_prevRangeAvg = rangeAvg;
			_prevEmaShort = emaShortValue;
			_prevEmaLong = emaLongValue;
			return;
		}

		if (_prevClose == null || _prevEmaKeltner == null || _prevRangeAvg == null || _prevEmaShort == null || _prevEmaLong == null)
		{
			_prevClose = close;
			_prevEmaKeltner = emaKeltnerValue;
			_prevRangeAvg = rangeAvg;
			_prevEmaShort = emaShortValue;
			_prevEmaLong = emaLongValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Keltner bands
		var upper = emaKeltnerValue + rangeAvg;
		var lower = emaKeltnerValue - rangeAvg;
		var prevUpper = _prevEmaKeltner.Value + _prevRangeAvg.Value;
		var prevLower = _prevEmaKeltner.Value - _prevRangeAvg.Value;

		// Buy: price breaks above upper Keltner + close > long EMA + short EMA crosses above upper
		var buySignal = close > upper && close > emaLongValue &&
			(_prevEmaShort.Value < prevUpper && emaShortValue > upper);

		// Sell: price breaks below lower Keltner + close < long EMA + short EMA crosses below lower
		var sellSignal = close < lower && close < emaLongValue &&
			(_prevEmaShort.Value > prevLower && emaShortValue < lower);

		if (buySignal)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (sellSignal)
		{
			if (Position > 0)
				SellMarket(Position);
			if (Position >= 0)
				SellMarket(volume);
		}

		_prevClose = close;
		_prevEmaKeltner = emaKeltnerValue;
		_prevRangeAvg = rangeAvg;
		_prevEmaShort = emaShortValue;
		_prevEmaLong = emaLongValue;
	}
}
