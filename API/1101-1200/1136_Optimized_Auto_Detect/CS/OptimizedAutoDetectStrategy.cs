using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OptimizedAutoDetectStrategy : Strategy
{
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	public int ShortMaPeriod { get => _shortMaPeriod.Value; set => _shortMaPeriod.Value = value; }
	public int LongMaPeriod { get => _longMaPeriod.Value; set => _longMaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OptimizedAutoDetectStrategy()
	{
		_shortMaPeriod = Param(nameof(ShortMaPeriod), 14).SetGreaterThanZero();
		_longMaPeriod = Param(nameof(LongMaPeriod), 40).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var shortSma = new SimpleMovingAverage { Length = ShortMaPeriod };
		var longSma = new SimpleMovingAverage { Length = LongMaPeriod };

		var prevS = 0m;
		var prevL = 0m;
		var init = false;
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(360);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortSma, longSma, (candle, s, l) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!shortSma.IsFormed || !longSma.IsFormed)
					return;

				if (!init)
				{
					prevS = s;
					prevL = l;
					init = true;
					return;
				}

				if (candle.OpenTime - lastSignal >= cooldown)
				{
					if (prevS <= prevL && s > l && Position <= 0)
					{
						BuyMarket();
						lastSignal = candle.OpenTime;
					}
					else if (prevS >= prevL && s < l && Position >= 0)
					{
						SellMarket();
						lastSignal = candle.OpenTime;
					}
				}

				prevS = s;
				prevL = l;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortSma);
			DrawIndicator(area, longSma);
			DrawOwnTrades(area);
		}
	}
}
