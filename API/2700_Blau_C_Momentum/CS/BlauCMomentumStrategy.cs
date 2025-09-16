using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Blau C-Momentum strategy converted from the MetaTrader expert advisor.
/// The strategy processes Blau's triple smoothed momentum and reacts either to zero breakouts or twists.
/// </summary>
public class BlauCMomentumStrategy : Strategy
{
	private readonly StrategyParam<decimal> _moneyManagement;
	private readonly StrategyParam<MarginMode> _marginMode;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;
	private readonly StrategyParam<EntryMode> _entryMode;
	private readonly StrategyParam<DataType> _candleType;

	private readonly StrategyParam<SmoothMethod> _smoothingMethod;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _firstSmoothLength;
	private readonly StrategyParam<int> _secondSmoothLength;
	private readonly StrategyParam<int> _thirdSmoothLength;
	private readonly StrategyParam<int> _phase;
	private readonly StrategyParam<AppliedPrice> _priceForClose;
	private readonly StrategyParam<AppliedPrice> _priceForOpen;
	private readonly StrategyParam<int> _signalBar;

	private BlauMomentumCalculator? _momentum;
	private readonly List<decimal> _indicatorHistory = new();
	private TimeSpan _candleSpan;
	private DateTimeOffset? _longTradeBlockUntil;
	private DateTimeOffset? _shortTradeBlockUntil;

	/// <summary>
	/// Initializes a new instance of the <see cref="BlauCMomentumStrategy"/> class.
	/// </summary>
	public BlauCMomentumStrategy()
	{
		_moneyManagement = Param(nameof(MoneyManagement), 0.1m)
			.SetDisplay("Money Management", "Fraction of capital used to size positions (negative value = fixed volume)", "Trading")
			.SetCanOptimize();

		_marginMode = Param(nameof(MarginMode), MarginMode.FreeMarginShare)
			.SetDisplay("Margin Mode", "Interpretation of money management parameter", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk")
			.SetCanOptimize();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk")
			.SetCanOptimize();

		_slippagePoints = Param(nameof(SlippagePoints), 10)
			.SetDisplay("Max Slippage", "Maximum slippage allowed in points", "Trading");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
			.SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
			.SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading");

		_enableLongExit = Param(nameof(EnableLongExit), true)
			.SetDisplay("Enable Long Exit", "Allow closing long positions", "Trading");

		_enableShortExit = Param(nameof(EnableShortExit), true)
			.SetDisplay("Enable Short Exit", "Allow closing short positions", "Trading");

		_entryMode = Param(nameof(EntryMode), EntryMode.Twist)
			.SetDisplay("Entry Mode", "Choose between zero breakout or twist logic", "Logic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Indicator Timeframe", "Candle type used for indicator calculations", "Data");

		_smoothingMethod = Param(nameof(SmoothingMethod), SmoothMethod.Exponential)
			.SetDisplay("Smoothing Method", "Smoothing method applied to the momentum", "Indicator")
			.SetCanOptimize();

		_momentumLength = Param(nameof(MomentumLength), 1)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Depth of raw momentum calculation", "Indicator")
			.SetCanOptimize();

		_firstSmoothLength = Param(nameof(FirstSmoothLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("First Smooth", "Length of the first smoothing stage", "Indicator")
			.SetCanOptimize();

		_secondSmoothLength = Param(nameof(SecondSmoothLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Second Smooth", "Length of the second smoothing stage", "Indicator")
			.SetCanOptimize();

		_thirdSmoothLength = Param(nameof(ThirdSmoothLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Third Smooth", "Length of the third smoothing stage", "Indicator")
			.SetCanOptimize();

		_phase = Param(nameof(Phase), 15)
			.SetDisplay("Phase", "Phase parameter used by Jurik-style moving averages", "Indicator");

		_priceForClose = Param(nameof(PriceForClose), AppliedPrice.Close)
			.SetDisplay("Close Price Source", "Applied price used as the reference close", "Indicator");

		_priceForOpen = Param(nameof(PriceForOpen), AppliedPrice.Open)
			.SetDisplay("Open Price Source", "Applied price used for the entry reference", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Bar index used for generating entry signals", "Logic")
			.SetCanOptimize();
	}

	/// <summary>
	/// Fraction of capital (or fixed lot size) used for trading.
	/// </summary>
	public decimal MoneyManagement
	{
		get => _moneyManagement.Value;
		set => _moneyManagement.Value = value;
	}

	/// <summary>
	/// Interpretation of the money management parameter.
	/// </summary>
	public MarginMode MarginMode
	{
		get => _marginMode.Value;
		set => _marginMode.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in instrument price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in instrument price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Maximum tolerated slippage in points.
	/// </summary>
	public int SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Enable closing long positions on indicator signals.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Enable closing short positions on indicator signals.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Entry logic: zero-line breakdown or twist detection.
	/// </summary>
	public EntryMode EntryMode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the indicator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to Blau momentum.
	/// </summary>
	public SmoothMethod SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Momentum averaging depth.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// First smoothing stage length.
	/// </summary>
	public int FirstSmoothLength
	{
		get => _firstSmoothLength.Value;
		set => _firstSmoothLength.Value = value;
	}

	/// <summary>
	/// Second smoothing stage length.
	/// </summary>
	public int SecondSmoothLength
	{
		get => _secondSmoothLength.Value;
		set => _secondSmoothLength.Value = value;
	}

	/// <summary>
	/// Third smoothing stage length.
	/// </summary>
	public int ThirdSmoothLength
	{
		get => _thirdSmoothLength.Value;
		set => _thirdSmoothLength.Value = value;
	}

	/// <summary>
	/// Phase parameter used by Jurik-styled smoothing.
	/// </summary>
	public int Phase
	{
		get => _phase.Value;
		set => _phase.Value = value;
	}

	/// <summary>
	/// Applied price for the "closing" component.
	/// </summary>
	public AppliedPrice PriceForClose
	{
		get => _priceForClose.Value;
		set => _priceForClose.Value = value;
	}

	/// <summary>
	/// Applied price for the "opening" component.
	/// </summary>
	public AppliedPrice PriceForOpen
	{
		get => _priceForOpen.Value;
		set => _priceForOpen.Value = value;
	}

	/// <summary>
	/// Index of the bar used for generating entry signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
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

		_indicatorHistory.Clear();
		_momentum?.Reset();
		_longTradeBlockUntil = null;
		_shortTradeBlockUntil = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicatorHistory.Clear();
		_momentum = new BlauMomentumCalculator(
			SmoothingMethod,
			MomentumLength,
			FirstSmoothLength,
			SecondSmoothLength,
			ThirdSmoothLength,
			Phase,
			PriceForClose,
			PriceForOpen
		);

		_candleSpan = CandleType.Arg is TimeSpan frame ? frame : TimeSpan.Zero;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 0m;
		var takeProfitUnit = TakeProfitPoints > 0 && step > 0m ? new Unit(TakeProfitPoints * step, UnitTypes.Absolute) : null;
		var stopLossUnit = StopLossPoints > 0 && step > 0m ? new Unit(StopLossPoints * step, UnitTypes.Absolute) : null;
		StartProtection(takeProfitUnit, stopLossUnit);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished || _momentum is null)
	return;

	var step = Security?.PriceStep ?? 1m;
	var indicatorValue = _momentum.Process(candle, step);
	if (indicatorValue is null)
	return;

	_indicatorHistory.Add(indicatorValue.Value);

	var requiredHistory = Math.Max(SignalBar + 3, 5);
	if (_indicatorHistory.Count > requiredHistory)
	_indicatorHistory.RemoveRange(0, _indicatorHistory.Count - requiredHistory);

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var current = GetHistoryValue(SignalBar);
	var previous = GetHistoryValue(SignalBar + 1);

	if (current is null || previous is null)
	return;

	var closeShort = false;
	var closeLong = false;
	var openLong = false;
	var openShort = false;

	switch (EntryMode)
	{
	case EntryMode.Breakdown:
	{
	if (previous.Value > 0m)
	{
	if (EnableLongEntry && current.Value <= 0m)
	{
	openLong = true;
	}

	if (EnableShortExit)
	{
	closeShort = true;
	}
	}

	if (previous.Value < 0m)
	{
	if (EnableShortEntry && current.Value >= 0m)
	{
	openShort = true;
	}

	if (EnableLongExit)
	{
	closeLong = true;
	}
	}
	break;
	}
	case EntryMode.Twist:
	{
	var older = GetHistoryValue(SignalBar + 2);
	if (older is null)
	return;

	if (previous.Value < older.Value)
	{
	if (EnableLongEntry && current.Value >= previous.Value)
	{
	openLong = true;
	}

	if (EnableShortExit)
	{
	closeShort = true;
	}
	}

	if (previous.Value > older.Value)
	{
	if (EnableShortEntry && current.Value <= previous.Value)
	{
	openShort = true;
	}

	if (EnableLongExit)
	{
	closeLong = true;
	}
	}
	break;
	}
	}

	if (closeLong && Position > 0m)
	{
	SellMarket(Position);
	}

	if (closeShort && Position < 0m)
	{
	BuyMarket(-Position);
	}

	if (openLong && Position <= 0m && CanEnterLong(candle.OpenTime))
	{
	var volume = CalculateTradeVolume(candle.ClosePrice);
	if (volume > 0m)
	{
	var totalVolume = volume + Math.Max(0m, -Position);
	if (totalVolume > 0m)
	{
	BuyMarket(totalVolume);
	SetLongBlock(candle.OpenTime);
	}
	}
	}

	if (openShort && Position >= 0m && CanEnterShort(candle.OpenTime))
	{
	var volume = CalculateTradeVolume(candle.ClosePrice);
	if (volume > 0m)
	{
	var totalVolume = volume + Math.Max(0m, Position);
	if (totalVolume > 0m)
	{
	SellMarket(totalVolume);
	SetShortBlock(candle.OpenTime);
	}
	}
	}
	}

	private decimal? GetHistoryValue(int shift)
	{
	if (shift < 0)
	return null;

	var index = _indicatorHistory.Count - shift - 1;
	if (index < 0 || index >= _indicatorHistory.Count)
	return null;

	return _indicatorHistory[index];
	}

	private bool CanEnterLong(DateTimeOffset signalTime)
	{
	return !_longTradeBlockUntil.HasValue || signalTime >= _longTradeBlockUntil.Value;
	}

	private bool CanEnterShort(DateTimeOffset signalTime)
	{
	return !_shortTradeBlockUntil.HasValue || signalTime >= _shortTradeBlockUntil.Value;
	}

	private void SetLongBlock(DateTimeOffset signalTime)
	{
	_longTradeBlockUntil = _candleSpan != TimeSpan.Zero ? signalTime + _candleSpan : signalTime;
	}

	private void SetShortBlock(DateTimeOffset signalTime)
	{
	_shortTradeBlockUntil = _candleSpan != TimeSpan.Zero ? signalTime + _candleSpan : signalTime;
	}

	private decimal CalculateTradeVolume(decimal price)
	{
	if (price <= 0m)
	return 0m;

	var step = Security?.VolumeStep ?? 1m;
	var minVolume = Security?.MinVolume ?? step;
	var capital = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
	var moneyManagement = MoneyManagement;
	decimal volume;

	if (moneyManagement < 0m)
	{
	volume = Math.Abs(moneyManagement);
	}
	else
	{
	if (capital <= 0m)
	return minVolume;

	switch (MarginMode)
	{
	case MarginMode.FreeMarginShare:
	case MarginMode.BalanceShare:
	{
	var budget = capital * moneyManagement;
	volume = budget / price;
	break;
	}
	case MarginMode.FreeMarginRisk:
	case MarginMode.BalanceRisk:
	{
	var riskCapital = capital * moneyManagement;
	var stepPrice = Security?.StepPrice ?? 1m;
	var stopLoss = StopLossPoints > 0 ? StopLossPoints * stepPrice : price;
	volume = stopLoss > 0m ? riskCapital / stopLoss : riskCapital / price;
	break;
	}
	default:
	{
	var budget = capital * moneyManagement;
	volume = budget / price;
	break;
	}
	}
	}

	if (step > 0m && volume > 0m)
	{
	volume = Math.Floor(volume / step) * step;
	}

	if (volume < minVolume)
	volume = minVolume;

	return volume;
	}

	/// <summary>
	/// Entry mode replication.
	/// </summary>
	public enum EntryMode
	{
	/// <summary>
	/// Entry when the indicator breaks zero.
	/// </summary>
	Breakdown,

	/// <summary>
	/// Entry when the indicator changes direction (twist).
	/// </summary>
	Twist
	}

	/// <summary>
	/// Applied price selection.
	/// </summary>
	public enum AppliedPrice
	{
	/// <summary>
	/// Closing price.
	/// </summary>
	Close = 1,

	/// <summary>
	/// Opening price.
	/// </summary>
	Open,

	/// <summary>
	/// High price.
	/// </summary>
	High,

	/// <summary>
	/// Low price.
	/// </summary>
	Low,

	/// <summary>
	/// Median price (HL/2).
	/// </summary>
	Median,

	/// <summary>
	/// Typical price (HLC/3).
	/// </summary>
	Typical,

	/// <summary>
	/// Weighted close (HLCC/4).
	/// </summary>
	Weighted,

	/// <summary>
	/// Simple price (OC/2).
	/// </summary>
	Simple,

	/// <summary>
	/// Quarted price (HLOC/4).
	/// </summary>
	Quarter,

	/// <summary>
	/// Trend-following price variant 1.
	/// </summary>
	TrendFollow1,

	/// <summary>
	/// Trend-following price variant 2.
	/// </summary>
	TrendFollow2,

	/// <summary>
	/// Demark price.
	/// </summary>
	Demark
	}

	/// <summary>
	/// Money management interpretation.
	/// </summary>
	public enum MarginMode
	{
	/// <summary>
	/// Use a fraction of account capital (approximation of free margin share).
	/// </summary>
	FreeMarginShare = 0,

	/// <summary>
	/// Use a fraction of balance (treated equally to free margin share in this port).
	/// </summary>
	BalanceShare = 1,

	/// <summary>
	/// Risk a fraction of capital with stop-loss distance.
	/// </summary>
	FreeMarginRisk = 2,

	/// <summary>
	/// Risk a fraction of balance with stop-loss distance.
	/// </summary>
	BalanceRisk = 3
	}

	/// <summary>
	/// Smoothing methods available for Blau momentum.
	/// </summary>
	public enum SmoothMethod
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
	/// Smoothed moving average (RMA/SMMA).
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	LinearWeighted,

	/// <summary>
	/// Jurik moving average.
	/// </summary>
	Jurik,

	/// <summary>
	/// Triple exponential moving average (approximation of T3).
	/// </summary>
	TripleExponential,

	/// <summary>
	/// Kaufman adaptive moving average.
	/// </summary>
	Adaptive
	}

	private sealed class BlauMomentumCalculator
	{
	private readonly SmoothMethod _method;
	private readonly int _momentumLength;
	private readonly int _firstLength;
	private readonly int _secondLength;
	private readonly int _thirdLength;
	private readonly int _phase;
	private readonly AppliedPrice _price1;
	private readonly AppliedPrice _price2;

	private readonly Queue<decimal> _priceBuffer = new();
	private readonly LengthIndicator<decimal> _ma1;
	private readonly LengthIndicator<decimal> _ma2;
	private readonly LengthIndicator<decimal> _ma3;

	public BlauMomentumCalculator(
	SmoothMethod method,
	int momentumLength,
	int firstLength,
	int secondLength,
	int thirdLength,
	int phase,
	AppliedPrice price1,
	AppliedPrice price2)
	{
	_method = method;
	_momentumLength = Math.Max(1, momentumLength);
	_firstLength = Math.Max(1, firstLength);
	_secondLength = Math.Max(1, secondLength);
	_thirdLength = Math.Max(1, thirdLength);
	_phase = phase;
	_price1 = price1;
	_price2 = price2;

	_ma1 = CreateMovingAverage(method, _firstLength, _phase);
	_ma2 = CreateMovingAverage(method, _secondLength, _phase);
	_ma3 = CreateMovingAverage(method, _thirdLength, _phase);
	}

	public decimal? Process(ICandleMessage candle, decimal point)
	{
	var value1 = GetAppliedPrice(_price1, candle);
	var value2 = GetAppliedPrice(_price2, candle);

	_priceBuffer.Enqueue(value2);
	if (_priceBuffer.Count > _momentumLength)
	_priceBuffer.Dequeue();

	if (_priceBuffer.Count < _momentumLength)
	return null;

	var reference = _priceBuffer.Peek();
	var momentum = value1 - reference;
	var time = candle.OpenTime;

	var smooth1 = _ma1.Process(momentum, time, true).ToDecimal();
	var smooth2 = _ma2.Process(smooth1, time, true).ToDecimal();
	var smooth3 = _ma3.Process(smooth2, time, true).ToDecimal();

	if (!_ma3.IsFormed)
	return null;

	return point > 0m ? smooth3 * 100m / point : smooth3;
	}

	public void Reset()
	{
	_priceBuffer.Clear();
	_ma1.Reset();
	_ma2.Reset();
	_ma3.Reset();
	}

	private static LengthIndicator<decimal> CreateMovingAverage(SmoothMethod method, int length, int phase)
	{
	return method switch
	{
	SmoothMethod.Simple => new SimpleMovingAverage { Length = length },
	SmoothMethod.Exponential => new ExponentialMovingAverage { Length = length },
	SmoothMethod.Smoothed => new SmoothedMovingAverage { Length = length },
	SmoothMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
	SmoothMethod.Jurik => new JurikMovingAverage { Length = length, Phase = phase },
	SmoothMethod.TripleExponential => new TripleExponentialMovingAverage { Length = length },
	SmoothMethod.Adaptive => new KaufmanAdaptiveMovingAverage { Length = length },
	_ => new ExponentialMovingAverage { Length = length }
	};
	}

	private static decimal GetAppliedPrice(AppliedPrice price, ICandleMessage candle)
	{
	return price switch
	{
	AppliedPrice.Close => candle.ClosePrice,
	AppliedPrice.Open => candle.OpenPrice,
	AppliedPrice.High => candle.HighPrice,
	AppliedPrice.Low => candle.LowPrice,
	AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
	AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
	AppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
	AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
	AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
	AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
	AppliedPrice.TrendFollow2 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
	AppliedPrice.Demark => CalculateDemarkPrice(candle),
	_ => candle.ClosePrice
	};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
	var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;
	if (candle.ClosePrice < candle.OpenPrice)
	res = (res + candle.LowPrice) / 2m;
	else if (candle.ClosePrice > candle.OpenPrice)
	res = (res + candle.HighPrice) / 2m;
	else
	res = (res + candle.ClosePrice) / 2m;

	return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
	}
	}
}
