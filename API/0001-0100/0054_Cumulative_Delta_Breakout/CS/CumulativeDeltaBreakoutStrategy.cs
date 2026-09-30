using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades breakouts of the running aggressor-side cumulative delta beyond its prior lookback range.
/// Fully exits when the cumulative delta crosses zero or actual-fill protection triggers.
/// </summary>
public class CumulativeDeltaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private readonly Queue<decimal> _priorCumulative = new();
	private decimal _cumulativeDelta;
	private decimal? _previousCumulative;
	private Order _pendingOrder;

	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public CumulativeDeltaBreakoutStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20).SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Number of prior cumulative delta values", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Trade-built signal candles", "General");
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
		_priorCumulative.Clear();
		_cumulativeDelta = 0m;
		_previousCumulative = null;
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
		// Archived minute candles do not always contain sided volumes; build bars from signed trades.
		var candles = new Subscription(CandleType, Security);
		candles.MarketData.BuildMode = MarketDataBuildModes.Build;
		candles.MarketData.BuildFrom = DataType.Ticks;
		SubscribeCandles(candles).Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection also watches the market between finished signal candles.
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var previous = _previousCumulative;
		_cumulativeDelta += (candle.BuyVolume ?? 0m) - (candle.SellVolume ?? 0m);
		var full = _priorCumulative.Count == LookbackPeriod;
		var up = full && _cumulativeDelta > _priorCumulative.Max();
		var down = full && _cumulativeDelta < _priorCumulative.Min();
		if (full)
			_priorCumulative.Dequeue();
		_priorCumulative.Enqueue(_cumulativeDelta);
		_previousCumulative = _cumulativeDelta;
		if (!IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m && previous is decimal priorLong && priorLong >= 0m && _cumulativeDelta < 0m)
			SellMarket(Position);
		else if (Position < 0m && previous is decimal priorShort && priorShort <= 0m && _cumulativeDelta > 0m)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m)
		{
			if (up) BuyMarket(Volume);
			else if (down) SellMarket(Volume);
		}
	}
}
