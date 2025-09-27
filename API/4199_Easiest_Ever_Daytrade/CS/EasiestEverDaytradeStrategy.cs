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
/// Simple day-trading strategy converted from the MetaTrader "Easiest ever" robot.
/// Follows the previous daily candle direction and closes positions at a configured hour.
/// </summary>
public class EasiestEverDaytradeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _entryHourLimit;
	private readonly StrategyParam<int> _marketCloseHour;
	private readonly StrategyParam<DataType> _intradayCandleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private decimal? _previousDailyOpen;
	private decimal? _previousDailyClose;
	private DateTime? _lastDailyCandleDate;

	/// <summary>
	/// Order volume used for market entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Latest hour (exclusive) when new trades can be opened.
	/// </summary>
	public int EntryHourLimit
	{
		get => _entryHourLimit.Value;
		set => _entryHourLimit.Value = value;
	}

	/// <summary>
	/// Hour when open positions are forcefully closed.
	/// </summary>
	public int MarketCloseHour
	{
		get => _marketCloseHour.Value;
		set => _marketCloseHour.Value = value;
	}

	/// <summary>
	/// Timeframe that drives entries and intraday management.
	/// </summary>
	public DataType IntradayCandleType
	{
		get => _intradayCandleType.Value;
		set => _intradayCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to obtain the previous day open and close.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EasiestEverDaytradeStrategy"/> class.
	/// </summary>
	public EasiestEverDaytradeStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume used for market entries", "Trading")
			.SetCanOptimize(true);

		_entryHourLimit = Param(nameof(EntryHourLimit), 1)
			.SetRange(0, 23)
			.SetDisplay("Entry Hour Limit", "Latest hour (exclusive) when new trades can be opened", "Schedule")
			.SetCanOptimize(true);

		_marketCloseHour = Param(nameof(MarketCloseHour), 20)
			.SetRange(0, 23)
			.SetDisplay("Market Close Hour", "Hour when open positions are closed", "Schedule")
			.SetCanOptimize(true);

		_intradayCandleType = Param(nameof(IntradayCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Intraday Candles", "Timeframe that drives entries and exits", "Timeframes");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Candles", "Timeframe used to detect previous day direction", "Timeframes");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, IntradayCandleType);

		if (DailyCandleType != IntradayCandleType)
			yield return (Security, DailyCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousDailyOpen = null;
		_previousDailyClose = null;
		_lastDailyCandleDate = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var intradaySubscription = SubscribeCandles(IntradayCandleType);
		intradaySubscription.Bind(ProcessIntradayCandle);

		if (DailyCandleType == IntradayCandleType)
		{
			intradaySubscription.Bind(ProcessDailyCandle);
			intradaySubscription.Start();
		}
		else
		{
			intradaySubscription.Start();

			var dailySubscription = SubscribeCandles(DailyCandleType);
			dailySubscription
				.Bind(ProcessDailyCandle)
				.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, intradaySubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store the previous day open and close to drive next session entries.
		_previousDailyOpen = candle.OpenPrice;
		_previousDailyClose = candle.ClosePrice;
		_lastDailyCandleDate = candle.OpenTime.Date;
	}

	private void ProcessIntradayCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var candleTime = candle.OpenTime;
		var currentHour = candleTime.Hour;

		if (Position != 0m && currentHour >= MarketCloseHour)
		{
			ClosePosition();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (TradeVolume <= 0m)
			return;

		if (_previousDailyOpen == null || _previousDailyClose == null || _lastDailyCandleDate == null)
			return;

		if (candleTime.Date <= _lastDailyCandleDate.Value)
			return;

		if (currentHour >= EntryHourLimit)
			return;

		if (Position != 0m)
			return;

		if (_previousDailyClose > _previousDailyOpen)
		{
			// Follow previous day's bullish direction with a market buy.
			BuyMarket(TradeVolume);
		}
		else if (_previousDailyClose < _previousDailyOpen)
		{
			// Follow previous day's bearish direction with a market sell.
			SellMarket(TradeVolume);
		}
	}

	private void ClosePosition()
	{
		// Exit any open exposure at the configured closing hour.
		var position = Position;

		if (position > 0m)
			SellMarket(position);
		else if (position < 0m)
			BuyMarket(Math.Abs(position));
	}
}
