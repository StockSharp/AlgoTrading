using System;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that logs when new bars start for multiple timeframes.
/// It tracks predefined periods and notifies when a new bar appears.
/// </summary>
public class SymrNewBarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	// Supported periods in ascending order.
	private static readonly TimeSpan[] _periods =
	{
		TimeSpan.FromMinutes(1),
		TimeSpan.FromMinutes(5),
		TimeSpan.FromMinutes(15),
		TimeSpan.FromMinutes(30),
		TimeSpan.FromHours(1),
		TimeSpan.FromHours(4),
		TimeSpan.FromDays(1),
		TimeSpan.FromMinutes(20),
		TimeSpan.FromMinutes(55),
	};

	// Last known open time for each period.
	private readonly DateTimeOffset[] _times = new DateTimeOffset[_periods.Length];

	private int _currentIndex;

	/// <summary>
	/// Base candle type used to detect new bars.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public SymrNewBarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe to monitor", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Determine index of the base timeframe.
		_currentIndex = Array.IndexOf(_periods, ((CandleTimeFrame)CandleType.Arg).TimeSpan);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentTime = candle.OpenTime;

		if (_times[_currentIndex] != currentTime)
		{
			_times[_currentIndex] = currentTime;
			OnBar(_periods[_currentIndex], currentTime);

			for (var i = _currentIndex + 1; i < _periods.Length; i++)
			{
				var period = _periods[i];
				var time0 = currentTime - TimeSpan.FromTicks(currentTime.Ticks % period.Ticks);

				if (_times[i] != time0)
				{
					_times[i] = time0;
					OnBar(period, time0);
				}
			}
		}

		OnTick();
	}

	private void OnTick()
	{
		// Called after each processed candle.
		// Placeholder for tick-based logic.
	}

	private void OnBar(TimeSpan period, DateTimeOffset time)
	{
		// Log new bar event for the specified period.
		LogInfo("New bar at {time} for period {period}.");
	}
}
