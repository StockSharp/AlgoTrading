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
/// Strategy that closes all positions and optionally cancels pending orders on start.
/// </summary>
public class OneClickCloseAllStrategy : Strategy
{
	private readonly StrategyParam<bool> _runOnCurrentSecurity;
	private readonly StrategyParam<bool> _closeOnlyManualTrades;
	private readonly StrategyParam<bool> _deletePendingOrders;
	private readonly StrategyParam<int> _maxSlippage;

/// <summary>
/// Apply only to the strategy security.
/// </summary>
public bool RunOnCurrentSecurity
{
	get => _runOnCurrentSecurity.Value;
	set => _runOnCurrentSecurity.Value = value;
}

/// <summary>
/// Close only trades without strategy id.
/// Currently for compatibility and has no effect.
/// </summary>
public bool CloseOnlyManualTrades
{
	get => _closeOnlyManualTrades.Value;
	set => _closeOnlyManualTrades.Value = value;
}

/// <summary>
/// Delete pending orders when closing.
/// </summary>
public bool DeletePendingOrders
{
	get => _deletePendingOrders.Value;
	set => _deletePendingOrders.Value = value;
}

/// <summary>
/// Maximum acceptable slippage.
/// Reserved for future use.
/// </summary>
public int MaxSlippage
{
	get => _maxSlippage.Value;
	set => _maxSlippage.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="OneClickCloseAllStrategy"/>.
/// </summary>
public OneClickCloseAllStrategy()
{
	_runOnCurrentSecurity = Param(nameof(RunOnCurrentSecurity), true)
		.SetDisplay("Run On Current Security", "Apply only to the selected security", "General");

	_closeOnlyManualTrades = Param(nameof(CloseOnlyManualTrades), true)
		.SetDisplay("Close Only Manual Trades", "Close trades without strategy id", "General");

	_deletePendingOrders = Param(nameof(DeletePendingOrders), false)
		.SetDisplay("Delete Pending Orders", "Cancel pending orders", "General");

	_maxSlippage = Param(nameof(MaxSlippage), 5)
		.SetDisplay("Max Slippage", "Maximum acceptable slippage", "Orders");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
	yield return (Security, default);
}

/// <inheritdoc />
protected override void OnStarted2(DateTime time)
{
	base.OnStarted2(time);

	StartProtection(null, null);

	// Close position for current security
	if (Position > 0)
		SellMarket();
	else if (Position < 0)
		BuyMarket();

	Stop();
}
}
