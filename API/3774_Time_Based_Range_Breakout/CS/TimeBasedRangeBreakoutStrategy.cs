using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that prepares daily buy/sell levels at a specified time.
/// The offset and profit targets are derived from the average range of previous days.
/// </summary>
public class TimeBasedRangeBreakoutStrategy : Strategy
{

	private readonly StrategyParam<int> _checkHour;
	private readonly StrategyParam<int> _checkMinute;
	private readonly StrategyParam<int> _daysToCheck;
	private readonly StrategyParam<int> _checkMode;
	private readonly StrategyParam<decimal> _profitFactor;
	private readonly StrategyParam<decimal> _lossFactor;
	private readonly StrategyParam<decimal> _offsetFactor;
	private readonly StrategyParam<int> _closeMode;
	private readonly StrategyParam<int> _tradesPerDay;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lastOpenHour;

	private readonly Queue<decimal> _rangeHistory = new();
	private readonly Queue<decimal> _closeDiffHistory = new();

	private DateTime? _currentDay;
	private DateTime? _levelsDay;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _buyBreakout;
	private decimal _sellBreakout;
	private decimal _profitDistance;
	private decimal _lossDistance;
	private decimal? _previousCheckClose;
	private decimal? _currentCheckClose;
	private int _tradesOpenedToday;
	private bool _levelsReady;

	/// <summary>
	/// Hour of the day when the reference range is calculated.
	/// </summary>
	public int CheckHour
	{
		get => _checkHour.Value;
		set => _checkHour.Value = value;
	}

	/// <summary>
	/// Minute of the hour when the reference range is calculated.
	/// </summary>
	public int CheckMinute
	{
		get => _checkMinute.Value;
		set => _checkMinute.Value = value;
	}

	/// <summary>
	/// Number of previous days used for averaging.
	/// </summary>
	public int DaysToCheck
	{
		get => _daysToCheck.Value;
		set => _daysToCheck.Value = value;
	}

	/// <summary>
	/// Mode of averaging: 1 - daily range, 2 - absolute close-to-close difference.
	/// </summary>
	public int CheckMode
	{
		get => _checkMode.Value;
		set => _checkMode.Value = value;
	}

	/// <summary>
	/// Divisor applied to convert the average range into a take-profit distance.
	/// </summary>
	public decimal ProfitFactor
	{
		get => _profitFactor.Value;
		set => _profitFactor.Value = value;
	}

	/// <summary>
	/// Divisor applied to convert the average range into a stop-loss distance.
	/// </summary>
	public decimal LossFactor
	{
		get => _lossFactor.Value;
		set => _lossFactor.Value = value;
	}

	/// <summary>
	/// Divisor applied to convert the average range into the breakout offset.
	/// </summary>
	public decimal OffsetFactor
	{
		get => _offsetFactor.Value;
		set => _offsetFactor.Value = value;
	}

	/// <summary>
	/// Defines whether to flatten at the daily boundary (2 = close on new day).
	/// </summary>
	public int CloseMode
	{
		get => _closeMode.Value;
		set => _closeMode.Value = value;
	}

	/// <summary>
	/// Maximum number of trades allowed per day.
	/// </summary>
	public int TradesPerDay
	{
		get => _tradesPerDay.Value;
		set => _tradesPerDay.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	/// <summary>
	/// Last hour of the day when breakout orders are allowed to remain open.
	/// </summary>
	public int LastOpenHour
	{
		get => _lastOpenHour.Value;
		set => _lastOpenHour.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TimeBasedRangeBreakoutStrategy()
	{
		_checkHour = Param(nameof(CheckHour), 8)
		.SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
		.SetRange(0, 23);

		_checkMinute = Param(nameof(CheckMinute), 0)
		.SetDisplay("Check Minute", "Minute of the hour used for daily calculations", "Schedule")
		.SetRange(0, 59);

		_daysToCheck = Param(nameof(DaysToCheck), 7)
		.SetGreaterThanZero()
		.SetDisplay("Days To Check", "Number of previous days used in averaging", "Averaging")
		.SetCanOptimize(true)
		.SetOptimize(3, 15, 1);

		_checkMode = Param(nameof(CheckMode), 1)
		.SetDisplay("Check Mode", "1 - use daily range, 2 - use absolute close difference", "Averaging")
		.SetValues(1, 2);

		_profitFactor = Param(nameof(ProfitFactor), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Profit Factor", "Divisor applied to average range for take-profit", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 4m, 0.5m);

		_lossFactor = Param(nameof(LossFactor), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Loss Factor", "Divisor applied to average range for stop-loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 4m, 0.5m);

		_offsetFactor = Param(nameof(OffsetFactor), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Offset Factor", "Divisor applied to average range for breakout levels", "Entries")
		.SetCanOptimize(true)
		.SetOptimize(1m, 4m, 0.5m);

		_closeMode = Param(nameof(CloseMode), 1)
		.SetDisplay("Close Mode", "1 - keep positions overnight, 2 - close on new day", "Risk")
		.SetValues(1, 2);

		_tradesPerDay = Param(nameof(TradesPerDay), 1)
		.SetGreaterThanZero()
		.SetDisplay("Trades Per Day", "Maximum entries allowed within one day", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1, 3, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series used by the strategy", "Data");
		_lastOpenHour = Param(nameof(LastOpenHour), 23)
			.SetDisplay("Last Open Hour", "Hour after which new trades are not opened", "Schedule")
			.SetRange(0, 23);
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

		_rangeHistory.Clear();
		_closeDiffHistory.Clear();
		_currentDay = null;
		_levelsDay = null;
		_dayHigh = 0m;
		_dayLow = 0m;
		_buyBreakout = 0m;
		_sellBreakout = 0m;
		_profitDistance = 0m;
		_lossDistance = 0m;
		_previousCheckClose = null;
		_currentCheckClose = null;
		_tradesOpenedToday = 0;
		_levelsReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

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

		UpdateDailyState(candle);
		TryCalculateLevels(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		ManageOpenPosition(candle);
		TryEnterPosition(candle);
	}

	private void UpdateDailyState(ICandleMessage candle)
	{
		var candleDate = candle.OpenTime.Date;

		if (_currentDay is null || candleDate != _currentDay.Value)
		{
			if (_currentDay is not null)
			FinalizePreviousDay();

			if (CloseMode == 2 && Position != 0m)
			ClosePosition();

			_currentDay = candleDate;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_levelsReady = false;
			_levelsDay = null;
			_currentCheckClose = null;
			_tradesOpenedToday = 0;
		}
		else
		{
			if (candle.HighPrice > _dayHigh)
			_dayHigh = candle.HighPrice;

			if (candle.LowPrice < _dayLow)
			_dayLow = candle.LowPrice;
		}
	}

	private void FinalizePreviousDay()
	{
		var dayRange = _dayHigh - _dayLow;
		if (dayRange > 0m)
		EnqueueWithLimit(_rangeHistory, dayRange, DaysToCheck);

		if (_currentCheckClose is decimal checkClose)
		{
			if (_previousCheckClose is decimal previousClose)
			{
				var difference = Math.Abs(checkClose - previousClose);
				if (difference > 0m)
				EnqueueWithLimit(_closeDiffHistory, difference, DaysToCheck);
			}

			_previousCheckClose = checkClose;
		}

		_currentCheckClose = null;
	}

	private void TryCalculateLevels(ICandleMessage candle)
	{
		if (candle.OpenTime.Hour != CheckHour || candle.OpenTime.Minute != CheckMinute)
		return;

		_currentCheckClose = candle.ClosePrice;

		if (Position != 0m)
		ClosePosition();

		if (!TryGetAverage(out var average))
		{
			_levelsReady = false;
			_levelsDay = null;
			return;
		}

		var offset = OffsetFactor > 0m ? average / OffsetFactor : 0m;
		_profitDistance = ProfitFactor > 0m ? average / ProfitFactor : 0m;
		_lossDistance = LossFactor > 0m ? average / LossFactor : 0m;

		_buyBreakout = _dayHigh + offset;
		_sellBreakout = _dayLow - offset;
		_levelsReady = true;
		_levelsDay = _currentDay;

		LogInfo($"Levels prepared for {candle.OpenTime:yyyy-MM-dd}. High={_dayHigh}, Low={_dayLow}, Avg={average}, BuyLevel={_buyBreakout}, SellLevel={_sellBreakout}.");
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0m)
		return;

		if (PositionPrice is not decimal entryPrice)
		return;

		if (Position > 0m)
		{
			var reachedProfit = _profitDistance > 0m && candle.ClosePrice - entryPrice >= _profitDistance;
			var reachedLoss = _lossDistance > 0m && entryPrice - candle.ClosePrice >= _lossDistance;

			if (reachedProfit || reachedLoss)
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0m)
		{
			var reachedProfit = _profitDistance > 0m && entryPrice - candle.ClosePrice >= _profitDistance;
			var reachedLoss = _lossDistance > 0m && candle.ClosePrice - entryPrice >= _lossDistance;

			if (reachedProfit || reachedLoss)
			BuyMarket(Math.Abs(Position));
		}
	}

	private void TryEnterPosition(ICandleMessage candle)
	{
		if (!_levelsReady || _levelsDay is null || _currentDay is null)
		return;

		if (_levelsDay != _currentDay)
		return;

		if (_tradesOpenedToday >= TradesPerDay)
		return;

		if (candle.OpenTime.Hour > LastOpenHour)
		return;

		if (Position != 0m)
		return;

		if (candle.ClosePrice >= _buyBreakout)
		{
			BuyMarket(Volume);
			_tradesOpenedToday++;
		}
		else if (candle.ClosePrice <= _sellBreakout)
		{
			SellMarket(Volume);
			_tradesOpenedToday++;
		}
	}

	private bool TryGetAverage(out decimal average)
	{
		average = 0m;
		var source = CheckMode == 2 ? _closeDiffHistory : _rangeHistory;

		var sum = 0m;
		var count = 0;

		foreach (var value in source)
		{
			sum += value;
			count++;
		}

		if (count == 0)
		return false;

		average = sum / count;
		return true;
	}

	private static void EnqueueWithLimit(Queue<decimal> queue, decimal value, int limit)
	{
		queue.Enqueue(value);

		while (queue.Count > limit)
		queue.Dequeue();
	}

	private void ClosePosition()
	{
		if (Position > 0m)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}

