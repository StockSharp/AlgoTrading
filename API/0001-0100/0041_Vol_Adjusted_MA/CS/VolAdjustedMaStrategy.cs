using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades strict SMA/current Wilder ATR band breakouts.
/// Exits on actual adverse SMA crossings or frozen entry-ATR actual-fill protection.
/// </summary>
public class VolAdjustedMaStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousClose;
	private decimal _previousMean;
	private Order _pendingOrder;
	private Unit _stopDistance;
	private bool _protectionStarted;

	public int MAPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int ATRPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal ATRMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal StopLossATRMultiplier { get => _stopLossAtrMultiplier.Value; set => _stopLossAtrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolAdjustedMaStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 5);
		_atrPeriod = Param(nameof(ATRPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetOptimize(7, 28, 7);
		_atrMultiplier = Param(nameof(ATRMultiplier), 2m).SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR to adjust MA bands", "Entry")
			.SetOptimize(1m, 3m, 0.5m);
		_stopLossAtrMultiplier = Param(nameof(StopLossATRMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Stop Multiplier", "Frozen entry ATR distance; zero disables it", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousClose = null;
		_previousMean = default;
		_pendingOrder = null;
		_stopDistance = null;
		_protectionStarted = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousClose = null;
		_previousMean = default;
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var ma = new SimpleMovingAverage { Length = MAPeriod };
		var atr = new AverageTrueRange { Length = ATRPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(ma, atr, ProcessCandle, false).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished || !maValue.Indicator.IsFormed || !atrValue.Indicator.IsFormed
			|| !IsFormedAndOnlineAndAllowTrading())
			return;
		var close = candle.ClosePrice;
		var mean = maValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var downwardCross = _previousClose is decimal down && down >= _previousMean && close < mean;
		var upwardCross = _previousClose is decimal up && up <= _previousMean && close > mean;
		_previousClose = close;
		_previousMean = mean;
		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m && downwardCross)
			SellMarket(Position);
		else if (Position < 0m && upwardCross)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m)
		{
			if (close > mean + ATRMultiplier * atr) Enter(Sides.Buy, atr);
			else if (close < mean - ATRMultiplier * atr) Enter(Sides.Sell, atr);
		}
	}

	private void Enter(Sides side, decimal atr)
	{
		var distance = atr * StopLossATRMultiplier;
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
			Comment = "Vol adjusted MA entry",
		});
	}
}
