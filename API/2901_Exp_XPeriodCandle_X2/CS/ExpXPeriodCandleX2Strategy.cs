using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smoothing methods available for XPeriod synthetic candles.
/// </summary>
public enum XPeriodCandleSmoothingMethod
{
	/// <summary>
	/// Simple moving average smoothing.
	/// </summary>
	Simple,

	/// <summary>
	/// Exponential moving average smoothing.
	/// </summary>
	Exponential,

	/// <summary>
	/// Smoothed moving average (RMA) smoothing.
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average smoothing.
	/// </summary>
	Weighted,

	/// <summary>
	/// Jurik moving average smoothing.
	/// </summary>
	Jurik,

	/// <summary>
	/// Hull moving average smoothing.
	/// </summary>
	Hull,

	/// <summary>
	/// Kaufman adaptive moving average smoothing.
	/// </summary>
	KaufmanAdaptive
}

/// <summary>
/// Multi-timeframe strategy that recreates the Exp_XPeriodCandle_X2 expert.
/// The higher timeframe smooth candle color defines the trend bias.
/// The entry timeframe waits for color flips with configurable exits before opening positions.
/// Optional stop loss and take profit mirror the original expert inputs.
/// </summary>
public class ExpXPeriodCandleX2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<DataType> _entryCandleType;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<int> _entryPeriod;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _entryLength;
	private readonly StrategyParam<decimal> _trendPhase;
	private readonly StrategyParam<decimal> _entryPhase;
	private readonly StrategyParam<int> _trendSignalBar;
	private readonly StrategyParam<int> _entrySignalBar;
	private readonly StrategyParam<XPeriodCandleSmoothingMethod> _trendSmoothing;
	private readonly StrategyParam<XPeriodCandleSmoothingMethod> _entrySmoothing;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _closeLongOnTrendFlip;
	private readonly StrategyParam<bool> _closeShortOnTrendFlip;
	private readonly StrategyParam<bool> _closeLongOnEntrySignal;
	private readonly StrategyParam<bool> _closeShortOnEntrySignal;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<int> _takeProfitTicks;

	private IIndicator _trendOpenMa = null!;
	private IIndicator _trendCloseMa = null!;
	private IIndicator _entryOpenMa = null!;
	private IIndicator _entryCloseMa = null!;

	private readonly Queue<decimal> _trendOpenQueue = new();
	private readonly Queue<decimal> _entryOpenQueue = new();
	private readonly List<int> _trendColors = new();
	private readonly List<int> _entryColors = new();

	private int _trend;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpXPeriodCandleX2Strategy"/>.
	/// </summary>
	public ExpXPeriodCandleX2Strategy()
	{
		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Trend Candle Type", "Higher timeframe used for trend detection", "General");

		_entryCandleType = Param(nameof(EntryCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Entry Candle Type", "Working timeframe used for entries", "General");

		_trendPeriod = Param(nameof(TrendPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Trend Period", "Number of smoothed candles defining delayed open", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_entryPeriod = Param(nameof(EntryPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Entry Period", "Number of smoothed candles defining delayed open", "Entry")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_trendLength = Param(nameof(TrendLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Smoothing length for trend candles", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_entryLength = Param(nameof(EntryLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Entry Length", "Smoothing length for entry candles", "Entry")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_trendPhase = Param(nameof(TrendPhase), 100m)
			.SetDisplay("Trend Phase", "Phase parameter for Jurik smoothing", "Trend")
			.SetRange(-100m, 100m);

		_entryPhase = Param(nameof(EntryPhase), 100m)
			.SetDisplay("Entry Phase", "Phase parameter for Jurik smoothing", "Entry")
			.SetRange(-100m, 100m);

		_trendSignalBar = Param(nameof(TrendSignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Trend Signal Bar", "Shift of the smoothed trend candle", "Trend");

		_entrySignalBar = Param(nameof(EntrySignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Entry Signal Bar", "Shift of the smoothed entry candle", "Entry");

		_trendSmoothing = Param(nameof(TrendSmoothing), XPeriodCandleSmoothingMethod.Jurik)
			.SetDisplay("Trend Smoothing", "Smoothing method for trend candles", "Trend");

		_entrySmoothing = Param(nameof(EntrySmoothing), XPeriodCandleSmoothingMethod.Jurik)
			.SetDisplay("Entry Smoothing", "Smoothing method for entry candles", "Entry");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_closeLongOnTrendFlip = Param(nameof(CloseLongOnTrendFlip), true)
			.SetDisplay("Close Long On Trend Flip", "Close longs when higher timeframe turns bearish", "Trading");

		_closeShortOnTrendFlip = Param(nameof(CloseShortOnTrendFlip), true)
			.SetDisplay("Close Short On Trend Flip", "Close shorts when higher timeframe turns bullish", "Trading");

		_closeLongOnEntrySignal = Param(nameof(CloseLongOnEntrySignal), true)
			.SetDisplay("Close Long On Entry Signal", "Close longs when entry candles flip bearish", "Trading");

		_closeShortOnEntrySignal = Param(nameof(CloseShortOnEntrySignal), true)
			.SetDisplay("Close Short On Entry Signal", "Close shorts when entry candles flip bullish", "Trading");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable protective stop loss", "Risk Management");

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Ticks", "Distance of the stop loss in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(200, 2000, 200);

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable protective take profit", "Risk Management");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Ticks", "Distance of the take profit in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(400, 4000, 200);
	}

	/// <summary>
	/// Higher timeframe used for the trend filter.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Working timeframe used for trade execution.
	/// </summary>
	public DataType EntryCandleType
	{
		get => _entryCandleType.Value;
		set => _entryCandleType.Value = value;
	}

	/// <summary>
	/// Number of smoothed candles used to build the delayed open on the trend timeframe.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	/// <summary>
	/// Number of smoothed candles used to build the delayed open on the entry timeframe.
	/// </summary>
	public int EntryPeriod
	{
		get => _entryPeriod.Value;
		set => _entryPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length for trend candles.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Smoothing length for entry candles.
	/// </summary>
	public int EntryLength
	{
		get => _entryLength.Value;
		set => _entryLength.Value = value;
	}

	/// <summary>
	/// Jurik phase parameter for trend smoothing.
	/// </summary>
	public decimal TrendPhase
	{
		get => _trendPhase.Value;
		set => _trendPhase.Value = value;
	}

	/// <summary>
	/// Jurik phase parameter for entry smoothing.
	/// </summary>
	public decimal EntryPhase
	{
		get => _entryPhase.Value;
		set => _entryPhase.Value = value;
	}

	/// <summary>
	/// Shift of the trend candle used for the trend state.
	/// </summary>
	public int TrendSignalBar
	{
		get => _trendSignalBar.Value;
		set => _trendSignalBar.Value = value;
	}

	/// <summary>
	/// Shift of the entry candle used for entry calculations.
	/// </summary>
	public int EntrySignalBar
	{
		get => _entrySignalBar.Value;
		set => _entrySignalBar.Value = value;
	}

	/// <summary>
	/// Smoothing method for the trend timeframe.
	/// </summary>
	public XPeriodCandleSmoothingMethod TrendSmoothing
	{
		get => _trendSmoothing.Value;
		set => _trendSmoothing.Value = value;
	}

	/// <summary>
	/// Smoothing method for the entry timeframe.
	/// </summary>
	public XPeriodCandleSmoothingMethod EntrySmoothing
	{
		get => _entrySmoothing.Value;
		set => _entrySmoothing.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _enableLongEntries.Value;
		set => _enableLongEntries.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _enableShortEntries.Value;
		set => _enableShortEntries.Value = value;
	}

	/// <summary>
	/// Close long positions when the higher timeframe trend flips bearish.
	/// </summary>
	public bool CloseLongOnTrendFlip
	{
		get => _closeLongOnTrendFlip.Value;
		set => _closeLongOnTrendFlip.Value = value;
	}

	/// <summary>
	/// Close short positions when the higher timeframe trend flips bullish.
	/// </summary>
	public bool CloseShortOnTrendFlip
	{
		get => _closeShortOnTrendFlip.Value;
		set => _closeShortOnTrendFlip.Value = value;
	}

	/// <summary>
	/// Close long positions when the entry candles flip bearish.
	/// </summary>
	public bool CloseLongOnEntrySignal
	{
		get => _closeLongOnEntrySignal.Value;
		set => _closeLongOnEntrySignal.Value = value;
	}

	/// <summary>
	/// Close short positions when the entry candles flip bullish.
	/// </summary>
	public bool CloseShortOnEntrySignal
	{
		get => _closeShortOnEntrySignal.Value;
		set => _closeShortOnEntrySignal.Value = value;
	}

	/// <summary>
	/// Enable stop loss protection.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Enable take profit protection.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	private int TrendPeriodLength => Math.Max(1, TrendPeriod);
	private int EntryPeriodLength => Math.Max(1, EntryPeriod);
	private int TrendSignalOffset => Math.Max(1, TrendSignalBar) - 1;
	private int EntrySignalOffset => Math.Max(1, EntrySignalBar) - 1;
	private int TrendHistorySize => TrendSignalOffset + 3;
	private int EntryHistorySize => EntrySignalOffset + 3;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, EntryCandleType);

		if (!Equals(TrendCandleType, EntryCandleType))
			yield return (Security, TrendCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trendOpenQueue.Clear();
		_entryOpenQueue.Clear();
		_trendColors.Clear();
		_entryColors.Clear();
		_trend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_trendOpenMa = CreateMovingAverage(TrendSmoothing, TrendLength, TrendPhase);
		_trendCloseMa = CreateMovingAverage(TrendSmoothing, TrendLength, TrendPhase);
		_entryOpenMa = CreateMovingAverage(EntrySmoothing, EntryLength, EntryPhase);
		_entryCloseMa = CreateMovingAverage(EntrySmoothing, EntryLength, EntryPhase);

		_trendOpenQueue.Clear();
		_entryOpenQueue.Clear();
		_trendColors.Clear();
		_entryColors.Clear();
		_trend = 0;

		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription.Bind(ProcessTrendCandle).Start();

		var entrySubscription = SubscribeCandles(EntryCandleType);
		entrySubscription.Bind(ProcessEntryCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, entrySubscription);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		var stopLoss = UseStopLoss ? new Unit(StopLossTicks * step, UnitTypes.Point) : null;
		var takeProfit = UseTakeProfit ? new Unit(TakeProfitTicks * step, UnitTypes.Point) : null;

		if (stopLoss is not null || takeProfit is not null)
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
		else
			StartProtection();
	}

	private void ProcessTrendCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;

		var smoothedOpen = _trendOpenMa.Process(candle.OpenPrice, time, true).ToDecimal();
		var smoothedClose = _trendCloseMa.Process(candle.ClosePrice, time, true).ToDecimal();

		UpdateQueue(_trendOpenQueue, smoothedOpen, TrendPeriodLength);

		if (!_trendOpenMa.IsFormed || !_trendCloseMa.IsFormed)
			return;

		if (_trendOpenQueue.Count < TrendPeriodLength)
			return;

		var delayedOpen = _trendOpenQueue.Peek();
		var color = smoothedClose >= delayedOpen ? 0 : 2;

		UpdateHistory(_trendColors, color, TrendHistorySize);

		if (TryGetHistoryValue(_trendColors, TrendSignalOffset, out var trendColor))
			_trend = trendColor switch
			{
				0 => 1,
				2 => -1,
				_ => 0
			};
	}

	private void ProcessEntryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;

		var smoothedOpen = _entryOpenMa.Process(candle.OpenPrice, time, true).ToDecimal();
		var smoothedClose = _entryCloseMa.Process(candle.ClosePrice, time, true).ToDecimal();

		UpdateQueue(_entryOpenQueue, smoothedOpen, EntryPeriodLength);

		if (!_entryOpenMa.IsFormed || !_entryCloseMa.IsFormed)
			return;

		if (_entryOpenQueue.Count < EntryPeriodLength)
			return;

		var delayedOpen = _entryOpenQueue.Peek();
		var color = smoothedClose >= delayedOpen ? 0 : 2;

		UpdateHistory(_entryColors, color, EntryHistorySize);

		if (!TryGetHistoryValue(_entryColors, EntrySignalOffset, out var currentColor) ||
			!TryGetHistoryValue(_entryColors, EntrySignalOffset + 1, out var previousColor))
		{
			return;
		}

		var closeLong = CloseLongOnEntrySignal && previousColor == 2;
		var closeShort = CloseShortOnEntrySignal && previousColor == 0;
		var shouldOpenLong = false;
		var shouldOpenShort = false;

		if (_trend < 0)
		{
			if (CloseLongOnTrendFlip)
				closeLong = true;

			if (EnableShortEntries && currentColor < 2 && previousColor == 2)
				shouldOpenShort = true;
		}
		else if (_trend > 0)
		{
			if (CloseShortOnTrendFlip)
				closeShort = true;

			if (EnableLongEntries && currentColor > 0 && previousColor == 0)
				shouldOpenLong = true;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (closeLong && Position > 0)
			SellMarket(Position);

		if (closeShort && Position < 0)
			BuyMarket(-Position);

		if (shouldOpenLong && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (shouldOpenShort && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private static void UpdateQueue(Queue<decimal> queue, decimal value, int maxLength)
	{
		queue.Enqueue(value);

		while (queue.Count > maxLength)
			queue.Dequeue();
	}

	private static void UpdateHistory(List<int> history, int value, int maxSize)
	{
		history.Insert(0, value);

		if (history.Count > maxSize)
			history.RemoveAt(history.Count - 1);
	}

	private static bool TryGetHistoryValue(List<int> history, int shift, out int value)
	{
		if (shift < 0 || shift >= history.Count)
		{
			value = default;
			return false;
		}

		value = history[shift];
		return true;
	}

	private static IIndicator CreateMovingAverage(XPeriodCandleSmoothingMethod method, int length, decimal phase)
	{
		length = Math.Max(1, length);

		IIndicator indicator = method switch
		{
			XPeriodCandleSmoothingMethod.Simple => new SimpleMovingAverage { Length = length },
			XPeriodCandleSmoothingMethod.Exponential => new ExponentialMovingAverage { Length = length },
			XPeriodCandleSmoothingMethod.Smoothed => new SMMA { Length = length },
			XPeriodCandleSmoothingMethod.Weighted => new WeightedMovingAverage { Length = length },
			XPeriodCandleSmoothingMethod.Hull => new HullMovingAverage { Length = length },
			XPeriodCandleSmoothingMethod.KaufmanAdaptive => new KaufmanAdaptiveMovingAverage { Length = length },
			_ => new JurikMovingAverage { Length = length }
		};

		if (indicator is JurikMovingAverage jurik)
		{
			var clamped = Math.Max(-100m, Math.Min(100m, phase));
			jurik.Phase = clamped;
		}

		return indicator;
	}
}
