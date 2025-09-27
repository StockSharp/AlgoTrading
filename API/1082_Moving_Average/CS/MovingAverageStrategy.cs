using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

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
	public enum MovingAverages
	{
		/// <summary>
		/// Simple Moving Average (SMA).
		/// </summary>
		SMA,
		/// <summary>
		/// Exponential Moving Average (EMA).
		/// </summary>
		EMA,
		/// <summary>
		/// Double Exponential Moving Average (DEMA).
		/// </summary>
		DEMA,
		/// <summary>
		/// Triple Exponential Moving Average (TEMA).
		/// </summary>
		TEMA,
		/// <summary>
		/// Weighted Moving Average (WMA).
		/// </summary>
		WMA,
		/// <summary>
		/// Volume Weighted Moving Average (VWMA).
		/// </summary>
		VWMA
	}

	public enum PriceTypes
	{
		/// <summary>
		/// Close price.
		/// </summary>
		Close,
		/// <summary>
		/// High price.
		/// </summary>
		High,
		/// <summary>
		/// Low price.
		/// </summary>
		Low,
		/// <summary>
		/// Open price.
		/// </summary>
		Open,
		/// <summary>
		/// Typical price (H+L+C)/3.
		/// </summary>
		Typical,
		/// <summary>
		/// Center price (H+L)/2.
		/// </summary>
		Center
	}

	private readonly StrategyParam<MovingAverages> _maType;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<PriceTypes> _priceType;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _fastMa;
	private IIndicator _slowMa;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MovingAverages MaType
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
	public PriceTypes PriceType
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
		_maType = Param(nameof(MaType), MovingAverages.EMA)
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

		_priceType = Param(nameof(PriceType), PriceTypes.Typical)
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
			PriceTypes.Close => candle.ClosePrice,
			PriceTypes.High => candle.HighPrice,
			PriceTypes.Open => candle.OpenPrice,
			PriceTypes.Low => candle.LowPrice,
			PriceTypes.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			PriceTypes.Center => (candle.HighPrice + candle.LowPrice) / 2m,
			_ => candle.ClosePrice
		};
	}

	private static IIndicator CreateMovingAverage(MovingAverages type, int length)
	{
		return type switch
		{
			MovingAverages.SMA => new SimpleMovingAverage { Length = length },
			MovingAverages.EMA => new ExponentialMovingAverage { Length = length },
			MovingAverages.DEMA => new DoubleExponentialMovingAverage { Length = length },
			MovingAverages.TEMA => new TripleExponentialMovingAverage { Length = length },
			MovingAverages.WMA => new WeightedMovingAverage { Length = length },
			MovingAverages.VWMA => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}
}
