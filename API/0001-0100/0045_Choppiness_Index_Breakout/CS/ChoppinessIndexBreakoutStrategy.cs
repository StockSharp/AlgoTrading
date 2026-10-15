using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CHOP = 100 log10(sum(TR, N) / (highest High - lowest Low)) / log10(N).
/// Trades low-index price/SMA levels and fully exits at a high index or percent stop.
/// </summary>
public class ChoppinessIndexBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _choppinessPeriod;
	private readonly StrategyParam<decimal> _choppinessThreshold;
	private readonly StrategyParam<decimal> _highChoppinessThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private Sum _rangeSum;
	private Order _pendingOrder;

	public int MAPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int ChoppinessPeriod { get => _choppinessPeriod.Value; set => _choppinessPeriod.Value = value; }
	public decimal ChoppinessThreshold { get => _choppinessThreshold.Value; set => _choppinessThreshold.Value = value; }
	public decimal HighChoppinessThreshold { get => _highChoppinessThreshold.Value; set => _highChoppinessThreshold.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ChoppinessIndexBreakoutStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 10);
		_choppinessPeriod = Param(nameof(ChoppinessPeriod), 14).SetRange(2, int.MaxValue)
			.SetDisplay("Choppiness Period", "Period for Choppiness Index calculation", "Indicators")
			.SetOptimize(10, 30, 5);
		_choppinessThreshold = Param(nameof(ChoppinessThreshold), 38.2m).SetRange(0m, 100m)
			.SetDisplay("Choppiness Threshold", "Threshold below which market is trending", "Entry")
			.SetOptimize(30m, 50m, 5m);
		_highChoppinessThreshold = Param(nameof(HighChoppinessThreshold), 61.8m).SetRange(0m, 100m)
			.SetDisplay("High Choppiness", "Threshold above which to exit positions", "Exit")
			.SetOptimize(55m, 75m, 5m);
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
		_rangeSum = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		if (ChoppinessThreshold >= HighChoppinessThreshold)
			throw new ArgumentException("ChoppinessThreshold must be less than HighChoppinessThreshold.");
		base.OnStarted2(time);
		_pendingOrder = null;
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var ma = new SimpleMovingAverage { Length = MAPeriod };
		var highest = new Highest { Length = ChoppinessPeriod };
		var lowest = new Lowest { Length = ChoppinessPeriod };
		var tr = new AverageTrueRange { Length = 1 };
		_rangeSum = new Sum { Length = ChoppinessPeriod };
		Indicators.Add(_rangeSum);
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(ma, highest, lowest, tr, ProcessCandle, false).Start();
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue highValue, IIndicatorValue lowValue, IIndicatorValue trValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		// Compose the documented formula locally; StockSharp platform code is unchanged.
		var sumValue = _rangeSum.Process(trValue);
		if (!maValue.Indicator.IsFormed || !highValue.Indicator.IsFormed || !lowValue.Indicator.IsFormed
			|| !_rangeSum.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;
		var width = highValue.GetValue<decimal>() - lowValue.GetValue<decimal>();
		var sumTr = sumValue.GetValue<decimal>();
		// A collapsed range has no defined logarithmic index, even if all windows are formed.
		if (width <= 0m || sumTr <= 0m)
			return;
		var choppiness = 100m * (decimal)Math.Log10((double)(sumTr / width)) / (decimal)Math.Log10(ChoppinessPeriod);
		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position != 0m && choppiness > HighChoppinessThreshold)
		{
			if (Position > 0m) SellMarket(Position);
			else BuyMarket(Math.Abs(Position));
		}
		else if (Position == 0m && choppiness < ChoppinessThreshold)
		{
			// Preserve the formal README's below-threshold LEVEL, not an extra crossing rule.
			var mean = maValue.GetValue<decimal>();
			if (candle.ClosePrice > mean) BuyMarket(Volume);
			else if (candle.ClosePrice < mean) SellMarket(Volume);
		}
	}
}
