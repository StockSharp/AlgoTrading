namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Absolutely No Lag LWMA strategy (simplified).
/// Uses two EMAs of different periods to detect trend direction changes.
/// </summary>
public class AbsolutelyNoLagLwmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

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

	public AbsolutelyNoLagLwmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");

		_fastPeriod = Param(nameof(FastPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		decimal prevFast = 0, prevSlow = 0;
		bool hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, (ICandleMessage candle, decimal fastValue, decimal slowValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevFast = fastValue;
					prevSlow = slowValue;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevFast = fastValue;
					prevSlow = slowValue;
					return;
				}

				if (prevFast <= prevSlow && fastValue > slowValue && Position <= 0)
				{
					BuyMarket();
				}
				else if (prevFast >= prevSlow && fastValue < slowValue && Position >= 0)
				{
					SellMarket();
				}

				prevFast = fastValue;
				prevSlow = slowValue;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}
}
