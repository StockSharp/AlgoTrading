using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that inserts take-profit and stop-loss orders for existing positions.
/// Designed to protect manual or external trades.
/// </summary>
public class TpslInsertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="TpslInsertStrategy"/>.
	/// </summary>
	public TpslInsertStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 35m)
			.SetDisplay("Take Profit (pips)", "Distance to take profit in pips", "Protection");

		_stopLossPips = Param(nameof(StopLossPips), 100m)
			.SetDisplay("Stop Loss (pips)", "Distance to stop loss in pips", "Protection");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var step = Security?.PriceStep ?? 1m;

		StartProtection(
			takeProfit: new Unit(TakeProfitPips * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossPips * step, UnitTypes.Point));
	}
}

