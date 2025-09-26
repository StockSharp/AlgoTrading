using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader 4 expert advisor "5MinutesScalpingEA v1.1" to the StockSharp high level API.
/// The strategy combines Hull moving averages, Fisher transform filters and ATR breakouts to generate scalping entries.
/// </summary>
public class FiveMinutesScalpingEaV11Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _indicatorShift;
	private readonly StrategyParam<SignalMode> _signalMode;
	private readonly StrategyParam<bool> _useIndicator1;
	private readonly StrategyParam<bool> _useIndicator2;
	private readonly StrategyParam<bool> _useIndicator3;
	private readonly StrategyParam<bool> _useIndicator4;
	private readonly StrategyParam<bool> _useIndicator5;
	private readonly StrategyParam<int> _period1;
	private readonly StrategyParam<int> _period2;
	private readonly StrategyParam<int> _period3;
	private readonly StrategyParam<PriceMode> _priceMode3;
	private readonly StrategyParam<int> _period4;
	private readonly StrategyParam<int> _period5;
	private readonly StrategyParam<bool> _closeOnSignal;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenPips;
	private readonly StrategyParam<decimal> _breakEvenAfterPips;
	private readonly StrategyParam<decimal> _tradeVolume;

	private HullMovingAverage _hullFast;
	private HullMovingAverage _hullSlow;
	private FisherTransform _fisherMomentum;
	private FisherTransform _fisherTrend;
	private AverageTrueRange _atr;

	private readonly List<decimal> _hullFastHistory = new();
	private readonly List<decimal> _hullSlowHistory = new();
	private readonly List<decimal> _fisherMomentumHistory = new();
	private readonly List<decimal> _fisherTrendHistory = new();
	private readonly List<decimal> _atrHistory = new();
	private readonly List<ICandleMessage> _candleHistory = new();

	private DateTimeOffset? _lastProcessedTime;
	private int _lastSignal;

	private decimal _pipSize;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private decimal? _longBreakEvenPrice;
	private decimal? _shortBreakEvenPrice;
	private decimal? _longTrailAnchor;
	private decimal? _shortTrailAnchor;

	/// <summary>
	/// Initializes a new instance of the <see cref="FiveMinutesScalpingEaV11Strategy"/> class.
	/// </summary>
	public FiveMinutesScalpingEaV11Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Signal Timeframe", "Primary timeframe used for the scalping logic", "General");

		_indicatorShift = Param(nameof(IndicatorShift), 1)
		.SetDisplay("Indicator Shift", "Number of completed candles used for confirmation", "General")
		.SetCanOptimize(true)
		.SetOptimize(0, 3, 1)
		.SetNotNegative();

		_signalMode = Param(nameof(SignalMode), SignalMode.Normal)
		.SetDisplay("Signal Direction", "Switches between normal and reversed signal interpretation", "General");

		_useIndicator1 = Param(nameof(UseIndicator1), true)
		.SetDisplay("Use Hull (30)", "Enable Hull moving average #1 filter", "Indicators");

		_useIndicator2 = Param(nameof(UseIndicator2), true)
		.SetDisplay("Use Hull (50)", "Enable Hull moving average #2 filter", "Indicators");

		_useIndicator3 = Param(nameof(UseIndicator3), true)
		.SetDisplay("Use Fisher", "Enable Fisher transform momentum filter", "Indicators");

		_useIndicator4 = Param(nameof(UseIndicator4), true)
		.SetDisplay("Use ATR Breakout", "Enable ATR based breakout arrows", "Indicators");

		_useIndicator5 = Param(nameof(UseIndicator5), true)
		.SetDisplay("Use Trend Fisher", "Enable Fisher trend confirmation", "Indicators");

		_period1 = Param(nameof(Period1), 30)
		.SetDisplay("Hull #1 Period", "Period of the first Hull moving average", "Indicators")
		.SetGreaterThanZero();

		_period2 = Param(nameof(Period2), 50)
		.SetDisplay("Hull #2 Period", "Period of the second Hull moving average", "Indicators")
		.SetGreaterThanZero();

		_period3 = Param(nameof(Period3), 10)
		.SetDisplay("Fisher Period", "Lookback for the momentum Fisher transform", "Indicators")
		.SetGreaterThanZero();

		_priceMode3 = Param(nameof(PriceMode3), PriceMode.HighLow)
		.SetDisplay("Fisher Price", "Price source for the Fisher momentum filter", "Indicators");

		_period4 = Param(nameof(Period4), 14)
		.SetDisplay("ATR Period", "ATR length for breakout detection", "Indicators")
		.SetGreaterThanZero();

		_period5 = Param(nameof(Period5), 18)
		.SetDisplay("Trend Fisher Period", "Lookback for the trend Fisher transform", "Indicators")
		.SetGreaterThanZero();

		_closeOnSignal = Param(nameof(CloseOnSignal), false)
		.SetDisplay("Close Opposite", "Close opposite positions when a new signal appears", "Risk");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
		.SetDisplay("Use Time Filter", "Restrict trading to a specific intraday window", "Filters");

		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Trading window start hour (0-23)", "Filters")
		.SetNotNegative();

		_endHour = Param(nameof(EndHour), 0)
		.SetDisplay("End Hour", "Trading window end hour (0-23)", "Filters")
		.SetNotNegative();

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
		.SetDisplay("Use Take Profit", "Enable take profit management", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
		.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk")
		.SetNotNegative();

		_useStopLoss = Param(nameof(UseStopLoss), true)
		.SetDisplay("Use Stop Loss", "Enable stop loss management", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 10m)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk")
		.SetNotNegative();

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
		.SetDisplay("Use Trailing Stop", "Enable ATR-style trailing stop", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 1m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
		.SetNotNegative();

		_trailingStepPips = Param(nameof(TrailingStepPips), 1m)
		.SetDisplay("Trailing Step (pips)", "Minimum move before updating the trailing stop", "Risk")
		.SetNotNegative();

		_useBreakEven = Param(nameof(UseBreakEven), false)
		.SetDisplay("Use Break-Even", "Move stop to break-even once the trade is in profit", "Risk");

		_breakEvenPips = Param(nameof(BreakEvenPips), 4m)
		.SetDisplay("Break-Even Offset (pips)", "Offset applied when the stop is moved to break-even", "Risk")
		.SetNotNegative();

		_breakEvenAfterPips = Param(nameof(BreakEvenAfterPips), 2m)
		.SetDisplay("Break-Even Trigger (pips)", "Additional profit required before break-even is armed", "Risk")
		.SetNotNegative();

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetDisplay("Trade Volume", "Order volume used for entries", "General")
		.SetGreaterThanZero();
	}

	/// <summary>
	/// Primary candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of bars applied as indicator shift.
	/// </summary>
	public int IndicatorShift
	{
		get => _indicatorShift.Value;
		set => _indicatorShift.Value = value;
	}

	/// <summary>
	/// Determines if signals are interpreted normally or reversed.
	/// </summary>
	public SignalMode SignalMode
	{
		get => _signalMode.Value;
		set => _signalMode.Value = value;
	}

	/// <summary>
	/// Enables the first Hull moving average filter.
	/// </summary>
	public bool UseIndicator1
	{
		get => _useIndicator1.Value;
		set => _useIndicator1.Value = value;
	}

	/// <summary>
	/// Enables the second Hull moving average filter.
	/// </summary>
	public bool UseIndicator2
	{
		get => _useIndicator2.Value;
		set => _useIndicator2.Value = value;
	}

	/// <summary>
	/// Enables the Fisher transform momentum filter.
	/// </summary>
	public bool UseIndicator3
	{
		get => _useIndicator3.Value;
		set => _useIndicator3.Value = value;
	}

	/// <summary>
	/// Enables the ATR breakout filter.
	/// </summary>
	public bool UseIndicator4
	{
		get => _useIndicator4.Value;
		set => _useIndicator4.Value = value;
	}

	/// <summary>
	/// Enables the trend Fisher confirmation filter.
	/// </summary>
	public bool UseIndicator5
	{
		get => _useIndicator5.Value;
		set => _useIndicator5.Value = value;
	}

	/// <summary>
	/// Determines whether opposite positions are closed when a fresh signal arrives.
	/// </summary>
	public bool CloseOnSignal
	{
		get => _closeOnSignal.Value;
		set => _closeOnSignal.Value = value;
	}

	/// <summary>
	/// Enables the intraday time filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Intraday trading start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Intraday trading end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Enables take profit management.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables stop loss management.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Enables trailing stop management.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum move required before updating the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enables break-even stop adjustments.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Break-even offset in pips.
	/// </summary>
	public decimal BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <summary>
	/// Extra pips required before break-even can be applied.
	/// </summary>
	public decimal BreakEvenAfterPips
	{
		get => _breakEvenAfterPips.Value;
		set => _breakEvenAfterPips.Value = value;
	}

	/// <summary>
	/// Trade volume used for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_pipSize = GetPipSize();
		ResetRiskState();
		_lastSignal = 0;

		_hullFast = new HullMovingAverage { Length = Period1 };
		_hullSlow = new HullMovingAverage { Length = Period2 };
		_fisherMomentum = new FisherTransform { Length = Period3 };
		_fisherTrend = new FisherTransform { Length = Period5 };
		_atr = new AverageTrueRange { Length = Period4 };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_hullFast, _hullSlow, ProcessHullValues)
		.Bind(_fisherMomentum, ProcessFisherMomentum)
		.Bind(_atr, ProcessAtr)
		.Bind(_fisherTrend, ProcessTrendFisher)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_hullFast != null)
			DrawIndicator(area, _hullFast);
			if (_hullSlow != null)
			DrawIndicator(area, _hullSlow);
			if (_fisherMomentum != null)
			DrawIndicator(area, _fisherMomentum);
			if (_fisherTrend != null)
			DrawIndicator(area, _fisherTrend);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetRiskState();
			return;
		}

		if (Position > 0m)
		{
			_shortStopPrice = null;
			_shortTakePrice = null;
			_shortBreakEvenPrice = null;
			_shortTrailAnchor = null;

			if (PositionPrice is decimal entry)
			{
				_longStopPrice = UseStopLoss ? entry - StepsToPrice(StopLossPips) : null;
				_longTakePrice = UseTakeProfit ? entry + StepsToPrice(TakeProfitPips) : null;
				_longBreakEvenPrice = null;
				_longTrailAnchor = entry;
			}
		}
		else if (Position < 0m)
		{
			_longStopPrice = null;
			_longTakePrice = null;
			_longBreakEvenPrice = null;
			_longTrailAnchor = null;

			if (PositionPrice is decimal entry)
			{
				_shortStopPrice = UseStopLoss ? entry + StepsToPrice(StopLossPips) : null;
				_shortTakePrice = UseTakeProfit ? entry - StepsToPrice(TakeProfitPips) : null;
				_shortBreakEvenPrice = null;
				_shortTrailAnchor = entry;
			}
		}
	}

	private void ProcessHullValues(ICandleMessage candle, decimal fastHull, decimal slowHull)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_hullFastHistory.Add(fastHull);
		_hullSlowHistory.Add(slowHull);
		TrimHistory(_hullFastHistory);
		TrimHistory(_hullSlowHistory);
		StoreCandle(candle);
	}

	private void ProcessFisherMomentum(ICandleMessage candle, decimal fisherValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var adjusted = ApplyPriceMode(candle, fisherValue);
		_fisherMomentumHistory.Add(adjusted);
		TrimHistory(_fisherMomentumHistory);
	}

	private void ProcessAtr(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_atrHistory.Add(atrValue);
		TrimHistory(_atrHistory);
	}

	private void ProcessTrendFisher(ICandleMessage candle, decimal fisherValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_fisherTrendHistory.Add(fisherValue);
		TrimHistory(_fisherTrendHistory);

		UpdateSignals(candle);
	}

	private void UpdateSignals(ICandleMessage candle)
	{
		if (_lastProcessedTime == candle.OpenTime)
		return;

		_lastProcessedTime = candle.OpenTime;

		if (!IndicatorsReady())
		return;

		ManageOpenPositions(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsWithinTradingWindow(candle.OpenTime))
		return;

		var shift = IndicatorShift;
		var hull1 = UseIndicator1 ? GetHullDirection(_hullFastHistory, shift) : TrendDirection.Neutral;
		var hull2 = UseIndicator2 ? GetHullDirection(_hullSlowHistory, shift) : TrendDirection.Neutral;
		var fisherMomentum = UseIndicator3 ? GetFisherDirection(_fisherMomentumHistory, shift) : TrendDirection.Neutral;
		var atrBreakout = UseIndicator4 ? GetAtrDirection(shift) : TrendDirection.Neutral;
		var fisherTrend = UseIndicator5 ? GetFisherDirection(_fisherTrendHistory, shift) : TrendDirection.Neutral;

		var longCondition = EvaluateLongCondition(hull1, hull2, fisherMomentum, atrBreakout, fisherTrend);
		var shortCondition = EvaluateShortCondition(hull1, hull2, fisherMomentum, atrBreakout, fisherTrend);

		if (longCondition && _lastSignal <= 0)
		{
			_lastSignal = 1;

			if (CloseOnSignal)
			CloseShort(candle.ClosePrice);

			if (Position <= 0m)
			EnterLong(candle.ClosePrice);
		}
		else if (shortCondition && _lastSignal >= 0)
		{
			_lastSignal = -1;

			if (CloseOnSignal)
			CloseLong(candle.ClosePrice);

			if (Position >= 0m)
			EnterShort(candle.ClosePrice);
		}
	}

	private bool IndicatorsReady()
	{
		var shift = IndicatorShift;
		var hullRequirement = shift + 2;
		var fisherRequirement = shift + 1;
		var atrRequirement = shift + 3;

		if (UseIndicator1 && _hullFastHistory.Count < hullRequirement)
		return false;

		if (UseIndicator2 && _hullSlowHistory.Count < hullRequirement)
		return false;

		if (UseIndicator3 && _fisherMomentumHistory.Count < fisherRequirement)
		return false;

		if (UseIndicator4 && (_atrHistory.Count < atrRequirement || _candleHistory.Count < atrRequirement))
		return false;

		if (UseIndicator5 && _fisherTrendHistory.Count < fisherRequirement)
		return false;

		return true;
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				CloseLong(stop);
				return;
			}

			if (_longTakePrice is decimal take && candle.HighPrice >= take)
			{
				CloseLong(take);
				return;
			}

			UpdateLongRisk(candle);

			if (_longStopPrice is decimal updatedStop && candle.LowPrice <= updatedStop)
			CloseLong(updatedStop);
		}
		else if (Position < 0m)
		{
			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				CloseShort(stop);
				return;
			}

			if (_shortTakePrice is decimal take && candle.LowPrice <= take)
			{
				CloseShort(take);
				return;
			}

			UpdateShortRisk(candle);

			if (_shortStopPrice is decimal updatedStop && candle.HighPrice >= updatedStop)
			CloseShort(updatedStop);
		}
	}

	private void UpdateLongRisk(ICandleMessage candle)
	{
		if (Position <= 0m || PositionPrice is not decimal entry)
		return;

		if (UseBreakEven && BreakEvenPips > 0m)
		{
			var trigger = entry + StepsToPrice(BreakEvenPips + BreakEvenAfterPips);
			var newStop = entry + StepsToPrice(BreakEvenPips);

			if (!_longBreakEvenPrice.HasValue && candle.HighPrice >= trigger)
			{
				_longBreakEvenPrice = newStop;
				if (!_longStopPrice.HasValue || newStop > _longStopPrice.Value)
				_longStopPrice = newStop;
			}
		}

		if (UseTrailingStop && TrailingStopPips > 0m)
		{
			var distance = StepsToPrice(TrailingStopPips);
			var step = StepsToPrice(TrailingStepPips);

			if (!_longTrailAnchor.HasValue)
			_longTrailAnchor = entry;

			var candidate = Math.Max(_longTrailAnchor.Value, candle.HighPrice);
			if (candidate - _longTrailAnchor.Value >= step)
			_longTrailAnchor = candidate;

			var trailingStop = _longTrailAnchor.Value - distance;
			if (!_longStopPrice.HasValue || trailingStop > _longStopPrice.Value)
			_longStopPrice = trailingStop;
		}
	}

	private void UpdateShortRisk(ICandleMessage candle)
	{
		if (Position >= 0m || PositionPrice is not decimal entry)
		return;

		if (UseBreakEven && BreakEvenPips > 0m)
		{
			var trigger = entry - StepsToPrice(BreakEvenPips + BreakEvenAfterPips);
			var newStop = entry - StepsToPrice(BreakEvenPips);

			if (!_shortBreakEvenPrice.HasValue && candle.LowPrice <= trigger)
			{
				_shortBreakEvenPrice = newStop;
				if (!_shortStopPrice.HasValue || newStop < _shortStopPrice.Value)
				_shortStopPrice = newStop;
			}
		}

		if (UseTrailingStop && TrailingStopPips > 0m)
		{
			var distance = StepsToPrice(TrailingStopPips);
			var step = StepsToPrice(TrailingStepPips);

			if (!_shortTrailAnchor.HasValue)
			_shortTrailAnchor = entry;

			var candidate = Math.Min(_shortTrailAnchor.Value, candle.LowPrice);
			if (_shortTrailAnchor.Value - candidate >= step)
			_shortTrailAnchor = candidate;

			var trailingStop = _shortTrailAnchor.Value + distance;
			if (!_shortStopPrice.HasValue || trailingStop < _shortStopPrice.Value)
			_shortStopPrice = trailingStop;
		}
	}

	private void EnterLong(decimal price)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
		return;

		BuyMarket(volume);
	}

	private void EnterShort(decimal price)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
		return;

		SellMarket(volume);
	}

	private void CloseLong(decimal price)
	{
		if (Position <= 0m)
		return;

		SellMarket(Position);
		ResetRiskState();
	}

	private void CloseShort(decimal price)
	{
		if (Position >= 0m)
		return;

		BuyMarket(-Position);
		ResetRiskState();
	}

	private bool EvaluateLongCondition(TrendDirection hull1, TrendDirection hull2, TrendDirection fisherMomentum, TrendDirection atrBreakout, TrendDirection fisherTrend)
	{
		var upHull1 = !UseIndicator1 || hull1 == TrendDirection.Up;
		var upHull2 = !UseIndicator2 || hull2 == TrendDirection.Up;
		var upFisherMomentum = !UseIndicator3 || fisherMomentum == TrendDirection.Up;
		var upAtr = !UseIndicator4 || atrBreakout == TrendDirection.Up;
		var upTrend = !UseIndicator5 || fisherTrend == TrendDirection.Up;

		var downHull1 = !UseIndicator1 || hull1 == TrendDirection.Down;
		var downHull2 = !UseIndicator2 || hull2 == TrendDirection.Down;
		var downFisherMomentum = !UseIndicator3 || fisherMomentum == TrendDirection.Down;
		var downAtr = !UseIndicator4 || atrBreakout == TrendDirection.Down;
		var downTrend = !UseIndicator5 || fisherTrend == TrendDirection.Down;

		return SignalMode == SignalMode.Normal
		? upHull1 && upHull2 && upFisherMomentum && upAtr && upTrend
		: downHull1 && downHull2 && downFisherMomentum && downAtr && downTrend;
	}

	private bool EvaluateShortCondition(TrendDirection hull1, TrendDirection hull2, TrendDirection fisherMomentum, TrendDirection atrBreakout, TrendDirection fisherTrend)
	{
		var downHull1 = !UseIndicator1 || hull1 == TrendDirection.Down;
		var downHull2 = !UseIndicator2 || hull2 == TrendDirection.Down;
		var downFisherMomentum = !UseIndicator3 || fisherMomentum == TrendDirection.Down;
		var downAtr = !UseIndicator4 || atrBreakout == TrendDirection.Down;
		var downTrend = !UseIndicator5 || fisherTrend == TrendDirection.Down;

		var upHull1 = !UseIndicator1 || hull1 == TrendDirection.Up;
		var upHull2 = !UseIndicator2 || hull2 == TrendDirection.Up;
		var upFisherMomentum = !UseIndicator3 || fisherMomentum == TrendDirection.Up;
		var upAtr = !UseIndicator4 || atrBreakout == TrendDirection.Up;
		var upTrend = !UseIndicator5 || fisherTrend == TrendDirection.Up;

		return SignalMode == SignalMode.Normal
		? downHull1 && downHull2 && downFisherMomentum && downAtr && downTrend
		: upHull1 && upHull2 && upFisherMomentum && upAtr && upTrend;
	}

	private TrendDirection GetHullDirection(List<decimal> history, int shift)
	{
		var index = history.Count - 1 - shift;
		if (index <= 0)
		return TrendDirection.Neutral;

		var current = history[index];
		var previous = history[index - 1];

		if (current > previous)
		return TrendDirection.Up;
		if (current < previous)
		return TrendDirection.Down;
		return TrendDirection.Neutral;
	}

	private TrendDirection GetFisherDirection(List<decimal> history, int shift)
	{
		var index = history.Count - 1 - shift;
		if (index < 0)
		return TrendDirection.Neutral;

		var value = history[index];
		if (value > 0m)
		return TrendDirection.Up;
		if (value < 0m)
		return TrendDirection.Down;
		return TrendDirection.Neutral;
	}

	private TrendDirection GetAtrDirection(int shift)
	{
		var index = _candleHistory.Count - 1 - shift;
		if (index < 0 || index + 2 >= _candleHistory.Count)
		return TrendDirection.Neutral;

		var current = _candleHistory[index];
		var previous1 = _candleHistory[index + 1];
		var previous2 = _candleHistory[index + 2];

		var atrIndex = Math.Max(0, Math.Min(_atrHistory.Count - 1, _atrHistory.Count - 1 - shift + 1));
		var atrValue = _atrHistory[Math.Max(0, Math.Min(atrIndex, _atrHistory.Count - 1))];

		var buyCondition = current.HighPrice > previous1.HighPrice + atrValue &&
		current.HighPrice > previous2.HighPrice + atrValue &&
		current.OpenPrice < previous1.ClosePrice + atrValue &&
		current.OpenPrice < previous2.ClosePrice + atrValue;

		var sellCondition = current.LowPrice < previous1.LowPrice - atrValue &&
		current.LowPrice < previous2.LowPrice - atrValue &&
		current.OpenPrice > previous1.ClosePrice - atrValue &&
		current.OpenPrice > previous2.ClosePrice - atrValue;

		if (buyCondition == sellCondition)
		return TrendDirection.Neutral;

		return buyCondition ? TrendDirection.Up : TrendDirection.Down;
	}

	private void StoreCandle(ICandleMessage candle)
	{
		_candleHistory.Add(candle);
		TrimHistory(_candleHistory);
	}

	private void TrimHistory<T>(List<T> list)
	{
		var capacity = IndicatorShift + 10;
		var overflow = list.Count - capacity;
		if (overflow > 0)
		list.RemoveRange(0, overflow);
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (!UseTimeFilter)
		return true;

		var hour = time.LocalDateTime.Hour;
		var start = StartHour % 24;
		var end = EndHour % 24;

		if (start == end)
		return true;

		if (start < end)
		return hour >= start && hour < end;

		return hour >= start || hour < end;
	}

	private decimal StepsToPrice(decimal steps)
	{
		if (_pipSize <= 0m)
		return 0m;

		return steps * _pipSize;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		return step < 1m ? step * 10m : step;
	}

	private void ResetRiskState()
	{
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_longBreakEvenPrice = null;
		_shortBreakEvenPrice = null;
		_longTrailAnchor = null;
		_shortTrailAnchor = null;
	}

	private decimal ApplyPriceMode(ICandleMessage candle, decimal fisherValue)
	{
		return fisherValue;
	}

	private int Period1 => _period1.Value;
	private int Period2 => _period2.Value;
	private int Period3 => _period3.Value;
	private int Period4 => _period4.Value;
	private int Period5 => _period5.Value;
	private PriceMode PriceMode3 => _priceMode3.Value;

	private enum TrendDirection
	{
		Neutral = 0,
		Up = 1,
		Down = -1
	}

	public enum SignalMode
	{
		Normal,
		Reverse
	}

	public enum PriceMode
	{
		HighLow,
		Open,
		Close,
		High,
		Low,
		HighLowClose,
		OpenHighLowClose,
		OpenClose
	}
}
