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
	private bool _wasAboveUpper;
	private bool _wasBelowLower;
	private bool _hasPrevSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int WmaPeriod { get => _wmaPeriod.Value; set => _wmaPeriod.Value = value; }
	public decimal Deviation { get => _deviation.Value; set => _deviation.Value = value; }

	public TemplateM5EnvelopesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_wmaPeriod = Param(nameof(WmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("WMA Period", "Weighted MA period", "Indicators");
		_deviation = Param(nameof(Deviation), 0.3m)
			.SetDisplay("Deviation %", "Envelope deviation percent", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_wasAboveUpper = false;
		_wasBelowLower = false;
		_hasPrevSignal = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrevSignal = false;
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
		var aboveUpper = close > upper;
		var belowLower = close < lower;

		if (_hasPrevSignal)
		{
			if (aboveUpper && !_wasAboveUpper && Position <= 0)
				BuyMarket();
			else if (belowLower && !_wasBelowLower && Position >= 0)
				SellMarket();
		}

		_wasAboveUpper = aboveUpper;
		_wasBelowLower = belowLower;
		_hasPrevSignal = true;
	}
}
