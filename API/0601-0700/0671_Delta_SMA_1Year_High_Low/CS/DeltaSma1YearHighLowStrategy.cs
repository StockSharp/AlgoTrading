using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Delta SMA 1-year high/low strategy.
/// The candle volume delta is buy minus sell volume (the candle volume signed by its direction when the split is unknown),
/// smoothed by an SMA. Its high and low are tracked over the last year of available history.
/// After the delta SMA has been below 70% of that low, a cross above zero opens a long.
/// After the delta SMA has risen above 70% of the high, a drop below 60% of the high closes the long.
/// </summary>
public class DeltaSma1YearHighLowStrategy : Strategy
{
	private static readonly TimeSpan _window = TimeSpan.FromDays(365);

	private readonly StrategyParam<int> _deltaSmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _deltaSma;
	private readonly LinkedList<(DateTime time, decimal value)> _maxQueue = new();
	private readonly LinkedList<(DateTime time, decimal value)> _minQueue = new();
	private decimal? _prevDeltaSma;
	private bool _wasLow;
	private bool _wasHigh;

	/// <summary>
	/// SMA length of the volume delta.
	/// </summary>
	public int DeltaSmaLength
	{
		get => _deltaSmaLength.Value;
		set => _deltaSmaLength.Value = value;
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
	/// Constructor.
	/// </summary>
	public DeltaSma1YearHighLowStrategy()
	{
		_deltaSmaLength = Param(nameof(DeltaSmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Delta SMA Length", "SMA length of the volume delta", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
	}

	private void ResetState()
	{
		_maxQueue.Clear();
		_minQueue.Clear();
		_prevDeltaSma = null;
		_wasLow = false;
		_wasHigh = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();
		_deltaSma = new SimpleMovingAverage { Length = DeltaSmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var delta = candle.BuyVolume is decimal buy && candle.SellVolume is decimal sell
			? buy - sell
			: candle.ClosePrice > candle.OpenPrice ? candle.TotalVolume
			: candle.ClosePrice < candle.OpenPrice ? -candle.TotalVolume
			: 0m;

		var smaValue = _deltaSma.Process(delta, candle.ServerTime, true);
		if (!smaValue.IsFormed)
			return;

		var deltaSma = smaValue.GetValue<decimal>();
		var time = candle.OpenTime;

		Push(_maxQueue, time, deltaSma, true);
		Push(_minQueue, time, deltaSma, false);

		var yearHigh = _maxQueue.First.Value.value;
		var yearLow = _minQueue.First.Value.value;

		var prev = _prevDeltaSma;
		_prevDeltaSma = deltaSma;

		if (deltaSma < yearLow * 0.7m)
			_wasLow = true;

		if (Position > 0 && deltaSma > yearHigh * 0.7m)
			_wasHigh = true;

		if (prev is not decimal prevDeltaSma)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0 && _wasLow && prevDeltaSma <= 0m && deltaSma > 0m)
		{
			BuyMarket();
			_wasLow = false;
			_wasHigh = false;
		}
		else if (Position > 0 && _wasHigh && deltaSma < yearHigh * 0.6m)
		{
			SellMarket(Position);
			_wasHigh = false;
		}
	}

	private static void Push(LinkedList<(DateTime time, decimal value)> queue, DateTime time, decimal value, bool isMax)
	{
		// Monotonic queue: the front always holds the extreme of the window.
		while (queue.Last != null && (isMax ? queue.Last.Value.value <= value : queue.Last.Value.value >= value))
			queue.RemoveLast();

		queue.AddLast((time, value));

		while (queue.First != null && queue.First.Value.time <= time - _window)
			queue.RemoveFirst();
	}
}
