using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range breakout follower adapted from the original MetaTrader 5 expert advisor.
/// Enters once price moves far enough away from the daily extremes and manages intraday stop and target levels.
/// </summary>
public class RangeFollowerStrategy : Strategy
{
	private const int DefaultAtrPeriod = 20;

	private readonly StrategyParam<int> _triggerPercent;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private Subscription? _dailySubscription;
	private Subscription? _intradaySubscription;

	private AverageTrueRange _dailyAtrIndicator = null!;

	private DateTime? _currentSessionDate;
	private decimal? _dailyHigh;
	private decimal? _dailyLow;
	private decimal? _dailyRange;
	private decimal? _triggerDistance;
	private decimal? _restDistance;
	private decimal? _atrValue;

	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;

	private bool _tradeOpened;
	private bool _dayInitialized;
	private bool _abortTradingForDay;
	private bool _orderPending;

	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal? _entryPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="RangeFollowerStrategy"/> class.
	/// </summary>
	public RangeFollowerStrategy()
	{
		_triggerPercent = Param(nameof(TriggerPercent), 60)
			.SetRange(10, 90)
			.SetDisplay("Trigger Percent", "Percentage of ATR used as breakout trigger", "Parameters");

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume for market entries", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for monitoring daily resets", "General");
	}

	/// <summary>
	/// Trigger percentage that splits the ATR into trigger and residual segments.
	/// </summary>
	public int TriggerPercent
	{
		get => _triggerPercent.Value;
		set => _triggerPercent.Value = value;
	}

	/// <summary>
	/// Order volume used for new market positions.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Intraday candle type that defines the trading session boundaries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(Security, TimeSpan.FromDays(1).TimeFrame())
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentSessionDate = null;
		_dailyHigh = null;
		_dailyLow = null;
		_dailyRange = null;
		_triggerDistance = null;
		_restDistance = null;
		_atrValue = null;
		_bestBidPrice = null;
		_bestAskPrice = null;
		_tradeOpened = false;
		_dayInitialized = false;
		_abortTradingForDay = false;
		_orderPending = false;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_dailyAtrIndicator = new AverageTrueRange
		{
			Length = DefaultAtrPeriod
		};

		_dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		_dailySubscription
			.Bind(_dailyAtrIndicator, ProcessDailyCandle)
			.Start();

		_intradaySubscription = SubscribeCandles(CandleType);
		_intradaySubscription
			.Bind(ProcessIntradayCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		StartProtection();
	}

	private void ProcessDailyCandle(ICandleMessage candle, decimal atrValue)
	{
		var candleDate = candle.OpenTime.Date;

		if (_currentSessionDate == null || candleDate > _currentSessionDate)
		{
			ResetDailyState(candleDate);
		}

		if (candleDate != _currentSessionDate)
		{
			return;
		}

		_dailyHigh = candle.HighPrice;
		_dailyLow = candle.LowPrice;
		_atrValue = _dailyAtrIndicator.IsFormed ? atrValue : null;
	}

	private void ProcessIntradayCandle(ICandleMessage candle)
	{
		var candleDate = candle.OpenTime.Date;

		if (_currentSessionDate == null || candleDate > _currentSessionDate)
		{
			ResetDailyState(candleDate);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
		{
			_bestBidPrice = bid;
		}

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
		{
			_bestAskPrice = ask;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (!UpdateTradingLevels())
		{
			return;
		}

		if (!_dayInitialized)
		{
			_dayInitialized = true;

			if (_dailyRange is decimal range && _triggerDistance is decimal trigger && range > trigger)
			{
				_abortTradingForDay = true;
			}
		}

		if (_abortTradingForDay)
		{
			return;
		}

		if (_bestBidPrice is not decimal currentBid || _bestAskPrice is not decimal currentAsk)
		{
			return;
		}

		CheckIntradayTargets(currentBid, currentAsk);

		if (_tradeOpened || _orderPending || Position != 0)
		{
			return;
		}

		if (_dailyLow is decimal dayLow && _triggerDistance is decimal triggerDistance)
		{
			var distanceToLow = currentBid - dayLow;

			if (distanceToLow > triggerDistance)
			{
				EnterLong(currentAsk);
				return;
			}
		}

		if (_dailyHigh is decimal dayHigh && _triggerDistance is decimal triggerDistanceShort)
		{
			var distanceToHigh = dayHigh - currentAsk;

			if (distanceToHigh > triggerDistanceShort)
			{
				EnterShort(currentBid);
			}
		}
	}

	private void CheckIntradayTargets(decimal currentBid, decimal currentAsk)
	{
		if (Position > 0)
		{
			if (_stopLossPrice is decimal longStop && currentBid <= longStop)
			{
				SellMarket(Position);
				_orderPending = true;
				_stopLossPrice = null;
				_takeProfitPrice = null;
				_entryPrice = null;
				return;
			}

			if (_takeProfitPrice is decimal longTarget && currentBid >= longTarget)
			{
				SellMarket(Position);
				_orderPending = true;
				_stopLossPrice = null;
				_takeProfitPrice = null;
				_entryPrice = null;
			}
		}
		else if (Position < 0)
		{
			if (_stopLossPrice is decimal shortStop && currentAsk >= shortStop)
			{
				BuyMarket(-Position);
				_orderPending = true;
				_stopLossPrice = null;
				_takeProfitPrice = null;
				_entryPrice = null;
				return;
			}

			if (_takeProfitPrice is decimal shortTarget && currentAsk <= shortTarget)
			{
				BuyMarket(-Position);
				_orderPending = true;
				_stopLossPrice = null;
				_takeProfitPrice = null;
				_entryPrice = null;
			}
		}
	}

	private void EnterLong(decimal askPrice)
	{
		if (_triggerDistance is not decimal trigger || _restDistance is not decimal rest)
		{
			return;
		}

		BuyMarket(Volume);
		_orderPending = true;
		_tradeOpened = true;
		_entryPrice = askPrice;
		_stopLossPrice = askPrice - trigger;
		_takeProfitPrice = askPrice + rest;
	}

	private void EnterShort(decimal bidPrice)
	{
		if (_triggerDistance is not decimal trigger || _restDistance is not decimal rest)
		{
			return;
		}

		SellMarket(Volume);
		_orderPending = true;
		_tradeOpened = true;
		_entryPrice = bidPrice;
		_stopLossPrice = bidPrice + trigger;
		_takeProfitPrice = bidPrice - rest;
	}

	private bool UpdateTradingLevels()
	{
		if (_currentSessionDate == null)
		{
			return false;
		}

		if (_dailyHigh is not decimal high || _dailyLow is not decimal low)
		{
			return false;
		}

		if (_atrValue is not decimal atr)
		{
			return false;
		}

		var trigger = atr * TriggerPercent / 100m;
		if (trigger <= 0m)
		{
			return false;
		}

		var rest = atr - trigger;
		if (rest <= 0m)
		{
			return false;
		}

		_dailyRange = high - low;
		_triggerDistance = trigger;
		_restDistance = rest;
		return true;
	}

	private void ResetDailyState(DateTime newDate)
	{
		if (_currentSessionDate != null && newDate <= _currentSessionDate)
		{
			return;
		}

		if (Position != 0)
		{
			ClosePosition();
			_orderPending = true;
		}

		_currentSessionDate = newDate;
		_dailyHigh = null;
		_dailyLow = null;
		_dailyRange = null;
		_triggerDistance = null;
		_restDistance = null;
		_atrValue = null;
		_dayInitialized = false;
		_abortTradingForDay = false;
		_tradeOpened = false;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		_orderPending = false;

		if (Position == 0)
		{
			_entryPrice = null;
		}
	}
}
