namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// EA Objprop Chart Id strategy: Standard Deviation breakout.
/// Buys when StdDev crosses above threshold with bullish candle, sells on bearish cross.
/// </summary>
public class EaObjpropChartIdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevStdDev;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public EaObjpropChartIdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "StdDev period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var stdDev = new StandardDeviation { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(stdDev, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdDevValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev && stdDevValue > 0)
		{
			var expanding = stdDevValue > _prevStdDev;
			var bullish = candle.ClosePrice > candle.OpenPrice;
			var bearish = candle.ClosePrice < candle.OpenPrice;

			if (expanding && bullish && Position <= 0)
				BuyMarket();
			else if (expanding && bearish && Position >= 0)
				SellMarket();
		}

		_prevStdDev = stdDevValue;
		_hasPrev = true;
	}
}
