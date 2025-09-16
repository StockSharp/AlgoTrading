using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the Exp_IBS_RSI_CCI_v4 MetaTrader strategy to StockSharp.
/// Combines internal bar strength, RSI, and CCI into a smoothed oscillator for contrarian entries.
/// </summary>
public class IbsRsiCciV4Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _ibsPeriod;
	private readonly StrategyParam<MovingAverageKind> _ibsAverageType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<int> _smoothPeriod;
	private readonly StrategyParam<MovingAverageKind> _rangeAverageType;
	private readonly StrategyParam<decimal> _stepThreshold;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableLongOpen;
	private readonly StrategyParam<bool> _enableShortOpen;
	private readonly StrategyParam<bool> _enableLongClose;
	private readonly StrategyParam<bool> _enableShortClose;
	private readonly StrategyParam<decimal> _volume;

	private RelativeStrengthIndex _rsi = null!;
	private CommodityChannelIndex _cci = null!;
	private IIndicator _ibsAverage = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private IIndicator _upperAverage = null!;
	private IIndicator _lowerAverage = null!;

	private bool _hasSignal;
	private decimal _lastSignal;
	private readonly List<decimal> _signalHistory = [];
	private readonly List<decimal> _baselineHistory = [];

	private const decimal IbsWeight = 700m;
	private const decimal RsiWeight = 9m;
	private const decimal CciWeight = 1m;

	/// <summary>
	/// Initializes a new instance of the <see cref="IbsRsiCciV4Strategy"/> class.
	/// </summary>
	public IbsRsiCciV4Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for calculations", "General")
		.SetCanOptimize(true);

		_ibsPeriod = Param(nameof(IbsPeriod), 5)
		.SetDisplay("IBS Period", "Smoothing period for the internal bar strength component", "Indicator")
		.SetCanOptimize(true);

		_ibsAverageType = Param(nameof(IbsAverageType), MovingAverageKind.Simple)
		.SetDisplay("IBS MA Type", "Moving average type applied to the IBS series", "Indicator")
		.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "Lookback period for the RSI filter", "Indicator")
		.SetCanOptimize(true);

		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetDisplay("CCI Period", "Lookback period for the CCI filter", "Indicator")
		.SetCanOptimize(true);

		_rangePeriod = Param(nameof(RangePeriod), 25)
		.SetDisplay("Range Period", "Window size for highest/lowest range calculation", "Indicator")
		.SetCanOptimize(true);

		_smoothPeriod = Param(nameof(SmoothPeriod), 3)
		.SetDisplay("Range Smooth", "Smoothing period for the range bands", "Indicator")
		.SetCanOptimize(true);

		_rangeAverageType = Param(nameof(RangeAverageType), MovingAverageKind.Simple)
		.SetDisplay("Range MA Type", "Moving average type applied to the range envelopes", "Indicator")
		.SetCanOptimize(true);

		_stepThreshold = Param(nameof(StepThreshold), 50m)
		.SetDisplay("Step Threshold", "Maximum adjustment applied when the composite signal jumps", "Trading")
		.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Number of closed candles used for confirmation", "Trading")
		.SetCanOptimize(true);

		_enableLongOpen = Param(nameof(EnableLongOpen), true)
		.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_enableShortOpen = Param(nameof(EnableShortOpen), true)
		.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_enableLongClose = Param(nameof(EnableLongClose), true)
		.SetDisplay("Enable Long Exits", "Allow closing existing long positions", "Trading");

		_enableShortClose = Param(nameof(EnableShortClose), true)
		.SetDisplay("Enable Short Exits", "Allow closing existing short positions", "Trading");

		_volume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Base volume used for market orders", "Trading")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type used to feed the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for smoothing the IBS component.
	/// </summary>
	public int IbsPeriod
	{
		get => _ibsPeriod.Value;
		set => _ibsPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type applied to the IBS series.
	/// </summary>
	public MovingAverageKind IbsAverageType
	{
		get => _ibsAverageType.Value;
		set => _ibsAverageType.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// CCI lookback period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Window used to search for highs and lows of the composite signal.
	/// </summary>
	public int RangePeriod
	{
		get => _rangePeriod.Value;
		set => _rangePeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for the signal envelopes.
	/// </summary>
	public int SmoothPeriod
	{
		get => _smoothPeriod.Value;
		set => _smoothPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type used for the envelope smoothing.
	/// </summary>
	public MovingAverageKind RangeAverageType
	{
		get => _rangeAverageType.Value;
		set => _rangeAverageType.Value = value;
	}

	/// <summary>
	/// Maximum step applied when the composite signal changes sharply.
	/// </summary>
	public decimal StepThreshold
	{
		get => _stepThreshold.Value;
		set => _stepThreshold.Value = value;
	}

	/// <summary>
	/// Number of closed candles used for confirmation logic.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enables long entries when <c>true</c>.
	/// </summary>
	public bool EnableLongOpen
	{
		get => _enableLongOpen.Value;
		set => _enableLongOpen.Value = value;
	}

	/// <summary>
	/// Enables short entries when <c>true</c>.
	/// </summary>
	public bool EnableShortOpen
	{
		get => _enableShortOpen.Value;
		set => _enableShortOpen.Value = value;
	}

	/// <summary>
	/// Enables long exits when <c>true</c>.
	/// </summary>
	public bool EnableLongClose
	{
		get => _enableLongClose.Value;
		set => _enableLongClose.Value = value;
	}

	/// <summary>
	/// Enables short exits when <c>true</c>.
	/// </summary>
	public bool EnableShortClose
	{
		get => _enableShortClose.Value;
		set => _enableShortClose.Value = value;
	}

	/// <summary>
	/// Volume used for new market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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

		Volume = OrderVolume;

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_ibsAverage = CreateMovingAverage(IbsAverageType, Math.Max(1, IbsPeriod));
		_highest = new Highest { Length = Math.Max(1, RangePeriod) };
		_lowest = new Lowest { Length = Math.Max(1, RangePeriod) };
		_upperAverage = CreateMovingAverage(RangeAverageType, Math.Max(1, SmoothPeriod));
		_lowerAverage = CreateMovingAverage(RangeAverageType, Math.Max(1, SmoothPeriod));

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, _cci, ProcessCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_rsi.IsFormed || !_cci.IsFormed)
		return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range == 0m)
		{
			var step = Security?.PriceStep ?? 0.0001m;
			if (step == 0m)
			step = 0.0001m;
			range = step;
		}

		var ibsRaw = (candle.ClosePrice - candle.LowPrice) / range;
		var ibsValue = _ibsAverage.Process(new DecimalIndicatorValue(_ibsAverage, ibsRaw, candle.OpenTime));
		if (ibsValue is not DecimalIndicatorValue { IsFinal: true, Value: var ibsSmoothed })
		return;

		var compositeTarget = ((ibsSmoothed - 0.5m) * IbsWeight + cciValue * CciWeight + (rsiValue - 50m) * RsiWeight) / 3m;
		var adjustedSignal = ApplyStepConstraint(compositeTarget);

		var highestValue = _highest.Process(new DecimalIndicatorValue(_highest, adjustedSignal, candle.OpenTime));
		var lowestValue = _lowest.Process(new DecimalIndicatorValue(_lowest, adjustedSignal, candle.OpenTime));
		if (highestValue is not DecimalIndicatorValue { IsFinal: true, Value: var highest })
		return;
		if (lowestValue is not DecimalIndicatorValue { IsFinal: true, Value: var lowest })
		return;

		var upperValue = _upperAverage.Process(new DecimalIndicatorValue(_upperAverage, highest, candle.OpenTime));
		var lowerValue = _lowerAverage.Process(new DecimalIndicatorValue(_lowerAverage, lowest, candle.OpenTime));
		if (upperValue is not DecimalIndicatorValue { IsFinal: true, Value: var upper })
		return;
		if (lowerValue is not DecimalIndicatorValue { IsFinal: true, Value: var lower })
		return;

		var baseline = (upper + lower) / 2m;

		UpdateHistory(adjustedSignal, baseline);

		var historyLength = Math.Min(_signalHistory.Count, _baselineHistory.Count);
		if (historyLength <= SignalBar)
		return;

		var previousIndex = historyLength - 1 - Math.Max(0, SignalBar);
		var previousSignal = _signalHistory[previousIndex];
		var previousBaseline = _baselineHistory[previousIndex];
		var currentSignal = _signalHistory[historyLength - 1];
		var currentBaseline = _baselineHistory[historyLength - 1];

		var position = Position;

		if (position > 0 && EnableLongClose && previousSignal < previousBaseline)
		{
			SellMarket(position);
			position = 0m;
		}
		else if (position < 0 && EnableShortClose && previousSignal > previousBaseline)
		{
			BuyMarket(Math.Abs(position));
			position = 0m;
		}

		if (EnableLongOpen && position <= 0m && previousSignal > previousBaseline && currentSignal <= currentBaseline)
		{
			var volume = Volume + Math.Abs(position);
			BuyMarket(volume);
		}
		else if (EnableShortOpen && position >= 0m && previousSignal < previousBaseline && currentSignal >= currentBaseline)
		{
			var volume = Volume + Math.Abs(position);
			SellMarket(volume);
		}
	}

	private decimal ApplyStepConstraint(decimal target)
	{
		if (!_hasSignal)
		{
			_lastSignal = target;
			_hasSignal = true;
			return _lastSignal;
		}

		var threshold = Math.Abs(StepThreshold);
		if (threshold <= 0m)
		{
			_lastSignal = target;
			return _lastSignal;
		}

		var diff = target - _lastSignal;
		if (Math.Abs(diff) > threshold)
		{
			var direction = diff > 0m ? 1m : -1m;
			_lastSignal = target - direction * threshold;
		}
		else
		{
			_lastSignal = target;
		}

		return _lastSignal;
	}

	private void UpdateHistory(decimal signal, decimal baseline)
	{
		var maxSize = Math.Max(2, Math.Max(SignalBar + 1, 2));

		_signalHistory.Add(signal);
		if (_signalHistory.Count > maxSize)
		_signalHistory.RemoveAt(0);

		_baselineHistory.Add(baseline);
		if (_baselineHistory.Count > maxSize)
		_baselineHistory.RemoveAt(0);
	}

	private static IIndicator CreateMovingAverage(MovingAverageKind kind, int length)
	{
		return kind switch
		{
			MovingAverageKind.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageKind.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageKind.LinearWeighted => new LinearWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Supported moving average families.
	/// </summary>
	public enum MovingAverageKind
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		LinearWeighted
	}
}
