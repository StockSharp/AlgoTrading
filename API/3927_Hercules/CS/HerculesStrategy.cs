using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor Hercules v1.3.
/// Detects a fast/slow moving average crossover and executes two staggered take profit targets.
/// Filters entries with RSI and envelope indicators across higher timeframes and blocks new signals after execution.
/// </summary>
public class HerculesStrategy : Strategy
{
private readonly StrategyParam<int> _stopShift;

private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _triggerPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _takeProfitFirstPips;
	private readonly StrategyParam<int> _takeProfitSecondPips;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<int> _lookbackMinutes;
	private readonly StrategyParam<int> _blackoutHours;
	private readonly StrategyParam<int> _dailyEnvelopePeriod;
	private readonly StrategyParam<decimal> _dailyEnvelopeDeviation;
	private readonly StrategyParam<int> _h4EnvelopePeriod;
	private readonly StrategyParam<decimal> _h4EnvelopeDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _rsiTimeFrame;
	private readonly StrategyParam<DataType> _dailyTimeFrame;
	private readonly StrategyParam<DataType> _h4TimeFrame;

	private readonly Queue<decimal> _fastHistory = new();
	private readonly Queue<decimal> _slowHistory = new();
	private readonly List<CandleInfo> _candleHistory = new();

	private ExponentialMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private RelativeStrengthIndex _rsiIndicator = null!;
	private SimpleMovingAverage _dailyEnvelopeMa = null!;
	private SimpleMovingAverage _h4EnvelopeMa = null!;

	private decimal _pipSize;
	private TimeSpan _timeFrame;
	private decimal? _triggerPrice;
	private decimal _crossPrice;
	private bool _isCrossUp;
	private DateTimeOffset? _crossTime;
	private decimal? _latestRsi;
	private decimal? _dailyUpper;
	private decimal? _dailyLower;
	private decimal? _h4Upper;
	private decimal? _h4Lower;
	private DateTimeOffset? _blackoutUntil;
	private decimal? _stopPrice;
	private decimal? _firstTargetPrice;
	private decimal? _secondTargetPrice;
	private decimal _firstTargetVolume;
	private decimal _secondTargetVolume;
	private bool _firstTargetActive;
	private bool _secondTargetActive;
private decimal _entryPrice;

public int StopShift
{
get => _stopShift.Value;
set => _stopShift.Value = value;
}

        /// <summary>
        /// Initializes default parameters that mirror the original advisor.
        /// </summary>
public HerculesStrategy()
{
_stopShift = Param(nameof(StopShift), 4)
.SetRange(1, 10)
.SetDisplay("Stop Shift", "Number of candles used to reference prior highs and lows.", "Risk");

_orderVolume = Param(nameof(OrderVolume), 0.01m)
.SetDisplay("Order Volume", "Volume for each market order", "Trading")
.SetCanOptimize(true);

		_useMoneyManagement = Param(nameof(UseMoneyManagement), true)
		.SetDisplay("Money Management", "Recalculate volume from portfolio balance", "Trading")
		.SetCanOptimize(false);

		_riskPercent = Param(nameof(RiskPercent), 2.5m)
		.SetDisplay("Risk %", "Portfolio percentage risked per trade", "Trading")
		.SetCanOptimize(true);

		_triggerPips = Param(nameof(TriggerPips), 38)
		.SetDisplay("Trigger (pips)", "Distance above the crossover used to trigger entries", "Signals")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 90)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance applied to both positions", "Risk")
		.SetCanOptimize(true);

		_takeProfitFirstPips = Param(nameof(TakeProfitFirstPips), 210)
		.SetDisplay("Take Profit #1 (pips)", "First partial take profit distance", "Risk")
		.SetCanOptimize(true);

		_takeProfitSecondPips = Param(nameof(TakeProfitSecondPips), 280)
		.SetDisplay("Take Profit #2 (pips)", "Second partial take profit distance", "Risk")
		.SetCanOptimize(true);

		_fastPeriod = Param(nameof(FastPeriod), 1)
		.SetDisplay("Fast EMA", "Fast EMA length used as trigger line", "Signals")
		.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 72)
		.SetDisplay("Slow SMA", "Slow SMA length used as baseline", "Signals")
		.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 10)
		.SetDisplay("RSI Period", "Length of the H1 RSI filter", "Filters")
		.SetCanOptimize(true);

		_rsiUpper = Param(nameof(RsiUpper), 55m)
		.SetDisplay("RSI Upper", "Upper threshold that enables long trades", "Filters")
		.SetCanOptimize(true);

		_rsiLower = Param(nameof(RsiLower), 45m)
		.SetDisplay("RSI Lower", "Lower threshold that enables short trades", "Filters")
		.SetCanOptimize(true);

		_lookbackMinutes = Param(nameof(LookbackMinutes), 120)
		.SetDisplay("High/Low Lookback (min)", "Minutes used to build the recent high/low filter", "Filters")
		.SetCanOptimize(true);

		_blackoutHours = Param(nameof(BlackoutHours), 144)
		.SetDisplay("Blackout (hours)", "Hours to block new setups after an execution", "Risk")
		.SetCanOptimize(false);

		_dailyEnvelopePeriod = Param(nameof(DailyEnvelopePeriod), 24)
		.SetDisplay("Daily Envelope SMA", "Period of the daily envelope baseline", "Filters")
		.SetCanOptimize(true);

		_dailyEnvelopeDeviation = Param(nameof(DailyEnvelopeDeviation), 0.99m)
		.SetDisplay("Daily Envelope %", "Percentage width of the daily envelope", "Filters")
		.SetCanOptimize(true);

		_h4EnvelopePeriod = Param(nameof(H4EnvelopePeriod), 96)
		.SetDisplay("H4 Envelope SMA", "Period of the H4 envelope baseline", "Filters")
		.SetCanOptimize(true);

		_h4EnvelopeDeviation = Param(nameof(H4EnvelopeDeviation), 0.10m)
		.SetDisplay("H4 Envelope %", "Percentage width of the H4 envelope", "Filters")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(1)))
		.SetDisplay("Main Timeframe", "Primary candle series used for execution", "Data")
		.SetCanOptimize(false);

		_rsiTimeFrame = Param(nameof(RsiTimeFrame), DataType.TimeFrame(TimeSpan.FromHours(1)))
		.SetDisplay("RSI Timeframe", "Candle series used for the RSI filter", "Data")
		.SetCanOptimize(false);

		_dailyTimeFrame = Param(nameof(DailyTimeFrame), DataType.TimeFrame(TimeSpan.FromDays(1)))
		.SetDisplay("Daily Timeframe", "Candle series used for the daily envelope", "Data")
		.SetCanOptimize(false);

		_h4TimeFrame = Param(nameof(H4TimeFrame), DataType.TimeFrame(TimeSpan.FromHours(4)))
		.SetDisplay("H4 Timeframe", "Candle series used for the H4 envelope", "Data")
		.SetCanOptimize(false);
	}

	/// <summary>
	/// Order volume for each entry (before money management adjustment).
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Enables dynamic recalculation of the trade volume.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Percentage of the portfolio value risked per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Distance from the crossover that must be exceeded to arm a signal.
	/// </summary>
	public int TriggerPips
	{
		get => _triggerPips.Value;
		set => _triggerPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// First partial take profit distance in pips.
	/// </summary>
	public int TakeProfitFirstPips
	{
		get => _takeProfitFirstPips.Value;
		set => _takeProfitFirstPips.Value = value;
	}

	/// <summary>
	/// Second partial take profit distance in pips.
	/// </summary>
	public int TakeProfitSecondPips
	{
		get => _takeProfitSecondPips.Value;
		set => _takeProfitSecondPips.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// RSI length on the confirmation timeframe.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Upper RSI threshold for long trades.
	/// </summary>
	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	/// <summary>
	/// Lower RSI threshold for short trades.
	/// </summary>
	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}

	/// <summary>
	/// Lookback window (minutes) for the recent high/low filter.
	/// </summary>
	public int LookbackMinutes
	{
		get => _lookbackMinutes.Value;
		set => _lookbackMinutes.Value = value;
	}

	/// <summary>
	/// Hours to block new trades after one executes.
	/// </summary>
	public int BlackoutHours
	{
		get => _blackoutHours.Value;
		set => _blackoutHours.Value = value;
	}

	/// <summary>
	/// Period of the daily envelope baseline.
	/// </summary>
	public int DailyEnvelopePeriod
	{
		get => _dailyEnvelopePeriod.Value;
		set => _dailyEnvelopePeriod.Value = value;
	}

	/// <summary>
	/// Percentage deviation of the daily envelope.
	/// </summary>
	public decimal DailyEnvelopeDeviation
	{
		get => _dailyEnvelopeDeviation.Value;
		set => _dailyEnvelopeDeviation.Value = value;
	}

	/// <summary>
	/// Period of the H4 envelope baseline.
	/// </summary>
	public int H4EnvelopePeriod
	{
		get => _h4EnvelopePeriod.Value;
		set => _h4EnvelopePeriod.Value = value;
	}

	/// <summary>
	/// Percentage deviation of the H4 envelope.
	/// </summary>
	public decimal H4EnvelopeDeviation
	{
		get => _h4EnvelopeDeviation.Value;
		set => _h4EnvelopeDeviation.Value = value;
	}

	/// <summary>
	/// Primary candle series used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Timeframe used for the RSI filter.
	/// </summary>
	public DataType RsiTimeFrame
	{
		get => _rsiTimeFrame.Value;
		set => _rsiTimeFrame.Value = value;
	}

	/// <summary>
	/// Timeframe used for the daily envelope filter.
	/// </summary>
	public DataType DailyTimeFrame
	{
		get => _dailyTimeFrame.Value;
		set => _dailyTimeFrame.Value = value;
	}

	/// <summary>
	/// Timeframe used for the H4 envelope filter.
	/// </summary>
	public DataType H4TimeFrame
	{
		get => _h4TimeFrame.Value;
		set => _h4TimeFrame.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new (Security sec, DataType dt)[]
		{
			(Security, CandleType),
			(Security, RsiTimeFrame),
			(Security, DailyTimeFrame),
			(Security, H4TimeFrame),
	};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastHistory.Clear();
		_slowHistory.Clear();
		_candleHistory.Clear();
		_triggerPrice = null;
		_crossPrice = 0m;
		_crossTime = null;
		_latestRsi = null;
		_dailyUpper = null;
		_dailyLower = null;
		_h4Upper = null;
		_h4Lower = null;
		_blackoutUntil = null;
		_stopPrice = null;
		_firstTargetPrice = null;
		_secondTargetPrice = null;
		_firstTargetVolume = 0m;
		_secondTargetVolume = 0m;
		_firstTargetActive = false;
		_secondTargetActive = false;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_fastMa = new ExponentialMovingAverage { Length = FastPeriod };
		_slowMa = new SimpleMovingAverage { Length = SlowPeriod };
		_rsiIndicator = new RelativeStrengthIndex { Length = RsiPeriod };
		_dailyEnvelopeMa = new SimpleMovingAverage { Length = DailyEnvelopePeriod };
		_h4EnvelopeMa = new SimpleMovingAverage { Length = H4EnvelopePeriod };

		_pipSize = GetPipSize();
		_timeFrame = GetTimeFrame(CandleType);

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.Bind(ProcessMainCandle)
		.Start();

		var rsiSubscription = SubscribeCandles(RsiTimeFrame);
		rsiSubscription
		.Bind(ProcessRsiCandle)
		.Start();

		var dailySubscription = SubscribeCandles(DailyTimeFrame);
		dailySubscription
		.Bind(ProcessDailyEnvelope)
		.Start();

		var h4Subscription = SubscribeCandles(H4TimeFrame);
		h4Subscription
		.Bind(ProcessH4Envelope)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_fastMa.Length = FastPeriod;
		_slowMa.Length = SlowPeriod;

		var fastValue = _fastMa.Process(candle.ClosePrice, candle.CloseTime, true);
		var slowValue = _slowMa.Process(candle.OpenPrice, candle.CloseTime, true);

		if (!fastValue.IsFinal || !slowValue.IsFinal)
		return;

		var fastDecimal = fastValue.ToDecimal();
		var slowDecimal = slowValue.ToDecimal();

		UpdateQueue(_fastHistory, fastDecimal);
		UpdateQueue(_slowHistory, slowDecimal);

		_candleHistory.Add(new CandleInfo(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.CloseTime));
		TrimHistory();

		HandleOpenPosition(candle);

		UpdateCrossoverState();

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_blackoutUntil.HasValue && CurrentTime < _blackoutUntil.Value)
		return;

		var hasWindow = _crossTime.HasValue && CurrentTime - _crossTime.Value <= TimeSpan.FromTicks(_timeFrame.Ticks * 2);
		if (!hasWindow || !_triggerPrice.HasValue)
		return;

		var (recentHigh, recentLow) = GetRecentRange();

		if (_latestRsi.HasValue && _dailyUpper.HasValue && _dailyLower.HasValue && _h4Upper.HasValue && _h4Lower.HasValue)
		{
			if (_isCrossUp && Position <= 0m)
			{
				if (candle.ClosePrice >= _triggerPrice.Value && _latestRsi.Value > RsiUpper && candle.ClosePrice > recentHigh && candle.ClosePrice > _dailyUpper.Value && candle.ClosePrice > _h4Upper.Value)
				{
					EnterLong(candle);
				}
			}
			else if (!_isCrossUp && Position >= 0m)
			{
				if (candle.ClosePrice <= _triggerPrice.Value && _latestRsi.Value < RsiLower && candle.ClosePrice < recentLow && candle.ClosePrice < _dailyLower.Value && candle.ClosePrice < _h4Lower.Value)
				{
					EnterShort(candle);
				}
			}
		}
	}

	private void ProcessRsiCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_rsiIndicator.Length = RsiPeriod;
		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var value = _rsiIndicator.Process(typicalPrice, candle.CloseTime, true);
		if (!value.IsFinal)
		return;

		_latestRsi = value.ToDecimal();
	}

	private void ProcessDailyEnvelope(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_dailyEnvelopeMa.Length = DailyEnvelopePeriod;
		var value = _dailyEnvelopeMa.Process(candle.ClosePrice, candle.CloseTime, true);
		if (!value.IsFinal)
		return;

		var baseline = value.ToDecimal();
		var deviation = DailyEnvelopeDeviation / 100m;
		_dailyUpper = baseline * (1m + deviation);
		_dailyLower = baseline * (1m - deviation);
	}

	private void ProcessH4Envelope(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_h4EnvelopeMa.Length = H4EnvelopePeriod;
		var value = _h4EnvelopeMa.Process(candle.ClosePrice, candle.CloseTime, true);
		if (!value.IsFinal)
		return;

		var baseline = value.ToDecimal();
		var deviation = H4EnvelopeDeviation / 100m;
		_h4Upper = baseline * (1m + deviation);
		_h4Lower = baseline * (1m - deviation);
	}

	private void EnterLong(ICandleMessage candle)
	{
		var stopPrice = GetStopPrice(true);
		if (!stopPrice.HasValue)
		return;

		var volume = CalculateEntryVolume(candle.ClosePrice, stopPrice.Value);
		if (volume <= 0m)
		return;

		_entryPrice = candle.ClosePrice;
		_blackoutUntil = CurrentTime + TimeSpan.FromHours(BlackoutHours);
		_stopPrice = stopPrice;

		var firstTarget = TakeProfitFirstPips > 0 ? candle.ClosePrice + TakeProfitFirstPips * _pipSize : (decimal?)null;
		var secondTarget = TakeProfitSecondPips > 0 ? candle.ClosePrice + TakeProfitSecondPips * _pipSize : (decimal?)null;

		var firstVolume = RoundVolume(volume);
		var secondVolume = RoundVolume(volume);

		if (firstTarget.HasValue)
		{
			BuyMarket(firstVolume);
			_firstTargetActive = true;
			_firstTargetPrice = firstTarget;
			_firstTargetVolume = firstVolume;
		}

		if (secondTarget.HasValue)
		{
			BuyMarket(secondVolume);
			_secondTargetActive = true;
			_secondTargetPrice = secondTarget;
			_secondTargetVolume = secondVolume;
		}

		if (!firstTarget.HasValue && !secondTarget.HasValue)
		{
			BuyMarket(firstVolume);
			_firstTargetActive = false;
			_secondTargetActive = false;
			_firstTargetVolume = firstVolume;
		}

		LogInfo($"Entered long at {candle.ClosePrice} with stop {stopPrice}.");
	}

	private void EnterShort(ICandleMessage candle)
	{
		var stopPrice = GetStopPrice(false);
		if (!stopPrice.HasValue)
		return;

		var volume = CalculateEntryVolume(candle.ClosePrice, stopPrice.Value);
		if (volume <= 0m)
		return;

		_entryPrice = candle.ClosePrice;
		_blackoutUntil = CurrentTime + TimeSpan.FromHours(BlackoutHours);
		_stopPrice = stopPrice;

		var firstTarget = TakeProfitFirstPips > 0 ? candle.ClosePrice - TakeProfitFirstPips * _pipSize : (decimal?)null;
		var secondTarget = TakeProfitSecondPips > 0 ? candle.ClosePrice - TakeProfitSecondPips * _pipSize : (decimal?)null;

		var firstVolume = RoundVolume(volume);
		var secondVolume = RoundVolume(volume);

		if (firstTarget.HasValue)
		{
			SellMarket(firstVolume);
			_firstTargetActive = true;
			_firstTargetPrice = firstTarget;
			_firstTargetVolume = firstVolume;
		}

		if (secondTarget.HasValue)
		{
			SellMarket(secondVolume);
			_secondTargetActive = true;
			_secondTargetPrice = secondTarget;
			_secondTargetVolume = secondVolume;
		}

		if (!firstTarget.HasValue && !secondTarget.HasValue)
		{
			SellMarket(firstVolume);
			_firstTargetActive = false;
			_secondTargetActive = false;
			_firstTargetVolume = firstVolume;
		}

		LogInfo($"Entered short at {candle.ClosePrice} with stop {stopPrice}.");
	}

	private void HandleOpenPosition(ICandleMessage candle)
	{
		if (Position == 0m)
		return;

		if (_stopPrice.HasValue)
		{
			if (Position > 0m && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (Position < 0m && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}
		}

		ApplyTrailingStop(candle);
		HandleTakeProfits(candle);

		if (Position == 0m)
		ResetPositionState();
	}

	private void ApplyTrailingStop(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || _pipSize <= 0m)
		return;

		var trailingDistance = TrailingStopPips * _pipSize;

		if (Position > 0m)
		{
			var newStop = candle.ClosePrice - trailingDistance;
			if (!_stopPrice.HasValue || newStop > _stopPrice.Value)
			_stopPrice = newStop;
		}
		else if (Position < 0m)
		{
			var newStop = candle.ClosePrice + trailingDistance;
			if (!_stopPrice.HasValue || newStop < _stopPrice.Value)
			_stopPrice = newStop;
		}
	}

	private void HandleTakeProfits(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_firstTargetActive && _firstTargetPrice.HasValue && candle.HighPrice >= _firstTargetPrice.Value)
			{
				var volume = RoundVolume(Math.Min(Math.Abs(Position), _firstTargetVolume));
				if (volume > 0m)
				SellMarket(volume);
				_firstTargetActive = false;
			}

			if (_secondTargetActive && _secondTargetPrice.HasValue && candle.HighPrice >= _secondTargetPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				_secondTargetActive = false;
			}
		}
		else if (Position < 0m)
		{
			if (_firstTargetActive && _firstTargetPrice.HasValue && candle.LowPrice <= _firstTargetPrice.Value)
			{
				var volume = RoundVolume(Math.Min(Math.Abs(Position), _firstTargetVolume));
				if (volume > 0m)
				BuyMarket(volume);
				_firstTargetActive = false;
			}

			if (_secondTargetActive && _secondTargetPrice.HasValue && candle.LowPrice <= _secondTargetPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				_secondTargetActive = false;
			}
		}
	}

	private void UpdateCrossoverState()
	{
		if (_fastHistory.Count < 3 || _slowHistory.Count < 3)
		return;

		var fast1 = GetFromEnd(_fastHistory, 1);
		var fast2 = GetFromEnd(_fastHistory, 2);
		var fast3 = GetFromEnd(_fastHistory, 3);

		var slow1 = GetFromEnd(_slowHistory, 1);
		var slow2 = GetFromEnd(_slowHistory, 2);
		var slow3 = GetFromEnd(_slowHistory, 3);

		if (fast1 > slow1 && fast2 < slow2)
		{
			_isCrossUp = true;
			_crossPrice = (fast1 + fast2 + slow1 + slow2) / 4m;
			_triggerPrice = _crossPrice + TriggerPips * _pipSize;
			_crossTime = CurrentTime - _timeFrame;
			return;
		}

		if (fast2 > slow2 && fast3 < slow3)
		{
			_isCrossUp = true;
			_crossPrice = (fast2 + fast3 + slow2 + slow3) / 4m;
			_triggerPrice = _crossPrice + TriggerPips * _pipSize;
			_crossTime = CurrentTime - _timeFrame * 2;
			return;
		}

		if (fast1 < slow1 && fast2 > slow2)
		{
			_isCrossUp = false;
			_crossPrice = (fast1 + fast2 + slow1 + slow2) / 4m;
			_triggerPrice = _crossPrice - TriggerPips * _pipSize;
			_crossTime = CurrentTime - _timeFrame;
			return;
		}

		if (fast2 < slow2 && fast3 > slow3)
		{
			_isCrossUp = false;
			_crossPrice = (fast2 + fast3 + slow2 + slow3) / 4m;
			_triggerPrice = _crossPrice - TriggerPips * _pipSize;
			_crossTime = CurrentTime - _timeFrame * 2;
		}
	}

	private (decimal high, decimal low) GetRecentRange()
	{
		var high = decimal.MinValue;
		var low = decimal.MaxValue;

		var minutes = Math.Max(1d, _timeFrame.TotalMinutes);
		var barsNeeded = (int)Math.Max(1d, Math.Round(LookbackMinutes / minutes));

		var count = Math.Min(barsNeeded, _candleHistory.Count - 1);
		if (count <= 0)
		return (_candleHistory.Count > 0 ? _candleHistory[^1].High : 0m, _candleHistory.Count > 0 ? _candleHistory[^1].Low : 0m);

		for (var i = 1; i <= count; i++)
		{
			var candle = GetCandle(i);
			if (candle.High > high)
			high = candle.High;
			if (candle.Low < low)
			low = candle.Low;
		}

		return (high, low);
	}

	private decimal? GetStopPrice(bool isLong)
	{
		if (_candleHistory.Count <= StopShift)
		return null;

		var candle = GetCandle(StopShift);
		return isLong ? candle.Low : candle.High;
	}

	private CandleInfo GetCandle(int shift)
	{
		var index = _candleHistory.Count - 1 - shift;
		if (index < 0)
		index = 0;
		return _candleHistory[index];
	}

	private decimal CalculateEntryVolume(decimal entryPrice, decimal stopPrice)
	{
		var volume = OrderVolume;
		if (!UseMoneyManagement || Portfolio == null)
		return RoundVolume(volume);

		var stopDistance = Math.Abs(entryPrice - stopPrice);
		if (stopDistance <= 0m)
		return RoundVolume(volume);

		var riskMoney = Portfolio.CurrentValue * RiskPercent / 100m;
		if (riskMoney <= 0m)
		return RoundVolume(volume);

		var rawVolume = riskMoney / stopDistance;
		return RoundVolume(Math.Max(volume, rawVolume));
	}

	private void ResetPositionState()
	{
		_stopPrice = null;
		_firstTargetPrice = null;
		_secondTargetPrice = null;
		_firstTargetVolume = 0m;
		_secondTargetVolume = 0m;
		_firstTargetActive = false;
		_secondTargetActive = false;
		_entryPrice = 0m;
	}

	private static void UpdateQueue(Queue<decimal> queue, decimal value, int max = 10)
	{
		queue.Enqueue(value);
		while (queue.Count > max)
		queue.Dequeue();
	}

	private static decimal GetFromEnd(Queue<decimal> queue, int position)
	{
		var index = queue.Count - position;
		var i = 0;
		foreach (var value in queue)
		{
			if (i == index)
			return value;
			i++;
		}

		return 0m;
	}

	private void TrimHistory()
	{
		const int maxCandles = 500;
		if (_candleHistory.Count <= maxCandles)
		return;

		var removeCount = _candleHistory.Count - maxCandles;
		_candleHistory.RemoveRange(0, removeCount);
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
		return 0.0001m;

		var step = security.PriceStep ?? 0.0001m;
		var decimals = security.Decimals ?? 0;

		if (decimals >= 3)
		return step * 10m;

		return step > 0m ? step : 0.0001m;
	}

	private decimal RoundVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return Math.Round(volume, 2, MidpointRounding.AwayFromZero);

		var step = security.VolumeStep ?? 0.01m;
		var minVolume = security.MinVolume ?? step;

		if (step <= 0m)
		return volume;

		var rounded = Math.Floor(volume / step) * step;
		if (rounded < minVolume)
		rounded = minVolume;

		return rounded;
	}

	private static TimeSpan GetTimeFrame(DataType dataType)
	{
		return dataType.Arg switch
		{
			TimeSpan span => span,
			_ => TimeSpan.FromMinutes(60),
	};
	}

	private readonly record struct CandleInfo(decimal Open, decimal High, decimal Low, decimal Close, DateTimeOffset CloseTime);
}
