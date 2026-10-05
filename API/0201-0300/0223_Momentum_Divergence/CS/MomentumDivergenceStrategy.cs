using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum Divergence strategy.
/// A close below the previous close while MomentumPeriod momentum rises above its previous value is a bullish divergence and goes long;
/// a close above the previous close while momentum falls is a bearish one and goes short, reversing an opposite position. A long closes
/// when momentum crosses below zero and a short when it crosses above zero, and a percent stop limits the loss.
/// </summary>
public class MomentumDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevMomentum;

	/// <summary>
	/// Period of the momentum indicator.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
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
	public MomentumDivergenceStrategy()
	{
		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Period of the momentum indicator", "Indicators");

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
		_prevClose = null;
		_prevMomentum = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevMomentum = null;

		var momentum = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(momentum, ProcessCandle)
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

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, momentum);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!momentumValue.IsFormed)
			return;

		var close = candle.ClosePrice;
		var value = momentumValue.GetValue<decimal>();
		var prevClose = _prevClose;
		var prevMomentum = _prevMomentum;
		_prevClose = close;
		_prevMomentum = value;

		if (prevClose is not decimal lastClose || prevMomentum is not decimal lastMomentum)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullish = close < lastClose && value > lastMomentum;
		var bearish = close > lastClose && value < lastMomentum;

		if (bullish && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (bearish && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && lastMomentum >= 0 && value < 0)
			SellMarket(Position);
		else if (Position < 0 && lastMomentum <= 0 && value > 0)
			BuyMarket(-Position);
	}
}
