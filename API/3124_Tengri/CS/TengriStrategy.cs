using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader "Tengri" grid strategy using StockSharp high level API.
/// Combines RSI direction filter, ATR based quiet-market detector, and EMA trend guidance.
/// Implements incremental position sizing with martingale style volume scaling.
/// </summary>
public class TengriStrategy : Strategy
{
	private const decimal RsiUpperThreshold = 70m;
	private const decimal RsiLowerThreshold = 30m;

	private readonly StrategyParam<DataType> _dealCandleType;
	private readonly StrategyParam<DataType> _entryCandleType;
	private readonly StrategyParam<DataType> _scaleCandleType;
	private readonly StrategyParam<DataType> _maCandleType;
	private readonly StrategyParam<DataType> _silence1CandleType;
	private readonly StrategyParam<DataType> _silence2CandleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _silencePeriod1;
	private readonly StrategyParam<int> _silenceInterpolation1;
	private readonly StrategyParam<decimal> _silenceLevel1;
	private readonly StrategyParam<int> _silencePeriod2;
	private readonly StrategyParam<int> _silenceInterpolation2;
	private readonly StrategyParam<decimal> _silenceLevel2;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _pipStep;
	private readonly StrategyParam<decimal> _pipStep2;
	private readonly StrategyParam<decimal> _pipStepExponent;
	private readonly StrategyParam<decimal> _lotExponent1;
	private readonly StrategyParam<decimal> _lotExponent2;
	private readonly StrategyParam<int> _stepX;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<bool> _fixLot;
	private readonly StrategyParam<decimal> _lotStep;
	private readonly StrategyParam<int> _slTpPips;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _useLimit;
	private readonly StrategyParam<decimal> _limitDivisor;
	private readonly StrategyParam<bool> _closeFriday;
	private readonly StrategyParam<int> _closeFridayHour;

	private RelativeStrengthIndex _rsi = null!;
	private AverageTrueRange _silenceAtr1 = null!;
	private ExponentialMovingAverage _silenceSmooth1 = null!;
	private AverageTrueRange _silenceAtr2 = null!;
	private ExponentialMovingAverage _silenceSmooth2 = null!;
	private ExponentialMovingAverage _ema = null!;

	private decimal _dealOpenPrice;
	private decimal _lastBid;
	private decimal _lastAsk;
	private decimal _pipValue;
	private decimal _lastRsi;
	private bool _rsiReady;
	private decimal _silence1Value;
	private bool _silence1Ready;
	private decimal _silence2Value;
	private bool _silence2Ready;
	private decimal _maValue;
	private bool _maReady;
	private DateTimeOffset? _lastEntryBarTime;
	private DateTimeOffset? _lastScaleBarTime;

	private decimal _netVolume;
	private int _longCount;
	private decimal _longVolume;
	private decimal _avgLongPrice;
	private decimal _lastBuyPrice;
	private decimal _lastBuyVolume;
	private decimal? _longTakeProfit;
	private int _shortCount;
	private decimal _shortVolume;
	private decimal _avgShortPrice;
	private decimal _lastSellPrice;
	private decimal _lastSellVolume;
	private decimal? _shortTakeProfit;

	/// <summary>
	/// Initializes a new instance of the <see cref="TengriStrategy"/> class.
	/// </summary>
	public TengriStrategy()
	{
		_dealCandleType = Param(nameof(DealCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Direction Candle", "Time frame used for determining the trade direction", "General");

		_entryCandleType = Param(nameof(EntryCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Entry Candle", "Time frame used for evaluating entry conditions", "General");

		_scaleCandleType = Param(nameof(ScaleCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Scale-In Candle", "Time frame used for averaging logic", "General");

		_maCandleType = Param(nameof(MaCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("EMA Candle", "Time frame used for the EMA trend filter", "Indicators");

		_silence1CandleType = Param(nameof(Silence1CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Silence #1 Candle", "Time frame for the first volatility filter", "Indicators");

		_silence2CandleType = Param(nameof(Silence2CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Silence #2 Candle", "Time frame for the second volatility filter", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Lookback period for the RSI filter", "Indicators");

		_silencePeriod1 = Param(nameof(SilencePeriod1), 11)
			.SetDisplay("Silence #1 Period", "ATR length for the first quiet-market detector", "Indicators")
			.SetGreaterThanZero();

		_silenceInterpolation1 = Param(nameof(SilenceInterpolation1), 220)
			.SetDisplay("Silence #1 Smoothing", "EMA smoothing length for the first ATR stream", "Indicators")
			.SetGreaterThanZero();

		_silenceLevel1 = Param(nameof(SilenceLevel1), 80m)
			.SetDisplay("Silence #1 Threshold", "Upper bound for low-volatility confirmation", "Indicators");

		_silencePeriod2 = Param(nameof(SilencePeriod2), 12)
			.SetDisplay("Silence #2 Period", "ATR length for the second quiet-market detector", "Indicators")
			.SetGreaterThanZero();

		_silenceInterpolation2 = Param(nameof(SilenceInterpolation2), 96)
			.SetDisplay("Silence #2 Smoothing", "EMA smoothing length for the second ATR stream", "Indicators")
			.SetGreaterThanZero();

		_silenceLevel2 = Param(nameof(SilenceLevel2), 80m)
			.SetDisplay("Silence #2 Threshold", "Upper bound for the scale-in volatility filter", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 30)
			.SetDisplay("EMA Period", "Length for the EMA trend filter", "Indicators")
			.SetGreaterThanZero();

		_pipStep = Param(nameof(PipStep), 10m)
			.SetDisplay("Primary Pip Step", "Base pip distance for grid additions", "Grid")
			.SetGreaterThanZero();

		_pipStep2 = Param(nameof(PipStep2), 20m)
			.SetDisplay("Secondary Pip Step", "Alternative pip distance used when volatility is high", "Grid")
			.SetGreaterThanZero();

		_pipStepExponent = Param(nameof(PipStepExponent), 1m)
			.SetDisplay("Pip Step Exponent", "Exponent applied to pip distances per additional trade", "Grid")
			.SetGreaterThanZero();

		_lotExponent1 = Param(nameof(LotExponent1), 1.70m)
			.SetDisplay("Lot Exponent #1", "Volume multiplier before the StepX threshold", "Risk")
			.SetGreaterThanZero();

		_lotExponent2 = Param(nameof(LotExponent2), 2.08m)
			.SetDisplay("Lot Exponent #2", "Volume multiplier after reaching StepX trades", "Risk")
			.SetGreaterThanZero();

		_stepX = Param(nameof(StepX), 5)
			.SetDisplay("Step X", "Number of trades before switching to the second multiplier", "Risk")
			.SetGreaterThanZero();

		_lotSize = Param(nameof(LotSize), 0.01m)
			.SetDisplay("Base Lot Size", "Reference lot size used for money management", "Risk")
			.SetGreaterThanZero();

		_fixLot = Param(nameof(FixLot), false)
			.SetDisplay("Use Fixed Lot", "When enabled trades use the base lot size regardless of equity", "Risk");

		_lotStep = Param(nameof(LotStep), 2000m)
			.SetDisplay("Lot Step Equity", "Equity divisor controlling dynamic lot sizing", "Risk")
			.SetGreaterThanZero();

		_slTpPips = Param(nameof(SlTpPips), 10)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips for the first position", "Risk");

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetDisplay("Maximum Trades", "Maximum number of scale-in trades per direction", "Risk")
			.SetGreaterThanZero();

		_useLimit = Param(nameof(UseLimit), true)
			.SetDisplay("Use Profit Limit", "Enable global profit target when both directions are open", "Risk");

		_limitDivisor = Param(nameof(LimitDivisor), 50m)
			.SetDisplay("Limit Divisor", "Equity divisor defining the global profit target", "Risk")
			.SetGreaterThanZero();

		_closeFriday = Param(nameof(CloseFriday), true)
			.SetDisplay("Avoid Late Friday", "Block new entries after the configured Friday hour", "Sessions");

		_closeFridayHour = Param(nameof(CloseFridayHour), 19)
			.SetDisplay("Friday Cutoff Hour", "Local hour when new entries are skipped on Friday", "Sessions")
			.SetGreaterThanZero();
	}

	/// <summary>
	/// Candle used for trend direction detection.
	/// </summary>
	public DataType DealCandleType
	{
		get => _dealCandleType.Value;
		set => _dealCandleType.Value = value;
	}

	/// <summary>
	/// Candle used to evaluate entry conditions.
	/// </summary>
	public DataType EntryCandleType
	{
		get => _entryCandleType.Value;
		set => _entryCandleType.Value = value;
	}

	/// <summary>
	/// Candle used for scale-in decisions.
	/// </summary>
	public DataType ScaleCandleType
	{
		get => _scaleCandleType.Value;
		set => _scaleCandleType.Value = value;
	}

	/// <summary>
	/// Candle used to compute the EMA trend filter.
	/// </summary>
	public DataType MaCandleType
	{
		get => _maCandleType.Value;
		set => _maCandleType.Value = value;
	}

	/// <summary>
	/// Candle used for the first ATR based "Silence" indicator.
	/// </summary>
	public DataType Silence1CandleType
	{
		get => _silence1CandleType.Value;
		set => _silence1CandleType.Value = value;
	}

	/// <summary>
	/// Candle used for the second ATR based "Silence" indicator.
	/// </summary>
	public DataType Silence2CandleType
	{
		get => _silence2CandleType.Value;
		set => _silence2CandleType.Value = value;
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
	/// ATR length for the first volatility filter.
	/// </summary>
	public int SilencePeriod1
	{
		get => _silencePeriod1.Value;
		set => _silencePeriod1.Value = value;
	}

	/// <summary>
	/// EMA smoothing length applied to the first ATR stream.
	/// </summary>
	public int SilenceInterpolation1
	{
		get => _silenceInterpolation1.Value;
		set => _silenceInterpolation1.Value = value;
	}

	/// <summary>
	/// Threshold for the first volatility filter.
	/// </summary>
	public decimal SilenceLevel1
	{
		get => _silenceLevel1.Value;
		set => _silenceLevel1.Value = value;
	}

	/// <summary>
	/// ATR length for the second volatility filter.
	/// </summary>
	public int SilencePeriod2
	{
		get => _silencePeriod2.Value;
		set => _silencePeriod2.Value = value;
	}

	/// <summary>
	/// EMA smoothing length applied to the second ATR stream.
	/// </summary>
	public int SilenceInterpolation2
	{
		get => _silenceInterpolation2.Value;
		set => _silenceInterpolation2.Value = value;
	}

	/// <summary>
	/// Threshold for the second volatility filter.
	/// </summary>
	public decimal SilenceLevel2
	{
		get => _silenceLevel2.Value;
		set => _silenceLevel2.Value = value;
	}

	/// <summary>
	/// Period for the EMA trend filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Primary pip step before volatility escalation.
	/// </summary>
	public decimal PipStep
	{
		get => _pipStep.Value;
		set => _pipStep.Value = value;
	}

	/// <summary>
	/// Secondary pip step used when the market is noisy.
	/// </summary>
	public decimal PipStep2
	{
		get => _pipStep2.Value;
		set => _pipStep2.Value = value;
	}

	/// <summary>
	/// Exponent applied to pip steps for each additional trade.
	/// </summary>
	public decimal PipStepExponent
	{
		get => _pipStepExponent.Value;
		set => _pipStepExponent.Value = value;
	}

	/// <summary>
	/// Volume multiplier before reaching the StepX threshold.
	/// </summary>
	public decimal LotExponent1
	{
		get => _lotExponent1.Value;
		set => _lotExponent1.Value = value;
	}

	/// <summary>
	/// Volume multiplier after reaching the StepX threshold.
	/// </summary>
	public decimal LotExponent2
	{
		get => _lotExponent2.Value;
		set => _lotExponent2.Value = value;
	}

	/// <summary>
	/// Number of trades before switching to the second multiplier.
	/// </summary>
	public int StepX
	{
		get => _stepX.Value;
		set => _stepX.Value = value;
	}

	/// <summary>
	/// Base lot size used by the risk model.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Indicates whether the strategy always uses the base lot size.
	/// </summary>
	public bool FixLot
	{
		get => _fixLot.Value;
		set => _fixLot.Value = value;
	}

	/// <summary>
	/// Equity divisor for proportional lot sizing.
	/// </summary>
	public decimal LotStep
	{
		get => _lotStep.Value;
		set => _lotStep.Value = value;
	}

	/// <summary>
	/// Take profit distance for the first position measured in pips.
	/// </summary>
	public int SlTpPips
	{
		get => _slTpPips.Value;
		set => _slTpPips.Value = value;
	}

	/// <summary>
	/// Maximum number of scale-in trades per direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Enables the global profit limit.
	/// </summary>
	public bool UseLimit
	{
		get => _useLimit.Value;
		set => _useLimit.Value = value;
	}

	/// <summary>
	/// Equity divisor for the global profit target.
	/// </summary>
	public decimal LimitDivisor
	{
		get => _limitDivisor.Value;
		set => _limitDivisor.Value = value;
	}

	/// <summary>
	/// Skip new entries late on Friday.
	/// </summary>
	public bool CloseFriday
	{
		get => _closeFriday.Value;
		set => _closeFriday.Value = value;
	}

	/// <summary>
	/// Local hour when entries are skipped on Friday.
	/// </summary>
	public int CloseFridayHour
	{
		get => _closeFridayHour.Value;
		set => _closeFridayHour.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	return new List<(Security, DataType)>
	{
	(Security, DataType.Level1),
	(Security, DealCandleType),
	(Security, EntryCandleType),
	(Security, ScaleCandleType),
	(Security, MaCandleType),
	(Security, Silence1CandleType),
	(Security, Silence2CandleType),
	(Security, TimeSpan.FromHours(1).TimeFrame()),
	}.Distinct();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_dealOpenPrice = 0m;
	_lastBid = 0m;
	_lastAsk = 0m;
	_pipValue = 0m;
	_lastRsi = 0m;
	_rsiReady = false;
	_silence1Value = 0m;
	_silence1Ready = false;
	_silence2Value = 0m;
	_silence2Ready = false;
	_maValue = 0m;
	_maReady = false;
	_lastEntryBarTime = null;
	_lastScaleBarTime = null;
	ResetPositionTracking();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
	_silenceAtr1 = new AverageTrueRange { Length = SilencePeriod1 };
	_silenceSmooth1 = new ExponentialMovingAverage { Length = Math.Max(1, SilenceInterpolation1) };
	_silenceAtr2 = new AverageTrueRange { Length = SilencePeriod2 };
	_silenceSmooth2 = new ExponentialMovingAverage { Length = Math.Max(1, SilenceInterpolation2) };
	_ema = new ExponentialMovingAverage { Length = MaPeriod };

	ResetPositionTracking();

	var step = Security.Step;
	if (step <= 0m)
	step = 1m;

	var pipFactor = Security.Decimals is 3 or 5 ? 10m : 1m;
	_pipValue = step * pipFactor;

	SubscribeLevel1().Bind(ProcessLevel1).Start();

	SubscribeCandles(DealCandleType).Bind(ProcessDealCandle).Start();
	SubscribeCandles(EntryCandleType).Bind(ProcessEntryCandle).Start();
	SubscribeCandles(ScaleCandleType).Bind(ProcessScaleCandle).Start();
	SubscribeCandles(MaCandleType).Bind(_ema, ProcessMa).Start();
	SubscribeCandles(Silence1CandleType).Bind(ProcessSilence1).Start();
	SubscribeCandles(Silence2CandleType).Bind(ProcessSilence2).Start();
	SubscribeCandles(TimeSpan.FromHours(1).TimeFrame()).Bind(_rsi, ProcessRsi).Start();

	StartProtection();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
	if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
	_lastBid = (decimal)bid;

	if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
	_lastAsk = (decimal)ask;

	CheckTakeProfitTargets();
	}

	private void ProcessDealCandle(ICandleMessage candle)
	{
	if (candle.OpenPrice > 0m)
	_dealOpenPrice = candle.OpenPrice;
	}

	private void ProcessRsi(ICandleMessage candle, decimal rsiValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	_lastRsi = rsiValue;
	_rsiReady = _rsi.IsFormed;
	}

	private void ProcessSilence1(ICandleMessage candle)
	{
	var atrValue = _silenceAtr1.Process(candle).ToNullableDecimal();
	if (atrValue == null)
	return;

	var smoothed = _silenceSmooth1.Process(atrValue.Value, candle.OpenTime, candle.State == CandleStates.Finished).ToNullableDecimal();
	if (smoothed == null)
	return;

	_silence1Value = smoothed.Value;
	_silence1Ready = _silenceSmooth1.IsFormed;
	}

	private void ProcessSilence2(ICandleMessage candle)
	{
	var atrValue = _silenceAtr2.Process(candle).ToNullableDecimal();
	if (atrValue == null)
	return;

	var smoothed = _silenceSmooth2.Process(atrValue.Value, candle.OpenTime, candle.State == CandleStates.Finished).ToNullableDecimal();
	if (smoothed == null)
	return;

	_silence2Value = smoothed.Value;
	_silence2Ready = _silenceSmooth2.IsFormed;
	}

	private void ProcessMa(ICandleMessage candle, decimal emaValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	_maValue = emaValue;
	_maReady = _ema.IsFormed;
	}

	private void ProcessEntryCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (_lastEntryBarTime == candle.OpenTime)
	return;

	_lastEntryBarTime = candle.OpenTime;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (ShouldSkipFriday(candle.OpenTime))
	return;

	var direction = DetermineDirection();

	if (direction > 0)
	TryOpenLong();
	else if (direction < 0)
	TryOpenShort();
	}

	private void ProcessScaleCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (_lastScaleBarTime == candle.OpenTime)
	return;

	_lastScaleBarTime = candle.OpenTime;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	TryApplyGlobalLimit();

	if (_longCount > 0 && ShouldAddLong())
	AddLongPosition();

	if (_shortCount > 0 && ShouldAddShort())
	AddShortPosition();
	}

	private void TryOpenLong()
	{
	if (!_rsiReady || !_silence1Ready || _pipValue <= 0m)
	return;

	if (_lastRsi >= RsiUpperThreshold)
	return;

	if (_silence1Value >= SilenceLevel1)
	return;

	if (_longCount > 0)
	return;

	if (Position < 0m)
	{
	BuyMarket(Math.Abs(Position));
	return;
	}

	var volume = CalculateNewVolume(Sides.Buy);
	if (volume <= 0m)
	return;

	var order = BuyMarket(volume);
	if (order == null)
	return;

	if (SlTpPips > 0 && _lastAsk > 0m)
	{
	_longTakeProfit = _lastAsk + _pipValue * SlTpPips;
	}
	}

	private void TryOpenShort()
	{
	if (!_rsiReady || !_silence1Ready || _pipValue <= 0m)
	return;

	if (_lastRsi <= RsiLowerThreshold)
	return;

	if (_silence1Value >= SilenceLevel1)
	return;

	if (_shortCount > 0)
	return;

	if (Position > 0m)
	{
	SellMarket(Position);
	return;
	}

	var volume = CalculateNewVolume(Sides.Sell);
	if (volume <= 0m)
	return;

	var order = SellMarket(volume);
	if (order == null)
	return;

	if (SlTpPips > 0 && _lastBid > 0m)
	{
	_shortTakeProfit = _lastBid - _pipValue * SlTpPips;
	}
	}

	private bool ShouldAddLong()
	{
	if (!_maReady || !_silence2Ready)
	return false;

	if (_maValue >= _lastBid || _lastBid <= 0m || _lastAsk <= 0m)
	return false;

	if (_lastBuyPrice <= 0m || _longCount >= MaxTrades)
	return false;

	var baseStep = _silence2Value < SilenceLevel2 ? PipStep : PipStep2;
	var multiplier = (decimal)Math.Pow((double)PipStepExponent, _longCount);
	var threshold = baseStep * multiplier * _pipValue;

	return _lastBuyPrice - _lastAsk >= threshold;
	}

	private bool ShouldAddShort()
	{
	if (!_maReady || !_silence2Ready)
	return false;

	if (_maValue <= _lastBid || _lastBid <= 0m || _lastAsk <= 0m)
	return false;

	if (_lastSellPrice <= 0m || _shortCount >= MaxTrades)
	return false;

	var baseStep = _silence2Value < SilenceLevel2 ? PipStep : PipStep2;
	var multiplier = (decimal)Math.Pow((double)PipStepExponent, _shortCount);
	var threshold = baseStep * multiplier * _pipValue;

	return _lastBid - _lastSellPrice >= threshold;
	}

	private void AddLongPosition()
	{
	var volume = CalculateNextVolume(Sides.Buy);
	if (volume <= 0m)
	return;

	BuyMarket(volume);
	}

	private void AddShortPosition()
	{
	var volume = CalculateNextVolume(Sides.Sell);
	if (volume <= 0m)
	return;

	SellMarket(volume);
	}

	private void TryApplyGlobalLimit()
	{
	if (!UseLimit)
	return;

	if (_longCount == 0 || _shortCount == 0)
	return;

	var equity = GetPortfolioEquity();
	if (equity <= 0m || LimitDivisor <= 0m)
	return;

	var profit = CalculateUnrealizedPnL();
	var target = equity / LimitDivisor;

	if (profit >= target)
	{
	ClosePosition();
	ResetPositionTracking();
	}
	}

	private decimal CalculateUnrealizedPnL()
	{
	var longPnL = _longVolume <= 0m || _lastBid <= 0m || _avgLongPrice <= 0m
	? 0m
	: (_lastBid - _avgLongPrice) * _longVolume;

	var shortPnL = _shortVolume <= 0m || _lastAsk <= 0m || _avgShortPrice <= 0m
	? 0m
	: (_avgShortPrice - _lastAsk) * _shortVolume;

	return longPnL + shortPnL;
	}

	private int DetermineDirection()
	{
	if (_dealOpenPrice <= 0m || _lastBid <= 0m)
	return 0;

	if (_dealOpenPrice < _lastBid)
	return 1;

	if (_dealOpenPrice > _lastBid)
	return -1;

	return 0;
	}

	private bool ShouldSkipFriday(DateTimeOffset time)
	{
	if (!CloseFriday)
	return false;

	var local = time.ToLocalTime();
	if (local.DayOfWeek != DayOfWeek.Friday)
	return false;

	return local.TimeOfDay >= TimeSpan.FromHours(CloseFridayHour);
	}

	private void CheckTakeProfitTargets()
	{
	if (_longTakeProfit.HasValue && Position > 0m && _lastBid >= _longTakeProfit.Value)
	{
	SellMarket(Position);
	_longTakeProfit = null;
	}

	if (_shortTakeProfit.HasValue && Position < 0m && _lastAsk <= _shortTakeProfit.Value)
	{
	BuyMarket(Math.Abs(Position));
	_shortTakeProfit = null;
	}
	}

	private decimal CalculateNewVolume(Sides side)
	{
		decimal volume;

		if (FixLot)
		{
			volume = LotSize;
		}
		else
		{
			var equity = GetPortfolioEquity();
			if (equity <= 0m || LotStep <= 0m)
				volume = LotSize;
			else
				volume = LotSize * (equity / LotStep);
		}

		return NormalizeVolume(volume);
	}

	private decimal CalculateNextVolume(Sides side)
	{
		var lastVolume = side == Sides.Buy ? _lastBuyVolume : _lastSellVolume;
		if (lastVolume <= 0m)
			return 0m;

		var count = side == Sides.Buy ? _longCount : _shortCount;
		var exponent = count >= StepX ? LotExponent2 : LotExponent1;
		var volume = lastVolume * exponent;

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security.VolumeStep.GetValueOrDefault();
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		var minVolume = Security.VolumeMin.GetValueOrDefault();
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security.VolumeMax.GetValueOrDefault();
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private decimal GetPortfolioEquity()
	{
		if (Portfolio is null)
			return 0m;

		if (Portfolio.CurrentValue > 0m)
			return Portfolio.CurrentValue;

		if (Portfolio.BeginValue > 0m)
			return Portfolio.BeginValue;

		return 0m;
	}

	private void ResetPositionTracking()
	{
		_netVolume = 0m;
		_longCount = 0;
		_longVolume = 0m;
		_avgLongPrice = 0m;
		_lastBuyPrice = 0m;
		_lastBuyVolume = 0m;
		_longTakeProfit = null;
		_shortCount = 0;
		_shortVolume = 0m;
		_avgShortPrice = 0m;
		_lastSellPrice = 0m;
		_lastSellVolume = 0m;
		_shortTakeProfit = null;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
	base.OnNewMyTrade(trade);

	if (trade.Order.Security != Security)
	return;

	var volume = trade.Trade.Volume;
	if (volume <= 0m)
	return;

	var price = trade.Trade.Price;
	var delta = trade.Order.Direction == Sides.Buy ? volume : -volume;
	var prevNet = _netVolume;
	_netVolume += delta;

	if (delta > 0m)
	{
	HandleBuyFill(price, volume, prevNet);
	}
	else if (delta < 0m)
	{
	HandleSellFill(price, volume, prevNet);
	}

	if (_netVolume == 0m)
	{
	ResetPositionTracking();
	}
	}

	private void HandleBuyFill(decimal price, decimal volume, decimal prevNet)
	{
	if (prevNet < 0m)
	{
	var closing = Math.Min(volume, Math.Abs(prevNet));
	_shortVolume = Math.Max(0m, _shortVolume - closing);

	if (_shortVolume <= 0m || _netVolume >= 0m)
	{
	_shortVolume = 0m;
	_shortCount = 0;
	_avgShortPrice = 0m;
	_lastSellPrice = 0m;
	_lastSellVolume = 0m;
	}

	var remaining = volume - closing;
	if (remaining > 0m)
	AddLongEntry(price, remaining);
	}
	else
	{
	AddLongEntry(price, volume);
	}
	}

	private void HandleSellFill(decimal price, decimal volume, decimal prevNet)
	{
	if (prevNet > 0m)
	{
	var closing = Math.Min(volume, prevNet);
	_longVolume = Math.Max(0m, _longVolume - closing);

	if (_longVolume <= 0m || _netVolume <= 0m)
	{
	_longVolume = 0m;
	_longCount = 0;
	_avgLongPrice = 0m;
	_lastBuyPrice = 0m;
	_lastBuyVolume = 0m;
	_longTakeProfit = null;
	}

	var remaining = volume - closing;
	if (remaining > 0m)
	AddShortEntry(price, remaining);
	}
	else
	{
	AddShortEntry(price, volume);
	}
	}

	private void AddLongEntry(decimal price, decimal volume)
	{
	if (volume <= 0m)
	return;

	_avgLongPrice = _longVolume <= 0m
	? price
	: ((_avgLongPrice * _longVolume) + price * volume) / (_longVolume + volume);

	_longVolume += volume;
	_longCount++;
	_lastBuyPrice = price;
	_lastBuyVolume = volume;
	}

	private void AddShortEntry(decimal price, decimal volume)
	{
	if (volume <= 0m)
	return;

	_avgShortPrice = _shortVolume <= 0m
	? price
	: ((_avgShortPrice * _shortVolume) + price * volume) / (_shortVolume + volume);

	_shortVolume += volume;
	_shortCount++;
	_lastSellPrice = price;
	_lastSellVolume = volume;
	}
}
