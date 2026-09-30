using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Compares each finished bar's close and Williams %R with the reading DivergencePeriod bars back
/// and trades the divergence in the %R extreme zone.
/// Exits at the opposite %R extreme or native actual-fill percent protection.
/// </summary>
public class WilliamsPercentRDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<int> _divergencePeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly Queue<(decimal Close, decimal WilliamsR)> _history = new();
	private WilliamsR _williamsR;
	private Order _pendingOrder;

	public int WilliamsRPeriod { get => _williamsRPeriod.Value; set => _williamsRPeriod.Value = value; }
	public int DivergencePeriod { get => _divergencePeriod.Value; set => _divergencePeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public WilliamsPercentRDivergenceStrategy()
	{
		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14).SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Highest-high/lowest-low lookback", "Indicators");
		_divergencePeriod = Param(nameof(DivergencePeriod), 5).SetGreaterThanZero()
			.SetDisplay("Divergence Period", "Bars back to the compared close and %R reading", "Pattern");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Williams %R and divergence timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearState();
	}

	private void ClearState()
	{
		_history.Clear();
		_williamsR = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearState();
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		_williamsR = new WilliamsR { Length = WilliamsRPeriod };
		var candles = SubscribeCandles(CandleType);
		candles.Bind(_williamsR, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, _williamsR);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native actual-fill protection evaluates executable quotes between signal candles.
	}

	private void ProcessCandle(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished || !_williamsR.IsFormed)
			return;
		var bullish = false;
		var bearish = false;
		if (_history.Count == DivergencePeriod)
		{
			var prior = _history.Peek();
			bullish = candle.ClosePrice < prior.Close && value > prior.WilliamsR && value < -80m;
			bearish = candle.ClosePrice > prior.Close && value < prior.WilliamsR && value > -20m;
		}
		_history.Enqueue((candle.ClosePrice, value));
		if (_history.Count > DivergencePeriod)
			_history.Dequeue();
		if (!IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m && value >= -20m)
			SellMarket(Position);
		else if (Position < 0m && value <= -80m)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m)
		{
			if (bullish)
				BuyMarket(Volume);
			else if (bearish)
				SellMarket(Volume);
		}
	}
}
