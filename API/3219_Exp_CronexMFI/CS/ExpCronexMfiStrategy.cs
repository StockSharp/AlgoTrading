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
/// Contrarian strategy based on the Cronex Money Flow Index (MFI) concept.
/// The original MQL version smooths MFI twice and opens positions against crossovers of the smoothed lines.
/// This port applies configurable moving averages to the MFI and reproduces the same reversal logic.
/// </summary>
public class ExpCronexMfiStrategy : Strategy
{
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<SmoothingMethod> _smoothing;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<DataType> _candleType;

	private MoneyFlowIndex _mfi = null!;
	private LengthIndicator<decimal> _fastSmoother = null!;
	private LengthIndicator<decimal> _slowSmoother = null!;
	private readonly List<(decimal fast, decimal slow)> _history = new();

	/// <summary>
	/// Available smoothing options for the Cronex MFI lines.
	/// </summary>
	public enum SmoothingMethod
	{
		/// <summary>
		/// Simple moving average (SMA).
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average (EMA).
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average (SMMA).
		/// </summary>
		Smoothed,

		/// <summary>
		/// Linear weighted moving average (LWMA).
		/// </summary>
		Weighted,

		/// <summary>
		/// Double exponential moving average (DEMA).
		/// </summary>
		DoubleExponential,

		/// <summary>
		/// Triple exponential moving average (TEMA).
		/// </summary>
		TripleExponential,

		/// <summary>
		/// Hull moving average (HMA).
		/// </summary>
		Hull,

		/// <summary>
		/// Zero-lag exponential moving average (ZLEMA).
		/// </summary>
		ZeroLagExponential,

		/// <summary>
		/// Arnaud Legoux moving average (ALMA).
		/// </summary>
		ArnaudLegoux,

		/// <summary>
		/// Kaufman adaptive moving average (KAMA).
		/// </summary>
		KaufmanAdaptive,
	}

	/// <summary>
	/// Gets or sets the Money Flow Index period.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Gets or sets the fast smoothing period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Gets or sets the slow smoothing period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Gets or sets the number of completed candles to delay signal evaluation.
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Gets or sets the smoothing method applied to both Cronex lines.
	/// </summary>
	public SmoothingMethod Smoothing
	{
		get => _smoothing.Value;
		set => _smoothing.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether long entries are allowed.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _enableLongEntries.Value;
		set => _enableLongEntries.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether short entries are allowed.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _enableShortEntries.Value;
		set => _enableShortEntries.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether long positions can be closed by signals.
	/// </summary>
	public bool EnableLongExits
	{
		get => _enableLongExits.Value;
		set => _enableLongExits.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether short positions can be closed by signals.
	/// </summary>
	public bool EnableShortExits
	{
		get => _enableShortExits.Value;
		set => _enableShortExits.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults close to the original expert advisor.
	/// </summary>
	public ExpCronexMfiStrategy()
	{
		_mfiPeriod = Param(nameof(MfiPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Length of the Money Flow Index", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast Smoothing", "Period of the fast Cronex line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow Smoothing", "Period of the slow Cronex line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_signalShift = Param(nameof(SignalShift), 1)
			.SetDisplay("Signal Shift", "Number of finished candles to wait before acting", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0, 3, 1);

		_smoothing = Param(nameof(Smoothing), SmoothingMethod.Simple)
			.SetDisplay("Smoothing", "Moving-average algorithm applied to both Cronex lines", "Indicators");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_enableLongExits = Param(nameof(EnableLongExits), true)
			.SetDisplay("Enable Long Exits", "Allow signals to close existing long positions", "Trading");

		_enableShortExits = Param(nameof(EnableShortExits), true)
			.SetDisplay("Enable Short Exits", "Allow signals to close existing short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Data type used for the strategy", "General");
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
		_history.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_mfi = new MoneyFlowIndex { Length = MfiPeriod };
		_fastSmoother = CreateSmoother(Smoothing, FastPeriod);
		_slowSmoother = CreateSmoother(Smoothing, SlowPeriod);

		_fastSmoother.Reset();
		_slowSmoother.Reset();
		_history.Clear();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_mfi, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);

			var indicatorArea = CreateChartArea();
			if (indicatorArea != null)
			{
				DrawIndicator(indicatorArea, _mfi);
				DrawIndicator(indicatorArea, _fastSmoother);
				DrawIndicator(indicatorArea, _slowSmoother);
			}
		}

		// Enable built-in protection once at start to manage unexpected positions.
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		// Work only with completed candles to match the original expert advisor.
		if (candle.State != CandleStates.Finished)
			return;

		// Smooth the raw MFI value with the selected moving averages.
		var fastResult = _fastSmoother.Process(new DecimalIndicatorValue(_fastSmoother, mfiValue, candle.OpenTime));
		if (fastResult is not DecimalIndicatorValue { IsFinal: true, Value: decimal fast })
			return;

		var slowResult = _slowSmoother.Process(new DecimalIndicatorValue(_slowSmoother, fast, candle.OpenTime));
		if (slowResult is not DecimalIndicatorValue { IsFinal: true, Value: decimal slow })
			return;

		_history.Add((fast, slow));

		var required = Math.Max(SignalShift + 2, 2);
		if (_history.Count < required)
			return;

		// Limit stored history to avoid unnecessary memory usage.
		var maxHistory = Math.Max(required + 1, 6);
		if (_history.Count > maxHistory)
			_history.RemoveRange(0, _history.Count - maxHistory);

		var currentIndex = _history.Count - 1 - SignalShift;
		var previousIndex = currentIndex - 1;

		if (previousIndex < 0)
			return;

		var (currentFast, currentSlow) = _history[currentIndex];
		var (previousFast, previousSlow) = _history[previousIndex];

		// A downward crossover of the fast line through the slow line triggers long opportunities.
		var crossDown = previousFast > previousSlow && currentFast <= currentSlow;
		// An upward crossover of the fast line through the slow line triggers short opportunities.
		var crossUp = previousFast < previousSlow && currentFast >= currentSlow;

		if (!crossDown && !crossUp)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (crossDown)
		{
			var volumeToBuy = 0m;

			if (EnableShortExits && Position < 0)
				volumeToBuy += Math.Abs(Position);

			if (EnableLongEntries && Position <= 0)
				volumeToBuy += Volume;

			if (volumeToBuy > 0)
				BuyMarket(volumeToBuy);
		}
		else if (crossUp)
		{
			var volumeToSell = 0m;

			if (EnableLongExits && Position > 0)
				volumeToSell += Math.Abs(Position);

			if (EnableShortEntries && Position >= 0)
				volumeToSell += Volume;

			if (volumeToSell > 0)
				SellMarket(volumeToSell);
		}
	}

	private static LengthIndicator<decimal> CreateSmoother(SmoothingMethod method, int length)
	{
		return method switch
		{
			SmoothingMethod.Simple => new SimpleMovingAverage { Length = length },
			SmoothingMethod.Exponential => new ExponentialMovingAverage { Length = length },
			SmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			SmoothingMethod.Weighted => new WeightedMovingAverage { Length = length },
			SmoothingMethod.DoubleExponential => new DoubleExponentialMovingAverage { Length = length },
			SmoothingMethod.TripleExponential => new TripleExponentialMovingAverage { Length = length },
			SmoothingMethod.Hull => new HullMovingAverage { Length = length },
			SmoothingMethod.ZeroLagExponential => new ZeroLagExponentialMovingAverage { Length = length },
			SmoothingMethod.ArnaudLegoux => new ArnaudLegouxMovingAverage { Length = length },
			SmoothingMethod.KaufmanAdaptive => new KaufmanAdaptiveMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}
}

