namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// 5 Mins Envelopes strategy: envelope breakout using SMA with deviation bands.
/// Buys when price crosses above upper envelope, sells when below lower envelope.
/// </summary>
public class FiveMinsEnvelopesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _deviation;
	private bool _wasAboveUpper;
	private bool _wasBelowLower;
	private bool _hasPrevSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public decimal Deviation { get => _deviation.Value; set => _deviation.Value = value; }

	public FiveMinsEnvelopesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Indicators");
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
		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var upper = smaValue * (1 + Deviation / 100m);
		var lower = smaValue * (1 - Deviation / 100m);
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
