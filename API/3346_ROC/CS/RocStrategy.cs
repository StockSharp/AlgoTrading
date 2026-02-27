namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Rate of Change strategy: uses ROC indicator with WMA trend filter.
/// Buys when ROC crosses above zero and fast WMA > slow WMA.
/// Sells when ROC crosses below zero and fast WMA less than slow WMA.
/// </summary>
public class RocStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rocPeriod;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;

	private decimal _prevRoc;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RocPeriod
	{
		get => _rocPeriod.Value;
		set => _rocPeriod.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public RocStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_rocPeriod = Param(nameof(RocPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("ROC Period", "Rate of Change period", "Indicators");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast WMA period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow WMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var roc = new RateOfChange { Length = RocPeriod };
		var fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
		var slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal roc, decimal fastMa, decimal slowMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevRoc = roc;
			_hasPrev = true;
			return;
		}

		// Buy: ROC crosses above zero + uptrend
		if (_prevRoc <= 0 && roc > 0 && fastMa > slowMa && Position <= 0)
			BuyMarket();
		// Sell: ROC crosses below zero + downtrend
		else if (_prevRoc >= 0 && roc < 0 && fastMa < slowMa && Position >= 0)
			SellMarket();

		_prevRoc = roc;
	}
}
