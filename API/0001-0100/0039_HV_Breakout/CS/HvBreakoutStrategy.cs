using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakouts beyond SMA-anchored levels widened by relative population price dispersion (StdDev / Close).
/// Exits on an adverse price/SMA crossing or native actual-fill percent protection.
/// </summary>
public class HvBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _hvPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousClose;
	private decimal _previousMean;
	private Order _pendingOrder;

	public int HvPeriod { get => _hvPeriod.Value; set => _hvPeriod.Value = value; }
	public int MAPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public HvBreakoutStrategy()
	{
		_hvPeriod = Param(nameof(HvPeriod), 20).SetGreaterThanZero()
			.SetDisplay("HV Period", "Period for Historical Volatility calculation", "Indicators")
			.SetOptimize(10, 30, 5);
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for the Moving Average that anchors the breakout levels and exits", "Indicators")
			.SetOptimize(10, 50, 5);
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
		_previousClose = null;
		_previousMean = default;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousClose = null;
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var deviation = new StandardDeviation { Length = HvPeriod };
		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(deviation, sma, ProcessCandle, false).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, deviation);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue deviationValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished || !deviationValue.Indicator.IsFormed || !smaValue.Indicator.IsFormed
			|| !IsFormedAndOnlineAndAllowTrading() || candle.ClosePrice <= 0m)
			return;
		var close = candle.ClosePrice;
		var mean = smaValue.GetValue<decimal>();
		if (_previousClose is not decimal prior)
		{
			_previousClose = close;
			_previousMean = mean;
			return;
		}
		var upwardCross = prior <= _previousMean && close > mean;
		var downwardCross = prior >= _previousMean && close < mean;
		_previousClose = close;
		_previousMean = mean;
		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m && downwardCross)
			SellMarket(Position);
		else if (Position < 0m && upwardCross)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m)
		{
			// HV is the relative price dispersion StdDev / Close.
			var relativeDeviation = deviationValue.GetValue<decimal>() / close;
			if (close > mean * (1m + relativeDeviation))
				BuyMarket(Volume);
			else if (close < mean * (1m - relativeDeviation))
				SellMarket(Volume);
		}
	}
}
