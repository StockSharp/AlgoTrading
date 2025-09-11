using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<TimeSpan> _endTime;

	private decimal _orHigh;
	private decimal _orLow;
	private bool _rangeSet;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private int _tradesToday;
	private DateTime _currentDay;
	private ICandleMessage _prevCandle;

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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Risk to reward ratio", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_maxTrades = Param(nameof(MaxTrades), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum trades per day", "General");

		_endTime = Param(nameof(EndTime), new TimeSpan(15, 0, 0))
			.SetDisplay("End Time", "Daily close time (UTC)", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentDay = time.Date;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
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
		}

		if (!_rangeSet)
		{
			_orHigh = candle.HighPrice;
			_orLow = candle.LowPrice;
			_rangeSet = true;
			_prevCandle = candle;
			return;
		}

		// Close positions at end of day
		if (openTime.TimeOfDay >= EndTime && Position != 0)
		{
			ClosePosition();
		}

		if (Position == 0 && _tradesToday < MaxTrades)
		{
			if (candle.ClosePrice > _orHigh)
			{
				BuyMarket();
				_tradesToday++;
				_stopPrice = _prevCandle.LowPrice;
				_targetPrice = candle.ClosePrice + (candle.ClosePrice - _stopPrice) * RiskReward;
			}
			else if (candle.ClosePrice < _orLow)
			{
				SellMarket();
				_tradesToday++;
				_stopPrice = _prevCandle.HighPrice;
				_targetPrice = candle.ClosePrice - (_stopPrice - candle.ClosePrice) * RiskReward;
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
				BuyMarket(Math.Abs(Position));
		}

		_prevCandle = candle;
	}
}
