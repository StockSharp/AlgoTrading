using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates exactly one market long entry after the first finished candle,
/// followed by the engine's fill-price-based native trailing stop.
/// </summary>
public class SimpleTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailPoints;
	private readonly StrategyParam<DataType> _candleType;
	private bool _submittedEntry;
	private Order _entryOrder;

	public decimal TrailPoints { get => _trailPoints.Value; set => _trailPoints.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SimpleTrailingStopStrategy()
	{
		_trailPoints = Param(nameof(TrailPoints), 100m).SetGreaterThanZero()
			.SetDisplay("Trail Points", "Trailing distance in security price steps", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_submittedEntry = false;
		_entryOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_submittedEntry = false;
		_entryOrder = null;

		StartProtection(
			takeProfit: null,
			stopLoss: new Unit(TrailPoints * (Security.PriceStep ?? 1m), UnitTypes.Absolute),
			isStopTrailing: true,
			useMarketOrders: true);

		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _submittedEntry ||
			!IsFormedAndOnlineAndAllowTrading())
			return;

		// Mark before sending: avoid duplicate opens on synchronous fills or callbacks.
		_submittedEntry = true;
		_entryOrder = BuyMarket(Volume);
		// Do not reopen after a protective stop fills: this is a one-shot example.
	}
}
