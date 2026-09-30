using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades strict native A/D changes confirmed by the Close/price-SMA direction.
/// Fully exits on a strict adverse A/D step or actual-fill percent protection.
/// </summary>
public class ADStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private decimal? _previousAd;
	private Order _pendingOrder;

	public int MAPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public ADStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Current-inclusive Close SMA length", "Indicators")
			.SetOptimize(10, 50, 10);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearSignalState();
	}

	private void ClearSignalState()
	{
		_previousAd = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearSignalState();
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var ma = new SimpleMovingAverage { Length = MAPeriod };
		var ad = new AccumulationDistributionLine();
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(ma, ad, ProcessCandle, false).Start();
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue adValue)
	{
		if (candle.State != CandleStates.Finished || !adValue.Indicator.IsFormed || adValue.IsEmpty)
			return;
		var ad = adValue.GetValue<decimal>();
		var previous = _previousAd;
		// Preserve real zero values and advance A/D during SMA warmup and pending orders.
		_previousAd = ad;
		if (!maValue.Indicator.IsFormed || maValue.IsEmpty || previous is not decimal previousAd ||
			!IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		var rising = ad > previousAd;
		var falling = ad < previousAd;
		// An unchanged A/D step is neutral, never a substitute for a strict decline.
		if (Position > 0m && falling) SellMarket(Position);
		else if (Position < 0m && rising) BuyMarket(Math.Abs(Position));
		else if (Position == 0m)
		{
			var mean = maValue.GetValue<decimal>();
			if (rising && candle.ClosePrice > mean) BuyMarket(Volume);
			else if (falling && candle.ClosePrice < mean) SellMarket(Volume);
		}
	}
}
