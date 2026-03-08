using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Hull Moving Average with standard deviation filter.
/// Opens positions when price deviates from the HMA by a defined multiplier.
/// </summary>
public class ColorHmaStDevStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _stdPeriod;
	private readonly StrategyParam<decimal> _k1;
	private readonly StrategyParam<DataType> _candleType;

	public int HmaPeriod { get => _hmaPeriod.Value; set => _hmaPeriod.Value = value; }
	public int StdPeriod { get => _stdPeriod.Value; set => _stdPeriod.Value = value; }
	public decimal K1 { get => _k1.Value; set => _k1.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorHmaStDevStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 13)
			.SetDisplay("HMA Period", "Hull Moving Average period", "Indicators")
			.SetOptimize(5, 30, 2);

		_stdPeriod = Param(nameof(StdPeriod), 9)
			.SetDisplay("StdDev Period", "Standard deviation period", "Indicators")
			.SetOptimize(5, 20, 1);

		_k1 = Param(nameof(K1), 0.5m)
			.SetDisplay("Entry Multiplier", "Deviation multiplier for entry", "Parameters")
			.SetOptimize(0.5m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to subscribe", "Common");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var hma = new HullMovingAverage { Length = HmaPeriod };
		var std = new StandardDeviation { Length = StdPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hma, std, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stdValue == 0)
			return;

		var upperEntry = hmaValue + K1 * stdValue;
		var lowerEntry = hmaValue - K1 * stdValue;

		if (candle.ClosePrice > upperEntry && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice < lowerEntry && Position >= 0)
			SellMarket();
	}
}
