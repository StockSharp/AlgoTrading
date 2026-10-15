using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ICT NY Kill Zone Auto Trading strategy.
/// Inside the New York kill zone (07:00-10:00 New York time, taken as 11:00-14:00 UTC) a bullish fair value gap, where the low of the
/// current candle stays above the high of the candle two bars back, whose first candle is a bearish order block goes long. A bearish
/// fair value gap whose first candle is a bullish order block goes short. An opposite signal reverses the position and every position
/// is protected by a stop loss and take profit in price steps.
/// </summary>
public class IctNyKillZoneAutoTradingStrategy : Strategy
{
	private const int _killZoneStartHour = 11;
	private const int _killZoneEndHour = 14;

	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prev1;
	private ICandleMessage _prev2;

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public IctNyKillZoneAutoTradingStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 30m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss in price steps", "Risk Management")
			.SetOptimize(10m, 100m, 10m);

		_takeProfit = Param(nameof(TakeProfit), 60m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit in price steps", "Risk Management")
			.SetOptimize(20m, 200m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prev1 = null;
		_prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prev1 = null;
		_prev2 = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;

		StartProtection(
			takeProfit: TakeProfit > 0 ? new Unit(TakeProfit * step, UnitTypes.Absolute) : new Unit(),
			stopLoss: StopLoss > 0 ? new Unit(StopLoss * step, UnitTypes.Absolute) : new Unit(),
			useMarketOrders: true,
			isLocalStop: true);

		// The stop and take have to see prices between candles, not only at their close.
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

		var first = _prev2;

		_prev2 = _prev1;
		_prev1 = candle;

		if (first == null || !IsFormedAndOnlineAndAllowTrading())
			return;

		var hour = candle.OpenTime.Hour;
		if (hour < _killZoneStartHour || hour >= _killZoneEndHour)
			return;

		var bullishFvg = first.HighPrice < candle.LowPrice;
		var bearishFvg = first.LowPrice > candle.HighPrice;
		var bearishOrderBlock = first.ClosePrice < first.OpenPrice;
		var bullishOrderBlock = first.ClosePrice > first.OpenPrice;

		if (bullishFvg && bearishOrderBlock && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (bearishFvg && bullishOrderBlock && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
