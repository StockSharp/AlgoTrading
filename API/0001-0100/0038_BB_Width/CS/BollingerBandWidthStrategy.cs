using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades a strict increase in absolute Bollinger width in the price/middle direction,
/// closes on any strict contraction, and protects actual fills with frozen entry ATR.
/// </summary>
public class BollingerBandWidthStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousWidth;
	private Order _pendingOrder;
	private Unit _stopDistance;
	private bool _protectionStarted;

	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BollingerBandWidthStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20).SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
			.SetOptimize(10, 30, 5);
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m).SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Deviation for Bollinger Bands calculation", "Indicators")
			.SetOptimize(1.5m, 2.5m, 0.25m);
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR lookback for protection", "Protection");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Stop Multiplier", "Frozen entry ATR stop distance; zero disables it", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousWidth = null;
		_pendingOrder = null;
		_stopDistance = null;
		_protectionStarted = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousWidth = null;
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(bollinger, atr, ProcessCandle, false).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished || !bollingerValue.Indicator.IsFormed || !atrValue.Indicator.IsFormed
			|| !IsFormedAndOnlineAndAllowTrading())
			return;
		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal middle)
			return;
		var width = upper - lower;
		var previous = _previousWidth;
		_previousWidth = width;
		// A fully formed zero-width bar is a valid baseline, not an uninitialized sentinel.
		if (previous is not decimal prev)
			return;
		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		// Equal width is neutral, not contraction; the middle is an entry-only filter.
		if (Position != 0m && width < prev)
		{
			if (Position > 0m) SellMarket(Position);
			else BuyMarket(Math.Abs(Position));
		}
		else if (Position == 0m && width > prev)
		{
			var atr = atrValue.GetValue<decimal>();
			if (candle.ClosePrice > middle) Enter(Sides.Buy, atr);
			else if (candle.ClosePrice < middle) Enter(Sides.Sell, atr);
		}
	}

	private void Enter(Sides side, decimal atr)
	{
		var distance = atr * AtrMultiplier;
		_stopDistance ??= new Unit(distance);
		// Keep the Unit reference held by native cached protection controllers.
		_stopDistance.Value = distance;
		if (!_protectionStarted && distance > 0m)
		{
			StartProtection(new Unit(), _stopDistance, useMarketOrders: true, isLocalStop: true);
			_protectionStarted = true;
		}
		RegisterOrder(new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = Volume,
			Comment = "Bollinger width entry",
		});
	}
}
