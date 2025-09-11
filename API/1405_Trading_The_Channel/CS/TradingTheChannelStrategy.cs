using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy implementing linear regression channel trading.
/// </summary>
public class TradingTheChannelStrategy : Strategy
{
	private enum TradeRule
	{
		TradeTrend,
		TradeBreakouts,
		TradeChannel
	}

	private enum RangeSource
	{
		Close,
		HighLow
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<TradeRule> _rule;
	private readonly StrategyParam<RangeSource> _rangeSource;
	private readonly StrategyParam<decimal> _zonePercent;
	private readonly StrategyParam<bool> _longOnly;
	private readonly StrategyParam<bool> _trendFilter;

	private readonly Queue<decimal> _closes = new();
	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();
	private int _prevSignal;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Regression length.
	/// </summary>
	public int Period { get => _period.Value; set => _period.Value = value; }

	/// <summary>
	/// Trading mode.
	/// </summary>
	public TradeRule Rule { get => _rule.Value; set => _rule.Value = value; }

	/// <summary>
	/// Source for band calculation.
	/// </summary>
	public RangeSource Source { get => _rangeSource.Value; set => _rangeSource.Value = value; }

	/// <summary>
	/// Zone width fraction for rule 3.
	/// </summary>
	public decimal ZonePercent { get => _zonePercent.Value; set => _zonePercent.Value = value; }

	/// <summary>
	/// Allow only long trades.
	/// </summary>
	public bool LongOnly { get => _longOnly.Value; set => _longOnly.Value = value; }

	/// <summary>
	/// Filter trades by slope sign.
	/// </summary>
	public bool TrendFilter { get => _trendFilter.Value; set => _trendFilter.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TradingTheChannelStrategy"/> class.
	/// </summary>
	public TradingTheChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_period = Param(nameof(Period), 40)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Bars for regression", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 20);

		_rule = Param(nameof(Rule), TradeRule.TradeTrend)
			.SetDisplay("Trade Rule", "Trading mode", "Parameters");

		_rangeSource = Param(nameof(Source), RangeSource.Close)
			.SetDisplay("Range Source", "Use close or high/low", "Parameters");

		_zonePercent = Param(nameof(ZonePercent), 0.2m)
			.SetDisplay("Zone Percent", "Zone width for rule 3", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.1m);

		_longOnly = Param(nameof(LongOnly), false)
			.SetDisplay("Long Only", "Allow only long trades", "Parameters");

		_trendFilter = Param(nameof(TrendFilter), false)
			.SetDisplay("Trend Filter", "Filter trades by slope sign", "Parameters");
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
		_closes.Clear();
		_highs.Clear();
		_lows.Clear();
		_prevSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_closes.Enqueue(candle.ClosePrice);
		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);

		if (_closes.Count > Period)
		{
			_closes.Dequeue();
			_highs.Dequeue();
			_lows.Dequeue();
		}

		if (_closes.Count < Period)
		return;

		var n = Period;
		var closes = _closes.ToArray();
		double sumX = 0;
		double sumY = 0;
		double sumXY = 0;
		double sumX2 = 0;

		for (var i = 0; i < n; i++)
		{
			var x = (double)i;
			var y = (double)closes[i];
			sumX += x;
			sumY += y;
			sumXY += x * y;
			sumX2 += x * x;
		}

		var denom = n * sumX2 - sumX * sumX;
		if (denom == 0)
		return;

		var slope = (decimal)((n * sumXY - sumX * sumY) / denom);
		var intercept = (decimal)((sumY - (double)slope * sumX) / n);
		var linreg = intercept + slope * (n - 1);
		var signal = Math.Sign(slope);

		decimal t = 0;
		decimal b = 0;
		var highs = _highs.ToArray();
		var lows = _lows.ToArray();

		for (var i = 0; i < n; i++)
		{
			var predicted = intercept + slope * i;
			if (Source == RangeSource.Close)
			{
				b = Math.Max(b, predicted - closes[i]);
				t = Math.Max(t, closes[i] - predicted);
			}
			else
			{
				b = Math.Max(b, predicted - lows[i]);
				t = Math.Max(t, highs[i] - predicted);
			}
		}

		var upper = linreg + t + slope;
		var lower = linreg - b + slope;

		switch (Rule)
		{
			case TradeRule.TradeTrend:
			if (signal != _prevSignal)
			{
			if (signal > 0 && Position <= 0)
			BuyMarket();
			else if (signal < 0 && Position >= 0 && !LongOnly)
			SellMarket();
			}
			break;

			case TradeRule.TradeBreakouts:
			if (candle.ClosePrice > upper && Position <= 0)
			BuyMarket();
			else if (candle.ClosePrice < lower && Position >= 0 && !LongOnly)
			SellMarket();
			break;

			case TradeRule.TradeChannel:
			var range = upper - lower;
			var buyZone = lower + ZonePercent * range;
			var sellZone = upper - ZonePercent * range;

			if (Position > 0 && (candle.ClosePrice >= sellZone || (TrendFilter && slope < 0)))
			SellMarket();
			else if (Position < 0 && (candle.ClosePrice <= buyZone || (TrendFilter && slope > 0)))
			BuyMarket();
			else if (Position <= 0 && candle.ClosePrice <= buyZone && (!TrendFilter || slope > 0))
			BuyMarket();
			else if (!LongOnly && Position >= 0 && candle.ClosePrice >= sellZone && (!TrendFilter || slope < 0))
			SellMarket();
			break;
		}

		_prevSignal = signal;
	}
}
