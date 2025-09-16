using System;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VR Overturn strategy that alternates between martingale and anti-martingale sizing rules.
/// It opens a single position at a time and reverses direction after losses while
/// optionally increasing size after wins depending on the selected mode.
/// </summary>
public class VrOverturnStrategy : Strategy
{
	private const decimal VolumeEpsilon = 1e-6m;

	private enum InitialDirection
	{
		/// <summary>
		/// Start with a long position.
		/// </summary>
		Buy,

		/// <summary>
		/// Start with a short position.
		/// </summary>
		Sell
	}

	private enum TradeMode
	{
		/// <summary>
		/// Increase size after losses and reset after wins.
		/// </summary>
		Martingale,

		/// <summary>
		/// Increase size after wins and reset after losses.
		/// </summary>
		AntiMartingale
	}

	private readonly StrategyParam<InitialDirection> _initialDirection;
	private readonly StrategyParam<TradeMode> _tradeMode;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _lotMultiplier;

	private decimal _pipSize;
	private Sides? _pendingEntrySide;
	private Sides? _activeSide;
	private decimal _entryPrice;
	private decimal _openedVolume;
	private decimal _closedVolume;
	private decimal _realizedPnL;

	private decimal _lastClosedVolume;
	private decimal _lastClosedProfit;
	private Sides? _lastClosedSide;
	private bool _hasClosedHistory;

	/// <summary>
	/// Initializes a new instance of the <see cref="VrOverturnStrategy"/> class.
	/// </summary>
	public VrOverturnStrategy()
	{
		_initialDirection = Param(nameof(FirstPositionDirection), InitialDirection.Buy)
		.SetDisplay("Initial Direction", "Direction of the very first trade", "Trading");

		_tradeMode = Param(nameof(Mode), TradeMode.Martingale)
		.SetDisplay("Trading Mode", "Choose martingale or anti-martingale sizing", "Trading");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Initial order size", "Risk")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 30)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Distance to stop loss in pips", "Risk")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 90)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Distance to take profit in pips", "Risk")
		.SetCanOptimize(true);

		_lotMultiplier = Param(nameof(LotMultiplier), 1.6m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Multiplier", "Multiplier applied after losses or wins", "Risk")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Direction of the very first position.
	/// </summary>
	public InitialDirection FirstPositionDirection
	{
		get => _initialDirection.Value;
		set => _initialDirection.Value = value;
	}

	/// <summary>
	/// Selected sizing regime.
	/// </summary>
	public TradeMode Mode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Base contract volume used for new sequences.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied after wins or losses depending on the selected mode.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_pendingEntrySide = null;
		_activeSide = null;
		_entryPrice = 0m;
		_openedVolume = 0m;
		_closedVolume = 0m;
		_realizedPnL = 0m;

		_lastClosedVolume = 0m;
		_lastClosedProfit = 0m;
		_lastClosedSide = null;
		_hasClosedHistory = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		StartProtection(
		takeProfit: new Unit(takeDistance, UnitTypes.Absolute),
		stopLoss: new Unit(stopDistance, UnitTypes.Absolute),
		useMarketOrders: true);

		TryOpenNextPosition();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade?.Order == null)
		return;

		var side = trade.Order.Side;
		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (volume <= 0m)
		return;

		if (_pendingEntrySide.HasValue && side == _pendingEntrySide.Value)
		{
			RegisterEntry(price, volume, side);
			_pendingEntrySide = null;
			return;
		}

		if (_activeSide == null)
		return;

		if (side == _activeSide)
		{
			RegisterEntry(price, volume, side);
			return;
		}

		RegisterExit(price, volume);
	}

	private void RegisterEntry(decimal price, decimal volume, Sides side)
	{
		var previousVolume = _openedVolume;
		_openedVolume += volume;

		_entryPrice = previousVolume <= 0m
		? price
		: (_entryPrice * previousVolume + price * volume) / _openedVolume;

		_activeSide = side;
	}

	private void RegisterExit(decimal price, decimal volume)
	{
		_closedVolume += volume;

		var profit = _activeSide == Sides.Buy
		? (price - _entryPrice) * volume
		: (_entryPrice - price) * volume;

		_realizedPnL += profit;

		if (_closedVolume + VolumeEpsilon < _openedVolume)
		return;

		var closedVolume = _openedVolume;

		_lastClosedSide = _activeSide;
		_lastClosedVolume = closedVolume;
		_lastClosedProfit = _realizedPnL;
		_hasClosedHistory = true;

		_activeSide = null;
		_openedVolume = 0m;
		_closedVolume = 0m;
		_realizedPnL = 0m;
		_entryPrice = 0m;

		TryOpenNextPosition();
	}

	private void TryOpenNextPosition()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0 || _pendingEntrySide.HasValue)
		return;

		var baseVolume = AdjustVolume(BaseVolume);
		if (baseVolume <= 0m)
		return;

		Sides nextSide;
		decimal orderVolume;

		if (!_hasClosedHistory)
		{
			nextSide = FirstPositionDirection == InitialDirection.Buy ? Sides.Buy : Sides.Sell;
			orderVolume = baseVolume;
		}
		else if (_lastClosedSide.HasValue)
		{
			var referenceVolume = _lastClosedVolume > 0m ? _lastClosedVolume : baseVolume;

			if (_lastClosedProfit > 0m && Mode == TradeMode.Martingale)
			referenceVolume = baseVolume;

			if (_lastClosedProfit < 0m && Mode == TradeMode.AntiMartingale)
			referenceVolume = baseVolume;

			if (_lastClosedSide == Sides.Buy)
			{
				if (_lastClosedProfit > 0m)
				{
					nextSide = Sides.Buy;
					orderVolume = referenceVolume * GetWinningMultiplier();
				}
				else if (_lastClosedProfit < 0m)
				{
					nextSide = Sides.Sell;
					orderVolume = referenceVolume * GetLosingMultiplier();
				}
				else
				{
					return;
				}
			}
			else
			{
				if (_lastClosedProfit > 0m)
				{
					nextSide = Sides.Sell;
					orderVolume = referenceVolume * GetWinningMultiplier();
				}
				else if (_lastClosedProfit < 0m)
				{
					nextSide = Sides.Buy;
					orderVolume = referenceVolume * GetLosingMultiplier();
				}
				else
				{
					return;
				}
			}
		}
		else
		{
			nextSide = FirstPositionDirection == InitialDirection.Buy ? Sides.Buy : Sides.Sell;
			orderVolume = baseVolume;
		}

		orderVolume = AdjustVolume(orderVolume);
		if (orderVolume <= 0m)
		return;

		_pendingEntrySide = nextSide;

		if (nextSide == Sides.Buy)
		BuyMarket(orderVolume);
		else
		SellMarket(orderVolume);
	}

	private decimal GetWinningMultiplier()
	{
		return Mode == TradeMode.Martingale ? 1m : LotMultiplier;
	}

	private decimal GetLosingMultiplier()
	{
		return Mode == TradeMode.Martingale ? LotMultiplier : 1m;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
		return 1m;

		var temp = step;
		var digits = 0;

		while (temp < 1m && digits < 10)
		{
			temp *= 10m;
			digits++;
		}

		return digits == 3 || digits == 5 ? step * 10m : step;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var stepsCount = Math.Floor(volume / step);
			volume = stepsCount * step;
		}

		return volume;
	}
}
