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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var roc = new RateOfChange { Length = RocPeriod };
		var fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
		var slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };

		decimal? prevRoc = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, fastMa, slowMa, (candle, rocVal, fastMaVal, slowMaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (prevRoc.HasValue)
				{
					if (prevRoc.Value <= 0 && rocVal > 0 && fastMaVal > slowMaVal && Position <= 0)
						BuyMarket();
					else if (prevRoc.Value >= 0 && rocVal < 0 && fastMaVal < slowMaVal && Position >= 0)
						SellMarket();
				}

				prevRoc = rocVal;
			})
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
}
