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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader 5 expert "BandOsMACustom.mq5" (folder MQL/45596).
/// Combines the MACD histogram (OsMA) with Bollinger Bands drawn over the histogram itself
/// and an additional moving average filter to detect momentum reversals.
/// </summary>
public class BandOsMaCustomStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastOsmaPeriod;
	private readonly StrategyParam<int> _slowOsmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<AppliedPriceTypes> _appliedPrice;
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<int> _bandsShift;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethods> _maMethod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _orderVolume;

	private MovingAverageConvergenceDivergenceHistogram _osma;
	private BollingerBands _osmaBands;
	private LengthIndicator<decimal> _osmaAverage;

	private readonly List<decimal> _osmaHistory = new();
	private readonly List<decimal> _upperHistory = new();
	private readonly List<decimal> _lowerHistory = new();
	private readonly List<decimal> _maHistory = new();

	private int _historyCapacity;
	private int _signalDirection;
	private decimal _pointValue;

	/// <summary>
	/// Primary candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period employed by the MACD histogram (OsMA).
	/// </summary>
	public int FastOsmaPeriod
	{
		get => _fastOsmaPeriod.Value;
		set => _fastOsmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period employed by the MACD histogram (OsMA).
	/// </summary>
	public int SlowOsmaPeriod
	{
		get => _slowOsmaPeriod.Value;
		set => _slowOsmaPeriod.Value = value;
	}

	/// <summary>
	/// Signal SMA period employed by the MACD histogram (OsMA).
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Applied price mapping that mirrors the MetaTrader PRICE_* constants.
	/// </summary>
	public AppliedPriceTypes AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Period of the Bollinger Bands calculated on the OsMA values.
	/// </summary>
	public int BandsPeriod
	{
		get => _bandsPeriod.Value;
		set => _bandsPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal shift (in bars) applied to the Bollinger Bands series.
	/// </summary>
	public int BandsShift
	{
		get => _bandsShift.Value;
		set => _bandsShift.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for the Bollinger Bands drawn on OsMA.
	/// </summary>
	public decimal BandsDeviation
	{
		get => _bandsDeviation.Value;
		set => _bandsDeviation.Value = value;
	}

	/// <summary>
	/// Length of the moving average that filters exit signals.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal shift (in bars) applied to the exit moving average.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average calculation method (SMA, EMA, SMMA, LWMA).
	/// </summary>
	public MovingAverageMethods MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Order volume that mirrors the MetaTrader Lots input.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Creates the strategy with defaults copied from the MetaTrader expert.
	/// </summary>
	public BandOsMaCustomStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used by the strategy", "General");

		_fastOsmaPeriod = Param(nameof(FastOsmaPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("Fast OsMA", "Fast EMA period for the MACD histogram", "Indicators")
		.SetCanOptimize(true);

		_slowOsmaPeriod = Param(nameof(SlowOsmaPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("Slow OsMA", "Slow EMA period for the MACD histogram", "Indicators")
		.SetCanOptimize(true);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("Signal", "Signal SMA period for the MACD histogram", "Indicators")
		.SetCanOptimize(true);

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceTypes.Typical)
		.SetDisplay("Applied Price", "Which candle price feeds the OsMA", "Indicators")
		.SetCanOptimize(true);

		_bandsPeriod = Param(nameof(BandsPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("Bands Period", "Length of the Bollinger Bands on the OsMA", "Indicators")
		.SetCanOptimize(true);

		_bandsShift = Param(nameof(BandsShift), 0)
		.SetDisplay("Bands Shift", "Bar shift applied to the Bollinger values", "Indicators")
		.SetCanOptimize(true);

		_bandsDeviation = Param(nameof(BandsDeviation), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bands Deviation", "Standard deviation multiplier for the bands", "Indicators")
		.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of the exit moving average", "Indicators")
		.SetCanOptimize(true);

		_maShift = Param(nameof(MaShift), 0)
		.SetDisplay("MA Shift", "Bar shift applied to the exit moving average", "Indicators")
		.SetCanOptimize(true);

		_maMethod = Param(nameof(MaMethod), MovingAverageMethods.Simple)
		.SetDisplay("MA Method", "Calculation method for the exit average", "Indicators")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (points)", "Stop distance expressed in price steps", "Risk")
		.SetCanOptimize(true);

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Trade volume that matches the Lots input", "General")
		.SetCanOptimize(true);
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

		_osma = null;
		_osmaBands = null;
		_osmaAverage = null;

		_osmaHistory.Clear();
		_upperHistory.Clear();
		_lowerHistory.Clear();
		_maHistory.Clear();

		_historyCapacity = 0;
		_signalDirection = 0;
		_pointValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 0m;
		Volume = OrderVolume;

		var stopLossUnit = CreateUnit(StopLossPoints);
		Unit trailingUnit = null;
		Unit trailingStepUnit = null;

		if (stopLossUnit != null)
		{
			trailingUnit = stopLossUnit;
			var trailingStep = StopLossPoints / 50m;
			if (trailingStep > 0m)
			{
				trailingStepUnit = CreateUnit(trailingStep);
			}
		}

		if (stopLossUnit != null)
		{
			StartProtection(
			stopLoss: stopLossUnit,
			trailingStop: trailingUnit,
			trailingStep: trailingStepUnit,
			useMarketOrders: true);
		}

		_osma = new MovingAverageConvergenceDivergenceHistogram
		{
			Macd =
			{
				ShortMa = { Length = FastOsmaPeriod },
				LongMa = { Length = SlowOsmaPeriod }
			},
			SignalMa = { Length = SignalPeriod }
		};

		_osmaBands = new BollingerBands
		{
			Length = BandsPeriod,
			Width = BandsDeviation
		};

		_osmaAverage = CreateMovingAverage(MaMethod, MaPeriod);

		_historyCapacity = Math.Max(4, Math.Max(Math.Max(BandsShift, MaShift), 0) + 3);

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenCandlesFinished(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_osma == null || _osmaBands == null || _osmaAverage == null)
		return;

		var price = GetAppliedPrice(candle);
		var osmaValue = _osma.Process(new DecimalIndicatorValue(_osma, price, candle.OpenTime, true));
		if (!osmaValue.IsFinal)
		return;

		var histogram = (MovingAverageConvergenceDivergenceHistogramValue)osmaValue;
		if (histogram.Macd is not decimal osma)
		return;

		var bandsValue = _osmaBands.Process(new DecimalIndicatorValue(_osmaBands, osma, candle.OpenTime, true));
		var maValue = _osmaAverage.Process(new DecimalIndicatorValue(_osmaAverage, osma, candle.OpenTime, true));

		if (!bandsValue.IsFinal || !maValue.IsFinal)
		return;

		var bands = (BollingerBandsValue)bandsValue;
		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower)
		return;

		var ma = maValue.GetValue<decimal>();

		AppendValue(_osmaHistory, osma);
		AppendValue(_upperHistory, upper);
		AppendValue(_lowerHistory, lower);
		AppendValue(_maHistory, ma);

		var currentOsma = GetShiftedValue(_osmaHistory, 0, 0);
		var previousOsma = GetShiftedValue(_osmaHistory, 0, 1);
		var currentLower = GetShiftedValue(_lowerHistory, BandsShift, 0);
		var previousLower = GetShiftedValue(_lowerHistory, BandsShift, 1);
		var currentUpper = GetShiftedValue(_upperHistory, BandsShift, 0);
		var previousUpper = GetShiftedValue(_upperHistory, BandsShift, 1);
		var currentMa = GetShiftedValue(_maHistory, MaShift, 0);
		var previousMa = GetShiftedValue(_maHistory, MaShift, 1);

		if (!currentOsma.HasValue || !previousOsma.HasValue || !currentLower.HasValue || !previousLower.HasValue ||
		!currentUpper.HasValue || !previousUpper.HasValue || !currentMa.HasValue || !previousMa.HasValue)
		return;

		var osmaNow = currentOsma.Value;
		var osmaPrev = previousOsma.Value;
		var lowerNow = currentLower.Value;
		var lowerPrev = previousLower.Value;
		var upperNow = currentUpper.Value;
		var upperPrev = previousUpper.Value;
		var maNow = currentMa.Value;
		var maPrev = previousMa.Value;

		if (_signalDirection > 0 && osmaNow >= maNow && osmaPrev < maPrev)
		{
			_signalDirection = 0;
		}
		else if (_signalDirection < 0 && osmaNow <= maNow && osmaPrev > maPrev)
		{
			_signalDirection = 0;
		}

		if (osmaNow <= lowerNow && osmaPrev > lowerPrev)
		{
			_signalDirection = 1;
		}
		else if (osmaNow >= upperNow && osmaPrev < upperPrev)
		{
			_signalDirection = -1;
		}

		ExecuteTrades();
	}

	private void ExecuteTrades()
	{
		var signal = _signalDirection;
		var needLong = signal == 1;
		var needShort = signal == -1;

		if (Position > 0)
		{
			if (!needLong)
			ClosePosition();
			return;
		}

		if (Position < 0)
		{
			if (!needShort)
			ClosePosition();
			return;
		}

		if (needLong)
		{
			BuyMarket(Volume);
		}
		else if (needShort)
		{
			SellMarket(Volume);
		}
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPriceTypes.Close => candle.ClosePrice,
			AppliedPriceTypes.Open => candle.OpenPrice,
			AppliedPriceTypes.High => candle.HighPrice,
			AppliedPriceTypes.Low => candle.LowPrice,
			AppliedPriceTypes.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceTypes.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceTypes.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private void AppendValue(List<decimal> series, decimal value)
	{
		series.Add(value);
		if (series.Count > _historyCapacity)
		{
			var remove = series.Count - _historyCapacity;
			series.RemoveRange(0, remove);
		}
	}

	private decimal? GetShiftedValue(List<decimal> series, int shift, int lookback)
	{
		if (shift < 0)
		shift = 0;

		var index = series.Count - 1 - shift - lookback;
		if (index < 0 || index >= series.Count)
		return null;

		return series[index];
	}

	private Unit CreateUnit(decimal distanceInPoints)
	{
		if (distanceInPoints <= 0m || _pointValue <= 0m)
		return null;

		return new Unit(distanceInPoints * _pointValue, UnitTypes.Absolute);
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethods method, int length)
	{
		return method switch
		{
			MovingAverageMethods.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethods.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethods.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethods.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Enum that mirrors the MetaTrader MODE_* applied price constants.
	/// </summary>
	public enum AppliedPriceTypes
	{
		Close = 0,
		Open = 1,
		High = 2,
		Low = 3,
		Median = 4,
		Typical = 5,
		Weighted = 6
	}

	/// <summary>
	/// Enum that mirrors the MetaTrader moving average methods.
	/// </summary>
	public enum MovingAverageMethods
	{
		Simple = 0,
		Exponential = 1,
		Smoothed = 2,
		LinearWeighted = 3
	}
}

