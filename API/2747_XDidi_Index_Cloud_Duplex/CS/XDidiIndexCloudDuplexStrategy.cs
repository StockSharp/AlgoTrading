using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades crossings between fast and slow XDidi index ratios calculated for long and short configurations.
/// </summary>
public class XDidiIndexCloudDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<SmoothingMethod> _longFastMethod;
	private readonly StrategyParam<int> _longFastLength;
	private readonly StrategyParam<SmoothingMethod> _longMediumMethod;
	private readonly StrategyParam<int> _longMediumLength;
	private readonly StrategyParam<SmoothingMethod> _longSlowMethod;
	private readonly StrategyParam<int> _longSlowLength;
	private readonly StrategyParam<AppliedPrice> _longAppliedPrice;
	private readonly StrategyParam<bool> _longEnableEntry;
	private readonly StrategyParam<bool> _longEnableExit;
	private readonly StrategyParam<bool> _longReverse;
	private readonly StrategyParam<int> _longSignalBar;

	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<SmoothingMethod> _shortFastMethod;
	private readonly StrategyParam<int> _shortFastLength;
	private readonly StrategyParam<SmoothingMethod> _shortMediumMethod;
	private readonly StrategyParam<int> _shortMediumLength;
	private readonly StrategyParam<SmoothingMethod> _shortSlowMethod;
	private readonly StrategyParam<int> _shortSlowLength;
	private readonly StrategyParam<AppliedPrice> _shortAppliedPrice;
	private readonly StrategyParam<bool> _shortEnableEntry;
	private readonly StrategyParam<bool> _shortEnableExit;
	private readonly StrategyParam<bool> _shortReverse;
	private readonly StrategyParam<int> _shortSignalBar;

	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private LengthIndicator<decimal> _longFastMa = null!;
	private LengthIndicator<decimal> _longMediumMa = null!;
	private LengthIndicator<decimal> _longSlowMa = null!;
	private LengthIndicator<decimal> _shortFastMa = null!;
	private LengthIndicator<decimal> _shortMediumMa = null!;
	private LengthIndicator<decimal> _shortSlowMa = null!;

	private decimal?[] _longFastHistory = Array.Empty<decimal?>();
	private decimal?[] _longSlowHistory = Array.Empty<decimal?>();
	private decimal?[] _shortFastHistory = Array.Empty<decimal?>();
	private decimal?[] _shortSlowHistory = Array.Empty<decimal?>();

	/// <summary>
	/// Initializes a new instance of the <see cref="XDidiIndexCloudDuplexStrategy"/> class.
	/// </summary>
	public XDidiIndexCloudDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Candle Type", "Timeframe used for the long XDidi calculation", "General");

		_longFastMethod = Param(nameof(LongFastMethod), SmoothingMethod.Sma)
			.SetDisplay("Long Fast Method", "Smoothing method for the short moving average in the long block", "Indicators");

		_longFastLength = Param(nameof(LongFastLength), 3)
			.SetDisplay("Long Fast Length", "Length for the short moving average in the long block", "Indicators")
			.SetGreaterThanZero();

		_longMediumMethod = Param(nameof(LongMediumMethod), SmoothingMethod.Sma)
			.SetDisplay("Long Medium Method", "Smoothing method for the middle moving average in the long block", "Indicators");

		_longMediumLength = Param(nameof(LongMediumLength), 8)
			.SetDisplay("Long Medium Length", "Length for the middle moving average in the long block", "Indicators")
			.SetGreaterThanZero();

		_longSlowMethod = Param(nameof(LongSlowMethod), SmoothingMethod.Sma)
			.SetDisplay("Long Slow Method", "Smoothing method for the slow moving average in the long block", "Indicators");

		_longSlowLength = Param(nameof(LongSlowLength), 20)
			.SetDisplay("Long Slow Length", "Length for the slow moving average in the long block", "Indicators")
			.SetGreaterThanZero();

		_longAppliedPrice = Param(nameof(LongAppliedPrice), AppliedPrice.Close)
			.SetDisplay("Long Applied Price", "Price source used for the long XDidi calculation", "Indicators");

		_longEnableEntry = Param(nameof(EnableLongEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_longEnableExit = Param(nameof(EnableLongExits), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions", "Trading");

		_longReverse = Param(nameof(LongReverse), false)
			.SetDisplay("Reverse Long Ratios", "Invert long XDidi ratios (matches original indicator option)", "Indicators");

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetDisplay("Long Signal Bar", "Bar shift used for long signals", "Trading")
			.SetGreaterOrEqualZero();

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Short Candle Type", "Timeframe used for the short XDidi calculation", "General");

		_shortFastMethod = Param(nameof(ShortFastMethod), SmoothingMethod.Sma)
			.SetDisplay("Short Fast Method", "Smoothing method for the short moving average in the short block", "Indicators");

		_shortFastLength = Param(nameof(ShortFastLength), 3)
			.SetDisplay("Short Fast Length", "Length for the short moving average in the short block", "Indicators")
			.SetGreaterThanZero();

		_shortMediumMethod = Param(nameof(ShortMediumMethod), SmoothingMethod.Sma)
			.SetDisplay("Short Medium Method", "Smoothing method for the middle moving average in the short block", "Indicators");

		_shortMediumLength = Param(nameof(ShortMediumLength), 8)
			.SetDisplay("Short Medium Length", "Length for the middle moving average in the short block", "Indicators")
			.SetGreaterThanZero();

		_shortSlowMethod = Param(nameof(ShortSlowMethod), SmoothingMethod.Sma)
			.SetDisplay("Short Slow Method", "Smoothing method for the slow moving average in the short block", "Indicators");

		_shortSlowLength = Param(nameof(ShortSlowLength), 20)
			.SetDisplay("Short Slow Length", "Length for the slow moving average in the short block", "Indicators")
			.SetGreaterThanZero();

		_shortAppliedPrice = Param(nameof(ShortAppliedPrice), AppliedPrice.Close)
			.SetDisplay("Short Applied Price", "Price source used for the short XDidi calculation", "Indicators");

		_shortEnableEntry = Param(nameof(EnableShortEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_shortEnableExit = Param(nameof(EnableShortExits), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions", "Trading");

		_shortReverse = Param(nameof(ShortReverse), false)
			.SetDisplay("Reverse Short Ratios", "Invert short XDidi ratios (matches original indicator option)", "Indicators");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetDisplay("Short Signal Bar", "Bar shift used for short signals", "Trading")
			.SetGreaterOrEqualZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss Points", "Protective stop in price steps applied to both directions", "Risk")
			.SetGreaterOrEqualZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit Points", "Protective target in price steps applied to both directions", "Risk")
			.SetGreaterOrEqualZero();
	}

	/// <summary>
	/// Candle type used for the long XDidi calculation.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Smoothing method for the fast moving average in the long block.
	/// </summary>
	public SmoothingMethod LongFastMethod
	{
		get => _longFastMethod.Value;
		set => _longFastMethod.Value = value;
	}

	/// <summary>
	/// Length for the fast moving average in the long block.
	/// </summary>
	public int LongFastLength
	{
		get => _longFastLength.Value;
		set => _longFastLength.Value = value;
	}

	/// <summary>
	/// Smoothing method for the medium moving average in the long block.
	/// </summary>
	public SmoothingMethod LongMediumMethod
	{
		get => _longMediumMethod.Value;
		set => _longMediumMethod.Value = value;
	}

	/// <summary>
	/// Length for the medium moving average in the long block.
	/// </summary>
	public int LongMediumLength
	{
		get => _longMediumLength.Value;
		set => _longMediumLength.Value = value;
	}

	/// <summary>
	/// Smoothing method for the slow moving average in the long block.
	/// </summary>
	public SmoothingMethod LongSlowMethod
	{
		get => _longSlowMethod.Value;
		set => _longSlowMethod.Value = value;
	}

	/// <summary>
	/// Length for the slow moving average in the long block.
	/// </summary>
	public int LongSlowLength
	{
		get => _longSlowLength.Value;
		set => _longSlowLength.Value = value;
	}

	/// <summary>
	/// Applied price for the long XDidi calculation.
	/// </summary>
	public AppliedPrice LongAppliedPrice
	{
		get => _longAppliedPrice.Value;
		set => _longAppliedPrice.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _longEnableEntry.Value;
		set => _longEnableEntry.Value = value;
	}

	/// <summary>
	/// Enable closing long positions.
	/// </summary>
	public bool EnableLongExits
	{
		get => _longEnableExit.Value;
		set => _longEnableExit.Value = value;
	}

	/// <summary>
	/// Invert ratios for the long XDidi block.
	/// </summary>
	public bool LongReverse
	{
		get => _longReverse.Value;
		set => _longReverse.Value = value;
	}

	/// <summary>
	/// Bar shift used for long signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Candle type used for the short XDidi calculation.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Smoothing method for the fast moving average in the short block.
	/// </summary>
	public SmoothingMethod ShortFastMethod
	{
		get => _shortFastMethod.Value;
		set => _shortFastMethod.Value = value;
	}

	/// <summary>
	/// Length for the fast moving average in the short block.
	/// </summary>
	public int ShortFastLength
	{
		get => _shortFastLength.Value;
		set => _shortFastLength.Value = value;
	}

	/// <summary>
	/// Smoothing method for the medium moving average in the short block.
	/// </summary>
	public SmoothingMethod ShortMediumMethod
	{
		get => _shortMediumMethod.Value;
		set => _shortMediumMethod.Value = value;
	}

	/// <summary>
	/// Length for the medium moving average in the short block.
	/// </summary>
	public int ShortMediumLength
	{
		get => _shortMediumLength.Value;
		set => _shortMediumLength.Value = value;
	}

	/// <summary>
	/// Smoothing method for the slow moving average in the short block.
	/// </summary>
	public SmoothingMethod ShortSlowMethod
	{
		get => _shortSlowMethod.Value;
		set => _shortSlowMethod.Value = value;
	}

	/// <summary>
	/// Length for the slow moving average in the short block.
	/// </summary>
	public int ShortSlowLength
	{
		get => _shortSlowLength.Value;
		set => _shortSlowLength.Value = value;
	}

	/// <summary>
	/// Applied price for the short XDidi calculation.
	/// </summary>
	public AppliedPrice ShortAppliedPrice
	{
		get => _shortAppliedPrice.Value;
		set => _shortAppliedPrice.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _shortEnableEntry.Value;
		set => _shortEnableEntry.Value = value;
	}

	/// <summary>
	/// Enable closing short positions.
	/// </summary>
	public bool EnableShortExits
	{
		get => _shortEnableExit.Value;
		set => _shortEnableExit.Value = value;
	}

	/// <summary>
	/// Invert ratios for the short XDidi block.
	/// </summary>
	public bool ShortReverse
	{
		get => _shortReverse.Value;
		set => _shortReverse.Value = value;
	}

	/// <summary>
	/// Bar shift used for short signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	if (ShortCandleType == LongCandleType)
	return [(Security, LongCandleType)];

	return new[]
	{
	(Security, LongCandleType),
	(Security, ShortCandleType)
	};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_longFastHistory = Array.Empty<decimal?>();
	_longSlowHistory = Array.Empty<decimal?>();
	_shortFastHistory = Array.Empty<decimal?>();
	_shortSlowHistory = Array.Empty<decimal?>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_longFastMa = CreateMovingAverage(LongFastMethod, LongFastLength);
	_longMediumMa = CreateMovingAverage(LongMediumMethod, LongMediumLength);
	_longSlowMa = CreateMovingAverage(LongSlowMethod, LongSlowLength);

	_shortFastMa = CreateMovingAverage(ShortFastMethod, ShortFastLength);
	_shortMediumMa = CreateMovingAverage(ShortMediumMethod, ShortMediumLength);
	_shortSlowMa = CreateMovingAverage(ShortSlowMethod, ShortSlowLength);

	_longFastHistory = new decimal?[Math.Max(LongSignalBar + 2, 2)];
	_longSlowHistory = new decimal?[Math.Max(LongSignalBar + 2, 2)];
	_shortFastHistory = new decimal?[Math.Max(ShortSignalBar + 2, 2)];
	_shortSlowHistory = new decimal?[Math.Max(ShortSignalBar + 2, 2)];

	var longSubscription = SubscribeCandles(LongCandleType);
	longSubscription.Bind(ProcessLongCandle);

	CandleSeries? shortSubscription = null;

	if (ShortCandleType == LongCandleType)
	{
	longSubscription.Bind(ProcessShortCandle);
	longSubscription.Start();
	}
	else
	{
	longSubscription.Start();
	shortSubscription = SubscribeCandles(ShortCandleType);
	shortSubscription.Bind(ProcessShortCandle).Start();
	}

	var primaryArea = CreateChartArea();
	if (primaryArea != null)
	{
	DrawCandles(primaryArea, longSubscription);
	DrawIndicator(primaryArea, _longFastMa);
	DrawIndicator(primaryArea, _longMediumMa);
	DrawIndicator(primaryArea, _longSlowMa);

	if (ShortCandleType == LongCandleType)
	{
	DrawIndicator(primaryArea, _shortFastMa);
	DrawIndicator(primaryArea, _shortMediumMa);
	DrawIndicator(primaryArea, _shortSlowMa);
	}

	DrawOwnTrades(primaryArea);
	}

	if (shortSubscription != null)
	{
	var secondaryArea = CreateChartArea();
	if (secondaryArea != null)
	{
	DrawCandles(secondaryArea, shortSubscription);
	DrawIndicator(secondaryArea, _shortFastMa);
	DrawIndicator(secondaryArea, _shortMediumMa);
	DrawIndicator(secondaryArea, _shortSlowMa);
	DrawOwnTrades(secondaryArea);
	}
	}

	var priceStep = Security?.PriceStep ?? 0m;
	Unit? stopLossUnit = null;
	Unit? takeProfitUnit = null;

	if (priceStep > 0m)
	{
	if (StopLossPoints > 0m)
	stopLossUnit = new Unit(StopLossPoints * priceStep, UnitTypes.Absolute);

	if (TakeProfitPoints > 0m)
	takeProfitUnit = new Unit(TakeProfitPoints * priceStep, UnitTypes.Absolute);
	}

	if (stopLossUnit != null || takeProfitUnit != null)
	StartProtection(stopLoss: stopLossUnit, takeProfit: takeProfitUnit);
	else
	StartProtection();
	}

	private void ProcessLongCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var price = GetAppliedPrice(LongAppliedPrice, candle);

	var fastValue = _longFastMa.Process(new DecimalIndicatorValue(_longFastMa, price, candle.OpenTime));
	var mediumValue = _longMediumMa.Process(new DecimalIndicatorValue(_longMediumMa, price, candle.OpenTime));
	var slowValue = _longSlowMa.Process(new DecimalIndicatorValue(_longSlowMa, price, candle.OpenTime));

	if (!fastValue.IsFinal || !mediumValue.IsFinal || !slowValue.IsFinal)
	return;

	var medium = mediumValue.GetValue<decimal>();
	if (medium == 0m)
	return;

	var fast = fastValue.GetValue<decimal>() / medium;
	var slow = slowValue.GetValue<decimal>() / medium;

	if (LongReverse)
	{
	fast = -fast;
	slow = -slow;
	}

	UpdateHistory(_longFastHistory, fast);
	UpdateHistory(_longSlowHistory, slow);

	if (!HasSignalData(_longFastHistory, _longSlowHistory, LongSignalBar))
	return;

	var currentFast = _longFastHistory[LongSignalBar]!.Value;
	var currentSlow = _longSlowHistory[LongSignalBar]!.Value;
	var previousFast = _longFastHistory[LongSignalBar + 1]!.Value;
	var previousSlow = _longSlowHistory[LongSignalBar + 1]!.Value;

	var openSignal = false;
	var closeSignal = false;

	if (previousFast > previousSlow && EnableLongEntries && currentFast <= currentSlow)
	openSignal = true;

	if (previousFast < previousSlow && EnableLongExits)
	closeSignal = true;

	ExecuteLongSignals(openSignal, closeSignal);
	}

	private void ProcessShortCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var price = GetAppliedPrice(ShortAppliedPrice, candle);

	var fastValue = _shortFastMa.Process(new DecimalIndicatorValue(_shortFastMa, price, candle.OpenTime));
	var mediumValue = _shortMediumMa.Process(new DecimalIndicatorValue(_shortMediumMa, price, candle.OpenTime));
	var slowValue = _shortSlowMa.Process(new DecimalIndicatorValue(_shortSlowMa, price, candle.OpenTime));

	if (!fastValue.IsFinal || !mediumValue.IsFinal || !slowValue.IsFinal)
	return;

	var medium = mediumValue.GetValue<decimal>();
	if (medium == 0m)
	return;

	var fast = fastValue.GetValue<decimal>() / medium;
	var slow = slowValue.GetValue<decimal>() / medium;

	if (ShortReverse)
	{
	fast = -fast;
	slow = -slow;
	}

	UpdateHistory(_shortFastHistory, fast);
	UpdateHistory(_shortSlowHistory, slow);

	if (!HasSignalData(_shortFastHistory, _shortSlowHistory, ShortSignalBar))
	return;

	var currentFast = _shortFastHistory[ShortSignalBar]!.Value;
	var currentSlow = _shortSlowHistory[ShortSignalBar]!.Value;
	var previousFast = _shortFastHistory[ShortSignalBar + 1]!.Value;
	var previousSlow = _shortSlowHistory[ShortSignalBar + 1]!.Value;

	var openSignal = false;
	var closeSignal = false;

	if (previousFast < previousSlow && EnableShortEntries && currentFast >= currentSlow)
	openSignal = true;

	if (previousFast > previousSlow && EnableShortExits)
	closeSignal = true;

	ExecuteShortSignals(openSignal, closeSignal);
	}

	private void ExecuteLongSignals(bool openSignal, bool closeSignal)
	{
	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (closeSignal && Position > 0)
	SellMarket(Position);

	if (openSignal && Position <= 0)
	{
	var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
	if (volume > 0m)
	BuyMarket(volume);
	}
	}

	private void ExecuteShortSignals(bool openSignal, bool closeSignal)
	{
	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (closeSignal && Position < 0)
	BuyMarket(Math.Abs(Position));

	if (openSignal && Position >= 0)
	{
	var volume = Volume + (Position > 0 ? Position : 0m);
	if (volume > 0m)
	SellMarket(volume);
	}
	}

	private static void UpdateHistory(decimal?[] buffer, decimal value)
	{
	for (var i = buffer.Length - 1; i > 0; i--)
	buffer[i] = buffer[i - 1];

	buffer[0] = value;
	}

	private static bool HasSignalData(decimal?[] fastHistory, decimal?[] slowHistory, int signalBar)
	{
	var requiredIndex = signalBar + 1;

	if (requiredIndex >= fastHistory.Length || requiredIndex >= slowHistory.Length)
	return false;

	return fastHistory[signalBar].HasValue &&
	fastHistory[requiredIndex].HasValue &&
	slowHistory[signalBar].HasValue &&
	slowHistory[requiredIndex].HasValue;
	}

	private static decimal GetAppliedPrice(AppliedPrice priceType, ICandleMessage candle)
	{
	return priceType switch
	{
	AppliedPrice.Close => candle.ClosePrice,
	AppliedPrice.Open => candle.OpenPrice,
	AppliedPrice.High => candle.HighPrice,
	AppliedPrice.Low => candle.LowPrice,
	AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
	AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
	AppliedPrice.Weighted => (candle.ClosePrice * 2m + candle.HighPrice + candle.LowPrice) / 4m,
	AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
	AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
	AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
	? candle.HighPrice
	: candle.ClosePrice < candle.OpenPrice
	? candle.LowPrice
	: candle.ClosePrice,
	AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
	? (candle.HighPrice + candle.ClosePrice) / 2m
	: candle.ClosePrice < candle.OpenPrice
	? (candle.LowPrice + candle.ClosePrice) / 2m
	: candle.ClosePrice,
	AppliedPrice.Demark =>
	{
	var baseSum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

	var adjusted = candle.ClosePrice < candle.OpenPrice
	? (baseSum + candle.LowPrice) / 2m
	: candle.ClosePrice > candle.OpenPrice
	? (baseSum + candle.HighPrice) / 2m
	: (baseSum + candle.ClosePrice) / 2m;

	return ((adjusted - candle.LowPrice) + (adjusted - candle.HighPrice)) / 2m;
	},
	_ => candle.ClosePrice,
	};
	}

	private static LengthIndicator<decimal> CreateMovingAverage(SmoothingMethod method, int length)
	{
	return method switch
	{
	SmoothingMethod.Sma => new SimpleMovingAverage { Length = length },
	SmoothingMethod.Ema => new ExponentialMovingAverage { Length = length },
	SmoothingMethod.Smma => new SmoothedMovingAverage { Length = length },
	SmoothingMethod.Lwma => new WeightedMovingAverage { Length = length },
	SmoothingMethod.T3 => new TripleExponentialMovingAverage { Length = length },
	SmoothingMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = length },
	_ => new ExponentialMovingAverage { Length = length }
	};
	}

	/// <summary>
	/// Available smoothing methods that approximate the original MQL implementation.
	/// </summary>
	public enum SmoothingMethod
	{
	Sma,
	Ema,
	Smma,
	Lwma,
	Jjma,
	JurX,
	ParMa,
	T3,
	Vidya,
	Ama
	}

	/// <summary>
	/// Price sources supported by the strategy.
	/// </summary>
	public enum AppliedPrice
	{
	Close = 1,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted,
	Simple,
	Quarter,
	TrendFollow0,
	TrendFollow1,
	Demark
	}
}
