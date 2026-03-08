using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on averaged RSI values.
/// Uses RSI smoothed by SMA to generate signals.
/// </summary>
public class AutoTradeWithRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _averagePeriod;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _rsiAvg;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int AveragePeriod { get => _averagePeriod.Value; set => _averagePeriod.Value = value; }
	public decimal BuyThreshold { get => _buyThreshold.Value; set => _buyThreshold.Value = value; }
	public decimal SellThreshold { get => _sellThreshold.Value; set => _sellThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AutoTradeWithRsiStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicator");

		_averagePeriod = Param(nameof(AveragePeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Average Period", "SMA period to smooth RSI", "Indicator");

		_buyThreshold = Param(nameof(BuyThreshold), 55m)
			.SetDisplay("Buy Threshold", "Averaged RSI above which to buy", "Rules");

		_sellThreshold = Param(nameof(SellThreshold), 45m)
			.SetDisplay("Sell Threshold", "Averaged RSI below which to sell", "Rules");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle data type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_rsiAvg = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiAvg = new ExponentialMovingAverage { Length = AveragePeriod };
		Indicators.Add(_rsiAvg);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var area2 = CreateChartArea();
			if (area2 != null)
				DrawIndicator(area2, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed)
			return;

		var avgResult = _rsiAvg.Process(rsiValue);
		if (!avgResult.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var avgRsi = avgResult.GetValue<decimal>();

		if (avgRsi > BuyThreshold && Position <= 0)
			BuyMarket();
		else if (avgRsi < SellThreshold && Position >= 0)
			SellMarket();
	}
}
