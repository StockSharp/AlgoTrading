using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades strict breaks of prior rolling OBV extrema with matching price direction.
/// Fully exits on any actual OBV/OBV-SMA crossing or actual-fill percent protection.
/// </summary>
public class ObvBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _obvMaPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _priorObvs = new();
	private SimpleMovingAverage _obvAverage;
	private decimal? _previousPrice;
	private decimal? _previousReadyObv;
	private decimal _previousMean;
	private Order _pendingOrder;

	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public int OBVMAPeriod { get => _obvMaPeriod.Value; set => _obvMaPeriod.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ObvBreakoutStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20).SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Number of PRIOR finished OBV values in the breakout range", "Entry");
		_obvMaPeriod = Param(nameof(OBVMAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("OBV MA Period", "Current-inclusive SMA length for OBV, not price", "Indicators")
			.SetOptimize(10, 50, 10);
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearSignalState();
		_obvAverage = null;
	}

	private void ClearSignalState()
	{
		_priorObvs.Clear();
		_previousPrice = null;
		_previousReadyObv = null;
		_previousMean = 0m;
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
		var obv = new OnBalanceVolume();
		_obvAverage = new SimpleMovingAverage { Length = OBVMAPeriod };
		Indicators.Add(_obvAverage);
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(obv, ProcessCandle, false).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue obvValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The native SMA averages OBV itself, including warmup and pending-order bars.
		var meanValue = _obvAverage.Process(obvValue);
		var currentObv = obvValue.GetValue<decimal>();
		var priorPrice = _previousPrice;
		_previousPrice = candle.ClosePrice;

		// Capture the PRIOR range before inserting the breakout candle.
		var rangeReady = _priorObvs.Count == LookbackPeriod;
		var upper = rangeReady ? _priorObvs.Max() : 0m;
		var lower = rangeReady ? _priorObvs.Min() : 0m;
		_priorObvs.Enqueue(currentObv);
		while (_priorObvs.Count > LookbackPeriod)
			_priorObvs.Dequeue();

		if (!obvValue.Indicator.IsFormed || !_obvAverage.IsFormed)
			return;
		var mean = meanValue.GetValue<decimal>();
		var upwardCross = _previousReadyObv is decimal up && up <= _previousMean && currentObv > mean;
		var downwardCross = _previousReadyObv is decimal down && down >= _previousMean && currentObv < mean;
		// Zero OBV/mean are valid values, not an uninitialized sentinel.
		_previousReadyObv = currentObv;
		_previousMean = mean;

		if (!IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position != 0m && (upwardCross || downwardCross))
		{
			if (Position > 0m) SellMarket(Position);
			else BuyMarket(Math.Abs(Position));
		}
		else if (Position == 0m && rangeReady && priorPrice is decimal previousClose)
		{
			if (currentObv > upper && candle.ClosePrice > previousClose) BuyMarket(Volume);
			else if (currentObv < lower && candle.ClosePrice < previousClose) SellMarket(Volume);
		}
	}
}
