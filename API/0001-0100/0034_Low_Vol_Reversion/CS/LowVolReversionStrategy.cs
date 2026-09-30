using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on mean reversion during periods of low volatility.
/// It identifies periods of low ATR and opens positions when price
/// deviates from its moving average, expecting a return to the mean.
/// </summary>
public class LowVolReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _atrLookbackPeriod;
	private readonly StrategyParam<decimal> _atrThresholdPercent;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private SimpleMovingAverage _atrAverage;
	private Order _pendingOrder;
	private Unit _stopDistance;
	private bool _protectionStarted;

	/// <summary>
	/// Period for Moving Average calculation.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Period for ATR calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for ATR average calculation.
	/// </summary>
	public int AtrLookbackPeriod
	{
		get => _atrLookbackPeriod.Value;
		set => _atrLookbackPeriod.Value = value;
	}

	/// <summary>
	/// ATR threshold as percentage of average ATR.
	/// </summary>
	public decimal AtrThresholdPercent
	{
		get => _atrThresholdPercent.Value;
		set => _atrThresholdPercent.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the stop-loss distance frozen at entry; zero disables the stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the Low Volatility Reversion strategy.
	/// </summary>
	public LowVolReversionStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetOptimize(7, 21, 7);

		_atrLookbackPeriod = Param(nameof(AtrLookbackPeriod), 20).SetGreaterThanZero()
			.SetDisplay("ATR Lookback", "Lookback period for ATR average calculation", "Indicators")
			.SetOptimize(10, 50, 10);

		_atrThresholdPercent = Param(nameof(AtrThresholdPercent), 50m).SetNotNegative()
			.SetDisplay("ATR Threshold %", "ATR threshold as percentage of average ATR", "Entry")
			.SetOptimize(30m, 90m, 10m);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Stop Multiplier", "Frozen entry ATR stop distance; zero disables it", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

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
		_atrAverage = null;
		_pendingOrder = null;
		_stopDistance = null;
		_protectionStarted = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atrAverage = new SimpleMovingAverage { Length = AtrLookbackPeriod, Name = "ATR rolling mean" };
		Indicators.Add(_atrAverage);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, atr, ProcessCandle, false)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished || !atrValue.Indicator.IsFormed)
			return;
		var atr = atrValue.GetValue<decimal>();
		// Feed only fully formed ATR samples; the current sample belongs to the rolling window.
		var average = _atrAverage.Process(new DecimalIndicatorValue(_atrAverage, atr, candle.OpenTime) { IsFinal = true }).GetValue<decimal>();
		if (!_atrAverage.IsFormed || !smaValue.Indicator.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;
		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		var mean = smaValue.GetValue<decimal>();
		// Quiet-market filtering never blocks an existing position's mean-touch exit.
		if (Position > 0m && candle.ClosePrice >= mean)
			SellMarket(Position);
		else if (Position < 0m && candle.ClosePrice <= mean)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && atr > 0m && atr < average * AtrThresholdPercent / 100m)
		{
			if (candle.ClosePrice < mean) Enter(Sides.Buy, atr);
			else if (candle.ClosePrice > mean) Enter(Sides.Sell, atr);
		}
	}

	private void Enter(Sides side, decimal atr)
	{
		var distance = atr * AtrMultiplier;
		_stopDistance ??= new Unit(distance);
		// Preserve the Unit reference retained by native cached protection controllers.
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
			Comment = "Low volatility entry",
		});
	}
}
