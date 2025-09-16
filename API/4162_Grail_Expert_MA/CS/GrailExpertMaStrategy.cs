using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout pullback strategy converted from the MetaTrader 4 expert advisor "GrailExpertMAV1.0".
/// Combines a typical price exponential moving average slope filter with high/low channel retests before entering trades.
/// </summary>
public class GrailExpertMaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<int> _highLowPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _maSlopePips;
	private readonly StrategyParam<decimal> _targetThresholdPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _recentHighs = new();
	private readonly Queue<decimal> _recentLows = new();

	private ExponentialMovingAverage _ema = null!;

	private decimal? _previousEma;
	private decimal? _previousPreviousEma;
	private int _maDirection;
	private decimal _rangeHigh;
	private decimal _rangeLow;
	private bool _hasRange;

	private decimal? _pendingLongEntry;
	private decimal? _pendingShortEntry;

	private decimal _pipSize;
	private DateTimeOffset? _currentCandleOpenTime;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Initializes a new instance of the <see cref="GrailExpertMaStrategy"/> class.
	/// </summary>
	public GrailExpertMaStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Trade volume expressed in lots or contracts.", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Distance to the profit target in pips.", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Distance to the protective stop in pips.", "Risk");

		_highLowPeriod = Param(nameof(HighLowPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Range Period", "Number of previous bars used for breakout levels.", "Filters");

		_maPeriod = Param(nameof(MaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of the typical price EMA filter.", "Filters");

		_maSlopePips = Param(nameof(MaSlopePips), 2m)
			.SetGreaterOrEqualZero()
			.SetDisplay("EMA Slope (pips)", "Minimum EMA advance in pips to confirm the trend.", "Filters");

		_targetThresholdPips = Param(nameof(TargetThresholdPips), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Breakout Buffer (pips)", "Extra distance beyond the extreme before arming entries.", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signal generation.", "General");
	}

	/// <summary>
	/// Order volume used for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
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
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Number of historical bars considered when measuring breakouts.
	/// </summary>
	public int HighLowPeriod
	{
		get => _highLowPeriod.Value;
		set => _highLowPeriod.Value = value;
	}

	/// <summary>
	/// Length of the EMA applied to the typical price.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Minimum EMA slope expressed in pips.
	/// </summary>
	public decimal MaSlopePips
	{
		get => _maSlopePips.Value;
		set => _maSlopePips.Value = value;
	}

	/// <summary>
	/// Entry buffer beyond the breakout extreme in pips.
	/// </summary>
	public decimal TargetThresholdPips
	{
		get => _targetThresholdPips.Value;
		set => _targetThresholdPips.Value = value;
	}

	/// <summary>
	/// Candle type requested for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_recentHighs.Clear();
		_recentLows.Clear();

		_previousEma = null;
		_previousPreviousEma = null;
		_maDirection = 0;
		_rangeHigh = 0m;
		_rangeLow = 0m;
		_hasRange = false;

		_pendingLongEntry = null;
		_pendingShortEntry = null;

		_pipSize = 0m;
		_currentCandleOpenTime = null;

		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = MaPeriod };

		_recentHighs.Clear();
		_recentLows.Clear();

		_previousEma = null;
		_previousPreviousEma = null;
		_maDirection = 0;
		_hasRange = false;

		_pendingLongEntry = null;
		_pendingShortEntry = null;

		_pipSize = CalculatePipSize();
		_currentCandleOpenTime = null;

		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_longExitRequested = false;
		_shortExitRequested = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Detect the beginning of a new candle to mirror the MQL4 timing logic.
		if (_currentCandleOpenTime != candle.OpenTime)
			BeginNewCandle(candle);

		// Update intrabar entry levels and manage open positions on every tick.
		UpdateIntrabar(candle);

		// Finalize calculations only once the candle closes.
		if (candle.State == CandleStates.Finished)
			CompleteCandle(candle);
	}

	private void BeginNewCandle(ICandleMessage candle)
	{
		// Store the open time of the active candle so intrabar logic runs with the same reference.
		_currentCandleOpenTime = candle.OpenTime;
	}

	private void UpdateIntrabar(ICandleMessage candle)
	{
		UpdatePendingEntries(candle);
		ManageOpenPositions(candle);

		if (Position != 0)
		{
			// The original expert blocks new entries while an order is active.
			_pendingLongEntry = null;
			_pendingShortEntry = null;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		TryEnterPositions(candle);
	}

	private void UpdatePendingEntries(ICandleMessage candle)
	{
		if (!_hasRange)
			return;

		var threshold = TargetThresholdPips * _pipSize;
		var takeProfitDistance = TakeProfitPips * _pipSize;

		// When a new low pierces the stored range we measure the bounce entry for shorts.
		if (candle.LowPrice < _rangeLow)
		{
			var entryPrice = candle.LowPrice + threshold + takeProfitDistance;
			var spread = _rangeHigh - candle.LowPrice;

			if (spread >= (2m * threshold) + takeProfitDistance)
				_pendingShortEntry = entryPrice;
			else
				_pendingShortEntry = null;
		}

		// When a new high extends the channel we prepare the pullback entry for longs.
		if (candle.HighPrice > _rangeHigh)
		{
			var entryPrice = candle.HighPrice - threshold - takeProfitDistance;
			var spread = candle.HighPrice - _rangeLow;

			if (spread >= (2m * threshold) + takeProfitDistance)
				_pendingLongEntry = entryPrice;
			else
				_pendingLongEntry = null;
		}
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longTakePrice is decimal take && candle.HighPrice >= take)
			{
				TryCloseLong();
				return;
			}

			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
				TryCloseLong();
		}
		else if (Position < 0)
		{
			if (_shortTakePrice is decimal take && candle.LowPrice <= take)
			{
				TryCloseShort();
				return;
			}

			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
				TryCloseShort();
		}
	}

	private void TryEnterPositions(ICandleMessage candle)
	{
		if (_maDirection == 1 && _pendingLongEntry is decimal longEntry && candle.LowPrice <= longEntry)
			EnterLong();

		if (_maDirection == -1 && _pendingShortEntry is decimal shortEntry && candle.HighPrice >= shortEntry)
			EnterShort();
	}

	private void EnterLong()
	{
		if (OrderVolume <= 0m)
			return;

		_pendingLongEntry = null;
		_pendingShortEntry = null;

		BuyMarket(volume: OrderVolume);
	}

	private void EnterShort()
	{
		if (OrderVolume <= 0m)
			return;

		_pendingLongEntry = null;
		_pendingShortEntry = null;

		SellMarket(volume: OrderVolume);
	}

	private void TryCloseLong()
	{
		if (_longExitRequested)
			return;

		_longExitRequested = true;
		SellMarket(volume: Position);
	}

	private void TryCloseShort()
	{
		if (_shortExitRequested)
			return;

		_shortExitRequested = true;
		BuyMarket(volume: Math.Abs(Position));
	}

	private void CompleteCandle(ICandleMessage candle)
	{
		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var emaValue = _ema.Process(typicalPrice, candle.OpenTime, true);

		if (emaValue.IsFinal)
		{
			var currentEma = emaValue.ToDecimal();
			_previousPreviousEma = _previousEma;
			_previousEma = currentEma;
			UpdateTrendDirection();
		}

		UpdateRange(candle);
		ValidatePendingEntries();
	}

	private void UpdateTrendDirection()
	{
		if (_previousEma is not decimal last || _previousPreviousEma is not decimal prior)
		{
			_maDirection = 0;
			return;
		}

		var slopeThreshold = MaSlopePips * _pipSize;
		var difference = last - prior;

		if (difference > slopeThreshold)
		{
			_maDirection = 1;
		}
		else if (difference < -slopeThreshold)
		{
			_maDirection = -1;
		}
		else
		{
			_maDirection = 0;
		}
	}

	private void UpdateRange(ICandleMessage candle)
	{
		EnqueueWithLimit(_recentHighs, candle.HighPrice, HighLowPeriod);
		EnqueueWithLimit(_recentLows, candle.LowPrice, HighLowPeriod);

		if (_recentHighs.Count == 0 || _recentLows.Count == 0)
		{
			_hasRange = false;
			return;
		}

		_rangeHigh = GetMax(_recentHighs);
		_rangeLow = GetMin(_recentLows);
		_hasRange = true;
	}

	private void ValidatePendingEntries()
	{
		if (!_hasRange)
			return;

		var threshold = TargetThresholdPips * _pipSize;
		var takeProfitDistance = TakeProfitPips * _pipSize;

		if (_pendingLongEntry is decimal longEntry)
		{
			var sourceHigh = longEntry + threshold + takeProfitDistance;
			if (sourceHigh > _rangeHigh)
				_pendingLongEntry = null;
		}

		if (_pendingShortEntry is decimal shortEntry)
		{
			var sourceLow = shortEntry - threshold - takeProfitDistance;
			if (sourceLow < _rangeLow)
				_pendingShortEntry = null;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_longStopPrice = null;
			_longTakePrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
			_longExitRequested = false;
			_shortExitRequested = false;
			_pendingLongEntry = null;
			_pendingShortEntry = null;
			return;
		}

		var entryPrice = PositionPrice;
		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		if (Position > 0 && delta > 0)
		{
			_longStopPrice = StopLossPips > 0 ? entryPrice - stopDistance : (decimal?)null;
			_longTakePrice = TakeProfitPips > 0 ? entryPrice + takeDistance : (decimal?)null;
			_longExitRequested = false;

			_shortStopPrice = null;
			_shortTakePrice = null;
			_shortExitRequested = false;
		}
		else if (Position < 0 && delta < 0)
		{
			_shortStopPrice = StopLossPips > 0 ? entryPrice + stopDistance : (decimal?)null;
			_shortTakePrice = TakeProfitPips > 0 ? entryPrice - takeDistance : (decimal?)null;
			_shortExitRequested = false;

			_longStopPrice = null;
			_longTakePrice = null;
			_longExitRequested = false;
		}
	}

	private static void EnqueueWithLimit(Queue<decimal> queue, decimal value, int maxCount)
	{
		queue.Enqueue(value);

		while (queue.Count > Math.Max(1, maxCount))
			queue.Dequeue();
	}

	private static decimal GetMax(Queue<decimal> values)
	{
		var enumerator = values.GetEnumerator();
		if (!enumerator.MoveNext())
			return 0m;

		var max = enumerator.Current;
		while (enumerator.MoveNext())
		{
			if (enumerator.Current > max)
				max = enumerator.Current;
		}

		return max;
	}

	private static decimal GetMin(Queue<decimal> values)
	{
		var enumerator = values.GetEnumerator();
		if (!enumerator.MoveNext())
			return 0m;

		var min = enumerator.Current;
		while (enumerator.MoveNext())
		{
			if (enumerator.Current < min)
				min = enumerator.Current;
		}

		return min;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}
}
