using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opening range breakout strategy.
/// Builds the opening range and trades breakouts with risk management.
/// </summary>
public class OpeningRangeBreakout2Strategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _orStart;
	private readonly StrategyParam<TimeSpan> _orEnd;
	private readonly StrategyParam<TimeSpan> _dayEnd;
	private readonly StrategyParam<decimal> _minRangePercent;
	private readonly StrategyParam<decimal> _rewardRisk;
	private readonly StrategyParam<decimal> _retrace;
	private readonly StrategyParam<bool> _oneTradePerDay;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _orHigh;
	private decimal? _orLow;
	private bool _tradeTaken;
	private bool _pendingLong;
	private bool _pendingShort;
	private decimal _rangeRisk;
	private decimal _stopLoss;
	private decimal _takeProfit;
	private bool _wasInOr;
	private DateTime _currentDay;

	/// <summary>
	/// Opening range start time.
	/// </summary>
	public TimeSpan OrStart { get => _orStart.Value; set => _orStart.Value = value; }

	/// <summary>
	/// Opening range end time.
	/// </summary>
	public TimeSpan OrEnd { get => _orEnd.Value; set => _orEnd.Value = value; }

	/// <summary>
	/// End of regular trading day.
	/// </summary>
	public TimeSpan DayEnd { get => _dayEnd.Value; set => _dayEnd.Value = value; }

	/// <summary>
	/// Minimum range width percent.
	/// </summary>
	public decimal MinRangePercent { get => _minRangePercent.Value; set => _minRangePercent.Value = value; }

	/// <summary>
	/// Reward to risk ratio.
	/// </summary>
	public decimal RewardRisk { get => _rewardRisk.Value; set => _rewardRisk.Value = value; }

	/// <summary>
	/// Retracement percent used for stop loss.
	/// </summary>
	public decimal Retrace { get => _retrace.Value; set => _retrace.Value = value; }

	/// <summary>
	/// Allow only one trade per day.
	/// </summary>
	public bool OneTradePerDay { get => _oneTradePerDay.Value; set => _oneTradePerDay.Value = value; }

	/// <summary>
	/// Reverse position on stop loss.
	/// </summary>
	public bool Reverse { get => _reverse.Value; set => _reverse.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public OpeningRangeBreakout2Strategy()
	{
		_orStart = Param(nameof(OrStart), new TimeSpan(9, 30, 0))
		.SetDisplay("OR Start", "Opening range start", "Sessions");

		_orEnd = Param(nameof(OrEnd), new TimeSpan(10, 15, 0))
		.SetDisplay("OR End", "Opening range end", "Sessions");

		_dayEnd = Param(nameof(DayEnd), new TimeSpan(15, 45, 0))
		.SetDisplay("Day End", "Regular trading end", "Sessions");

		_minRangePercent = Param(nameof(MinRangePercent), 0.35m)
		.SetDisplay("Min Range %", "Minimum opening range width percent", "Filters");

		_rewardRisk = Param(nameof(RewardRisk), 1.1m)
		.SetDisplay("Reward/Risk", "Reward to risk ratio", "Risk");

		_retrace = Param(nameof(Retrace), 0.5m)
		.SetDisplay("Retrace %", "Stop as percent of range", "Risk");

		_oneTradePerDay = Param(nameof(OneTradePerDay), false)
		.SetDisplay("One Trade", "Allow only one initial trade per day", "Settings");

		_reverse = Param(nameof(Reverse), true)
		.SetDisplay("Reverse", "Reverse on stop loss", "Settings");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		_orHigh = null;
		_orLow = null;
		_tradeTaken = false;
		_pendingLong = false;
		_pendingShort = false;
		_wasInOr = false;
		_currentDay = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var day = candle.OpenTime.Date;

		if (_currentDay != day)
		{
			_currentDay = day;
			_orHigh = null;
			_orLow = null;
			_tradeTaken = false;
			_pendingLong = false;
			_pendingShort = false;
		}

		var time = candle.OpenTime.TimeOfDay;
		var inOr = time >= OrStart && time < OrEnd;
		var inRtd = time >= OrStart && time < DayEnd;

		if (inOr)
		{
			_orHigh = _orHigh.HasValue ? Math.Max(_orHigh.Value, candle.HighPrice) : candle.HighPrice;
			_orLow = _orLow.HasValue ? Math.Min(_orLow.Value, candle.LowPrice) : candle.LowPrice;
		}

		if (_wasInOr && !inOr && _orHigh.HasValue && _orLow.HasValue)
		{
			var range = _orHigh.Value - _orLow.Value;

			if (range >= candle.ClosePrice * MinRangePercent / 100m && (!OneTradePerDay || !_tradeTaken))
			{
				_rangeRisk = range * Retrace;
				_pendingLong = true;
				_pendingShort = true;
			}
		}

		if (_pendingLong && _orHigh.HasValue && candle.HighPrice >= _orHigh.Value && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopLoss = _orHigh.Value - _rangeRisk;
			_takeProfit = _orHigh.Value + _rangeRisk * RewardRisk;
			_pendingLong = false;
			_pendingShort = false;
			_tradeTaken = true;
		}
		else if (_pendingShort && _orLow.HasValue && candle.LowPrice <= _orLow.Value && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopLoss = _orLow.Value + _rangeRisk;
			_takeProfit = _orLow.Value - _rangeRisk * RewardRisk;
			_pendingShort = false;
			_pendingLong = false;
			_tradeTaken = true;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLoss)
			{
				SellMarket(Position);

				if (Reverse && inRtd)
				{
					SellMarket(Volume);
					_stopLoss = candle.ClosePrice + _rangeRisk;
					_takeProfit = candle.ClosePrice - _rangeRisk * RewardRisk;
				}
			}
			else if (candle.HighPrice >= _takeProfit)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLoss)
			{
				BuyMarket(Math.Abs(Position));

				if (Reverse && inRtd)
				{
					BuyMarket(Volume);
					_stopLoss = candle.ClosePrice - _rangeRisk;
					_takeProfit = candle.ClosePrice + _rangeRisk * RewardRisk;
				}
			}
			else if (candle.LowPrice <= _takeProfit)
			{
				BuyMarket(Math.Abs(Position));
			}
		}

		if (!inRtd && Position != 0)
			ClosePosition();

		_wasInOr = inOr;
	}
}