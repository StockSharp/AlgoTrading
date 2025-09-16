using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Utility strategy that logs when the number of active orders changes.
/// </summary>
public class TforexPhotosStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private int _lastOrderCount;

	/// <summary>
	/// Candle type used to trigger order checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TforexPhotosStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to trigger checks", "General");
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

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		CheckOrders();
	}

	// Check the current number of active orders and log when it changes.
	private void CheckOrders()
	{
		var count = 0;
		foreach (var order in Orders)
		{
			if (order.State == OrderStates.Active)
				count++;
		}

		if (count == _lastOrderCount)
			return;

		_lastOrderCount = count;

		var now = CurrentTime;
		var photoName = $"{Security?.Id} {now:yyyyMMdd HHmm} M{CandleType}";
		LogInfo($"{photoName} active orders: {count}");
	}
}

