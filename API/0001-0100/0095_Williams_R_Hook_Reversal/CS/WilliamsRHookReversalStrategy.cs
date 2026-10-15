using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R Hook Reversal strategy.
/// A long opens when Williams %R was below the oversold level on the previous candle and turns up while the low makes a new low
/// against the previous candle; a short opens when it was above the overbought level and turns down while the high makes a new high.
/// The position closes when Williams %R turns the other way, reversing on the opposite signal, and a percent stop limits the loss.
/// </summary>
public class WilliamsRHookReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _willRPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevValue;
	private decimal _prevHigh;
	private decimal _prevLow;

	/// <summary>
	/// Period for Williams %R.
	/// </summary>
	public int WillRPeriod
	{
		get => _willRPeriod.Value;
		set => _willRPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R level of the oversold zone.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Williams %R level of the overbought zone.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
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
	public WilliamsRHookReversalStrategy()
	{
		_willRPeriod = Param(nameof(WillRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), -80m)
			.SetDisplay("Oversold Level", "Williams %R level of the oversold zone", "Levels");

		_overboughtLevel = Param(nameof(OverboughtLevel), -20m)
			.SetDisplay("Overbought Level", "Williams %R level of the overbought zone", "Levels");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevValue = null;
		_prevHigh = default;
		_prevLow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevValue = null;
		_prevHigh = default;
		_prevLow = default;

		var oscillator = new WilliamsR { Length = WillRPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(oscillator, ProcessCandle)
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
			DrawIndicator(area, oscillator);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished || !value.IsFormed || value.IsEmpty)
			return;

		var current = value.GetValue<decimal>();
		var previous = _prevValue;
		var prevHigh = _prevHigh;
		var prevLow = _prevLow;

		_prevValue = current;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;

		if (previous is not decimal last || !IsFormedAndOnlineAndAllowTrading())
			return;

		var hooksUp = current > last;
		var hooksDown = current < last;
		var longSignal = last < OversoldLevel && hooksUp && candle.LowPrice < prevLow;
		var shortSignal = last > OverboughtLevel && hooksDown && candle.HighPrice > prevHigh;

		if (Position > 0)
		{
			if (hooksDown)
				SellMarket(shortSignal ? Volume + Position : Position);
		}
		else if (Position < 0)
		{
			if (hooksUp)
				BuyMarket(longSignal ? Volume - Position : -Position);
		}
		else if (longSignal)
		{
			BuyMarket(Volume);
		}
		else if (shortSignal)
		{
			SellMarket(Volume);
		}
	}
}
