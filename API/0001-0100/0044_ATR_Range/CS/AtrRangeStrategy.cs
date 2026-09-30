using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades strict N-interval Close movement beyond current Wilder ATR at every Nth bar.
/// Exits on any ready bar's adverse SMA crossing or frozen entry-ATR actual-fill protection.
/// </summary>
public class AtrRangeStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _closes = new();
	private long _barCount;
	private decimal? _previousClose;
	private decimal _previousMean;
	private Order _pendingOrder;
	private Unit _stopDistance;
	private bool _protectionStarted;

	public int MAPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int ATRPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AtrRangeStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 10);
		_atrPeriod = Param(nameof(ATRPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetOptimize(7, 28, 7);
		_lookbackPeriod = Param(nameof(LookbackPeriod), 5).SetGreaterThanZero()
			.SetDisplay("Lookback Period", "N intervals for movement and N-bar entry-check cadence", "Entry")
			.SetOptimize(3, 10, 1);
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Stop Multiplier", "Frozen entry ATR distance; zero disables only protection", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	private void ResetState()
	{
		_closes.Clear();
		_barCount = 0;
		_previousClose = null;
		_previousMean = default;
		_pendingOrder = null;
		_stopDistance = null;
		_protectionStarted = false;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetState();
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
		if (candle.State != CandleStates.Finished)
			return;
		_barCount++;
		_closes.Enqueue(candle.ClosePrice);
		if (_closes.Count > LookbackPeriod + 1) _closes.Dequeue();
		if (!maValue.Indicator.IsFormed || !atrValue.Indicator.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;
		var close = candle.ClosePrice;
		var mean = maValue.GetValue<decimal>();
		var downwardCross = _previousClose is decimal down && down >= _previousMean && close < mean;
		var upwardCross = _previousClose is decimal up && up <= _previousMean && close > mean;
		_previousClose = close;
		_previousMean = mean;
		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		// Exits run on every ready bar, independent of the entry checkpoint.
		if (Position > 0m && downwardCross)
			SellMarket(Position);
		else if (Position < 0m && upwardCross)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && _barCount % LookbackPeriod == 0 && _closes.Count == LookbackPeriod + 1)
		{
			// N intervals need N+1 closes, not N closes in a non-overlapping block.
			var movement = close - _closes.Peek();
			var atr = atrValue.GetValue<decimal>();
			if (Math.Abs(movement) > atr)
				Enter(movement > 0m ? Sides.Buy : Sides.Sell, atr);
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
			Comment = "ATR range entry",
		});
	}
}
