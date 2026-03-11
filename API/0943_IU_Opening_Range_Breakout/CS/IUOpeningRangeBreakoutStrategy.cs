using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// IU Opening Range Breakout Strategy.
/// Trades breakouts of the first session bar with risk to reward management and daily trade limit.
/// </summary>
public class IUOpeningRangeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _cooldownDays;
	private readonly StrategyParam<TimeSpan> _endTime;

	private decimal _orHigh;
	private decimal _orLow;
	private bool _rangeSet;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private int _tradesToday;
	private DateTime _currentDay;
	private DateTime _nextTradeDate;
	private decimal _prevHigh;
	private decimal _prevLow;
	private int _orBarCount;

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Maximum number of trades per day.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Minimum days between entries.
	/// </summary>
	public int CooldownDays
	{
		get => _cooldownDays.Value;
		set => _cooldownDays.Value = value;
	}

	/// <summary>
	/// Time to close all positions.
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IUOpeningRangeBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Risk to reward ratio", "General")
			
			.SetOptimize(1m, 3m, 0.5m);

		_maxTrades = Param(nameof(MaxTrades), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum trades per day", "General");

		_cooldownDays = Param(nameof(CooldownDays), 3)
			.SetDisplay("Cooldown Days", "Minimum days between entries", "General");

		_endTime = Param(nameof(EndTime), new TimeSpan(15, 0, 0))
			.SetDisplay("End Time", "Daily close time (UTC)", "General");
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
		_orHigh = 0m;
		_orLow = 0m;
		_rangeSet = false;
		_stopPrice = 0m;
		_targetPrice = 0m;
		_tradesToday = 0;
		_currentDay = default;
		_nextTradeDate = DateTime.MinValue;
		_prevHigh = 0m;
		_prevLow = 0m;
		_orBarCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentDay = time.Date;
		_nextTradeDate = DateTime.MinValue;

		var dummyEma1 = new ExponentialMovingAverage { Length = 10 };
		var dummyEma2 = new ExponentialMovingAverage { Length = 20 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(dummyEma1, dummyEma2, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal d1, decimal d2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openTime = candle.OpenTime;

		// Reset for new day
		if (openTime.Date != _currentDay)
		{
			_currentDay = openTime.Date;
			_rangeSet = false;
			_tradesToday = 0;
			_orBarCount = 0;
			_orHigh = 0m;
			_orLow = decimal.MaxValue;
		}

		_orBarCount++;
		if (!_rangeSet)
		{
			_orHigh = Math.Max(_orHigh, candle.HighPrice);
			_orLow = Math.Min(_orLow, candle.LowPrice);
			if (_orBarCount >= 2)
				_rangeSet = true;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}

		// Close positions at end of day
		if (openTime.TimeOfDay >= EndTime && Position != 0)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
		}

		if (Position == 0 && _tradesToday < MaxTrades && openTime.Date >= _nextTradeDate)
		{
			if (candle.HighPrice > _orHigh)
			{
				BuyMarket();
				_tradesToday++;
				_nextTradeDate = openTime.Date.AddDays(CooldownDays);
				_stopPrice = _prevLow;
				_targetPrice = candle.ClosePrice + (candle.ClosePrice - _stopPrice) * RiskReward;
			}
			else if (candle.LowPrice < _orLow)
			{
				SellMarket();
				_tradesToday++;
				_nextTradeDate = openTime.Date.AddDays(CooldownDays);
				_stopPrice = _prevHigh;
				_targetPrice = candle.ClosePrice - (_stopPrice - candle.ClosePrice) * RiskReward;
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
				BuyMarket();
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
