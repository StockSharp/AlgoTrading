using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that keeps two symmetric pending orders around the market price.
/// Converted from the MetaTrader expert advisor "Two pending orders 2".
/// </summary>
public class TwoPendingOrders2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _minStepPoints;
	private readonly StrategyParam<decimal> _trailingActivatePoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<TradeMode> _tradeMode;
	private readonly StrategyParam<PendingOrderMode> _pendingType;
	private readonly StrategyParam<int> _pendingExpirationMinutes;
	private readonly StrategyParam<decimal> _pendingIndentPoints;
	private readonly StrategyParam<decimal> _pendingMaxSpreadPoints;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _reverseLevels;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private DateTimeOffset? _pendingExpiryTime;
	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal? _longAveragePrice;
	private decimal? _shortAveragePrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Creates a new instance of <see cref="TwoPendingOrders2Strategy"/>.
	/// </summary>
	public TwoPendingOrders2Strategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 150m)
		.SetDisplay("Stop Loss (points)", "Distance to the protective stop in points.", "Risk")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 460m)
		.SetDisplay("Take Profit (points)", "Distance to the target in points.", "Risk")
		.SetCanOptimize(true);

		_maxPositions = Param(nameof(MaxPositions), 5)
		.SetDisplay("Max Positions", "Maximum number of simultaneously active positions and pending orders.", "General")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_minStepPoints = Param(nameof(MinStepPoints), 150m)
		.SetDisplay("Min Step (points)", "Minimum spacing between existing trades and new pending orders.", "Risk")
		.SetCanOptimize(true);

		_trailingActivatePoints = Param(nameof(TrailingActivatePoints), 70m)
		.SetDisplay("Trailing Activation (points)", "Profit required before the trailing stop starts moving.", "Trailing")
		.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 250m)
		.SetDisplay("Trailing Stop (points)", "Distance of the trailing stop once it is activated.", "Trailing")
		.SetCanOptimize(true);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 50m)
		.SetDisplay("Trailing Step (points)", "Minimum increment to move the trailing stop.", "Trailing")
		.SetCanOptimize(true);

		_tradeMode = Param(nameof(TradeMode), TradeMode.BuySell)
		.SetDisplay("Trade Mode", "Allowed trade direction for new pending orders.", "General");

		_pendingType = Param(nameof(PendingType), PendingOrderMode.Stop)
		.SetDisplay("Pending Type", "Choose between stop or limit pending orders.", "General");

		_pendingExpirationMinutes = Param(nameof(PendingExpirationMinutes), 600)
		.SetDisplay("Pending Expiration (min)", "Life time of pending orders in minutes (0 disables expiration).", "General")
		.SetCanOptimize(true);

		_pendingIndentPoints = Param(nameof(PendingIndentPoints), 150m)
		.SetDisplay("Pending Indent (points)", "Offset from the current market price for new pending orders.", "General")
		.SetCanOptimize(true);

		_pendingMaxSpreadPoints = Param(nameof(PendingMaxSpreadPoints), 120m)
		.SetDisplay("Max Spread (points)", "Maximum allowed spread between bid and ask to place pending orders.", "Filters")
		.SetCanOptimize(true);

		_onlyOnePosition = Param(nameof(OnlyOnePosition), false)
		.SetDisplay("Only One Position", "Restrict the strategy to a single open position at a time.", "Risk");

		_reverseLevels = Param(nameof(ReverseLevels), false)
		.SetDisplay("Reverse Levels", "Swap the direction of the pending orders.", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Signal Candles", "Time frame used to refresh pending orders and manage exits.", "General");

		ResetState();
	}

	/// <summary>
	/// Protective stop distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously active positions and pending orders.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Minimum step between existing entries and new pending prices.
	/// </summary>
	public decimal MinStepPoints
	{
		get => _minStepPoints.Value;
		set => _minStepPoints.Value = value;
	}

	/// <summary>
	/// Required profit before the trailing stop becomes active.
	/// </summary>
	public decimal TrailingActivatePoints
	{
		get => _trailingActivatePoints.Value;
		set => _trailingActivatePoints.Value = value;
	}

	/// <summary>
	/// Distance of the trailing stop after activation.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum increment for trailing stop adjustments.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Trade direction allowed for fresh pending orders.
	/// </summary>
	public TradeMode TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Defines whether stop or limit orders are used.
	/// </summary>
	public PendingOrderMode PendingType
	{
		get => _pendingType.Value;
		set => _pendingType.Value = value;
	}

	/// <summary>
	/// Life time of the pending orders in minutes.
	/// </summary>
	public int PendingExpirationMinutes
	{
		get => _pendingExpirationMinutes.Value;
		set => _pendingExpirationMinutes.Value = value;
	}

	/// <summary>
	/// Offset from the current price for pending orders.
	/// </summary>
	public decimal PendingIndentPoints
	{
		get => _pendingIndentPoints.Value;
		set => _pendingIndentPoints.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread before skipping order placement.
	/// </summary>
	public decimal PendingMaxSpreadPoints
	{
		get => _pendingMaxSpreadPoints.Value;
		set => _pendingMaxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Restricts the strategy to a single simultaneous position.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Inverts the direction of the pending orders.
	/// </summary>
	public bool ReverseLevels
	{
		get => _reverseLevels.Value;
		set => _reverseLevels.Value = value;
	}

	/// <summary>
	/// Candle type used for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = DeterminePipSize();
		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order?.Direction is not Sides side)
		return;

		var volume = trade.Trade?.Volume ?? 0m;
		var price = trade.Trade?.Price ?? 0m;
		if (volume <= 0m || price <= 0m)
		return;

		if (side == Sides.Buy)
		{
			ProcessBuyTrade(volume, price);
		}
		else if (side == Sides.Sell)
		{
			ProcessSellTrade(volume, price);
		}

		if (_longVolume <= 0m)
		{
			_longVolume = 0m;
			_longAveragePrice = null;
			_longTrailingStop = null;
		}

		if (_shortVolume <= 0m)
		{
			_shortVolume = 0m;
			_shortAveragePrice = null;
			_shortTrailingStop = null;
		}
	}

	private void ProcessBuyTrade(decimal volume, decimal price)
	{
		if (_shortVolume > 0m)
		{
			var closing = Math.Min(volume, _shortVolume);
			_shortVolume -= closing;
			volume -= closing;

			if (_shortVolume <= 0m)
			{
				_shortAveragePrice = null;
				_shortTrailingStop = null;
			}
		}

		if (volume <= 0m)
		return;

		var previous = _longVolume;
		var updated = previous + volume;

		if (previous > 0m && _longAveragePrice is decimal avg)
		{
			_longAveragePrice = ((avg * previous) + (price * volume)) / updated;
		}
		else
		{
			_longAveragePrice = price;
		}

		_longVolume = updated;
	}

	private void ProcessSellTrade(decimal volume, decimal price)
	{
		if (_longVolume > 0m)
		{
			var closing = Math.Min(volume, _longVolume);
			_longVolume -= closing;
			volume -= closing;

			if (_longVolume <= 0m)
			{
				_longAveragePrice = null;
				_longTrailingStop = null;
			}
		}

		if (volume <= 0m)
		return;

		var previous = _shortVolume;
		var updated = previous + volume;

		if (previous > 0m && _shortAveragePrice is decimal avg)
		{
			_shortAveragePrice = ((avg * previous) + (price * volume)) / updated;
		}
		else
		{
			_shortAveragePrice = price;
		}

		_shortVolume = updated;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (PendingExpirationMinutes > 0 && _pendingExpiryTime is DateTimeOffset expiry && CurrentTime >= expiry)
		{
			CancelActiveOrders();
			_pendingExpiryTime = null;
		}

		ManageLongPosition(candle);
		ManageShortPosition(candle);

		TryPlacePendingOrders(candle);
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		if (_longVolume <= 0m || _longAveragePrice is not decimal entry)
		return;

		var stopLoss = PointsToPrice(StopLossPoints);
		var takeProfit = PointsToPrice(TakeProfitPoints);
		var trailingActivate = PointsToPrice(TrailingActivatePoints);
		var trailingDistance = PointsToPrice(TrailingStopPoints);
		var trailingStep = PointsToPrice(TrailingStepPoints);

		if (StopLossPoints > 0m && stopLoss > 0m && candle.LowPrice <= entry - stopLoss)
		{
			SellMarket(_longVolume);
			return;
		}

		if (TakeProfitPoints > 0m && takeProfit > 0m && candle.HighPrice >= entry + takeProfit)
		{
			SellMarket(_longVolume);
			return;
		}

		if (TrailingStopPoints > 0m && trailingDistance > 0m && TrailingActivatePoints > 0m && trailingActivate > 0m)
		{
			if (candle.HighPrice - entry >= trailingActivate)
			{
				var candidate = candle.HighPrice - trailingDistance;
				if (!_longTrailingStop.HasValue || candidate - _longTrailingStop.Value >= trailingStep)
				_longTrailingStop = candidate;
			}

			if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
			{
				SellMarket(_longVolume);
			}
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		if (_shortVolume <= 0m || _shortAveragePrice is not decimal entry)
		return;

		var stopLoss = PointsToPrice(StopLossPoints);
		var takeProfit = PointsToPrice(TakeProfitPoints);
		var trailingActivate = PointsToPrice(TrailingActivatePoints);
		var trailingDistance = PointsToPrice(TrailingStopPoints);
		var trailingStep = PointsToPrice(TrailingStepPoints);

		if (StopLossPoints > 0m && stopLoss > 0m && candle.HighPrice >= entry + stopLoss)
		{
			BuyMarket(_shortVolume);
			return;
		}

		if (TakeProfitPoints > 0m && takeProfit > 0m && candle.LowPrice <= entry - takeProfit)
		{
			BuyMarket(_shortVolume);
			return;
		}

		if (TrailingStopPoints > 0m && trailingDistance > 0m && TrailingActivatePoints > 0m && trailingActivate > 0m)
		{
			if (entry - candle.LowPrice >= trailingActivate)
			{
				var candidate = candle.LowPrice + trailingDistance;
				if (!_shortTrailingStop.HasValue || _shortTrailingStop.Value - candidate >= trailingStep)
				_shortTrailingStop = candidate;
			}

			if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
			{
				BuyMarket(_shortVolume);
			}
		}
	}

	private void TryPlacePendingOrders(ICandleMessage candle)
	{
		if (OnlyOnePosition && (_longVolume > 0m || _shortVolume > 0m))
		return;

		if (MaxPositions > 0 && CountActiveSlots() >= MaxPositions)
		return;

		var indent = PointsToPrice(PendingIndentPoints);
		if (indent < 0m)
		indent = 0m;

		var ask = GetCurrentPrice(Sides.Buy) ?? candle.ClosePrice;
		var bid = GetCurrentPrice(Sides.Sell) ?? candle.ClosePrice;

		if (PendingMaxSpreadPoints > 0m)
		{
			var spread = ask - bid;
			if (spread < 0m)
			spread = 0m;

			if (spread > PointsToPrice(PendingMaxSpreadPoints))
			return;
		}

		CancelActiveOrders();

		if (IsDirectionAllowed(Sides.Buy))
		{
			var price = PendingType == PendingOrderMode.Stop ? ask + indent : bid - indent;
			if (ReverseLevels)
			price = PendingType == PendingOrderMode.Stop ? bid - indent : ask + indent;

			price = NormalizePrice(price);
			if (IsFarFromPositions(price))
			{
				if (PendingType == PendingOrderMode.Stop)
				BuyStop(price);
				else
				BuyLimit(price);
			}
		}

		if (IsDirectionAllowed(Sides.Sell))
		{
			var price = PendingType == PendingOrderMode.Stop ? bid - indent : ask + indent;
			if (ReverseLevels)
			price = PendingType == PendingOrderMode.Stop ? ask + indent : bid - indent;

			price = NormalizePrice(price);
			if (IsFarFromPositions(price))
			{
				if (PendingType == PendingOrderMode.Stop)
				SellStop(price);
				else
				SellLimit(price);
			}
		}

		if (PendingExpirationMinutes > 0)
		{
			_pendingExpiryTime = CurrentTime + TimeSpan.FromMinutes(PendingExpirationMinutes);
		}
		else
		{
			_pendingExpiryTime = null;
		}
	}

	private int CountActiveSlots()
	{
		var count = 0;

		if (_longVolume > 0m)
		count++;

		if (_shortVolume > 0m)
		count++;

		foreach (var order in Orders)
		{
			if (order.State == OrderStates.Active && order.Security == Security)
			count++;
		}

		return count;
	}

	private bool IsDirectionAllowed(Sides side)
	{
		return TradeMode switch
		{
			TradeMode.Buy => side == Sides.Buy,
			TradeMode.Sell => side == Sides.Sell,
			_ => true,
		};
	}

	private decimal PointsToPrice(decimal points)
	{
		if (points <= 0m || _pipSize <= 0m)
		return 0m;

		return points * _pipSize;
	}

	private bool IsFarFromPositions(decimal price)
	{
		if (MinStepPoints <= 0m)
		return true;

		var minDistance = PointsToPrice(MinStepPoints);
		if (minDistance <= 0m)
		return true;

		if (_longAveragePrice is decimal longPrice && Math.Abs(longPrice - price) < minDistance)
		return false;

		if (_shortAveragePrice is decimal shortPrice && Math.Abs(shortPrice - price) < minDistance)
		return false;

		return true;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		return security != null ? security.ShrinkPrice(price) : price;
	}

	private decimal DeterminePipSize()
	{
		var step = Security?.PriceStep;
		if (step.HasValue && step.Value > 0m)
		return step.Value;

		return 0.0001m;
	}

	private void ResetState()
	{
		_pendingExpiryTime = null;
		_longVolume = 0m;
		_shortVolume = 0m;
		_longAveragePrice = null;
		_shortAveragePrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	/// <summary>
	/// Pending order type supported by the strategy.
	/// </summary>
	public enum PendingOrderMode
	{
		/// <summary>
		/// Use stop orders placed away from the market.
		/// </summary>
		Stop,

		/// <summary>
		/// Use limit orders placed inside the current spread.
		/// </summary>
		Limit,
	}

	/// <summary>
	/// Trade direction restrictions.
	/// </summary>
	public enum TradeMode
	{
		/// <summary>
		/// Long trades only.
		/// </summary>
		Buy,

		/// <summary>
		/// Short trades only.
		/// </summary>
		Sell,

		/// <summary>
		/// Allow both long and short trades.
		/// </summary>
		BuySell,
	}
}
