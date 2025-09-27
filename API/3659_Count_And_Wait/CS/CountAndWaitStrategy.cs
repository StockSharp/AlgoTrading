using System;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that counts incoming tick messages, triggers an action once the
/// count limit is reached, and optionally waits for an additional number of
/// ticks before resetting the counters.
/// </summary>
public class CountAndWaitStrategy : Strategy
{
	private readonly StrategyParam<int> _countLimit;
	private readonly StrategyParam<int> _waitLimit;

	private int _counter;
	private int _waitCounter;

	/// <summary>
	/// Number of ticks to collect before the strategy signals that the cycle completed.
	/// </summary>
	public int CountLimit
	{
		get => _countLimit.Value;
		set => _countLimit.Value = value;
	}

	/// <summary>
	/// Number of ticks to wait after the count limit was reached before a new cycle starts.
	/// </summary>
	public int WaitLimit
	{
		get => _waitLimit.Value;
		set => _waitLimit.Value = value;
	}

	/// <summary>
	/// Initializes parameters for controlling the counting and waiting phases.
	/// </summary>
	public CountAndWaitStrategy()
	{
		_countLimit = Param(nameof(CountLimit), 50)
			.SetDisplay("Count Limit", "Ticks required to execute the action", "General")
			.SetCanOptimize(true);

		_waitLimit = Param(nameof(WaitLimit), 0)
			.SetDisplay("Wait Limit", "Ticks to wait before restarting the count", "General")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_counter = 0;
		_waitCounter = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeTicks().Bind(ProcessTrade).Start();
	}

	private void ProcessTrade(ITickTradeMessage trade)
	{
		var limit = CountLimit;

		if (limit <= 0)
		{
			return;
		}

		if (_counter < limit)
		{
			_counter++;

			if (_counter == limit)
			{
				LogInfo($"Count limit {limit} reached. Executing cycle action.");
				_waitCounter = 0;

				if (WaitLimit <= 0)
				{
					_counter = 0;
				}
			}

			return;
		}

		var wait = WaitLimit;

		if (wait <= 0)
		{
			_counter = 0;
			return;
		}

		if (_waitCounter < wait)
		{
			_waitCounter++;

			if (_waitCounter == wait)
			{
				LogInfo($"Wait limit {wait} reached. Restarting counting phase.");
				_counter = 0;
				_waitCounter = 0;
			}
		}
	}
}
