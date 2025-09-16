using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the Hercules A.T.C. 2006 MetaTrader expert advisor.
/// Detects EMA/SMA crossovers with trigger windows and multiple filters
/// before submitting two staged take-profit orders and applying a trailing stop.
/// </summary>
public class HerculesATC2006Strategy : Strategy
{
	private readonly StrategyParam<int> _triggerPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _takeProfit1Pips;
	private readonly StrategyParam<int> _takeProfit2Pips;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _stopLossLookback;
	private readonly StrategyParam<int> _highLowHours;
	private readonly StrategyParam<int> _blackoutHours;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<int> _dailyEnvelopePeriod;
	private readonly StrategyParam<decimal> _dailyEnvelopeDeviation;
	private readonly StrategyParam<int> _h4EnvelopePeriod;
	private readonly StrategyParam<decimal> _h4EnvelopeDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _rsiTimeFrame;
	private readonly StrategyParam<DataType> _dailyEnvelopeTimeFrame;
	private readonly StrategyParam<DataType> _h4EnvelopeTimeFrame;

	private readonly RelativeStrengthIndex _rsi = new();
	private readonly SimpleMovingAverage _dailyEnvelopeMa = new();
	private readonly SimpleMovingAverage _h4EnvelopeMa = new();

	private readonly decimal[] _fastHistory = new decimal[4];
	private readonly decimal[] _slowHistory = new decimal[4];
	private readonly DateTimeOffset[] _timeHistory = new DateTimeOffset[4];
	private int _historyCount;

	private readonly decimal[] _highStopHistory = new decimal[5];
	private readonly decimal[] _lowStopHistory = new decimal[5];
	private int _stopHistoryCount;

	private readonly Queue<decimal> _recentHighs = new();
	private readonly Queue<decimal> _recentLows = new();
	private decimal _rollingHigh;
	private decimal _rollingLow;
	private bool _highLowReady;

	private decimal _priceStep;
	private decimal _pipSize;
	private TimeSpan _primaryTimeFrame;
	private int _highLowLength;

	private int _pendingDirection;
	private decimal _triggerPrice;
	private DateTimeOffset? _windowEndTime;
	private decimal _crossPrice;

	private decimal _lastRsi;
	private bool _rsiReady;

	private decimal _dailyUpper;
	private decimal _dailyLower;
	private bool _dailyReady;

	private decimal _h4Upper;
	private decimal _h4Lower;
	private bool _h4Ready;

	private DateTimeOffset? _blackoutUntil;

	private decimal? _entryPrice;
	private decimal? _stopLoss;
	private decimal? _tp1;
	private decimal? _tp2;
	private decimal? _trailingStop;
	private bool _tp1Hit;

	/// <summary>
	/// Number of pips added to the crossover price to form the trigger level.
	/// </summary>
	public int TriggerPips
	{
		get => _triggerPips.Value;
		set => _triggerPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// First take-profit distance in pips.
	/// </summary>
	public int TakeProfit1Pips
	{
		get => _takeProfit1Pips.Value;
		set => _takeProfit1Pips.Value = value;
	}

	/// <summary>
	/// Second take-profit distance in pips.
	/// </summary>
	public int TakeProfit2Pips
	{
		get => _takeProfit2Pips.Value;
		set => _takeProfit2Pips.Value = value;
	}

	/// <summary>
	/// Fast EMA period used for the trigger.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow SMA period used as the baseline.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Number of completed candles used to fetch the stop-loss reference.
	/// </summary>
	public int StopLossLookback
	{
		get => _stopLossLookback.Value;
		set => _stopLossLookback.Value = value;
	}

	/// <summary>
	/// Number of hours used for the rolling high/low breakout filter.
	/// </summary>
	public int HighLowHours
	{
		get => _highLowHours.Value;
		set => _highLowHours.Value = value;
	}

	/// <summary>
	/// Cooldown duration in hours after a successful trade.
	/// </summary>
	public int BlackoutHours
	{
		get => _blackoutHours.Value;
		set => _blackoutHours.Value = value;
	}

	/// <summary>
	/// RSI length applied on the higher timeframe filter.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Upper RSI threshold required for long positions.
	/// </summary>
	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	/// <summary>
	/// Lower RSI threshold required for short positions.
	/// </summary>
	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}
	/// <summary>
	/// Daily envelope moving average period.
	/// </summary>
	public int DailyEnvelopePeriod
	{
		get => _dailyEnvelopePeriod.Value;
		set => _dailyEnvelopePeriod.Value = value;
	}

	/// <summary>
	/// Daily envelope deviation in percent.
	/// </summary>
	public decimal DailyEnvelopeDeviation
	{
		get => _dailyEnvelopeDeviation.Value;
		set => _dailyEnvelopeDeviation.Value = value;
	}

	/// <summary>
	/// Four-hour envelope moving average period.
	/// </summary>
	public int H4EnvelopePeriod
	{
		get => _h4EnvelopePeriod.Value;
		set => _h4EnvelopePeriod.Value = value;
	}

	/// <summary>
	/// Four-hour envelope deviation in percent.
	/// </summary>
	public decimal H4EnvelopeDeviation
	{
		get => _h4EnvelopeDeviation.Value;
		set => _h4EnvelopeDeviation.Value = value;
	}

	/// <summary>
	/// Primary candle type that drives entries and exits.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used to compute RSI.
	/// </summary>
	public DataType RsiTimeFrame
	{
		get => _rsiTimeFrame.Value;
		set => _rsiTimeFrame.Value = value;
	}

	/// <summary>
	/// Candle type used for the daily envelope filter.
	/// </summary>
	public DataType DailyEnvelopeTimeFrame
	{
		get => _dailyEnvelopeTimeFrame.Value;
		set => _dailyEnvelopeTimeFrame.Value = value;
	}

	/// <summary>
	/// Candle type used for the four-hour envelope filter.
	/// </summary>
	public DataType H4EnvelopeTimeFrame
	{
		get => _h4EnvelopeTimeFrame.Value;
		set => _h4EnvelopeTimeFrame.Value = value;
	}
	/// <summary>
	/// Initializes a new instance of <see cref="HerculesATC2006Strategy"/>.
	/// </summary>
	public HerculesATC2006Strategy()
	{
		_triggerPips = Param(nameof(TriggerPips), 38)
			.SetGreaterThanZero()
			.SetDisplay("Trigger Pips", "Distance above/below crossover required to trigger", "Entries")
			.SetCanOptimize(true)
			.SetOptimize(10, 80, 5);

		_trailingStopPips = Param(nameof(TrailingStopPips), 90)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20, 150, 10);

		_takeProfit1Pips = Param(nameof(TakeProfit1Pips), 210)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Take Profit 1 (pips)", "First take-profit distance", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100, 260, 10);

		_takeProfit2Pips = Param(nameof(TakeProfit2Pips), 280)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Take Profit 2 (pips)", "Second take-profit distance", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(150, 360, 10);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Length of the fast EMA", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 72)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Length of the slow SMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 120, 4);

		_stopLossLookback = Param(nameof(StopLossLookback), 4)
			.SetGreaterThanZero()
			.SetDisplay("Stop-Loss Lookback", "Number of completed candles used for stop-loss", "Risk Management");

		_highLowHours = Param(nameof(HighLowHours), 10)
			.SetGreaterThanZero()
			.SetDisplay("High/Low Window (hours)", "Duration used for breakout filter", "Filters");

		_blackoutHours = Param(nameof(BlackoutHours), 144)
			.SetGreaterThanZero()
			.SetDisplay("Blackout Hours", "Cooldown after a trade", "Filters");

		_rsiLength = Param(nameof(RsiLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period on higher timeframe", "Filters");

		_rsiUpper = Param(nameof(RsiUpper), 55m)
			.SetDisplay("RSI Upper", "Upper RSI threshold for longs", "Filters");

		_rsiLower = Param(nameof(RsiLower), 45m)
			.SetDisplay("RSI Lower", "Lower RSI threshold for shorts", "Filters");

		_dailyEnvelopePeriod = Param(nameof(DailyEnvelopePeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("Daily Envelope Period", "Daily SMA length for envelope", "Filters");

		_dailyEnvelopeDeviation = Param(nameof(DailyEnvelopeDeviation), 0.99m)
			.SetGreaterThanZero()
			.SetDisplay("Daily Envelope %", "Envelope deviation in percent", "Filters");

		_h4EnvelopePeriod = Param(nameof(H4EnvelopePeriod), 96)
			.SetGreaterThanZero()
			.SetDisplay("H4 Envelope Period", "Four-hour SMA length for envelope", "Filters");

		_h4EnvelopeDeviation = Param(nameof(H4EnvelopeDeviation), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("H4 Envelope %", "Envelope deviation in percent", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Primary Candle", "Working timeframe for entries", "General");

		_rsiTimeFrame = Param(nameof(RsiTimeFrame), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("RSI Candle", "Timeframe used for RSI filter", "Filters");

		_dailyEnvelopeTimeFrame = Param(nameof(DailyEnvelopeTimeFrame), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Envelope TF", "Timeframe for the daily envelope", "Filters");

		_h4EnvelopeTimeFrame = Param(nameof(H4EnvelopeTimeFrame), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("H4 Envelope TF", "Timeframe for the four-hour envelope", "Filters");

		Volume = 2m;
	}
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var uniqueTypes = new HashSet<DataType> { CandleType, RsiTimeFrame, DailyEnvelopeTimeFrame, H4EnvelopeTimeFrame };

		foreach (var type in uniqueTypes)
		{
			yield return (Security, type);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Array.Clear(_fastHistory, 0, _fastHistory.Length);
		Array.Clear(_slowHistory, 0, _slowHistory.Length);
		Array.Clear(_timeHistory, 0, _timeHistory.Length);
		Array.Clear(_highStopHistory, 0, _highStopHistory.Length);
		Array.Clear(_lowStopHistory, 0, _lowStopHistory.Length);

		_historyCount = 0;
		_stopHistoryCount = 0;

		_recentHighs.Clear();
		_recentLows.Clear();
		_rollingHigh = 0m;
		_rollingLow = 0m;
		_highLowReady = false;

		_lastRsi = 0m;
		_rsiReady = false;

		_dailyUpper = 0m;
		_dailyLower = 0m;
		_dailyReady = false;

		_h4Upper = 0m;
		_h4Lower = 0m;
		_h4Ready = false;

		_blackoutUntil = null;

		_entryPrice = null;
		_stopLoss = null;
		_tp1 = null;
		_tp2 = null;
		_trailingStop = null;
		_tp1Hit = false;

		_pendingDirection = 0;
		_triggerPrice = 0m;
		_windowEndTime = null;
		_crossPrice = 0m;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_priceStep = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		var pipFactor = decimals is 3 or 5 ? 10m : 1m;
		_pipSize = _priceStep * pipFactor;

		_primaryTimeFrame = CandleType.Arg is TimeSpan span && span > TimeSpan.Zero ? span : TimeSpan.FromMinutes(1);
		_highLowLength = Math.Max(1, (int)Math.Round(HighLowHours * 60m / (decimal)_primaryTimeFrame.TotalMinutes, MidpointRounding.AwayFromZero));

		var fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		var slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };

		var mainSubscription = SubscribeCandles(CandleType);

		mainSubscription
			.Bind(fastMa, slowMa, ProcessPrimary)
			.Start();

		SubscribeCandles(RsiTimeFrame)
			.Bind(_rsi, ProcessRsi)
			.Start();

		_dailyEnvelopeMa.Length = DailyEnvelopePeriod;
		SubscribeCandles(DailyEnvelopeTimeFrame)
			.Bind(_dailyEnvelopeMa, ProcessDailyEnvelope)
			.Start();

		_h4EnvelopeMa.Length = H4EnvelopePeriod;
		SubscribeCandles(H4EnvelopeTimeFrame)
			.Bind(_h4EnvelopeMa, ProcessH4Envelope)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}
	private void ProcessPrimary(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateHighLow(candle);
		UpdateStopHistory(candle);
		UpdateHistory(candle, fast, slow);
		UpdateBlackout(candle.OpenTime);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		EvaluateEntry(candle);
		ManagePosition(candle);
	}

	private void ProcessRsi(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastRsi = rsiValue;
		_rsiReady = true;
	}

	private void ProcessDailyEnvelope(ICandleMessage candle, decimal basis)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var deviation = DailyEnvelopeDeviation / 100m;
		_dailyUpper = basis * (1 + deviation);
		_dailyLower = basis * (1 - deviation);
		_dailyReady = _dailyEnvelopeMa.IsFormed;
	}

	private void ProcessH4Envelope(ICandleMessage candle, decimal basis)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var deviation = H4EnvelopeDeviation / 100m;
		_h4Upper = basis * (1 + deviation);
		_h4Lower = basis * (1 - deviation);
		_h4Ready = _h4EnvelopeMa.IsFormed;
	}

	private void UpdateBlackout(DateTimeOffset currentTime)
	{
		if (_blackoutUntil is DateTimeOffset until && currentTime >= until)
		{
			_blackoutUntil = null;
		}
	}
	private void UpdateHistory(ICandleMessage candle, decimal fast, decimal slow)
	{
		ShiftHistory(_fastHistory, fast);
		ShiftHistory(_slowHistory, slow);
		ShiftHistory(_timeHistory, candle.OpenTime);

		if (_historyCount < _fastHistory.Length)
		{
			_historyCount++;
		}

		if (_historyCount < _fastHistory.Length)
			return;

		var crossUp1 = _fastHistory[1] > _slowHistory[1] && _fastHistory[2] < _slowHistory[2];
		var crossUp2 = _fastHistory[2] > _slowHistory[2] && _fastHistory[3] < _slowHistory[3];
		var crossDown1 = _fastHistory[1] < _slowHistory[1] && _fastHistory[2] > _slowHistory[2];
		var crossDown2 = _fastHistory[2] < _slowHistory[2] && _fastHistory[3] > _slowHistory[3];

		if (crossUp1)
		{
			PrepareTrigger(1, (_fastHistory[1] + _fastHistory[2] + _slowHistory[1] + _slowHistory[2]) / 4m, _timeHistory[1]);
		}
		else if (crossUp2)
		{
			PrepareTrigger(1, (_fastHistory[2] + _fastHistory[3] + _slowHistory[2] + _slowHistory[3]) / 4m, _timeHistory[2]);
		}
		else if (crossDown1)
		{
			PrepareTrigger(-1, (_fastHistory[1] + _fastHistory[2] + _slowHistory[1] + _slowHistory[2]) / 4m, _timeHistory[1]);
		}
		else if (crossDown2)
		{
			PrepareTrigger(-1, (_fastHistory[2] + _fastHistory[3] + _slowHistory[2] + _slowHistory[3]) / 4m, _timeHistory[2]);
		}
	}

	private void PrepareTrigger(int direction, decimal crossPrice, DateTimeOffset crossTime)
	{
		_pendingDirection = direction;
		_crossPrice = crossPrice;
		_triggerPrice = direction > 0 ? crossPrice + TriggerPips * _pipSize : crossPrice - TriggerPips * _pipSize;
		_windowEndTime = crossTime + _primaryTimeFrame + _primaryTimeFrame;
	}

	private void UpdateStopHistory(ICandleMessage candle)
	{
		ShiftHistory(_highStopHistory, candle.HighPrice);
		ShiftHistory(_lowStopHistory, candle.LowPrice);

		if (_stopHistoryCount < _highStopHistory.Length)
		{
			_stopHistoryCount++;
		}
	}

	private void UpdateHighLow(ICandleMessage candle)
	{
		_recentHighs.Enqueue(candle.HighPrice);
		_recentLows.Enqueue(candle.LowPrice);

		TrimQueue(_recentHighs, _highLowLength);
		TrimQueue(_recentLows, _highLowLength);

		if (_recentHighs.Count < _highLowLength || _recentLows.Count < _highLowLength)
		{
			_highLowReady = false;
			return;
		}

		_rollingHigh = GetExtreme(_recentHighs, true);
		_rollingLow = GetExtreme(_recentLows, false);
		_highLowReady = true;
	}
	private void EvaluateEntry(ICandleMessage candle)
	{
		if (_pendingDirection == 0)
			return;

		if (_windowEndTime is DateTimeOffset end && candle.OpenTime > end)
		{
			_pendingDirection = 0;
			return;
		}

		if (_blackoutUntil is not null && candle.OpenTime < _blackoutUntil)
			return;

		if (Position != 0 || _entryPrice.HasValue)
			return;

		if (!_rsiReady || !_highLowReady || !_dailyReady || !_h4Ready)
			return;

		var priceReached = _pendingDirection > 0
			? candle.HighPrice >= _triggerPrice
			: candle.LowPrice <= _triggerPrice;

		if (!priceReached)
			return;

		if (_pendingDirection > 0)
		{
			if (_lastRsi <= RsiUpper || candle.ClosePrice <= _rollingHigh || candle.ClosePrice <= _dailyUpper || candle.ClosePrice <= _h4Upper)
			{
				return;
			}

			var stopLoss = GetStopPrice(false);
			if (stopLoss is null)
				return;

			BuyMarket();

			InitializePositionState(candle.ClosePrice, stopLoss.Value, true);
		}
		else
		{
			if (_lastRsi >= RsiLower || candle.ClosePrice >= _rollingLow || candle.ClosePrice >= _dailyLower || candle.ClosePrice >= _h4Lower)
			{
				return;
			}

			var stopLoss = GetStopPrice(true);
			if (stopLoss is null)
				return;

			SellMarket();

			InitializePositionState(candle.ClosePrice, stopLoss.Value, false);
		}

		_blackoutUntil = candle.OpenTime + TimeSpan.FromHours(BlackoutHours);
		_pendingDirection = 0;
	}

	private decimal? GetStopPrice(bool isShort)
	{
		if (_stopHistoryCount <= StopLossLookback)
			return null;

		var index = StopLossLookback;
		return isShort ? _highStopHistory[index] : _lowStopHistory[index];
	}

	private void InitializePositionState(decimal entryPrice, decimal stopPrice, bool isLong)
	{
		_entryPrice = entryPrice;
		_stopLoss = stopPrice;
		_tp1Hit = false;
		_trailingStop = null;

		if (TakeProfit1Pips > 0)
		{
			_tp1 = isLong ? entryPrice + TakeProfit1Pips * _pipSize : entryPrice - TakeProfit1Pips * _pipSize;
		}
		else
		{
			_tp1 = null;
		}

		if (TakeProfit2Pips > 0)
		{
			_tp2 = isLong ? entryPrice + TakeProfit2Pips * _pipSize : entryPrice - TakeProfit2Pips * _pipSize;
		}
		else
		{
			_tp2 = null;
		}
	}
	private void ManagePosition(ICandleMessage candle)
	{
		if (_entryPrice is null)
			return;

		if (Position > 0)
		{
			UpdateTrailingStop(candle.ClosePrice, true);

			if (_stopLoss is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_trailingStop is decimal trail && candle.LowPrice <= trail)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (!_tp1Hit && _tp1 is decimal tp1 && candle.HighPrice >= tp1)
			{
				SellMarket(Position / 2m);
				_tp1Hit = true;
			}

			if (_tp2 is decimal tp2 && candle.HighPrice >= tp2)
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			UpdateTrailingStop(candle.ClosePrice, false);

			if (_stopLoss is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_trailingStop is decimal trail && candle.HighPrice >= trail)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (!_tp1Hit && _tp1 is decimal tp1 && candle.LowPrice <= tp1)
			{
				BuyMarket(Math.Abs(Position) / 2m);
				_tp1Hit = true;
			}

			if (_tp2 is decimal tp2 && candle.LowPrice <= tp2)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
		else
		{
			ResetPositionState();
		}
	}

	private void UpdateTrailingStop(decimal closePrice, bool isLong)
	{
		if (TrailingStopPips <= 0)
			return;

		var candidate = isLong
			? closePrice - TrailingStopPips * _pipSize
			: closePrice + TrailingStopPips * _pipSize;

		if (_trailingStop is null)
		{
			_trailingStop = candidate;
		}
		else if (isLong && candidate > _trailingStop)
		{
			_trailingStop = candidate;
		}
		else if (!isLong && candidate < _trailingStop)
		{
			_trailingStop = candidate;
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopLoss = null;
		_tp1 = null;
		_tp2 = null;
		_trailingStop = null;
		_tp1Hit = false;
	}

	private static void ShiftHistory<T>(T[] array, T value)
	{
		for (var i = array.Length - 1; i > 0; i--)
		{
			array[i] = array[i - 1];
		}

		array[0] = value;
	}

	private static void TrimQueue(Queue<decimal> queue, int maxLength)
	{
		while (queue.Count > maxLength)
		{
			queue.Dequeue();
		}
	}

	private static decimal GetExtreme(IEnumerable<decimal> values, bool isMax)
	{
		var extreme = isMax ? decimal.MinValue : decimal.MaxValue;

		foreach (var value in values)
		{
			extreme = isMax
				? (value > extreme ? value : extreme)
				: (value < extreme ? value : extreme);
		}

		return extreme;
	}
}
