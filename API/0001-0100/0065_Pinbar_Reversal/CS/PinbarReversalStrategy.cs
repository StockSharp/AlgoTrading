using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enters in the SMA-aligned direction of a formed pinbar and exits on the opposite pinbar or stop.
/// </summary>
public class PinbarReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tailToBodyRatio;
	private readonly StrategyParam<decimal> _oppositeTailRatio;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private Order _pendingOrder;

	public decimal TailToBodyRatio { get => _tailToBodyRatio.Value; set => _tailToBodyRatio.Value = value; }
	public decimal OppositeTailRatio { get => _oppositeTailRatio.Value; set => _oppositeTailRatio.Value = value; }
	public int MAPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public PinbarReversalStrategy()
	{
		_tailToBodyRatio = Param(nameof(TailToBodyRatio), 2m).SetRange(1m, 10m)
			.SetDisplay("Tail/Body Ratio", "Minimum dominant shadow relative to the body", "Pattern");
		_oppositeTailRatio = Param(nameof(OppositeTailRatio), 0.5m).SetRange(0m, 2m)
			.SetDisplay("Opposite Tail Ratio", "Maximum opposite shadow relative to the body", "Pattern");
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Close SMA trend filter", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Pinbar and MA timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_pendingOrder = null;
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var candles = SubscribeCandles(CandleType);
		candles.Bind(sma, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native actual-fill protection evaluates executable quotes between signal candles.
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		if (body <= 0m)
			return;
		var lower = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
		var upper = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var bullish = lower >= body * TailToBodyRatio && upper <= body * OppositeTailRatio;
		var bearish = upper >= body * TailToBodyRatio && lower <= body * OppositeTailRatio;

		if (Position > 0m && bearish)
			SellMarket(Position);
		else if (Position < 0m && bullish)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && bullish && candle.ClosePrice > smaValue)
			BuyMarket(Volume);
		else if (Position == 0m && bearish && candle.ClosePrice < smaValue)
			SellMarket(Volume);
	}
}
