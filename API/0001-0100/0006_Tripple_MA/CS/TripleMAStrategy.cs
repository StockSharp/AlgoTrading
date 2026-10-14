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
/// Strategy based on Triple Moving Average crossover.
/// It enters long when the short MA is above both the middle and the long MA, and short when it is below both.
/// A cross of the short and the middle MA closes the position, and a percent stop protects it.
/// </summary>
public class TripleMAStrategy : Strategy
{
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<int> _middleMaPeriod;
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	// Current state

	/// <summary>
	/// Period for short moving average.
	/// </summary>
	public int ShortMaPeriod
	{
		get => _shortMaPeriod.Value;
		set => _shortMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for middle moving average.
	/// </summary>
	public int MiddleMaPeriod
	{
		get => _middleMaPeriod.Value;
		set => _middleMaPeriod.Value = value;
	}

	/// <summary>
	/// Period for long moving average.
	/// </summary>
	public int LongMaPeriod
	{
		get => _longMaPeriod.Value;
		set => _longMaPeriod.Value = value;
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
	/// Initialize the Triple MA strategy.
	/// </summary>
	public TripleMAStrategy()
	{
		_shortMaPeriod = Param(nameof(ShortMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short MA Period", "Period for short moving average", "Indicators")

			.SetOptimize(3, 10, 1);

		_middleMaPeriod = Param(nameof(MiddleMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Middle MA Period", "Period for middle moving average", "Indicators")

			.SetOptimize(15, 30, 5);

		_longMaPeriod = Param(nameof(LongMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Long MA Period", "Period for long moving average", "Indicators")

			.SetOptimize(40, 100, 10);

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

	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create indicators
		var shortMa = new ExponentialMovingAverage { Length = ShortMaPeriod };
		var middleMa = new ExponentialMovingAverage { Length = MiddleMaPeriod };
		var longMa = new ExponentialMovingAverage { Length = LongMaPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortMa, middleMa, longMa, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, middleMa);
			DrawIndicator(area, longMa);
			DrawOwnTrades(area);
		}

	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortMaValue, decimal middleMaValue, decimal longMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longSetup = shortMaValue > middleMaValue && shortMaValue > longMaValue;
		var shortSetup = shortMaValue < middleMaValue && shortMaValue < longMaValue;

		if (Position > 0)
		{
			// The short MA has crossed below the middle one.
			if (shortMaValue < middleMaValue)
				SellMarket(shortSetup ? Volume + Position : Position);
		}
		else if (Position < 0)
		{
			if (shortMaValue > middleMaValue)
				BuyMarket(longSetup ? Volume - Position : -Position);
		}
		else if (longSetup)
		{
			BuyMarket(Volume);
		}
		else if (shortSetup)
		{
			SellMarket(Volume);
		}
	}
}
