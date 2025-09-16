using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Places buy and sell stop orders around the current price and manages a trailing stop.
/// </summary>
public class StraddleNewsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _pipsAway;
	private readonly StrategyParam<decimal> _balanceUsed;
	private readonly StrategyParam<decimal> _spreadOperation;
	private readonly StrategyParam<int> _leverage;

	private decimal _tickSize;
	private bool _ordersPlaced;
	private decimal? _maxPrice;
	private decimal? _minPrice;

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Offset from current price to place pending orders in points.
	/// </summary>
	public decimal PipsAway
	{
		get => _pipsAway.Value;
		set => _pipsAway.Value = value;
	}

	/// <summary>
	/// Fraction of portfolio used to calculate order volume.
	/// </summary>
	public decimal BalanceUsed
	{
		get => _balanceUsed.Value;
		set => _balanceUsed.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in points.
	/// </summary>
	public decimal SpreadOperation
	{
		get => _spreadOperation.Value;
		set => _spreadOperation.Value = value;
	}

	/// <summary>
	/// Broker leverage.
	/// </summary>
	public int Leverage
	{
		get => _leverage.Value;
		set => _leverage.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="StraddleNewsStrategy"/>.
	/// </summary>
	public StraddleNewsStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 100m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetGreaterThanZero();

		_takeProfit = Param(nameof(TakeProfit), 300m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetGreaterThanZero();

		_trailingStop = Param(nameof(TrailingStop), 50m)
			.SetDisplay("Trailing Stop", "Trailing distance in points", "Risk")
			.SetGreaterThanZero();

		_pipsAway = Param(nameof(PipsAway), 50m)
			.SetDisplay("Pips Away", "Offset for pending orders in points", "Order")
			.SetGreaterThanZero();

		_balanceUsed = Param(nameof(BalanceUsed), 0.01m)
			.SetDisplay("Balance Used", "Portfolio share for position size", "Order")
			.SetGreaterThanZero();

		_spreadOperation = Param(nameof(SpreadOperation), 25m)
			.SetDisplay("Max Spread", "Maximum allowed spread in points", "Order")
			.SetGreaterThanZero();

		_leverage = Param(nameof(Leverage), 400)
			.SetDisplay("Leverage", "Broker leverage", "Order")
			.SetGreaterThanZero();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ordersPlaced = false;
		_maxPrice = null;
		_minPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security.PriceStep ?? 0.0001m;

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		StartProtection(
			new Unit(TakeProfit * _tickSize, UnitTypes.Absolute),
			new Unit(StopLoss * _tickSize, UnitTypes.Absolute)
		);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		var changes = level1.Changes;

		if (!changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) ||
			!changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
			return;

		var bid = (decimal)bidObj;
		var ask = (decimal)askObj;

		if (!_ordersPlaced)
		{
			var spread = ask - bid;

			if (spread <= SpreadOperation * _tickSize)
			{
				var portfolioValue = Portfolio?.CurrentValue ?? 0m;
				var volume = portfolioValue * Leverage * BalanceUsed / (100000m * ask);

				var buyPrice = ask + PipsAway * _tickSize;
				var sellPrice = bid - PipsAway * _tickSize;

				BuyStop(volume, buyPrice);
				SellStop(volume, sellPrice);

				_ordersPlaced = true;
			}
		}

		if (Position > 0)
		{
			_maxPrice = _maxPrice.HasValue ? Math.Max(_maxPrice.Value, bid) : bid;

			var dist = TrailingStop * _tickSize;

			if (_maxPrice.Value - PositionPrice >= dist)
			{
				var stopLevel = _maxPrice.Value - dist;

				if (bid <= stopLevel)
				{
					SellMarket(Math.Abs(Position));
					_maxPrice = null;
				}
			}
		}
		else if (Position < 0)
		{
			_minPrice = _minPrice.HasValue ? Math.Min(_minPrice.Value, ask) : ask;

			var dist = TrailingStop * _tickSize;

			if (PositionPrice - _minPrice.Value >= dist)
			{
				var stopLevel = _minPrice.Value + dist;

				if (ask >= stopLevel)
				{
					BuyMarket(Math.Abs(Position));
					_minPrice = null;
				}
			}
		}
		else
		{
			_maxPrice = null;
			_minPrice = null;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position != 0)
			CancelActiveOrders();
	}
}
