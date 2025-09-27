using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor "Reversing Martingale".
/// Alternates trade direction after every closed position and applies a martingale multiplier after losses.
/// Positions are protected by symmetric stop-loss and take-profit levels defined in price points.
/// </summary>
public class ReversingMartingaleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<Sides> _firstTradeSide;
	private readonly StrategyParam<int> _targetPoints;
	private readonly StrategyParam<string> _orderComment;

	private decimal _previousPosition;
	private decimal _lastRealizedPnL;
	private decimal _nextVolume;
	private Sides _nextSide;
	private Sides? _activeSide;
	private bool _orderInFlight;

	/// <summary>
	/// Initializes a new instance of <see cref="ReversingMartingaleStrategy"/>.
	/// </summary>
	public ReversingMartingaleStrategy()
	{
		_startVolume = Param(nameof(StartVolume), 0.05m)
			.SetGreaterThanZero()
			.SetDisplay("Start Lot", "Initial trade volume used at the beginning of every winning cycle.", "Trading")
			.SetCanOptimize(true);

		_lotMultiplier = Param(nameof(LotMultiplier), 2m)
			.SetGreaterThan(1m)
			.SetDisplay("Lot Multiplier", "Volume multiplier applied after a losing trade.", "Risk")
			.SetCanOptimize(true);

		_firstTradeSide = Param(nameof(FirstTradeSide), Sides.Buy)
			.SetDisplay("First Trade Side", "Direction of the very first trade in a new session.", "Trading");

		_targetPoints = Param(nameof(TargetPoints), 500)
			.SetGreaterThanZero()
			.SetDisplay("Target (points)", "Protective stop-loss and take-profit distance expressed in price steps.", "Risk")
			.SetCanOptimize(true);

		_orderComment = Param(nameof(OrderComment), "Reversing Martingale EA")
			.SetDisplay("Order Comment", "Text tag assigned to every market order.", "General");
	}

	/// <summary>
	/// Initial trade volume used at the beginning of every winning cycle.
	/// </summary>
	public decimal StartVolume
	{
		get => _startVolume.Value;
		set => _startVolume.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied after a losing trade.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Direction of the very first trade in a new session.
	/// </summary>
	public Sides FirstTradeSide
	{
		get => _firstTradeSide.Value;
		set => _firstTradeSide.Value = value;
	}

	/// <summary>
	/// Protective stop-loss and take-profit distance expressed in price steps.
	/// </summary>
	public int TargetPoints
	{
		get => _targetPoints.Value;
		set => _targetPoints.Value = value;
	}

	/// <summary>
	/// Text tag assigned to every market order.
	/// </summary>
	public string OrderComment
	{
		get => _orderComment.Value;
		set => _orderComment.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousPosition = 0m;
		_lastRealizedPnL = 0m;
		_nextVolume = 0m;
		_nextSide = FirstTradeSide;
		_activeSide = null;
		_orderInFlight = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_nextSide = FirstTradeSide;
		_nextVolume = NormalizeVolume(StartVolume);
		_orderInFlight = false;

		var points = TargetPoints;
		if (points > 0)
		{
			var unit = new Unit(points, UnitTypes.Step);
			StartProtection(unit, unit);
		}
		else
		{
			StartProtection();
		}

		TryOpenPosition();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (_previousPosition == 0m && Position != 0m)
		{
			// A fresh position has been established.
			_activeSide = Position > 0m ? Sides.Buy : Sides.Sell;
			_lastRealizedPnL = PnL;
			_orderInFlight = false;
		}
		else if (_previousPosition != 0m && Position == 0m)
		{
			var tradePnL = PnL - _lastRealizedPnL;
			_lastRealizedPnL = PnL;

			var closedSide = _activeSide ?? _nextSide;
			var closedVolume = Math.Abs(_previousPosition);

			if (tradePnL < 0m)
			{
				var scaled = closedVolume * LotMultiplier;
				_nextVolume = NormalizeVolume(scaled);
			}
			else
			{
				_nextVolume = NormalizeVolume(StartVolume);
			}

			_nextSide = closedSide == Sides.Buy ? Sides.Sell : Sides.Buy;
			_activeSide = null;
			_orderInFlight = false;

			TryOpenPosition();
		}

		_previousPosition = Position;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position != 0m)
			_orderInFlight = false;
	}

	private void TryOpenPosition()
	{
		if (_orderInFlight)
			return;

		if (Position != 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = _nextVolume > 0m ? _nextVolume : NormalizeVolume(StartVolume);
		if (volume <= 0m)
			return;

		_orderInFlight = true;

		Order order;

		if (_nextSide == Sides.Buy)
			order = BuyMarket(volume);
		else
			order = SellMarket(volume);

		if (order != null && !OrderComment.IsEmpty())
			order.Comment = OrderComment;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		if (volume <= 0m)
			return 0m;

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
}
