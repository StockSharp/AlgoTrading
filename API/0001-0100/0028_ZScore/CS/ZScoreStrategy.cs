using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Z-Score indicator for mean reversion trading.
/// Z-Score measures the distance from the price to its moving average in standard deviations.
/// </summary>
public class ZScoreStrategy : Strategy
{
	private readonly StrategyParam<decimal> _zScoreEntryThreshold;
	private readonly StrategyParam<decimal> _zScoreExitThreshold;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private Order _pendingOrder;

	/// <summary>
	/// Z-Score threshold for entry (default: 2.0)
	/// </summary>
	public decimal ZScoreEntryThreshold
	{
		get => _zScoreEntryThreshold.Value;
		set => _zScoreEntryThreshold.Value = value;
	}

	/// <summary>
	/// Half-width of the neutral exit zone (default: 0).
	/// </summary>
	public decimal ZScoreExitThreshold
	{
		get => _zScoreExitThreshold.Value;
		set => _zScoreExitThreshold.Value = value;
	}

	/// <summary>
	/// Period for Moving Average calculation (default: 20)
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Period for Standard Deviation calculation (default: 20)
	/// </summary>
	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
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
	/// Initialize the Z-Score strategy.
	/// </summary>
	public ZScoreStrategy()
	{
		_zScoreEntryThreshold = Param(nameof(ZScoreEntryThreshold), 2m).SetGreaterThanZero()
			.SetDisplay("Z-Score Entry", "Distance from mean in std devs for entry", "Z-Score")
			.SetOptimize(1.5m, 3.0m, 0.5m);

		_zScoreExitThreshold = Param(nameof(ZScoreExitThreshold), 0m).SetNotNegative()
			.SetDisplay("Z-Score Exit", "Half-width of the neutral exit zone in standard deviations", "Z-Score")
			.SetOptimize(0.0m, 1.0m, 0.2m);

		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average", "Indicators")
			.SetOptimize(10, 50, 5);

		_stdDevPeriod = Param(nameof(StdDevPeriod), 20).SetGreaterThanZero()
			.SetDisplay("StdDev Period", "Period for Standard Deviation", "Indicators")
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

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
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (ZScoreExitThreshold >= ZScoreEntryThreshold)
			throw new InvalidOperationException("ZScoreExitThreshold must be below ZScoreEntryThreshold.");

		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var stdDev = new StandardDeviation { Length = StdDevPeriod };
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, stdDev, ProcessCandle, false)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, stdDev);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before the callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue stdDevValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maValue.IsFormed || !stdDevValue.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		var sigma = stdDevValue.GetValue<decimal>();
		// A constant-price variance window is explicitly neutral, not an exit blackout.
		var zScore = sigma == 0m ? 0m : (candle.ClosePrice - maValue.GetValue<decimal>()) / sigma;
		if (Position > 0m && zScore >= -ZScoreExitThreshold)
			SellMarket(Position);
		else if (Position < 0m && zScore <= ZScoreExitThreshold)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && zScore < -ZScoreEntryThreshold)
			BuyMarket(Volume);
		else if (Position == 0m && zScore > ZScoreEntryThreshold)
			SellMarket(Volume);
	}
}
