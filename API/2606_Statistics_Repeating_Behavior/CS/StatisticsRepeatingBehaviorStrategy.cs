using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that analyzes historical candle bodies for the same time of day.
/// Opens a position when bullish or bearish pressure dominates over recent days.
/// Implements simple martingale sizing after losing trades.
/// </summary>
public class StatisticsRepeatingBehaviorStrategy : Strategy
{
	private readonly StrategyParam<int> _historyDays;
	private readonly StrategyParam<int> _minimumBodyPoints;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _martingaleFactor;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<int, BodyStatistics> _bodyStatistics = new();

	private decimal _currentVolume;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private int _positionDirection;
	private decimal _priceStep;
	private TimeSpan _timeFrame;

	/// <summary>
	/// Number of historical days to aggregate for statistics.
	/// </summary>
	public int HistoryDays
	{
		get => _historyDays.Value;
		set => _historyDays.Value = value;
	}

	/// <summary>
	/// Minimum body size in points for a candle to contribute into the statistics.
	/// </summary>
	public int MinimumBodyPoints
	{
		get => _minimumBodyPoints.Value;
		set => _minimumBodyPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Initial order size used before applying martingale adjustments.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the order size after a losing trade.
	/// </summary>
	public decimal MartingaleFactor
	{
		get => _martingaleFactor.Value;
		set => _martingaleFactor.Value = value;
	}

	/// <summary>
	/// Candle type to analyze.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="StatisticsRepeatingBehaviorStrategy"/>.
	/// </summary>
	public StatisticsRepeatingBehaviorStrategy()
	{
		_historyDays = Param(nameof(HistoryDays), 10)
			.SetGreaterThanZero()
			.SetDisplay("History Days", "Number of days to collect statistics", "Parameters")
			.SetCanOptimize(true);

		_minimumBodyPoints = Param(nameof(MinimumBodyPoints), 10)
			.SetDisplay("Minimum Body (points)", "Ignore candles with smaller body", "Parameters")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 15)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Starting order size", "Trading")
			.SetCanOptimize(true);

		_martingaleFactor = Param(nameof(MartingaleFactor), 1.618m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Factor", "Multiplier after losing trade", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for analysis", "General");
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

		_bodyStatistics.Clear();
		_currentVolume = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_positionDirection = 0;
		_priceStep = 0m;
		_timeFrame = TimeSpan.Zero;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security.MinPriceStep ?? Security.PriceStep ?? 1m;

		_timeFrame = CandleType.Arg is TimeSpan span ? span : TimeSpan.Zero;
		if (_timeFrame <= TimeSpan.Zero)
			_timeFrame = TimeSpan.FromMinutes(1);

		_currentVolume = AdjustVolume(InitialVolume);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var nextOpen = candle.OpenTime + _timeFrame;
		var nextKey = GetMinuteKey(nextOpen);

		// Close existing position at the beginning of the new bar.
		if (_positionDirection != 0)
		{
			var exitPrice = candle.ClosePrice;
			var stopHit = false;

			if (_positionDirection > 0)
			{
				if (candle.LowPrice <= _stopPrice)
				{
					exitPrice = _stopPrice;
					stopHit = true;
				}
			}
			else
			{
				if (candle.HighPrice >= _stopPrice)
				{
					exitPrice = _stopPrice;
					stopHit = true;
				}
			}

			if (Position != 0)
				ClosePosition();

			UpdateVolumeAfterTrade(exitPrice, stopHit);
		}

		if (_positionDirection == 0 && _bodyStatistics.TryGetValue(nextKey, out var stats) && stats.Count > 0)
		{
			var bullSum = stats.BullSum;
			var bearSum = stats.BearSum;

			if (bullSum > bearSum && Position <= 0)
			{
				EnterPosition(candle, true);
			}
			else if (bearSum > bullSum && Position >= 0)
			{
				EnterPosition(candle, false);
			}
		}

		UpdateStatistics(candle);
	}

	private void EnterPosition(ICandleMessage candle, bool isLong)
	{
		var volume = _currentVolume;
		if (volume <= 0m)
			return;

		var stopDistance = StopLossPips * _priceStep;
		if (stopDistance <= 0m)
			stopDistance = _priceStep;

		if (isLong)
		{
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - stopDistance;
			_positionDirection = 1;
		}
		else
		{
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + stopDistance;
			_positionDirection = -1;
		}
	}

	private void UpdateVolumeAfterTrade(decimal exitPrice, bool stopHit)
	{
		if (_positionDirection == 0)
			return;

		var profit = (_positionDirection > 0 ? exitPrice - _entryPrice : _entryPrice - exitPrice);

		if (profit > 0m && !stopHit)
		{
			_currentVolume = AdjustVolume(InitialVolume);
		}
		else
		{
			var increased = AdjustVolume(InitialVolume * MartingaleFactor);
			_currentVolume = increased;
		}

		_entryPrice = 0m;
		_stopPrice = 0m;
		_positionDirection = 0;
	}

	private void UpdateStatistics(ICandleMessage candle)
	{
		var currentKey = GetMinuteKey(candle.OpenTime);
		if (!_bodyStatistics.TryGetValue(currentKey, out var stats))
		{
			stats = new BodyStatistics();
			_bodyStatistics.Add(currentKey, stats);
		}

		var body = candle.ClosePrice - candle.OpenPrice;
		var bodyPoints = body / _priceStep;
		var absBody = Math.Abs(bodyPoints);

		if (MinimumBodyPoints > 0 && absBody < MinimumBodyPoints)
			return;

		stats.Enqueue(bodyPoints);

		while (stats.Count > HistoryDays)
		{
			var removed = stats.Dequeue();
			if (removed > 0m)
				stats.BullSum -= removed;
			else if (removed < 0m)
				stats.BearSum -= Math.Abs(removed);
		}
	}

	private decimal AdjustVolume(decimal volume)
	{
		var step = Security.VolumeStep ?? 1m;

		if (volume <= 0m)
			return 0m;

		volume = Math.Floor(volume / step) * step;

		var minVolume = Security.MinVolume ?? step;
		if (volume < minVolume)
			return 0m;

		var maxVolume = Security.MaxVolume;
		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private static int GetMinuteKey(DateTimeOffset time)
	{
		return time.Hour * 60 + time.Minute;
	}

	private sealed class BodyStatistics
	{
		private readonly Queue<decimal> _values = new();

		public decimal BullSum { get; set; }
		public decimal BearSum { get; set; }

		public int Count => _values.Count;

		public void Enqueue(decimal value)
		{
			_values.Enqueue(value);
			if (value > 0m)
				BullSum += value;
			else if (value < 0m)
				BearSum += Math.Abs(value);
		}

		public decimal Dequeue()
		{
			return _values.Dequeue();
		}
	}
}
