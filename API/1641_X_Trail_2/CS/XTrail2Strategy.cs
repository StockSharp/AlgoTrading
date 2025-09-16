using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy based on X_trail_2.
/// </summary>
public class XTrail2Strategy : Strategy
{
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<MovingAverageTypeEnum> _ma1Type;
	private readonly StrategyParam<MovingAverageTypeEnum> _ma2Type;
	private readonly StrategyParam<AppliedPriceType> _ma1PriceType;
	private readonly StrategyParam<AppliedPriceType> _ma2PriceType;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _ma1;
	private IIndicator _ma2;

	private decimal? _ma1Prev;
	private decimal? _ma2Prev;
	private decimal? _ma1Prev2;
	private decimal? _ma2Prev2;

	/// <summary>
	/// Length of the first moving average.
	/// </summary>
	public int Ma1Length { get => _ma1Length.Value; set => _ma1Length.Value = value; }

	/// <summary>
	/// Length of the second moving average.
	/// </summary>
	public int Ma2Length { get => _ma2Length.Value; set => _ma2Length.Value = value; }

	/// <summary>
	/// Type of the first moving average.
	/// </summary>
	public MovingAverageTypeEnum Ma1Type { get => _ma1Type.Value; set => _ma1Type.Value = value; }

	/// <summary>
	/// Type of the second moving average.
	/// </summary>
	public MovingAverageTypeEnum Ma2Type { get => _ma2Type.Value; set => _ma2Type.Value = value; }

	/// <summary>
	/// Applied price for the first moving average.
	/// </summary>
	public AppliedPriceType Ma1PriceType { get => _ma1PriceType.Value; set => _ma1PriceType.Value = value; }

	/// <summary>
	/// Applied price for the second moving average.
	/// </summary>
	public AppliedPriceType Ma2PriceType { get => _ma2PriceType.Value; set => _ma2PriceType.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="XTrail2Strategy"/> class.
	/// </summary>
	public XTrail2Strategy()
	{
		_ma1Length = Param(nameof(Ma1Length), 1)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Length", "Length of the first MA", "Moving Averages");

		_ma2Length = Param(nameof(Ma2Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Length", "Length of the second MA", "Moving Averages");

		_ma1Type = Param(nameof(Ma1Type), MovingAverageTypeEnum.Simple)
			.SetDisplay("MA1 Type", "Type of the first MA", "Moving Averages");

		_ma2Type = Param(nameof(Ma2Type), MovingAverageTypeEnum.Simple)
			.SetDisplay("MA2 Type", "Type of the second MA", "Moving Averages");

		_ma1PriceType = Param(nameof(Ma1PriceType), AppliedPriceType.Median)
			.SetDisplay("MA1 Price", "Applied price for the first MA", "Moving Averages");

		_ma2PriceType = Param(nameof(Ma2PriceType), AppliedPriceType.Median)
			.SetDisplay("MA2 Price", "Applied price for the second MA", "Moving Averages");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ma1Prev = _ma2Prev = _ma1Prev2 = _ma2Prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_ma1 = CreateMa(Ma1Type, Ma1Length);
		_ma2 = CreateMa(Ma2Type, Ma2Length);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price1 = GetPrice(candle, Ma1PriceType);
		var price2 = GetPrice(candle, Ma2PriceType);

		var ma1 = _ma1.Process(price1, candle.OpenTime, true).ToDecimal();
		var ma2 = _ma2.Process(price2, candle.OpenTime, true).ToDecimal();

		if (_ma1Prev2 != null && _ma2Prev2 != null)
		{
			if (ma1 > ma2 && _ma1Prev > _ma2Prev && _ma1Prev2 < _ma2Prev2)
			{
				if (Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			}
			else if (ma1 < ma2 && _ma1Prev < _ma2Prev && _ma1Prev2 > _ma2Prev2)
			{
				if (Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}
		}

		_ma1Prev2 = _ma1Prev;
		_ma2Prev2 = _ma2Prev;
		_ma1Prev = ma1;
		_ma2Prev = ma2;
	}

	private static IIndicator CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private static decimal GetPrice(ICandleMessage candle, AppliedPriceType priceType)
	{
		return priceType switch
		{
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
			_ => candle.ClosePrice,
		};
	}
}

/// <summary>
/// Moving average calculation method.
/// </summary>
public enum MovingAverageTypeEnum
{
	/// <summary>Simple moving average.</summary>
	Simple,
	/// <summary>Exponential moving average.</summary>
	Exponential,
	/// <summary>Smoothed moving average.</summary>
	Smoothed,
	/// <summary>Weighted moving average.</summary>
	Weighted
}

/// <summary>
/// Price type used for indicator calculations.
/// </summary>
public enum AppliedPriceType
{
	/// <summary>Close price.</summary>
	Close,
	/// <summary>Open price.</summary>
	Open,
	/// <summary>High price.</summary>
	High,
	/// <summary>Low price.</summary>
	Low,
	/// <summary>Median price (high + low) / 2.</summary>
	Median,
	/// <summary>Typical price (high + low + close) / 3.</summary>
	Typical,
	/// <summary>Weighted price (high + low + close * 2) / 4.</summary>
	Weighted
}
