using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor "Nevalyashka".
/// Opens an opposite trade immediately after the previous one closes.
/// Implements a simple stop-loss / take-profit distance expressed in pips.
/// </summary>
public class NevalyashkaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<Sides> _initialDirection;

	private decimal _pipSize;
	private decimal _lastOrderVolume;
	private Sides _lastExecutedDirection;
	private Sides? _pendingDirection;

	/// <summary>
	/// Initializes a new instance of the <see cref="NevalyashkaStrategy"/> class.
	/// </summary>
	public NevalyashkaStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips applied to every trade.", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Profit target distance in pips applied to every trade.", "Risk")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Lot size used for the very first order.", "Trading")
			.SetCanOptimize(true);

		_initialDirection = Param(nameof(InitialDirection), Sides.Sell)
			.SetDisplay("Initial Direction", "Side of the very first market order.", "Trading");

		_pipSize = 0m;
		_lastOrderVolume = 0m;
		_lastExecutedDirection = _initialDirection.Value;
		_pendingDirection = null;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Base volume used for the very first order.
	/// The strategy stores the filled volume of the last trade and reuses it for subsequent entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Direction of the very first order.
	/// </summary>
	public Sides InitialDirection
	{
		get => _initialDirection.Value;
		set => _initialDirection.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_lastOrderVolume = 0m;
		_lastExecutedDirection = InitialDirection;
		_pendingDirection = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var stopLoss = StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Point) : null;
		var takeProfit = TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Point) : null;

		if (stopLoss != null || takeProfit != null)
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);

		TryOpenDirection(InitialDirection);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (delta == 0m)
			return;

		if (Position > 0m && delta > 0m)
		{
			// A new long position was opened.
			_lastOrderVolume = Math.Abs(Position);
			_lastExecutedDirection = Sides.Buy;
			_pendingDirection = null;
			return;
		}

		if (Position < 0m && delta < 0m)
		{
			// A new short position was opened.
			_lastOrderVolume = Math.Abs(Position);
			_lastExecutedDirection = Sides.Sell;
			_pendingDirection = null;
			return;
		}

		if (Position == 0m)
		{
			// A position was closed; immediately flip the direction.
			var nextDirection = _lastExecutedDirection == Sides.Buy ? Sides.Sell : Sides.Buy;
			TryOpenDirection(nextDirection);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderRegisterFailed(OrderFail fail)
	{
		base.OnOrderRegisterFailed(fail);

		if (_pendingDirection is { } pending && fail.Order.Direction == pending)
		{
			// Registration failed; release the pending direction so it can be retried later.
			_pendingDirection = null;
		}
	}

	private bool TryOpenDirection(Sides direction)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return false;

		if (Position != 0m)
			return false;

		var volume = _lastOrderVolume > 0m ? _lastOrderVolume : Volume;
		volume = AdjustVolume(volume);

		if (volume <= 0m)
			return false;

		_pendingDirection = direction;

		// Execute the market order on the requested side.
		var order = direction == Sides.Buy
			? BuyMarket(volume)
			: SellMarket(volume);

		if (order == null)
		{
			_pendingDirection = null;
			return false;
		}

		return true;
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;

		if (security == null)
			return volume;

		var step = security.VolumeStep;

		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;

		if (security == null)
			return 1m;

		var priceStep = security.PriceStep ?? 0m;

		if (priceStep <= 0m)
			return 1m;

		// Multiply by 10 for three- and five-digit Forex symbols to match MetaTrader points.
		return security.Decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}
}
