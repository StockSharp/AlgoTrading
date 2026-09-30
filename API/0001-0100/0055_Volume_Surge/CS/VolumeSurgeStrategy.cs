using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades strict current-volume surges above a rolling volume SMA in price/SMA direction.
/// Fully exits below the volume mean or through actual-fill percent protection.
/// </summary>
public class VolumeSurgeStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<decimal> _volumeSurgeMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeAverage;
	private Order _pendingOrder;

	public int MAPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int VolumeAvgPeriod { get => _volumeAvgPeriod.Value; set => _volumeAvgPeriod.Value = value; }
	public decimal VolumeSurgeMultiplier { get => _volumeSurgeMultiplier.Value; set => _volumeSurgeMultiplier.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolumeSurgeStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 10);
		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20).SetGreaterThanZero()
			.SetDisplay("Volume Average Period", "Current-inclusive rolling TotalVolume SMA length", "Indicators");
		_volumeSurgeMultiplier = Param(nameof(VolumeSurgeMultiplier), 2m).SetGreaterThanZero()
			.SetDisplay("Volume Surge Multiplier", "Current volume must strictly exceed volume SMA times multiplier", "Entry")
			.SetOptimize(1.5m, 3m, 0.5m);
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
		_volumeAverage = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_pendingOrder = null;
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		_volumeAverage = new SimpleMovingAverage { Length = VolumeAvgPeriod, Name = "Volume average" };
		Indicators.Add(_volumeAverage);
		var ma = new SimpleMovingAverage { Length = MAPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(ma, ProcessCandle, false).Start();
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		// Feed actual volume, not Close or the preceding candle, including during price-SMA warmup.
		var volumeValue = _volumeAverage.Process(new DecimalIndicatorValue(_volumeAverage, candle.TotalVolume, candle.OpenTime) { IsFinal = true });
		if (!maValue.Indicator.IsFormed || !_volumeAverage.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;
		var average = volumeValue.GetValue<decimal>();
		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position != 0m && candle.TotalVolume < average)
		{
			if (Position > 0m) SellMarket(Position);
			else BuyMarket(Math.Abs(Position));
		}
		else if (Position == 0m && average > 0m && candle.TotalVolume > average * VolumeSurgeMultiplier)
		{
			var mean = maValue.GetValue<decimal>();
			if (candle.ClosePrice > mean) BuyMarket(Volume);
			else if (candle.ClosePrice < mean) SellMarket(Volume);
		}
	}
}
