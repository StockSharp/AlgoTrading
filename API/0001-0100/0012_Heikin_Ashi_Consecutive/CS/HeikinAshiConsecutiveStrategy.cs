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
/// Strategy based on consecutive Heikin Ashi candles.
/// It enters long position after a sequence of bullish Heikin Ashi candles and
/// short position after a sequence of bearish Heikin Ashi candles,
/// and exits on the first opposite candle or at the percent stop.
/// </summary>
public class HeikinAshiConsecutiveStrategy : Strategy
{
	private readonly StrategyParam<int> _consecutiveCandles;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	// State tracking
	private int _bullishCount;
	private int _bearishCount;
	private decimal? _prevHaOpen;
	private decimal _prevHaClose;

	/// <summary>
	/// Number of consecutive candles required for signal.
	/// </summary>
	public int ConsecutiveCandles
	{
		get => _consecutiveCandles.Value;
		set => _consecutiveCandles.Value = value;
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
	/// Initialize the Heikin Ashi Consecutive strategy.
	/// </summary>
	public HeikinAshiConsecutiveStrategy()
	{
		_consecutiveCandles = Param(nameof(ConsecutiveCandles), 3)
			.SetGreaterThanZero()
			.SetDisplay("Consecutive Candles", "Number of consecutive candles required for signal", "Trading parameters")

			.SetOptimize(5, 10, 1);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
			
			.SetOptimize(1, 3, 0.5m);

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
		_bullishCount = default;
		_bearishCount = default;
		_prevHaOpen = null;
		_prevHaClose = default;

	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create subscription
		var subscription = SubscribeCandles(CandleType);
		
		// We need to calculate Heikin-Ashi candles in the ProcessCandle handler
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		// Start protection with stop loss
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
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

		var haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4;
		var haOpen = _prevHaOpen is decimal prevOpen
			? (prevOpen + _prevHaClose) / 2
			: (candle.OpenPrice + candle.ClosePrice) / 2;

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;

		var isBullish = haClose > haOpen;
		var isBearish = haClose < haOpen;

		_bullishCount = isBullish ? _bullishCount + 1 : 0;
		_bearishCount = isBearish ? _bearishCount + 1 : 0;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			// The first bearish candle ends a long position.
			if (isBearish)
				SellMarket(_bearishCount >= ConsecutiveCandles ? Volume + Position : Position);
		}
		else if (Position < 0)
		{
			if (isBullish)
				BuyMarket(_bullishCount >= ConsecutiveCandles ? Volume - Position : -Position);
		}
		else if (_bullishCount >= ConsecutiveCandles)
		{
			BuyMarket(Volume);
		}
		else if (_bearishCount >= ConsecutiveCandles)
		{
			SellMarket(Volume);
		}
	}
}
