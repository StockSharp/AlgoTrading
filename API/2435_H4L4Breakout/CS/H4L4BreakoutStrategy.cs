using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on calculated H4 and L4 levels.
/// When price range expands, places limit orders above and below to catch breakouts.
/// </summary>
public class H4L4BreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="H4L4BreakoutStrategy"/>.
	/// </summary>
	public H4L4BreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, (candle, ma) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var range = (candle.HighPrice - candle.LowPrice) * 1.1m / 2m;
				var h4 = candle.ClosePrice + range;
				var l4 = candle.ClosePrice - range;

				// Buy when price is below MA and near L4 level (mean reversion from below)
				// Sell when price is above MA and near H4 level (mean reversion from above)
				if (candle.ClosePrice < ma && candle.LowPrice <= l4 && Position <= 0)
				{
					BuyMarket();
				}
				else if (candle.ClosePrice > ma && candle.HighPrice >= h4 && Position >= 0)
				{
					SellMarket();
				}
			})
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}
}
