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
/// Strategy that captures the spread on FORTS by trading when spread is wide enough.
/// </summary>
public class HftSpreaderForFortsStrategy : Strategy
{
	private readonly StrategyParam<int> _spreadMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastBid;
	private decimal _lastAsk;

	/// <summary>
	/// Required spread in ticks to place both buy and sell orders.
	/// </summary>
	public int SpreadMultiplier
	{
		get => _spreadMultiplier.Value;
		set => _spreadMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for price feed.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="HftSpreaderForFortsStrategy"/>.
	/// </summary>
	public HftSpreaderForFortsStrategy()
	{
		_spreadMultiplier = Param(nameof(SpreadMultiplier), 4)
			.SetGreaterThanZero()
			.SetDisplay("Spread Multiplier", "Spread ticks required to place orders", "General")
			.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for price feed", "General");

		Volume = 1;
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

		StartProtection(null, null);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security.PriceStep ?? 1m;
		var spread = candle.HighPrice - candle.LowPrice;

		if (spread >= SpreadMultiplier * step)
		{
			if (Position == 0)
			{
				// Wide spread detected - buy at low, will exit when spread narrows
				BuyMarket();
			}
			else if (Position > 0)
			{
				// Close long position for spread capture
				SellMarket();
			}
			else if (Position < 0)
			{
				// Close short position for spread capture
				BuyMarket();
			}
		}
	}
}
