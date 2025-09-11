using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy.
/// Enters long when the fast moving average crosses above the slow moving average.
/// Closes the position when the fast average crosses back below the slow one.
/// </summary>
public class MovingAverageStrategy : Strategy
{
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<PriceTypeEnum> _priceType;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator? _fastMa;
	private IIndicator? _slowMa;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Short moving average length.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Long moving average length.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Price type for calculations.
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
	/// Initializes <see cref="MovingAverageStrategy"/>.
	/// </summary>
	public MovingAverageStrategy()
	{
		_maType = Param(nameof(MaType), MovingAverageTypeEnum.EMA)
			.SetDisplay("MA Type", "Moving average type", "Parameters");

		_shortLength = Param(nameof(ShortLength), 1)
			.SetGreaterThanZero()
			.SetDisplay("Short MA Length", "Length of short moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 50, 1);

		_longLength = Param(nameof(LongLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Long MA Length", "Length of long moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 5);

		_priceType = Param(nameof(PriceType), PriceTypeEnum.Typical)
			.SetDisplay("Price Type", "Source price for averages", "Parameters");

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

		_fastMa = null;
		_slowMa = null;
		_prevFast = 0m;
		_prevSlow = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = CreateMovingAverage(MaType, ShortLength);
		_slowMa = CreateMovingAverage(MaType, LongLength);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = GetPrice(candle);
		var fast = _fastMa!.Process(price, candle.OpenTime, true).ToDecimal();
		var slow = _slowMa!.Process(price, candle.OpenTime, true).ToDecimal();

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isInitialized = true;
			return;
		}

		var wasFastLess = _prevFast < _prevSlow;
		var isFastLess = fast < slow;

		if (wasFastLess && !isFastLess && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (!wasFastLess && isFastLess && Position > 0)
			SellMarket(Position);

		_prevFast = fast;
		_prevSlow = slow;
	}

	private decimal GetPrice(ICandleMessage candle)
	{
		return PriceType switch
		{
			PriceTypeEnum.Close => candle.ClosePrice,
			PriceTypeEnum.High => candle.HighPrice,
			PriceTypeEnum.Open => candle.OpenPrice,
			PriceTypeEnum.Low => candle.LowPrice,
			PriceTypeEnum.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			PriceTypeEnum.Center => (candle.HighPrice + candle.LowPrice) / 2m,
			_ => candle.ClosePrice
		};
	}

	private static IIndicator CreateMovingAverage(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.SMA => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.EMA => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.DEMA => new DoubleExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.TEMA => new TripleExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.WMA => new WeightedMovingAverage { Length = length },
			MovingAverageTypeEnum.VWMA => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}
}

public enum MovingAverageTypeEnum
{
	SMA,
	EMA,
	DEMA,
	TEMA,
	WMA,
	VWMA
}

public enum PriceTypeEnum
{
	Close,
	High,
	Open,
	Low,
	Typical,
	Center
}

