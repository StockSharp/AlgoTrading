using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA Crossover Strategy with RSI average and distance conditions.
/// </summary>
public class EmaCrossoverRsiDistanceStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaShortLength;
	private readonly StrategyParam<int> _emaMediumLength;
	private readonly StrategyParam<int> _emaLong1Length;
	private readonly StrategyParam<int> _emaLong2Length;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiAverageLength;
	private readonly StrategyParam<int> _distanceLength;

	private ExponentialMovingAverage _emaShort;
	private ExponentialMovingAverage _emaMedium;
	private ExponentialMovingAverage _emaLong1;
	private ExponentialMovingAverage _emaLong2;
	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _rsiAverage;
	private SimpleMovingAverage _distanceAverage;
	private decimal? _prevDistance4013;

	private SignalType _lastSignal;
	private enum SignalType
	{
		None,
		Long,
		Short,
		Neutral,
		Green,
		Red
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Short-term EMA length.
	/// </summary>
	public int EmaShortLength
	{
		get => _emaShortLength.Value;
		set => _emaShortLength.Value = value;
	}

	/// <summary>
	/// Medium-term EMA length.
	/// </summary>
	public int EmaMediumLength
	{
		get => _emaMediumLength.Value;
		set => _emaMediumLength.Value = value;
	}

	/// <summary>
	/// First long-term EMA length.
	/// </summary>
	public int EmaLong1Length
	{
		get => _emaLong1Length.Value;
		set => _emaLong1Length.Value = value;
	}

	/// <summary>
	/// Second long-term EMA length.
	/// </summary>
	public int EmaLong2Length
	{
		get => _emaLong2Length.Value;
		set => _emaLong2Length.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Average period for RSI.
	/// </summary>
	public int RsiAverageLength
	{
		get => _rsiAverageLength.Value;
		set => _rsiAverageLength.Value = value;
	}

	/// <summary>
	/// Averaging period for EMA distance.
	/// </summary>
	public int DistanceLength
	{
		get => _distanceLength.Value;
		set => _distanceLength.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="EmaCrossoverRsiDistanceStrategy"/>.
	/// </summary>
	public EmaCrossoverRsiDistanceStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_emaShortLength = Param(nameof(EmaShortLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("EMA Short", "Short-term EMA length", "Indicators");

		_emaMediumLength = Param(nameof(EmaMediumLength), 13)
		.SetGreaterThanZero()
		.SetDisplay("EMA Medium", "Medium-term EMA length", "Indicators");

		_emaLong1Length = Param(nameof(EmaLong1Length), 40)
		.SetGreaterThanZero()
		.SetDisplay("EMA Long1", "First long-term EMA length", "Indicators");

		_emaLong2Length = Param(nameof(EmaLong2Length), 55)
		.SetGreaterThanZero()
		.SetDisplay("EMA Long2", "Second long-term EMA length", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI calculation period", "Indicators");

		_rsiAverageLength = Param(nameof(RsiAverageLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Average", "Average period for RSI", "Indicators");

		_distanceLength = Param(nameof(DistanceLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Distance Length", "Averaging period for EMA distance", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaShort = new ExponentialMovingAverage { Length = EmaShortLength };
		_emaMedium = new ExponentialMovingAverage { Length = EmaMediumLength };
		_emaLong1 = new ExponentialMovingAverage { Length = EmaLong1Length };
		_emaLong2 = new ExponentialMovingAverage { Length = EmaLong2Length };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_rsiAverage = new SimpleMovingAverage { Length = RsiAverageLength };
		_distanceAverage = new SimpleMovingAverage { Length = DistanceLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_emaShort, _emaMedium, _emaLong1, _emaLong2, _rsi, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, _emaShort);
		DrawIndicator(area, _emaMedium);
		DrawIndicator(area, _emaLong1);
		DrawIndicator(area, _emaLong2);
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema5, decimal ema13, decimal ema40, decimal ema55, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var rsiAvgValue = _rsiAverage.Process(new DecimalIndicatorValue(_rsiAverage, rsi, candle.ServerTime));
		var distance = Math.Abs(ema5 - ema13);
		var distanceAvgValue = _distanceAverage.Process(new DecimalIndicatorValue(_distanceAverage, distance, candle.ServerTime));

		if (!rsiAvgValue.IsFormed || !distanceAvgValue.IsFormed)
		return;

		var avgRsi = rsiAvgValue.ToDecimal();
		var avgDistance = distanceAvgValue.ToDecimal();
		var distance4013 = Math.Abs(ema40 - ema13);
		var distanceCondition = !_prevDistance4013.HasValue || distance4013 > _prevDistance4013.Value;
		_prevDistance4013 = distance4013;

		var emaShortCond = ema5 > ema13;
		var emaLongCond = ema40 > ema55;
		var rsiCond = rsi > 50m && rsi > avgRsi;
		var neutralCond = distance < avgDistance || ema13 > ema5;
		var longCond = emaShortCond && emaLongCond && rsiCond && !neutralCond;
		var shortCond = ema40 > ema55;
		var greenCond = rsi > 60m;
		var redCond = rsi < 40m;

		if (candle.ClosePrice > ema5)
		{
		if (longCond && distanceCondition)
		_lastSignal = SignalType.Long;
		else if (shortCond && distanceCondition)
		_lastSignal = SignalType.Short;
		else if (neutralCond)
		_lastSignal = SignalType.Neutral;
		else if (greenCond)
		_lastSignal = SignalType.Green;
		else if (redCond)
		_lastSignal = SignalType.Red;
		}
		else
		{
		if (longCond)
		_lastSignal = SignalType.Long;
		else if (shortCond)
		_lastSignal = SignalType.Short;
		else if (greenCond)
		_lastSignal = SignalType.Green;
		else if (redCond)
		_lastSignal = SignalType.Red;
		else
		_lastSignal = SignalType.Neutral;
		}

		ExecuteSignal();
	}

	private void ExecuteSignal()
	{
		var volume = Volume + Math.Abs(Position);

		switch (_lastSignal)
		{
		case SignalType.Long:
		if (Position <= 0)
		BuyMarket(volume);
		break;
		case SignalType.Short:
		if (Position >= 0)
		SellMarket(volume);
		break;
		default:
		if (Position > 0)
		SellMarket(Position);
		else if (Position < 0)
		BuyMarket(-Position);
		break;
		}
	}
}
