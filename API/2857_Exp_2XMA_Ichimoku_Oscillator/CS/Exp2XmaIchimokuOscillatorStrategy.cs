namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Exp 2XMA Ichimoku Oscillator strategy (simplified).
/// Uses two SMAs of different periods as a crossover oscillator.
/// Buys when fast SMA crosses above slow SMA, sells when fast crosses below.
/// </summary>
public class Exp2XmaIchimokuOscillatorStrategy : Strategy
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

	public Exp2XmaIchimokuOscillatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");

		_fastPeriod = Param(nameof(FastPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast moving average length", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 80)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow moving average length", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new SimpleMovingAverage { Length = FastPeriod };
		var slowMa = new SimpleMovingAverage { Length = SlowPeriod };

		decimal prevFast = 0, prevSlow = 0;
		bool prevInitialized = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, (ICandleMessage candle, decimal fastValue, decimal slowValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevFast = fastValue;
					prevSlow = slowValue;
					prevInitialized = true;
					return;
				}

				if (!prevInitialized)
				{
					prevFast = fastValue;
					prevSlow = slowValue;
					prevInitialized = true;
					return;
				}

				// Buy on bullish crossover
				if (prevFast <= prevSlow && fastValue > slowValue && Position <= 0)
				{
					BuyMarket();
				}
				// Sell on bearish crossover
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
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}
}
