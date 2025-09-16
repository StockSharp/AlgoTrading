using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes every open position once a configurable stop time is reached.
/// Works across all securities handled by the strategy and sends market exit orders.
/// </summary>
public class CloseAllPositionsByTimeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _stopTime;

	private bool _stopTriggered;

	/// <summary>
	/// Candle type used to monitor the current time.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moment after which all positions must be closed.
	/// </summary>
	public DateTimeOffset StopTime
	{
		get => _stopTime.Value;
		set => _stopTime.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CloseAllPositionsByTimeStrategy"/>.
	/// </summary>
	public CloseAllPositionsByTimeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used to check time", "General");

		_stopTime = Param(nameof(StopTime), new DateTimeOffset(new DateTime(2030, 1, 1, 23, 59, 0, DateTimeKind.Utc)))
			.SetDisplay("Stop Time", "Timestamp to flatten all positions", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stopTriggered = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only completed candles to avoid premature checks.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var candleTime = candle.OpenTime;

		// Activate the closing routine once the target time is reached.
		if (!_stopTriggered && candleTime >= StopTime)
			_stopTriggered = true;

		if (!_stopTriggered)
			return;

		// Issue exit orders for every instrument until the book becomes flat.
		CloseAllOpenPositions();

		// Reset the flag after all positions are closed.
		if (!HasOpenPositions())
			_stopTriggered = false;
	}

	private void CloseAllOpenPositions()
	{
		if (Portfolio == null)
			return;

		var processed = new HashSet<Security>();

		// Flatten the primary security handled by the strategy instance.
		if (Security != null && processed.Add(Security))
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);
		}

		// Handle any additional securities traded by nested strategies.
		foreach (var position in Positions.ToArray())
		{
			var security = position.Security;
			if (security == null || !processed.Add(security))
				continue;

			var volume = GetPositionValue(security, Portfolio) ?? 0m;
			if (volume > 0)
				SellMarket(volume, security);
			else if (volume < 0)
				BuyMarket(-volume, security);
		}
	}

	private bool HasOpenPositions()
	{
		if (Portfolio == null)
			return false;

		// Check the main security position first for a quick exit.
		if (Security != null && Position != 0)
			return true;

		// Inspect the remaining positions maintained by the strategy hierarchy.
		foreach (var position in Positions)
		{
			var security = position.Security;
			if (security == null)
				continue;

			var volume = GetPositionValue(security, Portfolio) ?? 0m;
			if (volume != 0m)
				return true;
		}

		return false;
	}
}
