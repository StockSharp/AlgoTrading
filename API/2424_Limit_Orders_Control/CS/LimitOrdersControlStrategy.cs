using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that monitors pending limit and stop orders.
/// Cancels all pending orders when their count drops below two
/// and stops working when none remain.
/// </summary>
public class LimitOrdersControlStrategy : Strategy
{
	private readonly StrategyParam<int> _magicNumber;
	private readonly StrategyParam<bool> _writeComments;

	private bool _work;

	/// <summary>
	/// Magic number used to identify orders. Preserved for compatibility.
	/// </summary>
	public int MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <summary>
	/// Whether to log information about active orders.
	/// </summary>
	public bool WriteComments
	{
		get => _writeComments.Value;
		set => _writeComments.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public LimitOrdersControlStrategy()
	{
		_magicNumber = Param(nameof(MagicNumber), 0)
			.SetDisplay("Magic Number", "Identifier for pending orders", "General");

		_writeComments = Param(nameof(WriteComments), true)
			.SetDisplay("Write Comments", "Log order information", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_work = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		if (!_work)
			return;

		var countOrders = 0;
		foreach (var order in ActiveOrders)
		{
			if (order.Type == OrderTypes.Limit || order.Type == OrderTypes.Stop)
				countOrders++;
		}

		if (WriteComments)
		{
			LogInfo($"We have {countOrders} open orders");
			LogInfo(_work ? "Limit Orders Control - work" : "Limit Orders Control - STOPPED");
		}

		if (countOrders < 2)
			CancelPendingOrders();

		if (countOrders < 1)
		{
			_work = false;
			LogInfo("Limit Orders Control - Stopped");
		}
	}

	private void CancelPendingOrders()
	{
		foreach (var order in ActiveOrders.ToArray())
		{
			if (order.Type == OrderTypes.Limit || order.Type == OrderTypes.Stop)
				CancelOrder(order);
		}
	}
}

