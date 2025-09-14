
namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Alternating long/short strategy with martingale sizing.
/// </summary>
public class NevalyashkaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private decimal _currentVolume;
	private decimal _lastPnL;
	private Sides _nextSide;

	/// <summary>
	/// Initializes a new instance of <see cref="NevalyashkaStrategy"/>.
	/// </summary>
	public NevalyashkaStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Base order volume", "Trading");
		_lotMultiplier = Param(nameof(LotMultiplier), 1.5m)
			.SetDisplay("Lot Multiplier", "Volume multiplier after losing trade", "Trading");
		_stopLoss = Param(nameof(StopLoss), 10m)
			.SetDisplay("Stop Loss", "Stop loss in price points", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 20m)
			.SetDisplay("Take Profit", "Take profit in price points", "Risk");
	}

	/// <summary>
	/// Base trade volume.
	/// </summary>
	public new decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to volume after a losing trade.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentVolume = 0m;
		_lastPnL = 0m;
		_nextSide = Sides.Sell;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(new Unit(TakeProfit, UnitTypes.Price), new Unit(StopLoss, UnitTypes.Price));

		_currentVolume = Volume;
		SellMarket(_currentVolume);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
		return;

		var tradePnL = PnL - _lastPnL;
		_lastPnL = PnL;

		if (tradePnL < 0)
		_currentVolume *= LotMultiplier;
		else
		_currentVolume = Volume;

		_nextSide = _nextSide == Sides.Buy ? Sides.Sell : Sides.Buy;

		if (_nextSide == Sides.Buy)
		BuyMarket(_currentVolume);
		else
		SellMarket(_currentVolume);
	}
}
