namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Template M5 Envelopes strategy: WMA envelope breakout.
/// Buys above upper envelope, sells below lower envelope.
/// </summary>
public class TemplateM5EnvelopesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<decimal> _deviation;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int WmaPeriod { get => _wmaPeriod.Value; set => _wmaPeriod.Value = value; }
	public decimal Deviation { get => _deviation.Value; set => _deviation.Value = value; }

	public TemplateM5EnvelopesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_wmaPeriod = Param(nameof(WmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("WMA Period", "Weighted MA period", "Indicators");
		_deviation = Param(nameof(Deviation), 0.1m)
			.SetDisplay("Deviation %", "Envelope deviation percent", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var wma = new WeightedMovingAverage { Length = WmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wmaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var upper = wmaValue * (1 + Deviation / 100m);
		var lower = wmaValue * (1 - Deviation / 100m);

		if (close > upper && Position <= 0)
			BuyMarket();
		else if (close < lower && Position >= 0)
			SellMarket();
	}
}
