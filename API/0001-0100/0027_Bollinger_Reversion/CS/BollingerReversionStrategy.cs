using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Bollinger Bands mean reversion.
/// Enters against outside closes, exits on return inside, protects actual fills with frozen ATR.
/// </summary>
public class BollingerReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;

	private Order _pendingOrder;
	private Unit _stopDistance;
	private bool _protectionStarted;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerReversionStrategy"/>.
	/// </summary>
	public BollingerReversionStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetRange(5, 50)
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetRange(0.5m, 4m)
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR lookback for local protection.", "Protection");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Multiplier", "Frozen signal ATR distance; zero disables the stop.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pendingOrder = null;
		_stopDistance = null;
		_protectionStarted = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollingerBands = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};
		var atr = new AverageTrueRange { Length = AtrPeriod };
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollingerBands, atr, ProcessCandle, false)
			.Start();

		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollingerBands);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before the callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !atrValue.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		var bb = (IBollingerBandsValue)bollingerValue;

		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower)
			return;

		var close = candle.ClosePrice;

		// Return inside the entry-side band exits, without waiting for the middle band.
		if (Position > 0m && close >= lower)
			SellMarket(Position);
		else if (Position < 0m && close <= upper)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && close < lower)
			Enter(Sides.Buy, atrValue.GetValue<decimal>());
		else if (Position == 0m && close > upper)
			Enter(Sides.Sell, atrValue.GetValue<decimal>());
	}

	private void Enter(Sides side, decimal atr)
	{
		var distance = atr * AtrMultiplier;
		_stopDistance ??= new Unit(distance);
		// Flat entries update the SAME Unit retained by native cached position controllers.
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
			Comment = "Bollinger reversion entry",
		});
	}
}
