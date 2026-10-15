using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fades an outside Bollinger close only when the candle turns against the extension.
/// Exits at the middle band or native actual-fill protection with signal-bar ATR distance.
/// </summary>
public class BollingerBandReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private Order _pendingOrder;
	private Unit _stopDistance;
	private bool _protectionStarted;

	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BollingerBandReversalStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20).SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Close SMA and population deviation length", "Indicators");
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m).SetNotNegative()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR length", "Protection");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Multiplier", "Frozen signal ATR distance; zero disables the stop", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Bollinger and ATR candle timeframe", "General");
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
		var bands = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var candles = SubscribeCandles(CandleType);
		candles.BindEx(bands, atr, ProcessCandle, false).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, bands);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection evaluates executable quotes between finished signal candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bandsValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished || !bandsValue.IsFormed ||
			!atrValue.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;
		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		var bands = (IBollingerBandsValue)bandsValue;
		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower ||
			bands.MovingAverage is not decimal middle)
			return;

		var close = candle.ClosePrice;
		if (Position > 0m && close >= middle)
			SellMarket(Position);
		else if (Position < 0m && close <= middle)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && close < lower && close > candle.OpenPrice)
			Enter(Sides.Buy, atrValue.GetValue<decimal>());
		else if (Position == 0m && close > upper && close < candle.OpenPrice)
			Enter(Sides.Sell, atrValue.GetValue<decimal>());
	}

	private void Enter(Sides side, decimal atr)
	{
		var distance = atr * AtrMultiplier;
		_stopDistance ??= new Unit(distance);
		// Cached native controllers retain this Unit across flat-to-position cycles.
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
			Comment = "Bollinger reversal entry",
		});
	}
}
