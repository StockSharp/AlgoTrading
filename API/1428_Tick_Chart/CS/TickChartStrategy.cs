using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aggregates trades into tick candles without trading logic.
/// </summary>
public class TickChartStrategy : Strategy
{
	private readonly StrategyParam<int> _ticksPerCandle;

	private int _count;
	private decimal _open;
	private decimal _high;
	private decimal _low;
	private decimal _close;
	private decimal _volume;

	public int TicksPerCandle { get => _ticksPerCandle.Value; set => _ticksPerCandle.Value = value; }

	public TickChartStrategy()
	{
		_ticksPerCandle = Param(nameof(TicksPerCandle), 5);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice ?? 0m;
		var vol = trade.Volume ?? 0m;

		if (_count == 0)
		{
			_open = _high = _low = price;
			_volume = 0m;
		}

		_close = price;
		_high = Math.Max(_high, price);
		_low = Math.Min(_low, price);
		_volume += vol;
		_count++;

		if (_count >= TicksPerCandle)
			ResetCandle();
	}

	private void ResetCandle()
	{
		_count = 0;
		_open = _high = _low = _close = _volume = 0m;
	}
}
