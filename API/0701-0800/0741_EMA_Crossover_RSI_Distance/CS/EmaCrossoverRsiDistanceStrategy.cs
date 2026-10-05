using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover with RSI and distance strategy.
/// The long signal needs EMA short above EMA medium, EMA long1 above EMA long2, RSI above 50 and above its SMA, the distance
/// between EMA short and EMA medium above its average over DistanceLength, a growing distance between EMA long1 and EMA medium
/// and the close above EMA short. The short signal mirrors it. A position is opened on its signal, reversing an opposite one,
/// and closed as soon as the signal turns neutral.
/// </summary>
public class EmaCrossoverRsiDistanceStrategy : Strategy
{
	private readonly StrategyParam<int> _emaShortLength;
	private readonly StrategyParam<int> _emaMediumLength;
	private readonly StrategyParam<int> _emaLong1Length;
	private readonly StrategyParam<int> _emaLong2Length;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiAverageLength;
	private readonly StrategyParam<int> _distanceLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _rsiAverage;
	private SimpleMovingAverage _distanceAverage;
	private decimal? _prevLongDistance;

	/// <summary>
	/// Short EMA length.
	/// </summary>
	public int EmaShortLength
	{
		get => _emaShortLength.Value;
		set => _emaShortLength.Value = value;
	}

	/// <summary>
	/// Medium EMA length.
	/// </summary>
	public int EmaMediumLength
	{
		get => _emaMediumLength.Value;
		set => _emaMediumLength.Value = value;
	}

	/// <summary>
	/// First long EMA length.
	/// </summary>
	public int EmaLong1Length
	{
		get => _emaLong1Length.Value;
		set => _emaLong1Length.Value = value;
	}

	/// <summary>
	/// Second long EMA length.
	/// </summary>
	public int EmaLong2Length
	{
		get => _emaLong2Length.Value;
		set => _emaLong2Length.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Length of the RSI SMA.
	/// </summary>
	public int RsiAverageLength
	{
		get => _rsiAverageLength.Value;
		set => _rsiAverageLength.Value = value;
	}

	/// <summary>
	/// Length of the average of the short-medium EMA distance.
	/// </summary>
	public int DistanceLength
	{
		get => _distanceLength.Value;
		set => _distanceLength.Value = value;
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
	public EmaCrossoverRsiDistanceStrategy()
	{
		_emaShortLength = Param(nameof(EmaShortLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA Short", "Short EMA length", "Indicators");

		_emaMediumLength = Param(nameof(EmaMediumLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("EMA Medium", "Medium EMA length", "Indicators");

		_emaLong1Length = Param(nameof(EmaLong1Length), 40)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long 1", "First long EMA length", "Indicators");

		_emaLong2Length = Param(nameof(EmaLong2Length), 55)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long 2", "Second long EMA length", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_rsiAverageLength = Param(nameof(RsiAverageLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Average", "Length of the RSI SMA", "Indicators");

		_distanceLength = Param(nameof(DistanceLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Distance Length", "Length of the EMA distance average", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevLongDistance = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevLongDistance = null;

		var emaShort = new ExponentialMovingAverage { Length = EmaShortLength };
		var emaMedium = new ExponentialMovingAverage { Length = EmaMediumLength };
		var emaLong1 = new ExponentialMovingAverage { Length = EmaLong1Length };
		var emaLong2 = new ExponentialMovingAverage { Length = EmaLong2Length };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		_rsiAverage = new SimpleMovingAverage { Length = RsiAverageLength };
		_distanceAverage = new SimpleMovingAverage { Length = DistanceLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaShort, emaMedium, emaLong1, emaLong2, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaMedium);
			DrawIndicator(area, emaLong1);
			DrawIndicator(area, emaLong2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaShort, decimal emaMedium, decimal emaLong1, decimal emaLong2, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsiAverageValue = _rsiAverage.Process(new DecimalIndicatorValue(_rsiAverage, rsi, candle.OpenTime) { IsFinal = true });
		var distance = Math.Abs(emaShort - emaMedium);
		var distanceAverageValue = _distanceAverage.Process(new DecimalIndicatorValue(_distanceAverage, distance, candle.OpenTime) { IsFinal = true });

		var longDistance = Math.Abs(emaLong1 - emaMedium);
		var prevLongDistance = _prevLongDistance;
		_prevLongDistance = longDistance;

		if (!rsiAverageValue.IsFormed || !distanceAverageValue.IsFormed || prevLongDistance is not decimal prevDistance)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsiAverage = rsiAverageValue.GetValue<decimal>();
		var distanceAverage = distanceAverageValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		var strongTrend = distance > distanceAverage && longDistance > prevDistance;

		var longSignal = emaShort > emaMedium && emaLong1 > emaLong2 && rsi > 50m && rsi > rsiAverage && strongTrend && close > emaShort;
		var shortSignal = emaShort < emaMedium && emaLong1 < emaLong2 && rsi < 50m && rsi < rsiAverage && strongTrend && close < emaShort;

		if (longSignal)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortSignal)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}
	}
}
