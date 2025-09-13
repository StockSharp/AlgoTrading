namespace StockSharp.Samples.Strategies;

using System;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;

public class VRMarsStrategy : Strategy
{
	// Parameter for first lot size.
	private readonly StrategyParam<decimal> _lot1;

	// Parameter for second lot size.
	private readonly StrategyParam<decimal> _lot2;

	// Parameter for third lot size.
	private readonly StrategyParam<decimal> _lot3;

	// Parameter for fourth lot size.
	private readonly StrategyParam<decimal> _lot4;

	// Parameter for fifth lot size.
	private readonly StrategyParam<decimal> _lot5;

	// Parameter that selects active lot from 1 to 5.
	private readonly StrategyParam<int> _selectedLot;

	// Flag that triggers market buy order when true.
	private readonly StrategyParam<bool> _buy;

	// Flag that triggers market sell order when true.
	private readonly StrategyParam<bool> _sell;

	public VRMarsStrategy()
	{
		_lot1 = this.Param(nameof(Lot1), 0.01m).SetDisplay("Lot 1").SetCanOptimize(false);
		_lot2 = this.Param(nameof(Lot2), 0.02m).SetDisplay("Lot 2").SetCanOptimize(false);
		_lot3 = this.Param(nameof(Lot3), 0.03m).SetDisplay("Lot 3").SetCanOptimize(false);
		_lot4 = this.Param(nameof(Lot4), 0.04m).SetDisplay("Lot 4").SetCanOptimize(false);
		_lot5 = this.Param(nameof(Lot5), 0.05m).SetDisplay("Lot 5").SetCanOptimize(false);
		_selectedLot = this.Param(nameof(SelectedLot), 1).SetDisplay("Selected Lot").SetCanOptimize(false);
		_buy = this.Param(nameof(Buy), false).SetDisplay("Send Buy").SetCanOptimize(false);
		_sell = this.Param(nameof(Sell), false).SetDisplay("Send Sell").SetCanOptimize(false);
	}

	public decimal Lot1 { get => _lot1.Value; set => _lot1.Value = value; }
	public decimal Lot2 { get => _lot2.Value; set => _lot2.Value = value; }
	public decimal Lot3 { get => _lot3.Value; set => _lot3.Value = value; }
	public decimal Lot4 { get => _lot4.Value; set => _lot4.Value = value; }
	public decimal Lot5 { get => _lot5.Value; set => _lot5.Value = value; }
	public int SelectedLot { get => _selectedLot.Value; set => _selectedLot.Value = value; }
	public bool Buy { get => _buy.Value; set => _buy.Value = value; }
	public bool Sell { get => _sell.Value; set => _sell.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Activate stop protection once at strategy start.
		StartProtection();

		var volume = SelectedLot switch
		{
			1 => Lot1,
			2 => Lot2,
			3 => Lot3,
			4 => Lot4,
			_ => Lot5
		};

		// Execute chosen direction using high level market order helpers.
		if (Buy)
			BuyMarket(volume: volume);
		else if (Sell)
			SellMarket(volume: volume);
	}
}
