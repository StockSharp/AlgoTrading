using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci Retracement Reversal strategy.
/// The swing is the highest high and lowest low of the previous SwingLookbackPeriod candles; it rose when the low came first.
/// In a rising swing a bullish candle closing within FibLevelBuffer percent of the 61.8% or 78.6% retracement buys,
/// in a falling swing a bearish candle near those levels sells. The target is the swing's 50% level, and a percent stop protects the trade.
/// </summary>
public class FibonacciRetracementReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _swingLookbackPeriod;
	private readonly StrategyParam<decimal> _fibLevelBuffer;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal High, decimal Low)> _candles = [];
	private decimal _target;

	/// <summary>
	/// Number of previous candles that form the swing.
	/// </summary>
	public int SwingLookbackPeriod
	{
		get => _swingLookbackPeriod.Value;
		set => _swingLookbackPeriod.Value = value;
	}

	/// <summary>
	/// How close to a retracement level the close must be, in percent of the level.
	/// </summary>
	public decimal FibLevelBuffer
	{
		get => _fibLevelBuffer.Value;
		set => _fibLevelBuffer.Value = value;
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
	public FibonacciRetracementReversalStrategy()
	{
		_swingLookbackPeriod = Param(nameof(SwingLookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Swing Lookback", "Previous candles that form the swing", "Indicators");

		_fibLevelBuffer = Param(nameof(FibLevelBuffer), 0.5m)
			.SetNotNegative()
			.SetDisplay("Level Buffer %", "Distance from a retracement level that counts as a test, in percent", "Indicators");

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
		_candles.Clear();
		_target = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_candles.Clear();
		_target = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
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
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The swing is formed by the candles before this one.
		var swing = _candles.ToArray();

		_candles.Add((candle.HighPrice, candle.LowPrice));

		if (_candles.Count > SwingLookbackPeriod)
			_candles.RemoveAt(0);

		if (swing.Length < SwingLookbackPeriod || !IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close >= _target)
				SellMarket(Position);

			return;
		}

		if (Position < 0)
		{
			if (close <= _target)
				BuyMarket(-Position);

			return;
		}

		var highIndex = Array.FindIndex(swing, c => c.High == swing.Max(x => x.High));
		var lowIndex = Array.FindIndex(swing, c => c.Low == swing.Min(x => x.Low));
		var high = swing[highIndex].High;
		var low = swing[lowIndex].Low;
		var range = high - low;

		if (range <= 0)
			return;

		var middle = low + range / 2m;

		bool Near(decimal level) => Math.Abs(close - level) <= level * FibLevelBuffer / 100m;

		if (lowIndex < highIndex)
		{
			// Rising swing: buy a bullish candle at a deep pullback.
			if (close > candle.OpenPrice && close < middle && (Near(high - range * 0.618m) || Near(high - range * 0.786m)))
			{
				BuyMarket(Volume);
				_target = middle;
			}
		}
		else if (highIndex < lowIndex)
		{
			// Falling swing: sell a bearish candle at a deep rebound.
			if (close < candle.OpenPrice && close > middle && (Near(low + range * 0.618m) || Near(low + range * 0.786m)))
			{
				SellMarket(Volume);
				_target = middle;
			}
		}
	}
}
