namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy that manages external positions using moving average exits and ATR based trailing stops.
/// </summary>
public class ExpertClor2MaStopAtrStrategy : Strategy
{
	private readonly StrategyParam<bool> _maCloseEnabled;
	private readonly StrategyParam<bool> _atrCloseEnabled;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<MovingAverageMethod> _fastMaMethod;
	private readonly StrategyParam<PriceTypeEnum> _fastPriceType;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<MovingAverageMethod> _slowMaMethod;
	private readonly StrategyParam<PriceTypeEnum> _slowPriceType;
	private readonly StrategyParam<int> _breakevenPoints;
	private readonly StrategyParam<int> _atrShiftBars;
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
	private decimal? _longCandidate;
	private decimal? _shortCandidate;
	private int _longShiftCounter;
	private int _shortShiftCounter;
	private decimal _previousPosition;

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

	public int BreakevenPoints
	{
		get => _breakevenPoints.Value;
		set => _breakevenPoints.Value = value;
	}

	public int AtrShiftBars
	{
		get => _atrShiftBars.Value;
		set => _atrShiftBars.Value = value;
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

	public ExpertClor2MaStopAtrStrategy()
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

		_breakevenPoints = Param(nameof(BreakevenPoints), 15)
			.SetDisplay("Breakeven (points)", "Distance in points to move the stop to breakeven", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 50, 1);

		_atrShiftBars = Param(nameof(AtrShiftBars), 7)
			.SetDisplay("ATR Shift", "Delay in bars before tightening the ATR stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 20, 1);

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
		ResetStops();
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators according to the configured settings.
		_fastMa = CreateMovingAverage(FastMaMethod, FastMaPeriod);
		_slowMa = CreateMovingAverage(SlowMaMethod, SlowMaPeriod);
		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		// Start processing finished candles.
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
		// Only process fully formed candles to stay consistent with the original EA.
		if (candle.State != CandleStates.Finished)
			return;

		if (_fastMa == null || _slowMa == null || _atr == null)
			return;

		var currentSign = Math.Sign(Position);
		if (currentSign != Math.Sign(_previousPosition))
			ResetStops();

		var priceFast = GetPrice(candle, FastPriceType);
		var priceSlow = GetPrice(candle, SlowPriceType);

		var fastValue = _fastMa.Process(priceFast, candle.OpenTime, true).ToDecimal();
		var slowValue = _slowMa.Process(priceSlow, candle.OpenTime, true).ToDecimal();
		var atrValue = _atr.Process(candle).ToDecimal();
		var atrFormed = _atr.IsFormed;

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
		_previousPosition = Position;
	}

	private void ManageLongPosition(ICandleMessage candle, decimal atrValue, bool atrFormed)
	{
		var entry = PositionPrice;
		// Skip management until the average entry price is available.
		if (entry == 0m)
		{
			ResetLongTrail();
			return;
		}

		var step = GetPriceStep();
		var breakevenOffset = step * BreakevenPoints;
		if (BreakevenPoints > 0 && step > 0m)
		{
			var profit = candle.ClosePrice - entry;
			if (profit >= breakevenOffset)
			{
				var breakevenLevel = entry + step;
				if (!_longStop.HasValue || breakevenLevel > _longStop)
				{
					_longStop = breakevenLevel;
					_longCandidate = _longStop;
					_longShiftCounter = 0;
				}
			}
		}

		if (AtrCloseEnabled && atrFormed)
		{
			var baseStop = candle.ClosePrice - atrValue * AtrTarget;
			if (_longStop.HasValue && baseStop < _longStop)
				baseStop = _longStop.Value;

			if (_longCandidate.HasValue)
				_longCandidate = Math.Max(_longCandidate.Value, baseStop);
			else
				_longCandidate = baseStop;

			if (AtrShiftBars <= 0)
			{
				if (!_longStop.HasValue || _longCandidate > _longStop)
					_longStop = _longCandidate;
			}
			else
			{
				_longShiftCounter++;
				if (_longShiftCounter >= AtrShiftBars)
				{
					if (!_longStop.HasValue || _longCandidate > _longStop)
						_longStop = _longCandidate;
					_longShiftCounter = 0;
				}
			}
		}
		else
		{
			ResetLongTrail();
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
		// Skip management until the average entry price is available.
		if (entry == 0m)
		{
			ResetShortTrail();
			return;
		}

		var step = GetPriceStep();
		var breakevenOffset = step * BreakevenPoints;
		if (BreakevenPoints > 0 && step > 0m)
		{
			var profit = entry - candle.ClosePrice;
			if (profit >= breakevenOffset)
			{
				var breakevenLevel = entry - step;
				if (!_shortStop.HasValue || breakevenLevel < _shortStop)
				{
					_shortStop = breakevenLevel;
					_shortCandidate = _shortStop;
					_shortShiftCounter = 0;
				}
			}
		}

		if (AtrCloseEnabled && atrFormed)
		{
			var baseStop = candle.ClosePrice + atrValue * AtrTarget;
			if (_shortStop.HasValue && baseStop > _shortStop)
				baseStop = _shortStop.Value;

			if (_shortCandidate.HasValue)
				_shortCandidate = Math.Min(_shortCandidate.Value, baseStop);
			else
				_shortCandidate = baseStop;

			if (AtrShiftBars <= 0)
			{
				if (!_shortStop.HasValue || _shortCandidate < _shortStop)
					_shortStop = _shortCandidate;
			}
			else
			{
				_shortShiftCounter++;
				if (_shortShiftCounter >= AtrShiftBars)
				{
					if (!_shortStop.HasValue || _shortCandidate < _shortStop)
						_shortStop = _shortCandidate;
					_shortShiftCounter = 0;
				}
			}
		}
		else
		{
			ResetShortTrail();
		}

		if (_shortStop.HasValue && candle.HighPrice >= _shortStop)
		{
			ClosePosition();
			ResetStops();
		}
	}

	private void ResetStops()
	{
		// Clear trailing state to avoid stale levels for the next position.
		_longStop = null;
		_shortStop = null;
		ResetLongTrail();
		ResetShortTrail();
	}

	private void ResetLongTrail()
	{
		_longCandidate = null;
		_longShiftCounter = 0;
	}

	private void ResetShortTrail()
	{
		_shortCandidate = null;
		_shortShiftCounter = 0;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;
		return step;
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
