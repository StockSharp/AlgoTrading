namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Gonna Scalp strategy: WMA crossover with CCI momentum filter.
/// Buys when fast WMA above slow WMA and CCI above -100.
/// Sells when fast WMA below slow WMA and CCI below 100.
/// </summary>
public class GonnaScalpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _cciPeriod;

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

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public GonnaScalpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast WMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow WMA period", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new WeightedMovingAverage { Length = FastPeriod };
		var slow = new WeightedMovingAverage { Length = SlowPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		decimal? prevFast = null;
		decimal? prevSlow = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, cci, (candle, fastVal, slowVal, cciVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (prevFast.HasValue && prevSlow.HasValue)
				{
					var crossUp = prevFast.Value <= prevSlow.Value && fastVal > slowVal;
					var crossDown = prevFast.Value >= prevSlow.Value && fastVal < slowVal;

					if (crossUp && cciVal > -100m && Position <= 0)
						BuyMarket();
					else if (crossDown && cciVal < 100m && Position >= 0)
						SellMarket();
				}

				prevFast = fastVal;
				prevSlow = slowVal;
			})
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
}
