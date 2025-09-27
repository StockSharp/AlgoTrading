using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average breakout strategy converted from the MetaTrader "M.A break" experts.
/// </summary>
public class MaBreakStrategy : Strategy
{
	private readonly StrategyParam<decimal> _priceStepFallback;

	private readonly StrategyParam<int> _fastMa1Period;
	private readonly StrategyParam<int> _slowMa1Period;
	private readonly StrategyParam<int> _fastMa2Period;
	private readonly StrategyParam<int> _slowMa2Period;
	private readonly StrategyParam<int> _openFilterPeriod;
	private readonly StrategyParam<int> _pullbackPeriod;
	private readonly StrategyParam<int> _quietBarsCount;
	private readonly StrategyParam<decimal> _quietBarsMinRange;
	private readonly StrategyParam<decimal> _impulseStrength;
	private readonly StrategyParam<decimal> _bullUpperWickPercent;
	private readonly StrategyParam<decimal> _bullLowerWickPercent;
	private readonly StrategyParam<decimal> _bearUpperWickPercent;
	private readonly StrategyParam<decimal> _bearLowerWickPercent;
	private readonly StrategyParam<decimal> _candleMinSize;
	private readonly StrategyParam<decimal> _candleMaxSize;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastMa1 = null!;
	private ExponentialMovingAverage _slowMa1 = null!;
	private ExponentialMovingAverage _fastMa2 = null!;
	private ExponentialMovingAverage _slowMa2 = null!;
	private ExponentialMovingAverage _openFilterMa = null!;
	private ExponentialMovingAverage _pullbackMa = null!;

	private readonly List<ICandleMessage> _history = new();

	private decimal _priceStep;
	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;

	/// <summary>
	/// Initializes a new instance of the <see cref="MaBreakStrategy"/> class.
	/// </summary>
	public MaBreakStrategy()
	{
		_priceStepFallback = Param(nameof(PriceStepFallback), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Price Step Fallback", "Price increment used when the instrument does not specify one.", "General");

		_fastMa1Period = Param(nameof(FastMa1Period), 20)
			.SetDisplay("Fast MA 1", "Fast moving average period used for the first trend filter", "Filters")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_slowMa1Period = Param(nameof(SlowMa1Period), 30)
			.SetDisplay("Slow MA 1", "Slow moving average period used for the first trend filter", "Filters")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_fastMa2Period = Param(nameof(FastMa2Period), 30)
			.SetDisplay("Fast MA 2", "Fast moving average period used for the secondary trend filter", "Filters")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_slowMa2Period = Param(nameof(SlowMa2Period), 50)
			.SetDisplay("Slow MA 2", "Slow moving average period used for the secondary trend filter", "Filters")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_openFilterPeriod = Param(nameof(OpenFilterPeriod), 30)
			.SetDisplay("Open Filter MA", "Moving average period used to compare the previous open price", "Filters")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_pullbackPeriod = Param(nameof(PullbackMaPeriod), 20)
			.SetDisplay("Pullback MA", "Moving average period used to validate the pullback wick", "Filters")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_quietBarsCount = Param(nameof(QuietBarsCount), 2)
			.SetDisplay("Quiet Bars", "Number of calm candles used to measure the breakout impulse", "Impulse")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_quietBarsMinRange = Param(nameof(QuietBarsMinRange), 0m)
			.SetDisplay("Quiet Range (pips)", "Minimal range in pips required across the quiet candles", "Impulse")
			.SetCanOptimize(true)
			.SetNotNegative();

		_impulseStrength = Param(nameof(ImpulseStrength), 1.1m)
			.SetDisplay("Impulse Multiplier", "Breakout candle size multiplier relative to the quiet range", "Impulse")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_bullUpperWickPercent = Param(nameof(BullUpperWickPercent), 100m)
			.SetDisplay("Bull Upper Wick %", "Maximum upper wick of the bullish impulse candle in percent of the range", "Pattern")
			.SetCanOptimize(true)
			.SetNotNegative();

		_bullLowerWickPercent = Param(nameof(BullLowerWickPercent), 0m)
			.SetDisplay("Bull Lower Wick %", "Minimum lower wick of the bullish impulse candle in percent of the range", "Pattern")
			.SetCanOptimize(true)
			.SetNotNegative();

		_bearUpperWickPercent = Param(nameof(BearUpperWickPercent), 0m)
			.SetDisplay("Bear Upper Wick %", "Minimum upper wick of the bearish impulse candle in percent of the range", "Pattern")
			.SetCanOptimize(true)
			.SetNotNegative();

		_bearLowerWickPercent = Param(nameof(BearLowerWickPercent), 100m)
			.SetDisplay("Bear Lower Wick %", "Maximum lower wick of the bearish impulse candle in percent of the range", "Pattern")
			.SetCanOptimize(true)
			.SetNotNegative();

		_candleMinSize = Param(nameof(CandleMinSize), 0m)
			.SetDisplay("Min Candle Size (pips)", "Minimal total range in pips required for the impulse candle", "Pattern")
			.SetCanOptimize(true)
			.SetNotNegative();

		_candleMaxSize = Param(nameof(CandleMaxSize), 100m)
			.SetDisplay("Max Candle Size (pips)", "Maximum total range in pips allowed for the impulse candle", "Pattern")
			.SetCanOptimize(true)
			.SetNotNegative();


		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetDisplay("Stop-Loss (pips)", "Protective stop distance in pips", "Orders")
			.SetCanOptimize(true)
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetDisplay("Take-Profit (pips)", "Profit target distance in pips", "Orders")
			.SetCanOptimize(true)
			.SetNotNegative();

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow bullish breakout trades", "General");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow bearish breakout trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series used by the strategy", "General");
	}

	/// <summary>
	/// Price step used when the security does not expose one.
	/// </summary>
	public decimal PriceStepFallback
	{
		get => _priceStepFallback.Value;
		set => _priceStepFallback.Value = value;
	}

	/// <summary>
	/// Fast moving average period used for the first trend filter.
	/// </summary>
	public int FastMa1Period
	{
		get => _fastMa1Period.Value;
		set => _fastMa1Period.Value = value;
	}

	/// <summary>
	/// Slow moving average period used for the first trend filter.
	/// </summary>
	public int SlowMa1Period
	{
		get => _slowMa1Period.Value;
		set => _slowMa1Period.Value = value;
	}

	/// <summary>
	/// Fast moving average period used for the secondary trend filter.
	/// </summary>
	public int FastMa2Period
	{
		get => _fastMa2Period.Value;
		set => _fastMa2Period.Value = value;
	}

	/// <summary>
	/// Slow moving average period used for the secondary trend filter.
	/// </summary>
	public int SlowMa2Period
	{
		get => _slowMa2Period.Value;
		set => _slowMa2Period.Value = value;
	}

	/// <summary>
	/// Moving average period used to compare the previous open price.
	/// </summary>
	public int OpenFilterPeriod
	{
		get => _openFilterPeriod.Value;
		set => _openFilterPeriod.Value = value;
	}

	/// <summary>
	/// Moving average period used to validate the pullback wick.
	/// </summary>
	public int PullbackMaPeriod
	{
		get => _pullbackPeriod.Value;
		set => _pullbackPeriod.Value = value;
	}

	/// <summary>
	/// Number of calm candles used to measure the breakout impulse.
	/// </summary>
	public int QuietBarsCount
	{
		get => _quietBarsCount.Value;
		set => _quietBarsCount.Value = value;
	}

	/// <summary>
	/// Minimal range in pips required across the quiet candles.
	/// </summary>
	public decimal QuietBarsMinRange
	{
		get => _quietBarsMinRange.Value;
		set => _quietBarsMinRange.Value = value;
	}

	/// <summary>
	/// Breakout candle size multiplier relative to the quiet range.
	/// </summary>
	public decimal ImpulseStrength
	{
		get => _impulseStrength.Value;
		set => _impulseStrength.Value = value;
	}

	/// <summary>
	/// Maximum upper wick of the bullish impulse candle in percent of the range.
	/// </summary>
	public decimal BullUpperWickPercent
	{
		get => _bullUpperWickPercent.Value;
		set => _bullUpperWickPercent.Value = value;
	}

	/// <summary>
	/// Minimum lower wick of the bullish impulse candle in percent of the range.
	/// </summary>
	public decimal BullLowerWickPercent
	{
		get => _bullLowerWickPercent.Value;
		set => _bullLowerWickPercent.Value = value;
	}

	/// <summary>
	/// Minimum upper wick of the bearish impulse candle in percent of the range.
	/// </summary>
	public decimal BearUpperWickPercent
	{
		get => _bearUpperWickPercent.Value;
		set => _bearUpperWickPercent.Value = value;
	}

	/// <summary>
	/// Maximum lower wick of the bearish impulse candle in percent of the range.
	/// </summary>
	public decimal BearLowerWickPercent
	{
		get => _bearLowerWickPercent.Value;
		set => _bearLowerWickPercent.Value = value;
	}

	/// <summary>
	/// Minimal total range in pips required for the impulse candle.
	/// </summary>
	public decimal CandleMinSize
	{
		get => _candleMinSize.Value;
		set => _candleMinSize.Value = value;
	}

	/// <summary>
	/// Maximum total range in pips allowed for the impulse candle.
	/// </summary>
	public decimal CandleMaxSize
	{
		get => _candleMaxSize.Value;
		set => _candleMaxSize.Value = value;
	}


	/// <summary>
	/// Protective stop distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Profit target distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Allow bullish breakout trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow bearish breakout trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Primary candle series used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? PriceStepFallback;
		if (_priceStep <= 0m)
			_priceStep = PriceStepFallback;

		_fastMa1 = new ExponentialMovingAverage { Length = FastMa1Period };
		_slowMa1 = new ExponentialMovingAverage { Length = SlowMa1Period };
		_fastMa2 = new ExponentialMovingAverage { Length = FastMa2Period };
		_slowMa2 = new ExponentialMovingAverage { Length = SlowMa2Period };
		_openFilterMa = new ExponentialMovingAverage { Length = OpenFilterPeriod };
		_pullbackMa = new ExponentialMovingAverage { Length = PullbackMaPeriod };

		_history.Clear();
		ResetLongState();
		ResetShortState();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastMa1, _slowMa1, _fastMa2, _slowMa2, _openFilterMa, _pullbackMa, ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal fast1,
		decimal slow1,
		decimal fast2,
		decimal slow2,
		decimal openFilter,
		decimal pullback)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_history.Add(candle);
		if (_history.Count > QuietBarsCount + 5)
		_history.RemoveRange(0, _history.Count - (QuietBarsCount + 5));

		HandleActivePosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_fastMa1.IsFormed || !_slowMa1.IsFormed || !_fastMa2.IsFormed || !_slowMa2.IsFormed || !_openFilterMa.IsFormed || !_pullbackMa.IsFormed)
		return;

		if (_history.Count < QuietBarsCount + 1)
		return;

		var impulse = candle;
		var quietRange = CalculateQuietRange();
		var minQuietRange = QuietBarsMinRange * _priceStep;

		var impulseRange = impulse.HighPrice - impulse.LowPrice;
		var minRange = CandleMinSize > 0m ? CandleMinSize * _priceStep : 0m;
		var maxRange = CandleMaxSize > 0m ? CandleMaxSize * _priceStep : decimal.MaxValue;

		var canEnterLong = EnableLong && Position <= 0 &&
		fast1 > slow1 &&
		fast2 > slow2 &&
		impulse.OpenPrice > openFilter &&
		ValidateImpulseLong(impulse, impulseRange, quietRange, minQuietRange, minRange, maxRange) &&
		pullback >= 0m && impulse.LowPrice <= pullback;

		if (canEnterLong)
		TryEnterLong(impulse);

		var canEnterShort = EnableShort && Position >= 0 &&
		fast1 < slow1 &&
		fast2 < slow2 &&
		impulse.OpenPrice < openFilter &&
		ValidateImpulseShort(impulse, impulseRange, quietRange, minQuietRange, minRange, maxRange) &&
		pullback >= 0m && impulse.HighPrice >= pullback;

		if (canEnterShort)
		TryEnterShort(impulse);
	}

	private decimal CalculateQuietRange()
	{
	var impulseIndex = _history.Count - 1;
	var quietRange = 0m;
	for (var i = 1; i <= QuietBarsCount && impulseIndex - i >= 0; i++)
	{
	var quietCandle = _history[impulseIndex - i];
	var range = quietCandle.HighPrice - quietCandle.LowPrice;
	if (range > quietRange)
	quietRange = range;
	}

	return quietRange;
	}

	private bool ValidateImpulseLong(ICandleMessage impulse, decimal impulseRange, decimal quietRange, decimal minQuietRange, decimal minRange, decimal maxRange)
	{
	if (impulse.ClosePrice <= impulse.OpenPrice)
	return false;

	if (impulseRange < minRange || impulseRange > maxRange)
	return false;

	if (quietRange <= minQuietRange)
	return false;

	var impulseSize = impulse.ClosePrice - impulse.OpenPrice;
	if (impulseSize < quietRange * ImpulseStrength)
	return false;

	var upperWick = impulse.HighPrice - impulse.ClosePrice;
	var lowerWick = impulse.OpenPrice - impulse.LowPrice;

	var maxUpper = impulseRange * BullUpperWickPercent / 100m;
	var minLower = impulseRange * BullLowerWickPercent / 100m;

	return upperWick <= maxUpper && lowerWick >= minLower;
	}

	private bool ValidateImpulseShort(ICandleMessage impulse, decimal impulseRange, decimal quietRange, decimal minQuietRange, decimal minRange, decimal maxRange)
	{
	if (impulse.ClosePrice >= impulse.OpenPrice)
	return false;

	if (impulseRange < minRange || impulseRange > maxRange)
	return false;

	if (quietRange <= minQuietRange)
	return false;

	var impulseSize = impulse.OpenPrice - impulse.ClosePrice;
	if (impulseSize < quietRange * ImpulseStrength)
	return false;

	var upperWick = impulse.HighPrice - impulse.OpenPrice;
	var lowerWick = impulse.ClosePrice - impulse.LowPrice;

	var minUpper = impulseRange * BearUpperWickPercent / 100m;
	var maxLower = impulseRange * BearLowerWickPercent / 100m;

	return upperWick >= minUpper && lowerWick <= maxLower;
	}

	private void TryEnterLong(ICandleMessage impulse)
	{
	if (Volume <= 0m)
	return;

	if (Position < 0)
	{
	BuyMarket(-Position);
	ResetShortState();
	}

	BuyMarket(Volume);

	var entryPrice = impulse.ClosePrice;
	_longStop = StopLossPips > 0m ? entryPrice - StopLossPips * _priceStep : null;
	_longTarget = TakeProfitPips > 0m ? entryPrice + TakeProfitPips * _priceStep : null;
	}

	private void TryEnterShort(ICandleMessage impulse)
	{
	if (Volume <= 0m)
	return;

	if (Position > 0)
	{
	SellMarket(Position);
	ResetLongState();
	}

	SellMarket(Volume);

	var entryPrice = impulse.ClosePrice;
	_shortStop = StopLossPips > 0m ? entryPrice + StopLossPips * _priceStep : null;
	_shortTarget = TakeProfitPips > 0m ? entryPrice - TakeProfitPips * _priceStep : null;
	}

	private void HandleActivePosition(ICandleMessage candle)
	{
	if (Position > 0)
	{
	var stopHit = _longStop.HasValue && candle.LowPrice <= _longStop.Value;
	var targetHit = _longTarget.HasValue && candle.HighPrice >= _longTarget.Value;

	if (stopHit || targetHit)
	{
	SellMarket(Position);
	ResetLongState();
	}
	}
	else if (Position < 0)
	{
	var stopHit = _shortStop.HasValue && candle.HighPrice >= _shortStop.Value;
	var targetHit = _shortTarget.HasValue && candle.LowPrice <= _shortTarget.Value;

	if (stopHit || targetHit)
	{
	BuyMarket(-Position);
	ResetShortState();
	}
	}
	else
	{
	ResetLongState();
	ResetShortState();
	}
	}

	private void ResetLongState()
	{
	_longStop = null;
	_longTarget = null;
	}

	private void ResetShortState()
	{
	_shortStop = null;
	_shortTarget = null;
	}
}
