using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dskyz (DAFE) MAtrix with ATR-Powered Precision strategy.
/// </summary>
public class DskyzDafeMatrixAtrPrecisionStrategy : Strategy
{
	private readonly StrategyParam<MovingAverageType> _fastMaType;
	private readonly StrategyParam<MovingAverageType> _slowMaType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _volatilityThreshold;
	private readonly StrategyParam<int> _tradingStartHour;
	private readonly StrategyParam<int> _tradingEndHour;
	private readonly StrategyParam<decimal> _trailOffset;
	private readonly StrategyParam<decimal> _profitTargetAtrMult;
	private readonly StrategyParam<decimal> _fixedStopMultiplier;
	private readonly StrategyParam<int> _tradeQuantity;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _fastMa;
	private IIndicator _slowMa;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _volumeSma;
	private SimpleMovingAverage _fastMa15;
	private SimpleMovingAverage _slowMa15;

	private int _trend15m;
	private decimal _entryPrice;
	private decimal _trailingStop;

	/// <summary>
	/// Moving average type selection.
	/// </summary>
	public enum MovingAverageType
	{
		SMA,
		EMA,
		SMMA,
		HMA,
		TEMA,
		WMA,
		VWMA,
		ZLEMA,
		ALMA,
		KAMA,
		DEMA
	}

	/// <summary>
	/// Initializes <see cref="DskyzDafeMatrixAtrPrecisionStrategy"/>.
	/// </summary>
	public DskyzDafeMatrixAtrPrecisionStrategy()
	{
		_fastMaType = Param(nameof(FastMaType), MovingAverageType.SMA)
						  .SetDisplay("Fast MA Type", "Type of fast moving average", "Moving Averages");

		_slowMaType = Param(nameof(SlowMaType), MovingAverageType.SMA)
						  .SetDisplay("Slow MA Type", "Type of slow moving average", "Moving Averages");

		_fastLength = Param(nameof(FastLength), 9)
						  .SetDisplay("Fast MA Length", "Length of fast moving average", "Moving Averages")
						  .SetCanOptimize(true)
						  .SetOptimize(5, 20, 1);

		_slowLength = Param(nameof(SlowLength), 19)
						  .SetDisplay("Slow MA Length", "Length of slow moving average", "Moving Averages")
						  .SetCanOptimize(true)
						  .SetOptimize(10, 40, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
						 .SetDisplay("ATR Period", "Period for ATR calculation", "Risk Management")
						 .SetCanOptimize(true)
						 .SetOptimize(7, 28, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
							 .SetDisplay("ATR Multiplier", "ATR multiplier for price filter", "Risk Management")
							 .SetCanOptimize(true)
							 .SetOptimize(1m, 3m, 0.1m);

		_useTrendFilter =
			Param(nameof(UseTrendFilter), true).SetDisplay("Use Trend Filter", "Enable 15m trend filter", "Filters");

		_minVolume = Param(nameof(MinVolume), 10m).SetDisplay("Minimum Volume", "Minimum candle volume", "Filters");

		_volatilityThreshold = Param(nameof(VolatilityThreshold), 0.01m)
								   .SetDisplay("Volatility Threshold", "Maximum ATR/Close ratio", "Filters");

		_tradingStartHour = Param(nameof(TradingStartHour), 9)
								.SetDisplay("Trading Start Hour", "Allowed trading start hour", "Filters");

		_tradingEndHour =
			Param(nameof(TradingEndHour), 16).SetDisplay("Trading End Hour", "Allowed trading end hour", "Filters");

		_trailOffset = Param(nameof(TrailOffset), 0.5m)
						   .SetDisplay("Trailing Stop ATR Mult", "ATR multiplier for trailing stop", "Risk Management");

		_profitTargetAtrMult =
			Param(nameof(ProfitTargetAtrMult), 1.2m)
				.SetDisplay("Profit Target ATR Mult", "ATR multiplier for profit target", "Risk Management");

		_fixedStopMultiplier =
			Param(nameof(FixedStopMultiplier), 1.3m)
				.SetDisplay("Fixed Stop ATR Mult", "ATR multiplier for fixed stop", "Risk Management");

		_tradeQuantity =
			Param(nameof(TradeQuantity), 2).SetDisplay("Trade Quantity", "Fixed quantity per trade", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles used", "General");
	}

	/// <summary>
	/// Fast MA type.
	/// </summary>
	public MovingAverageType FastMaType
	{
		get => _fastMaType.Value;
		set => _fastMaType.Value = value;
	}

	/// <summary>
	/// Slow MA type.
	/// </summary>
	public MovingAverageType SlowMaType
	{
		get => _slowMaType.Value;
		set => _slowMaType.Value = value;
	}

	/// <summary>
	/// Fast MA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow MA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for price filter.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Use 15m trend filter.
	/// </summary>
	public bool UseTrendFilter
	{
		get => _useTrendFilter.Value;
		set => _useTrendFilter.Value = value;
	}

	/// <summary>
	/// Minimum volume per candle.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed ATR/Close ratio.
	/// </summary>
	public decimal VolatilityThreshold
	{
		get => _volatilityThreshold.Value;
		set => _volatilityThreshold.Value = value;
	}

	/// <summary>
	/// Allowed trading start hour.
	/// </summary>
	public int TradingStartHour
	{
		get => _tradingStartHour.Value;
		set => _tradingStartHour.Value = value;
	}

	/// <summary>
	/// Allowed trading end hour.
	/// </summary>
	public int TradingEndHour
	{
		get => _tradingEndHour.Value;
		set => _tradingEndHour.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing stop.
	/// </summary>
	public decimal TrailOffset
	{
		get => _trailOffset.Value;
		set => _trailOffset.Value = value;
	}

	/// <summary>
	/// ATR multiplier for profit target.
	/// </summary>
	public decimal ProfitTargetAtrMult
	{
		get => _profitTargetAtrMult.Value;
		set => _profitTargetAtrMult.Value = value;
	}

	/// <summary>
	/// ATR multiplier for fixed stop.
	/// </summary>
	public decimal FixedStopMultiplier
	{
		get => _fixedStopMultiplier.Value;
		set => _fixedStopMultiplier.Value = value;
	}

	/// <summary>
	/// Quantity per trade.
	/// </summary>
	public int TradeQuantity
	{
		get => _tradeQuantity.Value;
		set => _tradeQuantity.Value = value;
	}

	/// <summary>
	/// Type of candles used.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, TimeSpan.FromMinutes(15).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_trend15m = 0;
		_entryPrice = 0;
		_trailingStop = 0;
		_volumeSma = new SimpleMovingAverage { Length = 10 };
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = CreateMovingAverage(FastMaType, FastLength);
		_slowMa = CreateMovingAverage(SlowMaType, SlowLength);
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_volumeSma = new SimpleMovingAverage { Length = 10 };
		_fastMa15 = new SimpleMovingAverage { Length = FastLength };
		_slowMa15 = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, _slowMa, _atr, ProcessCandle).Start();

		var trendSubscription = SubscribeCandles(TimeSpan.FromMinutes(15).TimeFrame());
		trendSubscription.Bind(_fastMa15, _slowMa15, ProcessTrend).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private static IIndicator CreateMovingAverage(MovingAverageType type, int length)
	{
		return type switch { MovingAverageType.SMA => new SimpleMovingAverage { Length = length },
							 MovingAverageType.EMA => new ExponentialMovingAverage { Length = length },
							 MovingAverageType.SMMA => new SmoothedMovingAverage { Length = length },
							 MovingAverageType.HMA => new HullMovingAverage { Length = length },
							 MovingAverageType.TEMA => new TripleExponentialMovingAverage { Length = length },
							 MovingAverageType.WMA => new WeightedMovingAverage { Length = length },
							 MovingAverageType.VWMA => new VolumeWeightedMovingAverage { Length = length },
							 MovingAverageType.ZLEMA => new ZeroLagExponentialMovingAverage { Length = length },
							 MovingAverageType.ALMA => new ArnaudLegouxMovingAverage { Length = length },
							 MovingAverageType.KAMA => new KaufmanAdaptiveMovingAverage { Length = length },
							 MovingAverageType.DEMA => new DoubleExponentialMovingAverage { Length = length },
							 _ => new SimpleMovingAverage { Length = length } };
	}

	private void ProcessTrend(ICandleMessage candle, decimal fast15, decimal slow15)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_trend15m = fast15 > slow15 ? 1 : fast15 < slow15 ? -1 : 0;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volValue = _volumeSma.Process(candle.TotalVolume);
		if (volValue.Value is not decimal volumeAvg)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volumeOk = candle.TotalVolume >= MinVolume;
		var volumeSpike = candle.TotalVolume > 1.2m * volumeAvg;
		var hour = candle.OpenTime.Hour;
		var timeWindow = hour >= TradingStartHour && hour <= TradingEndHour;
		var volatilityOk = candle.ClosePrice != 0 && atrValue / candle.ClosePrice <= VolatilityThreshold;

		var atrFilterLong = candle.ClosePrice > slowMaValue + atrValue * AtrMultiplier;
		var atrFilterShort = candle.ClosePrice < slowMaValue - atrValue * AtrMultiplier;

		var maAbove = candle.ClosePrice > fastMaValue && fastMaValue > slowMaValue;
		var maBelow = candle.ClosePrice < fastMaValue && fastMaValue < slowMaValue;

		var trendLongOk = !UseTrendFilter || _trend15m >= 0;
		var trendShortOk = !UseTrendFilter || _trend15m <= 0;

		if (Position == 0)
		{
			if (maAbove && trendLongOk && atrFilterLong && volumeOk && volumeSpike && timeWindow && volatilityOk)
			{
				BuyMarket(TradeQuantity);
				_entryPrice = candle.ClosePrice;
				_trailingStop = _entryPrice - atrValue * TrailOffset;
			}
			else if (maBelow && trendShortOk && atrFilterShort && volumeOk && volumeSpike && timeWindow && volatilityOk)
			{
				SellMarket(TradeQuantity);
				_entryPrice = candle.ClosePrice;
				_trailingStop = _entryPrice + atrValue * TrailOffset;
			}
		}
		else if (Position > 0)
		{
			var stopPrice = _entryPrice - atrValue * FixedStopMultiplier;
			var targetPrice = _entryPrice + atrValue * ProfitTargetAtrMult;
			var newTrail = candle.ClosePrice - atrValue * TrailOffset;
			if (newTrail > _trailingStop)
				_trailingStop = newTrail;

			if (candle.LowPrice <= _trailingStop || candle.LowPrice <= stopPrice || candle.HighPrice >= targetPrice)
				SellMarket(Position);
		}
		else
		{
			var stopPrice = _entryPrice + atrValue * FixedStopMultiplier;
			var targetPrice = _entryPrice - atrValue * ProfitTargetAtrMult;
			var newTrail = candle.ClosePrice + atrValue * TrailOffset;
			if (newTrail < _trailingStop || _trailingStop == 0)
				_trailingStop = newTrail;

			if (candle.HighPrice >= _trailingStop || candle.HighPrice >= stopPrice || candle.LowPrice <= targetPrice)
				BuyMarket(Math.Abs(Position));
		}
	}
}
