using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range-reversal strategy translated from the Pipso MQL5 expert advisor.
/// The system fades breakouts of the recent high/low range during a configurable trading session.
/// </summary>
public class PipsoStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _stopRangePercent;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private decimal _previousHighest;
	private decimal _previousLowest;
	private bool _isChannelInitialized;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private Sides? _entrySide;

	/// <summary>
	/// Trade volume expressed in lots or contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Number of candles used to compute the high/low channel.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Hour when the strategy is allowed to start trading.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour when trading should stop.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the channel width to compute the stop distance.
	/// </summary>
	public decimal StopRangePercent
	{
		get => _stopRangePercent.Value;
		set => _stopRangePercent.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public PipsoStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume per trade", "General");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 36)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Number of candles used for high/low extremes", "Channel");

		_startHour = Param(nameof(StartHour), 21)
			.SetDisplay("Start Hour", "Session start hour (0-23)", "Session");

		_endHour = Param(nameof(EndHour), 9)
			.SetDisplay("End Hour", "Session end hour (0-23)", "Session");

		_stopRangePercent = Param(nameof(StopRangePercent), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Range %", "Extra percentage of the channel width for stop distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for calculations", "General");
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
		_previousHighest = 0m;
		_previousLowest = 0m;
		_isChannelInitialized = false;
		ResetTradeState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_highest = new Highest { Length = LookbackPeriod };
		_lowest = new Lowest { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
		{
			_previousHighest = highestValue;
			_previousLowest = lowestValue;
			return;
		}

		if (!_isChannelInitialized)
		{
			_previousHighest = highestValue;
			_previousLowest = lowestValue;
			_isChannelInitialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousHighest = highestValue;
			_previousLowest = lowestValue;
			return;
		}

		var channelHigh = _previousHighest;
		var channelLow = _previousLowest;

		ManageStopLoss(candle);

		var channelRange = channelHigh - channelLow;
		var breakoutHigh = candle.HighPrice >= channelHigh && channelRange > 0m;
		var breakoutLow = candle.LowPrice <= channelLow && channelRange > 0m;
		var canTrade = IsWithinTradingWindow(candle.OpenTime);

		if (breakoutHigh && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			ResetTradeState();
		}

		if (breakoutLow && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			ResetTradeState();
		}

		if (channelRange > 0m)
		{
			var stopDistance = channelRange * (1m + StopRangePercent / 100m);

			if (breakoutHigh && Position == 0 && canTrade)
			{
				SellMarket(OrderVolume);
				_entrySide = Sides.Sell;
				_entryPrice = channelHigh;
				_stopPrice = _entryPrice + stopDistance;
			}
			else if (breakoutLow && Position == 0 && canTrade)
			{
				BuyMarket(OrderVolume);
				_entrySide = Sides.Buy;
				_entryPrice = channelLow;
				_stopPrice = _entryPrice - stopDistance;
			}
		}

		if (Position == 0)
			ResetTradeState();

		_previousHighest = highestValue;
		_previousLowest = lowestValue;
	}

	private void ManageStopLoss(ICandleMessage candle)
	{
		if (_entrySide is null || _stopPrice is null)
			return;

		if (_entrySide == Sides.Buy)
		{
			if (Position <= 0)
			{
				ResetTradeState();
				return;
			}

			if (candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
			}
		}
		else if (_entrySide == Sides.Sell)
		{
			if (Position >= 0)
			{
				ResetTradeState();
				return;
			}

			if (candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
			}
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var normalizedStart = ((StartHour % 24) + 24) % 24;
		var normalizedEnd = ((EndHour % 24) + 24) % 24;

		if (normalizedStart == normalizedEnd)
			return false;

		var start = new TimeSpan(normalizedStart, 0, 0);
		var end = new TimeSpan(normalizedEnd, 0, 0);
		var current = time.TimeOfDay;

		return normalizedStart < normalizedEnd
			? current >= start && current <= end
			: current >= start || current <= end;
	}

	private void ResetTradeState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_entrySide = null;
	}
}
