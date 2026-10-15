using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA Moving Away strategy (long only).
/// Buys when the close is at least MovingAwayPercent below the EMA, optionally requiring a streak of bearish candles
/// (BearishStreak) and a minimum candle body (MinBodyPercent). The long closes when price returns to the EMA, and a
/// percent stop-loss protects it if the reversion fails.
/// </summary>
public class EmaMovingAwayStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _movingAwayPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _bearishStreak;
	private readonly StrategyParam<decimal> _minBodyPercent;
	private readonly StrategyParam<DataType> _candleType;

	private int _bearishCount;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Required distance of the close below the EMA, in percent.
	/// </summary>
	public decimal MovingAwayPercent
	{
		get => _movingAwayPercent.Value;
		set => _movingAwayPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage. 0 disables it.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Consecutive bearish candles required before an entry. 0 disables the filter.
	/// </summary>
	public int BearishStreak
	{
		get => _bearishStreak.Value;
		set => _bearishStreak.Value = value;
	}

	/// <summary>
	/// Minimum body of the signal candle in percent of its open. 0 disables the filter.
	/// </summary>
	public decimal MinBodyPercent
	{
		get => _minBodyPercent.Value;
		set => _minBodyPercent.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EmaMovingAwayStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 55)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Moving Average");

		_movingAwayPercent = Param(nameof(MovingAwayPercent), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Moving away (%)", "Required distance of the close below the EMA", "Strategy");

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk");

		_bearishStreak = Param(nameof(BearishStreak), 0)
			.SetNotNegative()
			.SetDisplay("Bearish Streak", "Consecutive bearish candles required, 0 disables", "Filters");

		_minBodyPercent = Param(nameof(MinBodyPercent), 0m)
			.SetNotNegative()
			.SetDisplay("Min Body %", "Minimum body of the signal candle in percent, 0 disables", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bearishCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bearishCount = 0;

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, ProcessCandle)
			.Start();

		if (StopLossPercent > 0)
			StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_bearishCount = candle.ClosePrice < candle.OpenPrice ? _bearishCount + 1 : 0;

		if (!emaValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ema = emaValue.ToDecimal();
		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close >= ema)
				SellMarket(Position);

			return;
		}

		var stretched = close <= ema * (1m - MovingAwayPercent / 100m);
		var streakOk = BearishStreak <= 0 || _bearishCount >= BearishStreak;
		var bodyOk = MinBodyPercent <= 0 || candle.OpenPrice > 0 && Math.Abs(candle.OpenPrice - close) / candle.OpenPrice * 100m >= MinBodyPercent;

		if (stretched && streakOk && bodyOk)
			BuyMarket(Volume + Math.Abs(Position));
	}
}
