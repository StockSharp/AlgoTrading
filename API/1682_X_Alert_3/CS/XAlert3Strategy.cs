using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alert strategy based on crossover of two moving averages.
/// Generates log messages when MA1 crosses MA2 up or down.
/// </summary>
public class XAlert3Strategy : Strategy
{
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<MovingAverageTypeEnum> _ma1Type;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<MovingAverageTypeEnum> _ma2Type;
	private readonly StrategyParam<PriceTypeEnum> _priceType;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator? _ma1;
	private IIndicator? _ma2;
	private decimal? _prevDiff1;
	private decimal? _prevDiff2;

	/// <summary>
	/// Period of the first moving average.
	/// </summary>
	public int Ma1Period
	{
		get => _ma1Period.Value;
		set => _ma1Period.Value = value;
	}

	/// <summary>
	/// Type of the first moving average.
	/// </summary>
	public MovingAverageTypeEnum Ma1Type
	{
		get => _ma1Type.Value;
		set => _ma1Type.Value = value;
	}

	/// <summary>
	/// Period of the second moving average.
	/// </summary>
	public int Ma2Period
	{
		get => _ma2Period.Value;
		set => _ma2Period.Value = value;
	}

	/// <summary>
	/// Type of the second moving average.
	/// </summary>
	public MovingAverageTypeEnum Ma2Type
	{
		get => _ma2Type.Value;
		set => _ma2Type.Value = value;
	}

	/// <summary>
	/// Source price used for calculations.
	/// </summary>
	public PriceTypeEnum PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="XAlert3Strategy"/>.
	/// </summary>
	public XAlert3Strategy()
	{
		_ma1Period = Param(nameof(Ma1Period), 1)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Length of the first moving average", "MA1")
			.SetCanOptimize(true);

		_ma1Type = Param(nameof(Ma1Type), MovingAverageTypeEnum.Simple)
			.SetDisplay("MA1 Type", "Type of the first moving average", "MA1");

		_ma2Period = Param(nameof(Ma2Period), 14)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Length of the second moving average", "MA2")
			.SetCanOptimize(true);

		_ma2Type = Param(nameof(Ma2Type), MovingAverageTypeEnum.Simple)
			.SetDisplay("MA2 Type", "Type of the second moving average", "MA2");

		_priceType = Param(nameof(PriceType), PriceTypeEnum.Median)
			.SetDisplay("Price Type", "Source price for calculations", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_ma1 = null;
		_ma2 = null;
		_prevDiff1 = null;
		_prevDiff2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma1 = CreateMovingAverage(Ma1Type, Ma1Period);
		_ma2 = CreateMovingAverage(Ma2Type, Ma2Period);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma1);
			DrawIndicator(area, _ma2);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = GetPrice(candle, PriceType);
		var ma1 = _ma1!.Process(price, candle.OpenTime, true).ToDecimal();
		var ma2 = _ma2!.Process(price, candle.OpenTime, true).ToDecimal();

		var diff = ma1 - ma2;

		if (_prevDiff1 is not null && _prevDiff2 is not null)
		{
			if (diff > 0 && _prevDiff1 > 0 && _prevDiff2 < 0)
				AddInfoLog("MA1 crossed above MA2");
			else if (diff < 0 && _prevDiff1 < 0 && _prevDiff2 > 0)
				AddInfoLog("MA1 crossed below MA2");
		}

		_prevDiff2 = _prevDiff1;
		_prevDiff1 = diff;
	}

	private static IIndicator CreateMovingAverage(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private static decimal GetPrice(ICandleMessage candle, PriceTypeEnum type)
	{
		return type switch
		{
			PriceTypeEnum.Open => candle.OpenPrice,
			PriceTypeEnum.High => candle.HighPrice,
			PriceTypeEnum.Low => candle.LowPrice,
			PriceTypeEnum.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			PriceTypeEnum.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			PriceTypeEnum.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}
}

public enum MovingAverageTypeEnum
{
	/// <summary>Simple Moving Average.</summary>
	Simple,
	/// <summary>Exponential Moving Average.</summary>
	Exponential,
	/// <summary>Smoothed Moving Average.</summary>
	Smoothed,
	/// <summary>Weighted Moving Average.</summary>
	Weighted
}

public enum PriceTypeEnum
{
	/// <summary>Close price.</summary>
	Close,
	/// <summary>Open price.</summary>
	Open,
	/// <summary>High price.</summary>
	High,
	/// <summary>Low price.</summary>
	Low,
	/// <summary>Median price (high+low)/2.</summary>
	Median,
	/// <summary>Typical price (high+low+close)/3.</summary>
	Typical,
	/// <summary>Weighted close price (high+low+close*2)/4.</summary>
	Weighted
}

