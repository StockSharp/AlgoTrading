using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes any open position for the configured security once the specified stop time has passed.
/// </summary>
public class ClosePositionsByTimeStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _stopTime;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Initializes a new instance of the <see cref="ClosePositionsByTimeStrategy"/> class.
	/// </summary>
	public ClosePositionsByTimeStrategy()
	{
		_stopTime = Param(nameof(StopTime), new DateTimeOffset(2030, 1, 1, 23, 59, 0, TimeSpan.Zero))
			.SetDisplay("Stop Time", "Time after which all positions will be closed", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used to monitor time", "General");
	}

	/// <summary>
	/// Time after which the strategy will close positions and block further trading.
	/// </summary>
	public DateTimeOffset StopTime
	{
		get => _stopTime.Value;
		set => _stopTime.Value = value;
	}

	/// <summary>
	/// Candle type used to track the market time progression.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Enable position protection so the base strategy observes unexpected fills.
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var candleTime = candle.CloseTime;

		if (candleTime <= StopTime)
			return;

		// Cancel any working orders before attempting to liquidate the position.
		CancelActiveOrders();

		if (Position != 0)
		{
			// ClosePosition sends the appropriate market order based on the current net position.
			ClosePosition();
		}
	}
}
