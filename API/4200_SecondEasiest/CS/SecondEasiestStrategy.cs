namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;

public class SecondEasiestStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _entryCutoffHour;
	private readonly StrategyParam<int> _marketCloseHour;
	private readonly StrategyParam<decimal> _rangePoints;

	private DateTime? _currentSessionDate;
	private decimal _dailyOpen;
	private decimal _dailyHigh;
	private decimal _dailyLow;
	private bool _hasDailyRange;

	public SecondEasiestStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Intraday Candle", "Timeframe used to monitor intraday price action.", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Lot size used when opening market positions.", "Trading");

		_entryCutoffHour = Param(nameof(EntryCutoffHour), 16)
			.SetDisplay("Entry Cutoff Hour", "Hour of day after which new entries are ignored.", "Trading");

		_marketCloseHour = Param(nameof(MarketCloseHour), 20)
			.SetDisplay("Market Close Hour", "Hour of day when existing positions are closed.", "Trading");

		_rangePoints = Param(nameof(RangePointsThreshold), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Range Threshold (points)", "Minimum distance between the daily open and extremes.", "Filters");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public int EntryCutoffHour
	{
		get => _entryCutoffHour.Value;
		set => _entryCutoffHour.Value = value;
	}

	public int MarketCloseHour
	{
		get => _marketCloseHour.Value;
		set => _marketCloseHour.Value = value;
	}

	public decimal RangePointsThreshold
	{
		get => _rangePoints.Value;
		set => _rangePoints.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentSessionDate = null;
		_dailyOpen = 0m;
		_dailyHigh = 0m;
		_dailyLow = 0m;
		_hasDailyRange = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		StartProtection();

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDailyRange(candle);

		TryCloseAtEndOfDay(candle);

		if (Position != 0m)
			return;

		if (!_hasDailyRange)
			return;

		var hour = candle.OpenTime.Hour;
		if (EntryCutoffHour >= 0 && hour >= EntryCutoffHour)
			return;

		var threshold = GetPointDistance(RangePointsThreshold);
		if (threshold <= 0m)
			return;

		var currentPrice = candle.ClosePrice;

		var bullishSetup = currentPrice > _dailyOpen && (_dailyOpen - _dailyLow) > threshold;
		if (bullishSetup)
		{
			// Daily open is acting as support and price is rallying above it.
			BuyMarket();
			return;
		}

		var bearishSetup = currentPrice < _dailyOpen && (_dailyHigh - _dailyOpen) > threshold;
		if (bearishSetup)
		{
			// Daily open is acting as resistance and price is breaking below it.
			SellMarket();
		}
	}

	private void UpdateDailyRange(ICandleMessage candle)
	{
		var sessionDate = candle.OpenTime.Date;

		if (_currentSessionDate != sessionDate)
		{
			// First candle of the trading session establishes the reference open/high/low.
			_currentSessionDate = sessionDate;
			_dailyOpen = candle.OpenPrice;
			_dailyHigh = candle.HighPrice;
			_dailyLow = candle.LowPrice;
			_hasDailyRange = true;
			return;
		}

		if (!_hasDailyRange)
			return;

		if (candle.HighPrice > _dailyHigh)
			_dailyHigh = candle.HighPrice;

		if (candle.LowPrice < _dailyLow)
			_dailyLow = candle.LowPrice;
	}

	private void TryCloseAtEndOfDay(ICandleMessage candle)
	{
		if (Position == 0m)
			return;

		var closeHour = MarketCloseHour;
		if (closeHour < 0)
			return;

		var hour = candle.OpenTime.Hour;
		if (hour < closeHour)
			return;

		// Flatten the position when the market close time is reached.
		ClosePosition();
	}

	private decimal GetPointDistance(decimal points)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step == 0m)
			return points;

		return points * step;
	}
}
