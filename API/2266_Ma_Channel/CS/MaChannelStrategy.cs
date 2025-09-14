using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average channel breakout strategy.
/// Buys when price crosses above the upper channel and sells when price crosses below the lower channel.
/// </summary>
public class MaChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _offset;
	private readonly StrategyParam<DataType> _candleType;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Offset { get => _offset.Value; set => _offset.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaChannelStrategy()
	{
		_length = Param(nameof(Length), 8)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Moving average period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_offset = Param(nameof(Offset), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Offset", "Price offset from the average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5m, 20m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create moving averages for channel boundaries
		var maHigh = new ExponentialMovingAverage { Length = Length };
		var maLow = new ExponentialMovingAverage { Length = Length };

		// Trend state: +1 for uptrend, -1 for downtrend
		var trend = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(maHigh, maLow, (candle, highMa, lowMa) =>
			{
				// Process only finished candles
				if (candle.State != CandleStates.Finished)
					return;

				// Ensure trading is allowed and data is ready
				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				// Calculate upper and lower channel values
				var upper = highMa + Offset;
				var lower = lowMa - Offset;

				var prevTrend = trend;

				// Update trend depending on price position
				if (candle.HighPrice > upper)
					trend = +1;
				else if (candle.LowPrice < lower)
					trend = -1;

				// Generate trading signals when trend changes
				if (prevTrend <= 0 && trend > 0)
				{
					// Enter long or close short position
					if (Position <= 0)
						BuyMarket(Volume + Math.Abs(Position));
				}
				else if (prevTrend >= 0 && trend < 0)
				{
					// Enter short or close long position
					if (Position >= 0)
						SellMarket(Volume + Math.Abs(Position));
				}
			})
			.Start();
	}
}
