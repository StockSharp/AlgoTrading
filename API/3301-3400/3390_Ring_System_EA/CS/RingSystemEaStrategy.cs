namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Ring System EA strategy: ROC momentum with SMA filter.
/// Buys when ROC is positive and price above SMA, sells when ROC is negative and below SMA.
/// </summary>
public class RingSystemEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _rocPeriod;
	private bool _wasBullish;
	private bool _hasPrevSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int RocPeriod { get => _rocPeriod.Value; set => _rocPeriod.Value = value; }

	public RingSystemEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_smaPeriod = Param(nameof(SmaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA trend filter period", "Indicators");
		_rocPeriod = Param(nameof(RocPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ROC Period", "Rate of Change period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_wasBullish = false;
		_hasPrevSignal = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrevSignal = false;
		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var roc = new RateOfChange { Length = RocPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, roc, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal roc)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;
		var isBullish = roc > 0 && close > sma;

		if (_hasPrevSignal && isBullish != _wasBullish)
		{
			if (isBullish && Position <= 0)
				BuyMarket();
			else if (!isBullish && roc < 0 && close < sma && Position >= 0)
				SellMarket();
		}

		_wasBullish = isBullish;
		_hasPrevSignal = true;
	}
}
