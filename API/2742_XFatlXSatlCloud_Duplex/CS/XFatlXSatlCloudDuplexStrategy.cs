using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Duplex strategy based on XFatlXSatlCloud indicator crossovers.
/// </summary>
public class XFatlXSatlCloudDuplexStrategy : Strategy
{
	private readonly StrategyParam<decimal> _longVolume;
	private readonly StrategyParam<decimal> _shortVolume;
	private readonly StrategyParam<bool> _longAllowOpen;
	private readonly StrategyParam<bool> _longAllowClose;
	private readonly StrategyParam<bool> _shortAllowOpen;
	private readonly StrategyParam<bool> _shortAllowClose;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<XmaMethod> _longMethod1;
	private readonly StrategyParam<int> _longLength1;
	private readonly StrategyParam<int> _longPhase1;
	private readonly StrategyParam<XmaMethod> _longMethod2;
	private readonly StrategyParam<int> _longLength2;
	private readonly StrategyParam<int> _longPhase2;
	private readonly StrategyParam<AppliedPrice> _longPriceType;
	private readonly StrategyParam<XmaMethod> _shortMethod1;
	private readonly StrategyParam<int> _shortLength1;
	private readonly StrategyParam<int> _shortPhase1;
	private readonly StrategyParam<XmaMethod> _shortMethod2;
	private readonly StrategyParam<int> _shortLength2;
	private readonly StrategyParam<int> _shortPhase2;
	private readonly StrategyParam<AppliedPrice> _shortPriceType;
	private readonly StrategyParam<decimal> _longStopLoss;
	private readonly StrategyParam<decimal> _longTakeProfit;
	private readonly StrategyParam<decimal> _shortStopLoss;
	private readonly StrategyParam<decimal> _shortTakeProfit;

	private XFatlXSatlCloudIndicator _longIndicator = null!;
	private XFatlXSatlCloudIndicator _shortIndicator = null!;
	private readonly List<(decimal fast, decimal slow)> _longHistory = new();
	private readonly List<(decimal fast, decimal slow)> _shortHistory = new();
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="XFatlXSatlCloudDuplexStrategy"/>.
	/// </summary>
	public XFatlXSatlCloudDuplexStrategy()
	{
		_longVolume = Param(nameof(LongVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Long Volume", "Order volume for long entries", "Trading");

		_shortVolume = Param(nameof(ShortVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Short Volume", "Order volume for short entries", "Trading");

		_longAllowOpen = Param(nameof(LongAllowOpen), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_longAllowClose = Param(nameof(LongAllowClose), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions", "Trading");

		_shortAllowOpen = Param(nameof(ShortAllowOpen), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_shortAllowClose = Param(nameof(ShortAllowClose), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions", "Trading");

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetGreaterOrEqual(0)
			.SetDisplay("Long Signal Shift", "Bars to look back for long signals", "Signals");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetGreaterOrEqual(0)
			.SetDisplay("Short Signal Shift", "Bars to look back for short signals", "Signals");

		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Candle Type", "Timeframe for long indicator", "Data");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Short Candle Type", "Timeframe for short indicator", "Data");

		_longMethod1 = Param(nameof(LongMethod1), XmaMethod.Jurik)
			.SetDisplay("Long Fast Method", "Smoothing method for the fast long line", "Indicators");

		_longLength1 = Param(nameof(LongLength1), 3)
			.SetGreaterThanZero()
			.SetDisplay("Long Fast Length", "Length for the fast long smoother", "Indicators");

		_longPhase1 = Param(nameof(LongPhase1), 15)
			.SetDisplay("Long Fast Phase", "Phase parameter for the fast long smoother", "Indicators");

		_longMethod2 = Param(nameof(LongMethod2), XmaMethod.Jurik)
			.SetDisplay("Long Slow Method", "Smoothing method for the slow long line", "Indicators");

		_longLength2 = Param(nameof(LongLength2), 5)
			.SetGreaterThanZero()
			.SetDisplay("Long Slow Length", "Length for the slow long smoother", "Indicators");

		_longPhase2 = Param(nameof(LongPhase2), 15)
			.SetDisplay("Long Slow Phase", "Phase parameter for the slow long smoother", "Indicators");

		_longPriceType = Param(nameof(LongAppliedPrice), AppliedPrice.Close)
			.SetDisplay("Long Applied Price", "Price type used for the long indicator", "Indicators");

		_shortMethod1 = Param(nameof(ShortMethod1), XmaMethod.Jurik)
			.SetDisplay("Short Fast Method", "Smoothing method for the fast short line", "Indicators");

		_shortLength1 = Param(nameof(ShortLength1), 3)
			.SetGreaterThanZero()
			.SetDisplay("Short Fast Length", "Length for the fast short smoother", "Indicators");

		_shortPhase1 = Param(nameof(ShortPhase1), 15)
			.SetDisplay("Short Fast Phase", "Phase parameter for the fast short smoother", "Indicators");

		_shortMethod2 = Param(nameof(ShortMethod2), XmaMethod.Jurik)
			.SetDisplay("Short Slow Method", "Smoothing method for the slow short line", "Indicators");

		_shortLength2 = Param(nameof(ShortLength2), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short Slow Length", "Length for the slow short smoother", "Indicators");

		_shortPhase2 = Param(nameof(ShortPhase2), 15)
			.SetDisplay("Short Slow Phase", "Phase parameter for the slow short smoother", "Indicators");

		_shortPriceType = Param(nameof(ShortAppliedPrice), AppliedPrice.Close)
			.SetDisplay("Short Applied Price", "Price type used for the short indicator", "Indicators");

		_longStopLoss = Param(nameof(LongStopLoss), 0m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Long Stop Loss", "Price distance for long stop loss (0 disables)", "Risk");

		_longTakeProfit = Param(nameof(LongTakeProfit), 0m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Long Take Profit", "Price distance for long take profit (0 disables)", "Risk");

		_shortStopLoss = Param(nameof(ShortStopLoss), 0m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Short Stop Loss", "Price distance for short stop loss (0 disables)", "Risk");

		_shortTakeProfit = Param(nameof(ShortTakeProfit), 0m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Short Take Profit", "Price distance for short take profit (0 disables)", "Risk");
	}

	/// <summary>
	/// Gets or sets volume for long trades.
	/// </summary>
	public decimal LongVolume
	{
		get => _longVolume.Value;
		set => _longVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets volume for short trades.
	/// </summary>
	public decimal ShortVolume
	{
		get => _shortVolume.Value;
		set => _shortVolume.Value = value;
	}

	/// <summary>
	/// Allow opening long trades.
	/// </summary>
	public bool LongAllowOpen
	{
		get => _longAllowOpen.Value;
		set => _longAllowOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long trades.
	/// </summary>
	public bool LongAllowClose
	{
		get => _longAllowClose.Value;
		set => _longAllowClose.Value = value;
	}

	/// <summary>
	/// Allow opening short trades.
	/// </summary>
	public bool ShortAllowOpen
	{
		get => _shortAllowOpen.Value;
		set => _shortAllowOpen.Value = value;
	}

	/// <summary>
	/// Allow closing short trades.
	/// </summary>
	public bool ShortAllowClose
	{
		get => _shortAllowClose.Value;
		set => _shortAllowClose.Value = value;
	}

	/// <summary>
	/// Number of bars to shift long signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Number of bars to shift short signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Candle type for long indicator.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Candle type for short indicator.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Smoothing method for fast long line.
	/// </summary>
	public XmaMethod LongMethod1
	{
		get => _longMethod1.Value;
		set => _longMethod1.Value = value;
	}

	/// <summary>
	/// Length for fast long smoother.
	/// </summary>
	public int LongLength1
	{
		get => _longLength1.Value;
		set => _longLength1.Value = value;
	}

	/// <summary>
	/// Phase for fast long smoother.
	/// </summary>
	public int LongPhase1
	{
		get => _longPhase1.Value;
		set => _longPhase1.Value = value;
	}

	/// <summary>
	/// Smoothing method for slow long line.
	/// </summary>
	public XmaMethod LongMethod2
	{
		get => _longMethod2.Value;
		set => _longMethod2.Value = value;
	}

	/// <summary>
	/// Length for slow long smoother.
	/// </summary>
	public int LongLength2
	{
		get => _longLength2.Value;
		set => _longLength2.Value = value;
	}

	/// <summary>
	/// Phase for slow long smoother.
	/// </summary>
	public int LongPhase2
	{
		get => _longPhase2.Value;
		set => _longPhase2.Value = value;
	}

	/// <summary>
	/// Applied price for long calculations.
	/// </summary>
	public AppliedPrice LongAppliedPrice
	{
		get => _longPriceType.Value;
		set => _longPriceType.Value = value;
	}

	/// <summary>
	/// Smoothing method for fast short line.
	/// </summary>
	public XmaMethod ShortMethod1
	{
		get => _shortMethod1.Value;
		set => _shortMethod1.Value = value;
	}

	/// <summary>
	/// Length for fast short smoother.
	/// </summary>
	public int ShortLength1
	{
		get => _shortLength1.Value;
		set => _shortLength1.Value = value;
	}

	/// <summary>
	/// Phase for fast short smoother.
	/// </summary>
	public int ShortPhase1
	{
		get => _shortPhase1.Value;
		set => _shortPhase1.Value = value;
	}

	/// <summary>
	/// Smoothing method for slow short line.
	/// </summary>
	public XmaMethod ShortMethod2
	{
		get => _shortMethod2.Value;
		set => _shortMethod2.Value = value;
	}

	/// <summary>
	/// Length for slow short smoother.
	/// </summary>
	public int ShortLength2
	{
		get => _shortLength2.Value;
		set => _shortLength2.Value = value;
	}

	/// <summary>
	/// Phase for slow short smoother.
	/// </summary>
	public int ShortPhase2
	{
		get => _shortPhase2.Value;
		set => _shortPhase2.Value = value;
	}

	/// <summary>
	/// Applied price for short calculations.
	/// </summary>
	public AppliedPrice ShortAppliedPrice
	{
		get => _shortPriceType.Value;
		set => _shortPriceType.Value = value;
	}

	/// <summary>
	/// Stop loss distance for long positions.
	/// </summary>
	public decimal LongStopLoss
	{
		get => _longStopLoss.Value;
		set => _longStopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance for long positions.
	/// </summary>
	public decimal LongTakeProfit
	{
		get => _longTakeProfit.Value;
		set => _longTakeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance for short positions.
	/// </summary>
	public decimal ShortStopLoss
	{
		get => _shortStopLoss.Value;
		set => _shortStopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance for short positions.
	/// </summary>
	public decimal ShortTakeProfit
	{
		get => _shortTakeProfit.Value;
		set => _shortTakeProfit.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var result = new List<(Security, DataType)> { (Security, LongCandleType) };

		if (ShortCandleType != LongCandleType)
		result.Add((Security, ShortCandleType));

		return result;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_longHistory.Clear();
		_shortHistory.Clear();
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicator instances with current parameter values for both directions.
		_longIndicator = new XFatlXSatlCloudIndicator(LongMethod1, LongLength1, LongPhase1, LongMethod2, LongLength2, LongPhase2, LongAppliedPrice);
		_shortIndicator = new XFatlXSatlCloudIndicator(ShortMethod1, ShortLength1, ShortPhase1, ShortMethod2, ShortLength2, ShortPhase2, ShortAppliedPrice);

		// Subscribe to candles that drive the long side of the strategy.
		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription.BindEx(_longIndicator, ProcessLong).Start();

		// Subscribe separately for the short side (timeframe can differ from the long one).
		var shortSubscription = SubscribeCandles(ShortCandleType);
		shortSubscription.BindEx(_shortIndicator, ProcessShort).Start();
	}

	private void ProcessLong(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
	if (candle.State != CandleStates.Finished || !indicatorValue.IsFinal)
	return;

	var value = (XFatlXSatlValue)indicatorValue;
	// Store the latest indicator readings so we can evaluate the configured shift.
	_longHistory.Insert(0, (value.Fast, value.Slow));
	var maxSize = Math.Max(LongSignalBar + 2, 2);
	if (_longHistory.Count > maxSize)
	_longHistory.RemoveAt(_longHistory.Count - 1);

	// Risk management can close the position immediately before analyzing crossovers.
	if (HandleLongRisk(candle))
	return;

	if (_longHistory.Count <= LongSignalBar + 1)
	return;

	var current = _longHistory[LongSignalBar];
	var previous = _longHistory[LongSignalBar + 1];
	var crossUp = current.fast > current.slow && previous.fast <= previous.slow;
	var crossDown = current.fast < current.slow && previous.fast >= previous.slow;

	// Close an existing long when the fast line drops below the slow line.
	if (LongAllowClose && crossDown && Position > 0m)
	{
	SellMarket(Position);
	_longEntryPrice = null;
	}

	if (!LongAllowOpen || !crossUp)
	return;

	// Flatten shorts before reversing into a long position.
	if (Position < 0m)
	{
	if (!ShortAllowClose)
	return;

	BuyMarket(-Position);
	_shortEntryPrice = null;
	}

	// Open the long trade only if no opposite exposure remains.
	if (Position <= 0m)
	{
	BuyMarket(LongVolume);
	_longEntryPrice = candle.ClosePrice;
	}
	}

	private bool HandleLongRisk(ICandleMessage candle)
	{
	if (!LongAllowClose || Position <= 0m || _longEntryPrice is not decimal entry)
	return false;

	// Hard stop: candle low moved below entry minus configured distance.
	if (LongStopLoss > 0m && candle.LowPrice <= entry - LongStopLoss)
	{
	SellMarket(Position);
	_longEntryPrice = null;
	return true;
	}

	// Hard target: candle high exceeded entry plus configured distance.
	if (LongTakeProfit > 0m && candle.HighPrice >= entry + LongTakeProfit)
	{
	SellMarket(Position);
	_longEntryPrice = null;
	return true;
	}

	return false;
	}

	private void ProcessShort(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
	if (candle.State != CandleStates.Finished || !indicatorValue.IsFinal)
	return;

	var value = (XFatlXSatlValue)indicatorValue;
	// Maintain the rolling history for the short configuration.
	_shortHistory.Insert(0, (value.Fast, value.Slow));
	var maxSize = Math.Max(ShortSignalBar + 2, 2);
	if (_shortHistory.Count > maxSize)
	_shortHistory.RemoveAt(_shortHistory.Count - 1);

	// Stop or target may close the short before trend analysis.
	if (HandleShortRisk(candle))
	return;

	if (_shortHistory.Count <= ShortSignalBar + 1)
	return;

	var current = _shortHistory[ShortSignalBar];
	var previous = _shortHistory[ShortSignalBar + 1];
	var crossDown = current.fast < current.slow && previous.fast >= previous.slow;
	var crossUp = current.fast > current.slow && previous.fast <= previous.slow;

	// Cover a short when the fast line rises above the slow line again.
	if (ShortAllowClose && crossUp && Position < 0m)
	{
	BuyMarket(-Position);
	_shortEntryPrice = null;
	}

	if (!ShortAllowOpen || !crossDown)
	return;

	// Close existing longs before flipping into a short position.
	if (Position > 0m)
	{
	if (!LongAllowClose)
	return;

	SellMarket(Position);
	_longEntryPrice = null;
	}

	// Enter the new short once the direction is clear.
	if (Position >= 0m)
	{
	SellMarket(ShortVolume);
	_shortEntryPrice = candle.ClosePrice;
	}
	}

	private bool HandleShortRisk(ICandleMessage candle)
	{
	if (!ShortAllowClose || Position >= 0m || _shortEntryPrice is not decimal entry)
	return false;

	// Stop loss for the short side is triggered by a move above the entry price.
	if (ShortStopLoss > 0m && candle.HighPrice >= entry + ShortStopLoss)
	{
	BuyMarket(-Position);
	_shortEntryPrice = null;
	return true;
	}

	// Take profit for shorts fires when the low pierces the target distance.
	if (ShortTakeProfit > 0m && candle.LowPrice <= entry - ShortTakeProfit)
	{
	BuyMarket(-Position);
	_shortEntryPrice = null;
	return true;
	}

	return false;
	}

	private sealed class XFatlXSatlCloudIndicator : Indicator<ICandleMessage>
	{
	private static readonly decimal[] FatlCoefficients =
	{
	0.4360409450m,
	0.3658689069m,
	0.2460452079m,
	0.1104506886m,
	-0.0054034585m,
	-0.0760367731m,
	-0.0933058722m,
	-0.0670110374m,
	-0.0190795053m,
	0.0259609206m,
	0.0502044896m,
	0.0477818607m,
	0.0249252327m,
	-0.0047706151m,
	-0.0272432537m,
	-0.0338917071m,
	-0.0244141482m,
	-0.0055774838m,
	0.0128149838m,
	0.0226522218m,
	0.0208778257m,
	0.0100299086m,
	-0.0036771622m,
	-0.0136744850m,
	-0.0160483392m,
	-0.0108597376m,
	-0.0016060704m,
	0.0069480557m,
	0.0110573605m,
	0.0095711419m,
	0.0040444064m,
	-0.0023824623m,
	-0.0067093714m,
	-0.0072003400m,
	-0.0047717710m,
	0.0005541115m,
	0.0007860160m,
	0.0130129076m,
	0.0040364019m,
	};

	private static readonly decimal[] SatlCoefficients =
	{
	0.0982862174m,
	0.0975682269m,
	0.0961401078m,
	0.0940230544m,
	0.0912437090m,
	0.0878391006m,
	0.0838544303m,
	0.0793406350m,
	0.0743569346m,
	0.0689666682m,
	0.0632381578m,
	0.0572428925m,
	0.0510534242m,
	0.0447468229m,
	0.0383959950m,
	0.0320735368m,
	0.0258537721m,
	0.0198005183m,
	0.0139807863m,
	0.0084512448m,
	0.0032639979m,
	-0.0015350359m,
	-0.0059060082m,
	-0.0098190256m,
	-0.0132507215m,
	-0.0161875265m,
	-0.0186164872m,
	-0.0205446727m,
	-0.0219739146m,
	-0.0229204861m,
	-0.0234080863m,
	-0.0234566315m,
	-0.0231017777m,
	-0.0223796900m,
	-0.0213300463m,
	-0.0199924534m,
	-0.0184126992m,
	-0.0166377699m,
	-0.0147139428m,
	-0.0126796776m,
	-0.0105938331m,
	-0.0084736770m,
	-0.0063841850m,
	-0.0043466731m,
	-0.0023956944m,
	-0.0005535180m,
	0.0011421469m,
	0.0026845693m,
	0.0040471369m,
	0.0052380201m,
	0.0062194591m,
	0.0070340085m,
	0.0076266453m,
	0.0080376628m,
	0.0083037666m,
	0.0083694798m,
	0.0082901022m,
	0.0080741359m,
	0.0077543820m,
	0.0073260526m,
	0.0068163569m,
	0.0062325477m,
	0.0056078229m,
	0.0049516078m,
	0.0161380976m,
	};

	private readonly IIndicator _fastSmoother;
	private readonly IIndicator _slowSmoother;
	private readonly AppliedPrice _appliedPrice;
	private readonly decimal[] _priceBuffer = new decimal[SatlCoefficients.Length];
	private int _bufferIndex;
	private int _bufferCount;

	public XFatlXSatlCloudIndicator(XmaMethod fastMethod, int fastLength, int fastPhase, XmaMethod slowMethod, int slowLength, int slowPhase, AppliedPrice appliedPrice)
	{
	_fastSmoother = CreateSmoother(fastMethod, fastLength, fastPhase);
	_slowSmoother = CreateSmoother(slowMethod, slowLength, slowPhase);
	_appliedPrice = appliedPrice;
	}

	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
	var candle = input.GetValue<ICandleMessage>();
	var price = SelectPrice(candle, _appliedPrice);
	// Feed the latest price into the circular buffer used by the FIR filters.
	_priceBuffer[_bufferIndex] = price;
	_bufferIndex = (_bufferIndex + 1) % _priceBuffer.Length;
	if (_bufferCount < _priceBuffer.Length)
	_bufferCount++;

	var fastRaw = ComputeFilter(FatlCoefficients);
	var slowRaw = ComputeFilter(SatlCoefficients);

	// Smooth both raw filters with the configured moving averages.
	var fastValue = _fastSmoother.Process(new DecimalIndicatorValue(_fastSmoother, fastRaw, input.Time));
	var slowValue = _slowSmoother.Process(new DecimalIndicatorValue(_slowSmoother, slowRaw, input.Time));
	var fast = fastValue.ToDecimal();
	var slow = slowValue.ToDecimal();

	IsFormed = _bufferCount >= SatlCoefficients.Length && fastValue.IsFinal && slowValue.IsFinal;
	return new XFatlXSatlValue(this, input, fast, slow, fastRaw, slowRaw);
	}

	public override void Reset()
	{
	base.Reset();
	Array.Clear(_priceBuffer, 0, _priceBuffer.Length);
	_bufferIndex = 0;
	_bufferCount = 0;
	_fastSmoother.Reset();
	_slowSmoother.Reset();
	}

	private decimal ComputeFilter(IReadOnlyList<decimal> coefficients)
	{
	if (_bufferCount < coefficients.Count)
	return 0m;

	decimal sum = 0m;
	for (var i = 0; i < coefficients.Count; i++)
	{
	// Traverse the ring buffer backwards to align with the newest price first.
	var index = _bufferIndex - 1 - i;
	if (index < 0)
	index += _priceBuffer.Length;

	sum += coefficients[i] * _priceBuffer[index];
	}

	return sum;
	}

	private static IIndicator CreateSmoother(XmaMethod method, int length, int phase)
	{
	length = Math.Max(1, length);

	// Map the MQL smoothing options to the closest available StockSharp moving averages.
	return method switch
	{
	XmaMethod.Sma => new SMA { Length = length },
	XmaMethod.Ema => new EMA { Length = length },
	XmaMethod.Smma => new SmoothedMovingAverage { Length = length },
	XmaMethod.Lwma => new WMA { Length = length },
	XmaMethod.Jurik => new JurikMovingAverage { Length = length },
	XmaMethod.ZeroLag => new ZeroLagExponentialMovingAverage { Length = length },
	XmaMethod.Kaufman => new KaufmanAdaptiveMovingAverage { Length = length },
	_ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported smoothing method."),
	};
	}

	private static decimal SelectPrice(ICandleMessage candle, AppliedPrice price)
	{
	return price switch
	{
	AppliedPrice.Open => candle.OpenPrice,
	AppliedPrice.High => candle.HighPrice,
	AppliedPrice.Low => candle.LowPrice,
	AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
	AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
	AppliedPrice.Weighted => (candle.ClosePrice * 2m + candle.HighPrice + candle.LowPrice) / 4m,
	AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
	AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
	AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
	AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
	AppliedPrice.Demark => CalculateDemarkPrice(candle),
	_ => candle.ClosePrice,
	};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
	var sum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;
	if (candle.ClosePrice < candle.OpenPrice)
	sum = (sum + candle.LowPrice) / 2m;
	else if (candle.ClosePrice > candle.OpenPrice)
	sum = (sum + candle.HighPrice) / 2m;
	else
	sum = (sum + candle.ClosePrice) / 2m;

	return ((sum - candle.LowPrice) + (sum - candle.HighPrice)) / 2m;
	}
	}

	private sealed class XFatlXSatlValue : ComplexIndicatorValue
	{
	public XFatlXSatlValue(IIndicator indicator, IIndicatorValue input, decimal fast, decimal slow, decimal fastRaw, decimal slowRaw)
	: base(indicator, input, (nameof(Fast), fast), (nameof(Slow), slow), (nameof(FastRaw), fastRaw), (nameof(SlowRaw), slowRaw))
	{
	}

	public decimal Fast => (decimal)GetValue(nameof(Fast));

	public decimal Slow => (decimal)GetValue(nameof(Slow));

	public decimal FastRaw => (decimal)GetValue(nameof(FastRaw));

	public decimal SlowRaw => (decimal)GetValue(nameof(SlowRaw));
	}
}

/// <summary>
/// Supported smoothing methods for XFatlXSatlCloud indicator.
/// </summary>
public enum XmaMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Sma,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Ema,

	/// <summary>
	/// Smoothed moving average (RMA).
	/// </summary>
	Smma,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Lwma,

	/// <summary>
	/// Jurik moving average.
	/// </summary>
	Jurik,

	/// <summary>
	/// Zero lag exponential moving average.
	/// </summary>
	ZeroLag,

	/// <summary>
	/// Kaufman adaptive moving average.
	/// </summary>
	Kaufman,
}

/// <summary>
/// Price types supported by XFatlXSatlCloud indicator.
/// </summary>
public enum AppliedPrice
{
	/// <summary>
	/// Closing price.
	/// </summary>
	Close,

	/// <summary>
	/// Opening price.
	/// </summary>
	Open,

	/// <summary>
	/// Highest price of the bar.
	/// </summary>
	High,

	/// <summary>
	/// Lowest price of the bar.
	/// </summary>
	Low,

	/// <summary>
	/// (High + Low) / 2.
	/// </summary>
	Median,

	/// <summary>
	/// (High + Low + Close) / 3.
	/// </summary>
	Typical,

	/// <summary>
	/// (Close * 2 + High + Low) / 4.
	/// </summary>
	Weighted,

	/// <summary>
	/// (Open + Close) / 2.
	/// </summary>
	Simple,

	/// <summary>
	/// (Open + Close + High + Low) / 4.
	/// </summary>
	Quarter,

	/// <summary>
	/// Trend follow price using bar extremes.
	/// </summary>
	TrendFollow0,

	/// <summary>
	/// Trend follow price averaging close with the extreme.
	/// </summary>
	TrendFollow1,

	/// <summary>
	/// Demark pivot price.
	/// </summary>
	Demark,
}
