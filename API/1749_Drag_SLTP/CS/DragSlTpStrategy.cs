using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that automatically attaches stop-loss and take-profit orders
/// at a fixed distance from the entry price.
/// </summary>
public class DragSlTpStrategy : Strategy
{
	private readonly StrategyParam<bool> _autoSetSl;
	private readonly StrategyParam<decimal> _slPoints;
	private readonly StrategyParam<bool> _autoSetTp;
	private readonly StrategyParam<decimal> _tpPoints;

	/// <summary>
	/// Determines whether stop-loss orders are automatically placed.
	/// </summary>
	public bool AutoSetSl
	{
		get => _autoSetSl.Value;
		set => _autoSetSl.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal SlPoints
	{
		get => _slPoints.Value;
		set => _slPoints.Value = value;
	}

	/// <summary>
	/// Determines whether take-profit orders are automatically placed.
	/// </summary>
	public bool AutoSetTp
	{
		get => _autoSetTp.Value;
		set => _autoSetTp.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TpPoints
	{
		get => _tpPoints.Value;
		set => _tpPoints.Value = value;
	}

	public DragSlTpStrategy()
	{
		_autoSetSl = Param(nameof(AutoSetSl), true)
			.SetDisplay("Auto Set SL", "Automatically set stop-loss for new positions", "Risk")
			.SetCanOptimize(true);

		_slPoints = Param(nameof(SlPoints), 300m)
			.SetDisplay("SL Points", "Stop-loss distance in price steps", "Risk")
			.SetCanOptimize(true);

		_autoSetTp = Param(nameof(AutoSetTp), false)
			.SetDisplay("Auto Set TP", "Automatically set take-profit for new positions", "Risk")
			.SetCanOptimize(true);

		_tpPoints = Param(nameof(TpPoints), 30m)
			.SetDisplay("TP Points", "Take-profit distance in price steps", "Risk")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			AutoSetTp ? new Unit(TpPoints, UnitTypes.Step) : default,
			AutoSetSl ? new Unit(SlPoints, UnitTypes.Step) : default);
	}
}
