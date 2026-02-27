namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Smart AC Trader: EMA trend + ROC momentum filter.
/// Buys when fast EMA above slow EMA and ROC positive.
/// Sells when fast EMA below slow EMA and ROC negative.
/// </summary>
public class SmartAcTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rocPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int RocPeriod
	{
		get => _rocPeriod.Value;
		set => _rocPeriod.Value = value;
	}

	public SmartAcTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_rocPeriod = Param(nameof(RocPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("ROC Period", "Rate of Change period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var roc = new RateOfChange { Length = RocPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, roc, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal roc)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (fast > slow && roc > 0 && Position <= 0)
			BuyMarket();
		else if (fast < slow && roc < 0 && Position >= 0)
			SellMarket();
	}
}
