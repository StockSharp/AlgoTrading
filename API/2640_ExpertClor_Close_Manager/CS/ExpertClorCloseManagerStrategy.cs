namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy that manages existing positions using moving average cross exits and ATR based trailing stops.
/// </summary>
public class ExpertClorCloseManagerStrategy : Strategy
{
	private readonly StrategyParam<bool> _maCloseEnabled;
	private readonly StrategyParam<bool> _atrCloseEnabled;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<MovingAverageMethod> _fastMaMethod;
	private readonly StrategyParam<PriceTypeEnum> _fastPriceType;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<MovingAverageMethod> _slowMaMethod;
	private readonly StrategyParam<PriceTypeEnum> _slowPriceType;
	private readonly StrategyParam<int> _breakevenPips;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrTarget;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal>? _fastMa;
	private LengthIndicator<decimal>? _slowMa;
	private AverageTrueRange? _atr;

	private decimal? _fastPrev1;
	private decimal? _fastPrev2;
	private decimal? _slowPrev1;
	private decimal? _slowPrev2;
	private decimal? _longStop;
	private decimal? _shortStop;

	public bool MaCloseEnabled
	{
		get => _maCloseEnabled.Value;
		set => _maCloseEnabled.Value = value;
	}

	public bool AtrCloseEnabled
	{
		get => _atrCloseEnabled.Value;
		set => _atrCloseEnabled.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public MovingAverageMethod FastMaMethod
	{
		get => _fastMaMethod.Value;
		set => _fastMaMethod.Value = value;
	}

	public PriceTypeEnum FastPriceType
	{
		get => _fastPriceType.Value;
		set => _fastPriceType.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public MovingAverageMethod SlowMaMethod
	{
		get => _slowMaMethod.Value;
		set => _slowMaMethod.Value = value;
	}

	public PriceTypeEnum SlowPriceType
	{
		get => _slowPriceType.Value;
		set => _slowPriceType.Value = value;
	}

	public int BreakevenPips
	{
		get => _breakevenPips.Value;
		set => _breakevenPips.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal AtrTarget
	{
		get => _atrTarget.Value;
		set => _atrTarget.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ExpertClorCloseManagerStrategy()
	{
		_maCloseEnabled = Param(nameof(MaCloseEnabled), true)
			.SetDisplay("MA Close", "Enable moving average based exits", "General");

		_atrCloseEnabled = Param(nameof(AtrCloseEnabled), true)
			.SetDisplay("ATR Close", "Enable ATR trailing exit", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 5)
			.SetDisplay("Fast MA Period", "Length of the fast moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(3, 30, 1);

		_fastMaMethod = Param(nameof(FastMaMethod), MovingAverageMethod.Exponential)
			.SetDisplay("Fast MA Method", "Type of the fast moving average", "Moving Averages");

		_fastPriceType = Param(nameof(FastPriceType), PriceTypeEnum.Close)
			.SetDisplay("Fast Price", "Applied price for the fast moving average", "Moving Averages");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 7)
			.SetDisplay("Slow MA Period", "Length of the slow moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 1);

		_slowMaMethod = Param(nameof(SlowMaMethod), MovingAverageMethod.Exponential)
			.SetDisplay("Slow MA Method", "Type of the slow moving average", "Moving Averages");

		_slowPriceType = Param(nameof(SlowPriceType), PriceTypeEnum.Open)
			.SetDisplay("Slow Price", "Applied price for the slow moving average", "Moving Averages");

		_breakevenPips = Param(nameof(BreakevenPips), 0)
			.SetDisplay("Breakeven (pips)", "Distance in pips to move the stop to entry", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 12)
			.SetDisplay("ATR Period", "ATR period for the trailing stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_atrTarget = Param(nameof(AtrTarget), 2m)
			.SetDisplay("ATR Target", "Multiplier for the ATR trailing stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");
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

		_fastPrev1 = null;
		_fastPrev2 = null;
		_slowPrev1 = null;
		_slowPrev2 = null;
		_longStop = null;
		_shortStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		// Create indicators based on user configuration.

		_fastMa = CreateMovingAverage(FastMaMethod, FastMaPeriod);
		_slowMa = CreateMovingAverage(SlowMaMethod, SlowMaPeriod);
		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		// Start candle processing pipeline.
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_fastMa != null)
				DrawIndicator(area, _fastMa);
			if (_slowMa != null)
				DrawIndicator(area, _slowMa);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only act on completed candles to avoid partial data.
		if (candle.State != CandleStates.Finished)
			return;

		if (_fastMa == null || _slowMa == null || _atr == null)
			return;

		var priceFast = GetPrice(candle, FastPriceType);
		var priceSlow = GetPrice(candle, SlowPriceType);
		// Calculate indicator values for the current bar.

		var fastValue = _fastMa.Process(priceFast, candle.OpenTime, true).ToDecimal();
		var slowValue = _slowMa.Process(priceSlow, candle.OpenTime, true).ToDecimal();
		var atrValue = _atr.Process(candle).ToDecimal();
		var atrFormed = _atr.IsFormed;
		// Track whether the moving average cross should force an exit.

		var closeLongSignal = false;
		var closeShortSignal = false;

		if (MaCloseEnabled && _fastPrev1.HasValue && _fastPrev2.HasValue && _slowPrev1.HasValue && _slowPrev2.HasValue)
		{
			closeLongSignal = _fastPrev1 <= _slowPrev1 && _fastPrev2 > _slowPrev2;
			closeShortSignal = _fastPrev1 >= _slowPrev1 && _fastPrev2 < _slowPrev2;
		}

		if (closeLongSignal && Position > 0)
		{
			ClosePosition();
			ResetStops();
		}
		else if (closeShortSignal && Position < 0)
		{
			ClosePosition();
			ResetStops();
		}
		else
		{
			// Update trailing logic depending on the position side.
			if (Position > 0)
			{
				ManageLongPosition(candle, atrValue, atrFormed);
			}
			else if (Position < 0)
			{
				ManageShortPosition(candle, atrValue, atrFormed);
			}
			else
			{
				ResetStops();
			}
		}

		_fastPrev2 = _fastPrev1;
		_fastPrev1 = fastValue;
		_slowPrev2 = _slowPrev1;
		_slowPrev1 = slowValue;
	}

	private void ManageLongPosition(ICandleMessage candle, decimal atrValue, bool atrFormed)
	{
		var entry = PositionPrice;
		// Skip management if the average entry price is not available yet.

		if (entry == 0m)
		{
			_longStop = null;
			return;
		}

		var breakevenOffset = GetBreakevenOffset();
		if (BreakevenPips > 0 && breakevenOffset > 0)
		{
			var profit = candle.ClosePrice - entry;
			if (profit >= breakevenOffset && (!_longStop.HasValue || _longStop < entry))
				_longStop = entry;
		}

		if (AtrCloseEnabled && atrFormed)
		{
			var atrStop = candle.ClosePrice - atrValue * AtrTarget;
			if (!_longStop.HasValue || atrStop > _longStop)
				_longStop = atrStop;
		}

		if (_longStop.HasValue && candle.LowPrice <= _longStop)
		{
			ClosePosition();
			ResetStops();
		}
	}

	private void ManageShortPosition(ICandleMessage candle, decimal atrValue, bool atrFormed)
	{
		var entry = PositionPrice;
		// Skip management if the average entry price is not available yet.

		if (entry == 0m)
		{
			_shortStop = null;
			return;
		}

		var breakevenOffset = GetBreakevenOffset();
		if (BreakevenPips > 0 && breakevenOffset > 0)
		{
			var profit = entry - candle.ClosePrice;
			if (profit >= breakevenOffset && (!_shortStop.HasValue || _shortStop > entry))
				_shortStop = entry;
		}

		if (AtrCloseEnabled && atrFormed)
		{
			var atrStop = candle.ClosePrice + atrValue * AtrTarget;
			if (!_shortStop.HasValue || atrStop < _shortStop)
				_shortStop = atrStop;
		}

		if (_shortStop.HasValue && candle.HighPrice >= _shortStop)
		{
			ClosePosition();
			ResetStops();
		}
	}

	private void ResetStops()
	{
		// Clear stored levels to avoid reusing obsolete stops.
		_longStop = null;
		_shortStop = null;
	}

	private decimal GetBreakevenOffset()
	{
		// Convert pip-based distance to absolute price difference.
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0)
			step = 1m;

		return BreakevenPips * step;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	private static decimal GetPrice(ICandleMessage candle, PriceTypeEnum type)
	{
		return type switch
		{
			PriceTypeEnum.Open => candle.OpenPrice,
			PriceTypeEnum.High => candle.HighPrice,
			PriceTypeEnum.Low => candle.LowPrice,
			PriceTypeEnum.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			PriceTypeEnum.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			PriceTypeEnum.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	public enum MovingAverageMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}

	public enum PriceTypeEnum
	{
		Close,
		Open,
		High,
		Low,
		Typical,
		Median,
		Weighted
	}
}
