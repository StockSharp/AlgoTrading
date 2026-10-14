using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trendline Bounce strategy.
/// Support is the regression line of the lows of the previous TrendlinePeriod candles and resistance that of their highs,
/// both extended to the current candle. While flat, a bullish candle whose low comes within BounceThresholdPercent of a rising
/// support and that closes above the moving average buys; a bearish candle at a falling resistance closing below it sells.
/// A cross of the moving average or a percent stop closes the position.
/// </summary>
public class TrendlineBounceStrategy : Strategy
{
	private readonly StrategyParam<int> _trendlinePeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _bounceThresholdPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = [];
	private readonly List<decimal> _lows = [];

	/// <summary>
	/// Number of previous candles the trendlines are fitted to.
	/// </summary>
	public int TrendlinePeriod
	{
		get => _trendlinePeriod.Value;
		set => _trendlinePeriod.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// How close to a trendline the candle must come, in percent of the line.
	/// </summary>
	public decimal BounceThresholdPercent
	{
		get => _bounceThresholdPercent.Value;
		set => _bounceThresholdPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TrendlineBounceStrategy()
	{
		_trendlinePeriod = Param(nameof(TrendlinePeriod), 20)
			.SetRange(2, 1000)
			.SetDisplay("Trendline Period", "Previous candles the trendlines are fitted to", "Indicators");

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA", "Indicators");

		_bounceThresholdPercent = Param(nameof(BounceThresholdPercent), 0.5m)
			.SetNotNegative()
			.SetDisplay("Bounce Threshold %", "How close to a trendline the candle must come", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_highs.Clear();
		_lows.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs.Clear();
		_lows.Clear();

		var sma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The lines are fitted to the candles before this one.
		var ready = _highs.Count == TrendlinePeriod;
		var (supportSlope, support) = ready ? FitAndExtend(_lows) : default;
		var (resistanceSlope, resistance) = ready ? FitAndExtend(_highs) : default;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > TrendlinePeriod)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (!ready || !smaValue.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var ma = smaValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close < ma)
				SellMarket(Position);

			return;
		}

		if (Position < 0)
		{
			if (close > ma)
				BuyMarket(-Position);

			return;
		}

		var threshold = BounceThresholdPercent / 100m;

		if (supportSlope > 0 && candle.LowPrice <= support * (1 + threshold) && close > candle.OpenPrice && close > ma)
			BuyMarket(Volume);
		else if (resistanceSlope < 0 && candle.HighPrice >= resistance * (1 - threshold) && close < candle.OpenPrice && close < ma)
			SellMarket(Volume);
	}

	private static (decimal Slope, decimal Next) FitAndExtend(List<decimal> values)
	{
		// Least squares over x = 0..n-1, extended to x = n.
		var n = values.Count;
		decimal sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

		for (var i = 0; i < n; i++)
		{
			sumX += i;
			sumY += values[i];
			sumXY += i * values[i];
			sumX2 += i * i;
		}

		var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
		var intercept = (sumY - slope * sumX) / n;

		return (slope, intercept + slope * n);
	}
}
