namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// SpaceX Delete SL/TP strategy: Standard Deviation breakout.
/// Buys when price breaks above upper StdDev band, sells on break below lower.
/// </summary>
public class SpaceXDeleteStopLossTakeProfitButtonStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _stdDevPeriod;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int StdDevPeriod { get => _stdDevPeriod.Value; set => _stdDevPeriod.Value = value; }

	public SpaceXDeleteStopLossTakeProfitButtonStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period for baseline", "Indicators");
		_stdDevPeriod = Param(nameof(StdDevPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Period", "Standard Deviation period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var stdDev = new StandardDeviation { Length = StdDevPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, stdDev, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdDevValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var upper = smaValue + 2 * stdDevValue;
		var lower = smaValue - 2 * stdDevValue;

		if (candle.ClosePrice > upper && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice < lower && Position >= 0)
			SellMarket();
	}
}
