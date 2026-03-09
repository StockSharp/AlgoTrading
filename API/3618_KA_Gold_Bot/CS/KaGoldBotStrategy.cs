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
/// Buys when short EMA crosses above long EMA and close is above Keltner center.
/// Sells on reverse conditions.
/// </summary>
public class KaGoldBotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _keltnerPeriod;
	private readonly StrategyParam<int> _emaShortPeriod;
	private readonly StrategyParam<int> _emaLongPeriod;

	private ExponentialMovingAverage _emaShort;
	private ExponentialMovingAverage _emaLong;

	// Manual ATR-like range average for Keltner
	private readonly Queue<decimal> _rangeQueue = new();
	private decimal _rangeSum;
	private ExponentialMovingAverage _emaKeltner;

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

		_keltnerPeriod = Param(nameof(KeltnerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Period", "EMA period for Keltner channel center", "Indicators");

		_emaShortPeriod = Param(nameof(EmaShortPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Short Period", "Short EMA for crossover signal", "Indicators");

		_emaLongPeriod = Param(nameof(EmaLongPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long Period", "Long EMA for crossover signal", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_emaShort = new ExponentialMovingAverage { Length = EmaShortPeriod };
		_emaLong = new ExponentialMovingAverage { Length = EmaLongPeriod };
		_emaKeltner = new ExponentialMovingAverage { Length = KeltnerPeriod };

		_rangeQueue.Clear();
		_rangeSum = 0;
		_prevEmaShort = null;
		_prevEmaLong = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaShort, _emaLong, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaShort);
			DrawIndicator(area, _emaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaShortValue, decimal emaLongValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Process Keltner EMA manually
		var keltnerInput = new DecimalIndicatorValue(_emaKeltner, close, candle.OpenTime);
		var keltnerResult = _emaKeltner.Process(keltnerInput);
		var emaKeltnerValue = keltnerResult.IsEmpty ? close : keltnerResult.GetValue<decimal>();

		// Calculate range average (manual SMA of high-low) for Keltner bands
		var range = candle.HighPrice - candle.LowPrice;
		_rangeQueue.Enqueue(range);
		_rangeSum += range;
		while (_rangeQueue.Count > KeltnerPeriod)
			_rangeSum -= _rangeQueue.Dequeue();

		if (_prevEmaShort == null || _prevEmaLong == null)
		{
			_prevEmaShort = emaShortValue;
			_prevEmaLong = emaLongValue;
			return;
		}

		// Keltner bands
		var rangeAvg = _rangeQueue.Count > 0 ? _rangeSum / _rangeQueue.Count : 0;
		var upper = emaKeltnerValue + rangeAvg;
		var lower = emaKeltnerValue - rangeAvg;

		// Buy: short EMA crosses above long EMA and close above Keltner center
		var buySignal = _prevEmaShort.Value <= _prevEmaLong.Value && emaShortValue > emaLongValue
			&& close > emaKeltnerValue;

		// Sell: short EMA crosses below long EMA and close below Keltner center
		var sellSignal = _prevEmaShort.Value >= _prevEmaLong.Value && emaShortValue < emaLongValue
			&& close < emaKeltnerValue;

		if (buySignal)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (sellSignal)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevEmaShort = emaShortValue;
		_prevEmaLong = emaLongValue;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_emaShort = null;
		_emaLong = null;
		_emaKeltner = null;
		_rangeQueue.Clear();
		_rangeSum = 0;
		_prevEmaShort = null;
		_prevEmaLong = null;

		base.OnReseted();
	}
}
