using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fades a directional outside close against separately configured EMA/ATR bands.
/// Exits at the EMA or native actual-fill protection with frozen signal ATR distance.
/// </summary>
public class KeltnerChannelReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private Order _pendingOrder;
	private Unit _stopDistance;
	private bool _protectionStarted;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal StopLossAtrMultiplier { get => _stopLossAtrMultiplier.Value; set => _stopLossAtrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public KeltnerChannelReversalStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20).SetGreaterThanZero()
			.SetDisplay("EMA Period", "Close EMA length for middle band", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR length for channel width and stop", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Multiplier", "ATR multiple for channel width", "Indicators");
		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2m).SetNotNegative()
			.SetDisplay("Stop ATR Multiplier", "Frozen entry ATR distance; zero disables the stop", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Keltner and stop candle timeframe", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_pendingOrder = null;
		_stopDistance = null;
		_protectionStarted = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_pendingOrder = null;
		_stopDistance = null;
		_protectionStarted = false;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var candles = SubscribeCandles(CandleType);
		candles.BindEx(ema, atr, ProcessCandle, false).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, ema);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection evaluates executable quotes between finished signal candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished || !emaValue.IsFormed ||
			!atrValue.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;
		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		var middle = emaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var lower = middle - atr * AtrMultiplier;
		var upper = middle + atr * AtrMultiplier;
		var close = candle.ClosePrice;
		if (Position > 0m && close >= middle)
			SellMarket(Position);
		else if (Position < 0m && close <= middle)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && close < lower && close > candle.OpenPrice)
			Enter(Sides.Buy, atr);
		else if (Position == 0m && close > upper && close < candle.OpenPrice)
			Enter(Sides.Sell, atr);
	}

	private void Enter(Sides side, decimal atr)
	{
		var distance = atr * StopLossAtrMultiplier;
		_stopDistance ??= new Unit(distance);
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
			Comment = "Keltner reversal entry",
		});
	}
}
