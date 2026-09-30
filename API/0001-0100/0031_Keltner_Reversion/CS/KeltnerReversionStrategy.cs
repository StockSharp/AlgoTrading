using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on mean reversion using Keltner Channels.
/// Enters against outside closes confirmed by RSI, exits on return inside, protects actual fills with frozen ATR.
/// </summary>
public class KeltnerReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private Order _pendingOrder;
	private Unit _stopDistance;
	private bool _protectionStarted;

	/// <summary>
	/// Period for EMA calculation (middle band) (default: 20)
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Period for ATR calculation (default: 14)
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for Keltner Channel width (default: 2.0)
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Period of the RSI confirming entries (default: 14)
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level below which a long entry is confirmed (default: 30)
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI level above which a short entry is confirmed (default: 70)
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop-loss calculation (default: 2.0)
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the Keltner Reversion strategy
	/// </summary>
	public KeltnerReversionStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20).SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA calculation (middle band)", "Technical Parameters")
			
			.SetOptimize(10, 50, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR lookback for channel width and protection", "Technical Parameters")
			
			.SetOptimize(7, 21, 7);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m).SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel width", "Technical Parameters")
			
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of the RSI confirming entries", "Technical Parameters")
			.SetOptimize(7, 21, 7);

		_rsiOversold = Param(nameof(RsiOversold), 30m).SetRange(0m, 100m)
			.SetDisplay("RSI Oversold", "Long entries below the lower band require RSI below this level", "Technical Parameters");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m).SetRange(0m, 100m)
			.SetDisplay("RSI Overbought", "Short entries above the upper band require RSI above this level", "Technical Parameters");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2.0m).SetNotNegative()
			.SetDisplay("ATR Multiplier (Stop Loss)", "Frozen entry ATR stop distance; zero disables protection", "Risk Management")
			
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Technical Parameters");

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

		// Create indicators
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, atr, rsi, ProcessCandle, false)
			.Start();

		// Configure chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, atr);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Process candle and check for Keltner Channel signals
	/// </summary>
	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before the callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue atrValue, IIndicatorValue rsiValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!emaValue.IsFormed || !atrValue.IsFormed || !rsiValue.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		var ema = emaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var upperBand = ema + atr * AtrMultiplier;
		var lowerBand = ema - atr * AtrMultiplier;
		if (Position > 0m && candle.ClosePrice >= lowerBand)
			SellMarket(Position);
		else if (Position < 0m && candle.ClosePrice <= upperBand)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && candle.ClosePrice < lowerBand && rsi < RsiOversold)
			Enter(Sides.Buy, atr);
		else if (Position == 0m && candle.ClosePrice > upperBand && rsi > RsiOverbought)
			Enter(Sides.Sell, atr);
	}

	private void Enter(Sides side, decimal atr)
	{
		var distance = atr * StopLossAtrMultiplier;
		_stopDistance ??= new Unit(distance);
		// Update the same Unit retained by native cached controllers between flat entries.
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
			Comment = "Keltner reversion entry",
		});
	}
}
