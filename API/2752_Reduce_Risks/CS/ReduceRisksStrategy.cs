using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe trend-following strategy converted from the "Reduce risks" MQL5 expert.
/// </summary>
public class ReduceRisksStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _initialDeposit;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _m1CandleType;
	private readonly StrategyParam<DataType> _m15CandleType;
	private readonly StrategyParam<DataType> _h1CandleType;

	private SimpleMovingAverage? _m1Sma5;
	private SimpleMovingAverage? _m1Sma8;
	private SimpleMovingAverage? _m1Sma13;
	private SimpleMovingAverage? _m1Sma60;
	private SimpleMovingAverage? _m15Sma4;
	private SimpleMovingAverage? _m15Sma5;
	private SimpleMovingAverage? _m15Sma8;
	private SimpleMovingAverage? _h1Sma24;

	private RollingValues _m1Sma5Values;
	private RollingValues _m1Sma8Values;
	private RollingValues _m1Sma13Values;
	private RollingValues _m1Sma60Values;
	private RollingValues _m15Sma4Values;
	private RollingValues _m15Sma5Values;
	private RollingValues _m15Sma8Values;
	private RollingValues _h1Sma24Values;

	private ICandleMessage? _m1Prev1;
	private ICandleMessage? _m1Prev2;
	private ICandleMessage? _m1Prev3;
	private ICandleMessage? _m15Prev1;
	private ICandleMessage? _m15Prev2;
	private ICandleMessage? _m15Prev3;

	private decimal _pipSize;
	private decimal _priceStep;
	private decimal _riskThreshold;
	private int _riskExceededCounter;

	private decimal? _highestSinceEntry;
	private decimal? _lowestSinceEntry;
	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;
	private int _longBarsSinceEntry;
	private int _shortBarsSinceEntry;
	private decimal _previousPosition;

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Reference initial deposit used for equity based risk limitation.
	/// </summary>
	public decimal InitialDeposit
	{
		get => _initialDeposit.Value;
		set => _initialDeposit.Value = value;
	}

	/// <summary>
	/// Percentage of the initial deposit allowed to be lost before new entries are blocked.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Main working timeframe (defaults to 1 minute).
	/// </summary>
	public DataType M1CandleType
	{
		get => _m1CandleType.Value;
		set => _m1CandleType.Value = value;
	}

	/// <summary>
	/// Confirmation timeframe (defaults to 15 minutes).
	/// </summary>
	public DataType M15CandleType
	{
		get => _m15CandleType.Value;
		set => _m15CandleType.Value = value;
	}

	/// <summary>
	/// Trend filter timeframe (defaults to 1 hour).
	/// </summary>
	public DataType H1CandleType
	{
		get => _h1CandleType.Value;
		set => _h1CandleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ReduceRisksStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 30)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 60)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Target distance in pips", "Risk");

		_initialDeposit = Param(nameof(InitialDeposit), 10000m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Deposit", "Reference equity for drawdown protection", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetRange(0m, 100m)
		.SetDisplay("Risk Percent", "Maximum loss allowed relative to the initial deposit", "Risk");

		_m1CandleType = Param(nameof(M1CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("M1 Timeframe", "Primary trading timeframe", "Timeframes");

		_m15CandleType = Param(nameof(M15CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("M15 Timeframe", "Higher timeframe for confirmation", "Timeframes");

		_h1CandleType = Param(nameof(H1CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("H1 Timeframe", "Trend filter timeframe", "Timeframes");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, M1CandleType),
		(Security, M15CandleType),
		(Security, H1CandleType)
	];
}

/// <inheritdoc />
protected override void OnReseted()
{
	base.OnReseted();

	_m1Sma5 = null;
	_m1Sma8 = null;
	_m1Sma13 = null;
	_m1Sma60 = null;
	_m15Sma4 = null;
	_m15Sma5 = null;
	_m15Sma8 = null;
	_h1Sma24 = null;

	_m1Sma5Values = default;
	_m1Sma8Values = default;
	_m1Sma13Values = default;
	_m1Sma60Values = default;
	_m15Sma4Values = default;
	_m15Sma5Values = default;
	_m15Sma8Values = default;
	_h1Sma24Values = default;

	_m1Prev1 = null;
	_m1Prev2 = null;
	_m1Prev3 = null;
	_m15Prev1 = null;
	_m15Prev2 = null;
	_m15Prev3 = null;

	_pipSize = 0m;
	_priceStep = 0m;
	_riskThreshold = 0m;
	_riskExceededCounter = 0;

	_highestSinceEntry = null;
	_lowestSinceEntry = null;
	_longEntryTime = null;
	_shortEntryTime = null;
	_longBarsSinceEntry = 0;
	_shortBarsSinceEntry = 0;
	_previousPosition = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	_priceStep = Security?.PriceStep ?? 0.0001m;
	var decimals = Security?.Decimals ?? 4;
	_pipSize = decimals is 3 or 5 ? _priceStep * 10m : _priceStep;
	if (_pipSize == 0m)
	_pipSize = 0.0001m;

	_riskThreshold = InitialDeposit * (100m - RiskPercent) / 100m;

	_m1Sma5 = new SimpleMovingAverage { Length = 5 };
	_m1Sma8 = new SimpleMovingAverage { Length = 8 };
	_m1Sma13 = new SimpleMovingAverage { Length = 13 };
	_m1Sma60 = new SimpleMovingAverage { Length = 60 };
	_m15Sma4 = new SimpleMovingAverage { Length = 4 };
	_m15Sma5 = new SimpleMovingAverage { Length = 5 };
	_m15Sma8 = new SimpleMovingAverage { Length = 8 };
	_h1Sma24 = new SimpleMovingAverage { Length = 24 };

	var m1Subscription = SubscribeCandles(M1CandleType);
	m1Subscription.Bind(ProcessM1).Start();

	var m15Subscription = SubscribeCandles(M15CandleType);
	m15Subscription.Bind(ProcessM15).Start();

	var h1Subscription = SubscribeCandles(H1CandleType);
	h1Subscription.Bind(ProcessH1).Start();

	Unit? takeProfitUnit = null;
	if (TakeProfitPips > 0 && _priceStep > 0m)
	{
		var steps = TakeProfitPips * _pipSize / _priceStep;
		takeProfitUnit = new Unit(steps, UnitTypes.Step);
	}

	Unit? stopLossUnit = null;
	if (StopLossPips > 0 && _priceStep > 0m)
	{
		var steps = StopLossPips * _pipSize / _priceStep;
		stopLossUnit = new Unit(steps, UnitTypes.Step);
	}

	StartProtection(takeProfitUnit, stopLossUnit, useMarketOrders: true);

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, m1Subscription);
		DrawIndicator(area, _m1Sma5);
		DrawIndicator(area, _m1Sma8);
		DrawIndicator(area, _m1Sma13);
		DrawIndicator(area, _m1Sma60);
		DrawOwnTrades(area);
	}
}

private void ProcessM1(ICandleMessage candle)
{
	if (candle.State != CandleStates.Finished)
	return;

	if (_m1Sma5 is null || _m1Sma8 is null || _m1Sma13 is null || _m1Sma60 is null ||
	_m15Sma4 is null || _m15Sma5 is null || _m15Sma8 is null || _h1Sma24 is null)
	{
		UpdateM1History(candle);
		_previousPosition = Position;
		return;
	}

	var typical = GetTypicalPrice(candle);
	ProcessSma(_m1Sma5, ref _m1Sma5Values, typical, candle.OpenTime);
	ProcessSma(_m1Sma8, ref _m1Sma8Values, typical, candle.OpenTime);
	ProcessSma(_m1Sma13, ref _m1Sma13Values, typical, candle.OpenTime);
	ProcessSma(_m1Sma60, ref _m1Sma60Values, typical, candle.OpenTime);

	var ready =
	_m1Sma5Values[0] is decimal sma5 &&
	_m1Sma5Values[2] is decimal sma5Prev2 &&
	_m1Sma8Values[0] is decimal sma8 &&
	_m1Sma8Values[1] is decimal sma8Prev1 &&
	_m1Sma8Values[2] is decimal sma8Prev2 &&
	_m1Sma8Values[3] is decimal sma8Prev3 &&
	_m1Sma13Values[0] is decimal sma13 &&
	_m1Sma60Values[0] is decimal sma60 &&
	_m1Sma60Values[2] is decimal sma60Prev2 &&
	_m15Sma4Values[0] is decimal sma4M15 &&
	_m15Sma4Values[1] is decimal sma4M15Prev1 &&
	_m15Sma4Values[2] is decimal sma4M15Prev2 &&
	_m15Sma5Values[1] is decimal sma5M15Prev1 &&
	_m15Sma8Values[1] is decimal sma8M15Prev1 &&
	_h1Sma24Values[0] is decimal sma24H1;

	var hasHistory = _m1Prev1 != null && _m1Prev2 != null && _m1Prev3 != null &&
	_m15Prev1 != null && _m15Prev2 != null && _m15Prev3 != null;

	if (!ready || !hasHistory)
	{
		HandlePositionState();
		UpdateM1History(candle);
		_previousPosition = Position;
		return;
	}

	var equity = Portfolio?.CurrentValue ?? InitialDeposit;
	var riskExceeded = equity <= _riskThreshold && InitialDeposit > 0m;

	if (riskExceeded)
	{
		if (_riskExceededCounter < 15)
		{
			AddWarningLog("Entry blocked. Risk limit of {0}% reached (equity={1:0.##}).", RiskPercent, equity);
			_riskExceededCounter++;
		}
	}
	else
	{
		_riskExceededCounter = 0;
	}

	if (!IsFormedAndOnlineAndAllowTrading())
	{
		HandlePositionState();
		UpdateM1History(candle);
		_previousPosition = Position;
		return;
	}

	if (Position == 0 && !riskExceeded)
	{
		var longEntered = TryEnterLong(candle, sma5, sma8, sma13, sma60, sma5Prev2, sma8Prev1, sma8Prev2, sma8Prev3, sma4M15, sma4M15Prev1, sma4M15Prev2, sma5M15Prev1, sma8M15Prev1, sma24H1);
		if (!longEntered)
		TryEnterShort(candle, sma5, sma8, sma13, sma60, sma5Prev2, sma8Prev1, sma8Prev2, sma8Prev3, sma4M15, sma4M15Prev1, sma4M15Prev2, sma5M15Prev1, sma8M15Prev1, sma24H1);
	}
	else
	{
		HandleActivePosition(candle, riskExceeded);
	}

	HandlePositionState();
	UpdateM1History(candle);
	_previousPosition = Position;
}

private void ProcessM15(ICandleMessage candle)
{
	if (candle.State != CandleStates.Finished || _m15Sma4 is null || _m15Sma5 is null || _m15Sma8 is null)
	return;

	var typical = GetTypicalPrice(candle);
	ProcessSma(_m15Sma4, ref _m15Sma4Values, typical, candle.OpenTime);
	ProcessSma(_m15Sma5, ref _m15Sma5Values, typical, candle.OpenTime);
	ProcessSma(_m15Sma8, ref _m15Sma8Values, typical, candle.OpenTime);

	_m15Prev3 = _m15Prev2;
	_m15Prev2 = _m15Prev1;
	_m15Prev1 = candle;
}

private void ProcessH1(ICandleMessage candle)
{
	if (candle.State != CandleStates.Finished || _h1Sma24 is null)
	return;

	var typical = GetTypicalPrice(candle);
	ProcessSma(_h1Sma24, ref _h1Sma24Values, typical, candle.OpenTime);
}

private bool TryEnterLong(
ICandleMessage candle,
decimal sma5,
decimal sma8,
decimal sma13,
decimal sma60,
decimal sma5Prev2,
decimal sma8Prev1,
decimal sma8Prev2,
decimal sma8Prev3,
decimal sma4M15,
decimal sma4M15Prev1,
decimal sma4M15Prev2,
decimal sma5M15Prev1,
decimal sma8M15Prev1,
decimal sma24H1)
{
	if (_m1Prev1 is null || _m1Prev2 is null || _m1Prev3 is null ||
	_m15Prev1 is null || _m15Prev2 is null || _m15Prev3 is null)
	return false;

	var amplitudeM1 =
	CandleRange(_m1Prev1) <= 20m * _pipSize &&
	CandleRange(_m1Prev2) <= 20m * _pipSize &&
	CandleRange(_m1Prev3) <= 20m * _pipSize;

	var amplitudeM15 =
	CandleRange(_m15Prev1) <= 30m * _pipSize &&
	CandleRange(_m15Prev2) <= 30m * _pipSize &&
	CandleRange(_m15Prev3) <= 30m * _pipSize;

	var channelM15 = _m15Prev1.HighPrice - _m15Prev3.LowPrice <= 30m * _pipSize;

	var activityPrevM1 =
	CandleRange(_m1Prev1) >= 1.1m * CandleRange(_m1Prev2) &&
	CandleRange(_m1Prev1) < 3m * CandleRange(_m1Prev2);

	var resistanceCleared =
	candle.ClosePrice > _m1Prev1.HighPrice &&
	candle.ClosePrice > _m15Prev1.HighPrice;

	var waveStartM1 =
	IsBetween(sma8Prev1, _m1Prev1) ||
	IsBetween(sma8Prev2, _m1Prev2) ||
	IsBetween(sma8Prev3, _m1Prev3);

	var waveStartM15 = IsBetween(sma5M15Prev1, _m15Prev1);

	var secondBarUp = _m1Prev2.ClosePrice > _m1Prev2.OpenPrice;
	var previousBarUp = _m1Prev1.ClosePrice > _m1Prev1.OpenPrice;

	var maSlope = sma5 > sma5Prev2 && sma60 > sma60Prev2;
	var maHierarchy = sma5 > sma8 && sma8 > sma13;
	var priceAboveMa =
	candle.ClosePrice > sma5 &&
	candle.ClosePrice > sma8 &&
	candle.ClosePrice > sma13 &&
	candle.ClosePrice > sma60;

	var previousM15Up = _m15Prev1.ClosePrice > _m15Prev1.OpenPrice;
	var ma4Slope = sma4M15 > sma4M15Prev2;
	var maHierarchyM15 = sma4M15Prev1 > sma8M15Prev1;
	var priceAboveM15 = candle.ClosePrice > sma4M15;
	var priceAboveH1 = candle.ClosePrice > sma24H1;

	var m15Body = _m15Prev1.ClosePrice - _m15Prev1.OpenPrice > 0.5m * CandleRange(_m15Prev1);
	var m15ShallowPullback = _m15Prev1.HighPrice - _m15Prev1.ClosePrice < 0.25m * CandleRange(_m15Prev1);
	var m15HigherHighs = _m15Prev1.HighPrice > _m15Prev2.HighPrice;
	var m15Shadow = _m15Prev1.OpenPrice < _m15Prev1.HighPrice && _m15Prev1.OpenPrice > _m15Prev1.LowPrice;

	var m1Body = _m1Prev1.ClosePrice - _m1Prev1.OpenPrice > 0.5m * CandleRange(_m1Prev1);
	var m1NotFlat = CandleRange(_m1Prev1) > 7m * _pipSize;
	var m1ShallowPullback = _m1Prev2.HighPrice - _m1Prev2.ClosePrice < 0.25m * CandleRange(_m1Prev2);
	var m1HigherHighs = _m1Prev1.HighPrice > _m1Prev2.HighPrice;
	var m1Shadow = _m1Prev1.OpenPrice < _m1Prev1.HighPrice && _m1Prev1.OpenPrice > _m1Prev1.LowPrice;

	var conditionsMet = amplitudeM1 && amplitudeM15 && channelM15 && activityPrevM1 && resistanceCleared && waveStartM1 && waveStartM15 &&
	secondBarUp && previousBarUp && maSlope && maHierarchy && priceAboveMa && previousM15Up && ma4Slope && maHierarchyM15 &&
	priceAboveM15 && priceAboveH1 && m15Body && m15ShallowPullback && m15HigherHighs && m15Shadow && m1Body && m1NotFlat &&
	m1ShallowPullback && m1HigherHighs && m1Shadow;

	if (!conditionsMet)
	return false;

	BuyMarket();
	AddInfoLog("Opened long position.");
	return true;
}

private void TryEnterShort(
ICandleMessage candle,
decimal sma5,
decimal sma8,
decimal sma13,
decimal sma60,
decimal sma5Prev2,
decimal sma8Prev1,
decimal sma8Prev2,
decimal sma8Prev3,
decimal sma4M15,
decimal sma4M15Prev1,
decimal sma4M15Prev2,
decimal sma5M15Prev1,
decimal sma8M15Prev1,
decimal sma24H1)
{
	if (_m1Prev1 is null || _m1Prev2 is null || _m1Prev3 is null ||
	_m15Prev1 is null || _m15Prev2 is null || _m15Prev3 is null)
	return;

	var amplitudeM1 =
	CandleRange(_m1Prev1) <= 20m * _pipSize &&
	CandleRange(_m1Prev2) <= 20m * _pipSize &&
	CandleRange(_m1Prev3) <= 20m * _pipSize;

	var amplitudeM15 =
	CandleRange(_m15Prev1) <= 30m * _pipSize &&
	CandleRange(_m15Prev2) <= 30m * _pipSize &&
	CandleRange(_m15Prev3) <= 30m * _pipSize;

	var channelM15 = _m15Prev1.HighPrice - _m15Prev3.LowPrice <= 30m * _pipSize;

	var activityPrevM1 =
	CandleRange(_m1Prev1) >= 1.1m * CandleRange(_m1Prev2) &&
	CandleRange(_m1Prev1) < 3m * CandleRange(_m1Prev2);

	var resistanceBroken =
	candle.ClosePrice < _m1Prev1.LowPrice &&
	candle.ClosePrice < _m15Prev1.LowPrice;

	var waveStartM1 =
	IsBetween(sma8Prev1, _m1Prev1) ||
	IsBetween(sma8Prev2, _m1Prev2) ||
	IsBetween(sma8Prev3, _m1Prev3);

	var waveStartM15 = IsBetween(sma5M15Prev1, _m15Prev1);

	var secondBarDown = _m1Prev2.ClosePrice < _m1Prev2.OpenPrice;
	var previousBarDown = _m1Prev1.ClosePrice < _m1Prev1.OpenPrice;

	var maSlope = sma5 < sma5Prev2 && sma60 < sma60Prev2;
	var maHierarchy = sma5 < sma8 && sma8 < sma13;
	var priceBelowMa =
	candle.ClosePrice < sma5 &&
	candle.ClosePrice < sma8 &&
	candle.ClosePrice < sma13 &&
	candle.ClosePrice < sma60;

	var previousM15Down = _m15Prev1.ClosePrice < _m15Prev1.OpenPrice;
	var ma4Slope = sma4M15 < sma4M15Prev2;
	var maHierarchyM15 = sma4M15Prev1 < sma8M15Prev1;
	var priceBelowM15 = candle.ClosePrice < sma4M15;
	var priceBelowH1 = candle.ClosePrice < sma24H1;

	var m15Body = _m15Prev1.OpenPrice - _m15Prev1.ClosePrice > 0.5m * CandleRange(_m15Prev1);
	var m15ShallowPullback = _m15Prev1.ClosePrice - _m15Prev1.LowPrice < 0.25m * CandleRange(_m15Prev1);
	var m15LowerLows = _m15Prev1.LowPrice < _m15Prev2.LowPrice;
	var m15Shadow = _m15Prev1.OpenPrice < _m15Prev1.HighPrice && _m15Prev1.OpenPrice > _m15Prev1.LowPrice;

	var m1Body = _m1Prev1.OpenPrice - _m1Prev1.ClosePrice > 0.5m * CandleRange(_m1Prev1);
	var m1NotFlat = CandleRange(_m1Prev1) > 7m * _pipSize;
	var m1ShallowPullback = _m1Prev2.ClosePrice - _m1Prev2.LowPrice < 0.25m * CandleRange(_m1Prev2);
	var m1LowerLows = _m1Prev1.LowPrice < _m1Prev2.LowPrice;
	var m1Shadow = _m1Prev1.OpenPrice < _m1Prev1.HighPrice && _m1Prev1.OpenPrice > _m1Prev1.LowPrice;

	var conditionsMet = amplitudeM1 && amplitudeM15 && channelM15 && activityPrevM1 && resistanceBroken && waveStartM1 && waveStartM15 &&
	secondBarDown && previousBarDown && maSlope && maHierarchy && priceBelowMa && previousM15Down && ma4Slope && maHierarchyM15 &&
	priceBelowM15 && priceBelowH1 && m15Body && m15ShallowPullback && m15LowerLows && m15Shadow && m1Body && m1NotFlat &&
	m1ShallowPullback && m1LowerLows && m1Shadow;

	if (!conditionsMet)
	return;

	SellMarket();
	AddInfoLog("Opened short position.");
}

private void HandleActivePosition(ICandleMessage candle, bool riskExceeded)
{
	if (Position > 0)
	{
		if (_previousPosition <= 0)
		{
			_highestSinceEntry = candle.HighPrice;
			_longEntryTime = candle.OpenTime;
			_longBarsSinceEntry = 0;
		}
		else
		{
			_highestSinceEntry = _highestSinceEntry is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;
			_longBarsSinceEntry++;
		}

		var entryPrice = PositionAvgPrice;
		var collapseM1 = candle.ClosePrice <= candle.OpenPrice - 10m * _pipSize;
		var collapsePrev =
		_longEntryTime.HasValue && candle.OpenTime - _longEntryTime.Value > TimeSpan.FromMinutes(1) &&
		_m1Prev1 != null &&
		_m1Prev1.ClosePrice < _m1Prev1.OpenPrice &&
		_m1Prev1.OpenPrice - _m1Prev1.ClosePrice >= 20m * _pipSize;

		var profitZone = candle.ClosePrice - entryPrice >= 10m * _pipSize;
		var trailing =
		_longBarsSinceEntry >= 1 &&
		_highestSinceEntry is decimal high &&
		high > entryPrice &&
		high - candle.ClosePrice >= 20m * _pipSize;

		var stopLossHit = entryPrice - candle.ClosePrice >= 20m * _pipSize;

		if (collapseM1 || collapsePrev || profitZone || trailing || stopLossHit || riskExceeded)
		{
			ClosePosition();
			AddInfoLog("Closed long position by risk control.");
		}
	}
	else if (Position < 0)
	{
		if (_previousPosition >= 0)
		{
			_lowestSinceEntry = candle.LowPrice;
			_shortEntryTime = candle.OpenTime;
			_shortBarsSinceEntry = 0;
		}
		else
		{
			_lowestSinceEntry = _lowestSinceEntry is decimal l ? Math.Min(l, candle.LowPrice) : candle.LowPrice;
			_shortBarsSinceEntry++;
		}

		var entryPrice = PositionAvgPrice;
		var collapseM1 = candle.ClosePrice >= candle.OpenPrice + 10m * _pipSize;
		var collapsePrev =
		_shortEntryTime.HasValue && candle.OpenTime - _shortEntryTime.Value > TimeSpan.FromMinutes(1) &&
		_m1Prev1 != null &&
		_m1Prev1.ClosePrice > _m1Prev1.OpenPrice &&
		_m1Prev1.ClosePrice - _m1Prev1.OpenPrice >= 20m * _pipSize;

		var profitZone = entryPrice - candle.ClosePrice >= 10m * _pipSize;
		var trailing =
		_shortBarsSinceEntry >= 1 &&
		_lowestSinceEntry is decimal low &&
		low < entryPrice &&
		candle.ClosePrice - low >= 20m * _pipSize;

		var stopLossHit = candle.ClosePrice - entryPrice >= 20m * _pipSize;

		if (collapseM1 || collapsePrev || profitZone || trailing || stopLossHit || riskExceeded)
		{
			ClosePosition();
			AddInfoLog("Closed short position by risk control.");
		}
	}
}

private void HandlePositionState()
{
	if (Position <= 0)
	{
		_highestSinceEntry = null;
		_longEntryTime = null;
		_longBarsSinceEntry = 0;
	}

	if (Position >= 0)
	{
		_lowestSinceEntry = null;
		_shortEntryTime = null;
		_shortBarsSinceEntry = 0;
	}
}

private void UpdateM1History(ICandleMessage candle)
{
	_m1Prev3 = _m1Prev2;
	_m1Prev2 = _m1Prev1;
	_m1Prev1 = candle;
}

private static decimal GetTypicalPrice(ICandleMessage candle)
{
	return (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
}

private static decimal CandleRange(ICandleMessage candle)
{
	return candle.HighPrice - candle.LowPrice;
}

private static bool IsBetween(decimal value, ICandleMessage candle)
{
	return value > candle.LowPrice && value < candle.HighPrice;
}

private void ProcessSma(SimpleMovingAverage sma, ref RollingValues values, decimal input, DateTimeOffset time)
{
	var indicatorValue = sma.Process(new DecimalIndicatorValue(sma, input, time));
	if (!indicatorValue.IsFinal || indicatorValue is not DecimalIndicatorValue decimalValue)
	return;

	values.Add(decimalValue.Value);
}

private struct RollingValues
{
	public decimal? Current;
	public decimal? Prev1;
	public decimal? Prev2;
	public decimal? Prev3;

	public decimal? this[int index] => index switch
	{
		0 => Current,
		1 => Prev1,
		2 => Prev2,
		3 => Prev3,
		_ => null
	};

	public void Add(decimal value)
	{
		Prev3 = Prev2;
		Prev2 = Prev1;
		Prev1 = Current;
		Current = value;
	}
}
}
