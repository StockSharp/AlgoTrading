using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Midnight pullback strategy converted from the MetaTrader "1_Otkat_Sys" expert advisor.
/// Trades at the start of the day based on the previous session range and directional bias.
/// </summary>
public class OtkatSysStrategy : Strategy
{
	private readonly StrategyParam<decimal> _longExtraTakeProfit;

	private readonly StrategyParam<DataType> _entryCandleType;
	private readonly StrategyParam<DataType> _dailyCandleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _pullbackThreshold;
	private readonly StrategyParam<decimal> _corridorThreshold;
	private readonly StrategyParam<decimal> _toleranceThreshold;
	private readonly StrategyParam<decimal> _tradeVolume;

	private decimal _dailyOpen;
	private decimal _dailyClose;
	private decimal _dailyHigh;
	private decimal _dailyLow;
	private bool _hasDailyData;
	private DateTime? _lastTradeDate;

	/// <summary>
	/// Entry candle type, defaults to one-minute candles.
	/// </summary>
	public DataType EntryCandleType
	{
		get => _entryCandleType.Value;
		set => _entryCandleType.Value = value;
	}

	/// <summary>
	/// Daily candle type used to gather previous session statistics.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Extra take profit points added to long positions.
	/// </summary>
	public decimal LongExtraTakeProfit
	{
		get => _longExtraTakeProfit.Value;
		set => _longExtraTakeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Pullback trigger (Otkat) in points.
	/// </summary>
	public decimal PullbackThreshold
	{
		get => _pullbackThreshold.Value;
		set => _pullbackThreshold.Value = value;
	}

	/// <summary>
	/// Corridor trigger (KoridorOC) in points.
	/// </summary>
	public decimal CorridorThreshold
	{
		get => _corridorThreshold.Value;
		set => _corridorThreshold.Value = value;
	}

	/// <summary>
	/// Corridor tolerance (KoridorOt) in points.
	/// </summary>
	public decimal ToleranceThreshold
	{
		get => _toleranceThreshold.Value;
		set => _toleranceThreshold.Value = value;
	}

	/// <summary>
	/// Trade volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="OtkatSysStrategy"/>.
	/// </summary>
	public OtkatSysStrategy()
	{
		_entryCandleType = Param(nameof(EntryCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Entry Candles", "Primary timeframe", "General");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Candles", "Session statistics timeframe", "General");

		_longExtraTakeProfit = Param(nameof(LongExtraTakeProfit), 3m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Long Bonus Take Profit", "Additional points added to long take profit", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 10m, 0.5m);

		_takeProfit = Param(nameof(TakeProfit), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Target distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(2m, 20m, 1m);

		_stopLoss = Param(nameof(StopLoss), 51m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Protection distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 120m, 5m);

		_pullbackThreshold = Param(nameof(PullbackThreshold), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Pullback", "Otkat threshold", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 1m);

		_corridorThreshold = Param(nameof(CorridorThreshold), 18m)
			.SetGreaterThanZero()
			.SetDisplay("Corridor", "KoridorOC threshold", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(10m, 30m, 1m);

		_toleranceThreshold = Param(nameof(ToleranceThreshold), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Tolerance", "KoridorOt tolerance", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Lots per entry", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[]
		{
			(Security, EntryCandleType),
			(Security, DailyCandleType)
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_dailyOpen = 0m;
		_dailyClose = 0m;
		_dailyHigh = 0m;
		_dailyLow = 0m;
		_hasDailyData = false;
		_lastTradeDate = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var entrySubscription = SubscribeCandles(EntryCandleType);
		entrySubscription
			.WhenCandlesFinished(ProcessEntryCandle)
			.Start();

		SubscribeCandles(DailyCandleType)
			.WhenCandlesFinished(ProcessDailyCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, entrySubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Cache the last completed daily candle for the midnight decision.
		_dailyOpen = candle.OpenPrice;
		_dailyClose = candle.ClosePrice;
		_dailyHigh = candle.HighPrice;
		_dailyLow = candle.LowPrice;
		_hasDailyData = true;
	}

	private void ProcessEntryCandle(ICandleMessage candle)
	{
		var openTime = candle.OpenTime;

		if (Position != 0m && openTime.Hour == 22 && openTime.Minute >= 45)
		{
			CloseOpenPosition();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_hasDailyData || TradeVolume <= 0m)
		return;

		if (Position != 0m)
		return;

		if (openTime.Hour != 0 || openTime.Minute > 3)
		return;

		if (openTime.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Friday)
		return;

		if (_lastTradeDate == openTime.Date)
		return;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return;

		var openMinusClose = _dailyOpen - _dailyClose;
		var closeMinusOpen = -openMinusClose;
		var closeMinusLow = _dailyClose - _dailyLow;
		var highMinusClose = _dailyHigh - _dailyClose;

		var corridor = CorridorThreshold * step;
		var tolerance = ToleranceThreshold * step;
		var pullback = PullbackThreshold * step;

		var longTakeProfit = (TakeProfit + LongExtraTakeProfit) * step;
		var shortTakeProfit = TakeProfit * step;
		var stopDistance = StopLoss * step;

		var allowLong = (openMinusClose > corridor && closeMinusLow < pullback - tolerance) ||
		(closeMinusOpen > corridor && highMinusClose > pullback + tolerance);

		var allowShort = (closeMinusOpen > corridor && highMinusClose < pullback - tolerance) ||
		(openMinusClose > corridor && closeMinusLow > pullback + tolerance);

		if (!allowLong && !allowShort)
		return;

		var entryPrice = candle.OpenPrice;

		if (allowLong)
		{
			var resultingPosition = Position + TradeVolume;
			BuyMarket(TradeVolume);

			if (stopDistance > 0m)
			SetStopLoss(stopDistance, entryPrice, resultingPosition);

			if (TakeProfit > 0m)
			SetTakeProfit(longTakeProfit, entryPrice, resultingPosition);

			_lastTradeDate = openTime.Date;
			return;
		}

		if (allowShort)
		{
			var resultingPosition = Position - TradeVolume;
			SellMarket(TradeVolume);

			if (stopDistance > 0m)
			SetStopLoss(stopDistance, entryPrice, resultingPosition);

			if (TakeProfit > 0m)
			SetTakeProfit(shortTakeProfit, entryPrice, resultingPosition);

			_lastTradeDate = openTime.Date;
		}
	}

	private void CloseOpenPosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
		}
	}
}
