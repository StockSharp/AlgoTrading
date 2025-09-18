using System;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Risk and order management module inspired by the MetaTrader "Master Exit Plan" expert advisor.
/// </summary>
public class MasterExitPlanStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableTargetEquity;
	private readonly StrategyParam<decimal> _targetEquityPercent;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _enableDynamicStopLoss;
	private readonly StrategyParam<decimal> _dynamicStopLossPoints;
	private readonly StrategyParam<bool> _enableHiddenStopLoss;
	private readonly StrategyParam<decimal> _hiddenStopLossPoints;
	private readonly StrategyParam<bool> _enableHiddenDynamicStopLoss;
	private readonly StrategyParam<decimal> _hiddenDynamicStopLossPoints;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingTargetPercent;
	private readonly StrategyParam<decimal> _sureProfitPoints;
	private readonly StrategyParam<bool> _enableTrailPendingOrders;
	private readonly StrategyParam<decimal> _trailPendingOrderPoints;

	private decimal _equityTarget;
	private decimal? _lastMinuteOpen;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Enables the portfolio equity based exit.
	/// </summary>
	public bool EnableTargetEquity
	{
		get => _enableTargetEquity.Value;
		set => _enableTargetEquity.Value = value;
	}

	/// <summary>
	/// Percentage gain over current balance that triggers the equity exit.
	/// </summary>
	public decimal TargetEquityPercent
	{
		get => _targetEquityPercent.Value;
		set => _targetEquityPercent.Value = value;
	}

	/// <summary>
	/// Enables the fixed stop-loss expressed in points from the entry price.
	/// </summary>
	public bool EnableStopLoss
	{
		get => _enableStopLoss.Value;
		set => _enableStopLoss.Value = value;
	}

	/// <summary>
	/// Fixed stop-loss distance in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enables the dynamic stop anchored to the previous one-minute open price.
	/// </summary>
	public bool EnableDynamicStopLoss
	{
		get => _enableDynamicStopLoss.Value;
		set => _enableDynamicStopLoss.Value = value;
	}

	/// <summary>
	/// Dynamic stop distance in MetaTrader points.
	/// </summary>
	public decimal DynamicStopLossPoints
	{
		get => _dynamicStopLossPoints.Value;
		set => _dynamicStopLossPoints.Value = value;
	}

	/// <summary>
	/// Enables the hidden stop-loss executed by the strategy code.
	/// </summary>
	public bool EnableHiddenStopLoss
	{
		get => _enableHiddenStopLoss.Value;
		set => _enableHiddenStopLoss.Value = value;
	}

	/// <summary>
	/// Hidden stop-loss distance in MetaTrader points.
	/// </summary>
	public decimal HiddenStopLossPoints
	{
		get => _hiddenStopLossPoints.Value;
		set => _hiddenStopLossPoints.Value = value;
	}

	/// <summary>
	/// Enables the hidden dynamic stop based on the latest minute candle.
	/// </summary>
	public bool EnableHiddenDynamicStopLoss
	{
		get => _enableHiddenDynamicStopLoss.Value;
		set => _enableHiddenDynamicStopLoss.Value = value;
	}

	/// <summary>
	/// Hidden dynamic stop distance in MetaTrader points.
	/// </summary>
	public decimal HiddenDynamicStopLossPoints
	{
		get => _hiddenDynamicStopLossPoints.Value;
		set => _hiddenDynamicStopLossPoints.Value = value;
	}

	/// <summary>
	/// Enables the trailing stop block.
	/// </summary>
	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in MetaTrader points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Profit percentage of the account balance required before the trailing stop activates.
	/// </summary>
	public decimal TrailingTargetPercent
	{
		get => _trailingTargetPercent.Value;
		set => _trailingTargetPercent.Value = value;
	}

	/// <summary>
	/// Additional buffer in points that must be accumulated before arming the trailing stop.
	/// </summary>
	public decimal SureProfitPoints
	{
		get => _sureProfitPoints.Value;
		set => _sureProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables trailing of pending stop orders.
	/// </summary>
	public bool EnableTrailPendingOrders
	{
		get => _enableTrailPendingOrders.Value;
		set => _enableTrailPendingOrders.Value = value;
	}

	/// <summary>
	/// Distance in points between the market price and trailing pending orders.
	/// </summary>
	public decimal TrailPendingOrderPoints
	{
		get => _trailPendingOrderPoints.Value;
		set => _trailPendingOrderPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MasterExitPlanStrategy"/> class.
	/// </summary>
	public MasterExitPlanStrategy()
	{
		_enableTargetEquity = Param(nameof(EnableTargetEquity), false)
		.SetDisplay("Enable Target Equity", "Close all trades once equity reaches the configured percentage gain.", "Equity management");

		_targetEquityPercent = Param(nameof(TargetEquityPercent), 1m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Target Equity %", "Percentage gain over balance required before closing every position.", "Equity management");

		_enableStopLoss = Param(nameof(EnableStopLoss), false)
		.SetDisplay("Enable Stop-Loss", "Activate the broker-side style stop-loss logic.", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 2000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop-Loss Points", "Static stop distance in MetaTrader points.", "Risk");

		_enableDynamicStopLoss = Param(nameof(EnableDynamicStopLoss), false)
		.SetDisplay("Enable Dynamic Stop-Loss", "Re-anchor the stop to the latest minute candle open.", "Risk");

		_dynamicStopLossPoints = Param(nameof(DynamicStopLossPoints), 2000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Dynamic Stop Points", "Dynamic stop distance in MetaTrader points.", "Risk");

		_enableHiddenStopLoss = Param(nameof(EnableHiddenStopLoss), false)
		.SetDisplay("Enable Hidden Stop", "Close positions internally when the hidden static level is breached.", "Risk");

		_hiddenStopLossPoints = Param(nameof(HiddenStopLossPoints), 800m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Hidden Stop Points", "Hidden static stop distance in MetaTrader points.", "Risk");

		_enableHiddenDynamicStopLoss = Param(nameof(EnableHiddenDynamicStopLoss), false)
		.SetDisplay("Enable Hidden Dynamic Stop", "Close positions internally when the minute-based dynamic level is breached.", "Risk");

		_hiddenDynamicStopLossPoints = Param(nameof(HiddenDynamicStopLossPoints), 800m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Hidden Dynamic Stop Points", "Hidden dynamic stop distance in MetaTrader points.", "Risk");

		_enableTrailingStop = Param(nameof(EnableTrailingStop), false)
		.SetDisplay("Enable Trailing Stop", "Track profits once the configured percentage gain is reached.", "Trailing");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 5m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop Points", "Trailing distance maintained behind the market.", "Trailing");

		_trailingTargetPercent = Param(nameof(TrailingTargetPercent), 0.2m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Target %", "Minimum percentage gain (of balance) required to activate the trailing stop.", "Trailing");

		_sureProfitPoints = Param(nameof(SureProfitPoints), 30m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Sure Profit Points", "Additional cushion that must be accumulated before moving the stop.", "Trailing");

		_enableTrailPendingOrders = Param(nameof(EnableTrailPendingOrders), false)
		.SetDisplay("Enable Pending Order Trailing", "Allow the strategy to re-place pending stop orders closer to the market.", "Pending orders");

		_trailPendingOrderPoints = Param(nameof(TrailPendingOrderPoints), 10m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Pending Trailing Points", "Distance in points between the market price and pending stop orders.", "Pending orders");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_equityTarget = CalculateNextEquityTarget();
		TimerInterval = TimeSpan.FromSeconds(1);

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(1).TimeFrame());
		subscription.Bind(ProcessMinuteCandle).Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_equityTarget = 0m;
		_lastMinuteOpen = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private void ProcessMinuteCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastMinuteOpen = candle.OpenPrice;
	}

	/// <inheritdoc />
	protected override void OnTimer()
	{
		base.OnTimer();

		var security = Security;
		if (security == null)
			return;

		var bid = security.BestBid?.Price ?? security.LastTrade?.Price ?? 0m;
		var ask = security.BestAsk?.Price ?? security.LastTrade?.Price ?? 0m;

		if (EnableTargetEquity)
			CheckTargetEquity();

		if (EnableStopLoss || EnableDynamicStopLoss)
			CheckHardStops(bid, ask);

		if (EnableHiddenStopLoss || EnableHiddenDynamicStopLoss)
			CheckHiddenStops(bid, ask);

		if (EnableTrailingStop)
			CheckTrailingStop(bid, ask);
		else
			ResetTrailingStops();

		if (EnableTrailPendingOrders)
			TrailPendingOrders(bid, ask);
	}

	private void CheckTargetEquity()
	{
		if (_equityTarget <= 0m)
			return;

		var portfolio = Portfolio;
		var equity = portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return;

		if (equity < _equityTarget)
			return;

		LogInfo($"Target equity {_equityTarget:F2} reached. Closing all exposure.");
		CloseAllPositions();
		_equityTarget = CalculateNextEquityTarget();
	}

	private void CheckHardStops(decimal bid, decimal ask)
	{
		var position = Position;
		if (position == 0m)
			return;

		var point = GetPointValue();
		if (point <= 0m)
			return;

		if (position > 0m)
		{
			var staticStop = EnableStopLoss && StopLossPoints > 0m
				? PositionAvgPrice - StopLossPoints * point
				: (decimal?)null;

			var dynamicStop = EnableDynamicStopLoss && _lastMinuteOpen is decimal open
				? open - DynamicStopLossPoints * point
				: (decimal?)null;

			var stop = CombineLongStops(staticStop, dynamicStop);
			if (stop is decimal level && level > 0m && bid > 0m && bid <= level)
			{
				LogInfo($"Hard stop triggered at {level:F5}. Bid={bid:F5}.");
				SellMarket(Math.Abs(position));
			}
		}
		else if (position < 0m)
		{
			var staticStop = EnableStopLoss && StopLossPoints > 0m
				? PositionAvgPrice + StopLossPoints * point
				: (decimal?)null;

			var dynamicStop = EnableDynamicStopLoss && _lastMinuteOpen is decimal open
				? open + DynamicStopLossPoints * point
				: (decimal?)null;

			var stop = CombineShortStops(staticStop, dynamicStop);
			if (stop is decimal level && level > 0m && ask > 0m && ask >= level)
			{
				LogInfo($"Hard stop triggered at {level:F5}. Ask={ask:F5}.");
				BuyMarket(Math.Abs(position));
			}
		}
	}

	private void CheckHiddenStops(decimal bid, decimal ask)
	{
		var position = Position;
		if (position == 0m)
			return;

		var point = GetPointValue();
		if (point <= 0m)
			return;

		if (position > 0m)
		{
			var staticStop = EnableHiddenStopLoss && HiddenStopLossPoints > 0m
				? PositionAvgPrice - HiddenStopLossPoints * point
				: (decimal?)null;

			var dynamicStop = EnableHiddenDynamicStopLoss && _lastMinuteOpen is decimal open
				? open - HiddenDynamicStopLossPoints * point
				: (decimal?)null;

			var stop = CombineLongStops(staticStop, dynamicStop);
			if (stop is decimal level && level > 0m && ask > 0m && ask <= level)
			{
				LogInfo($"Hidden stop triggered at {level:F5}. Ask={ask:F5}.");
				SellMarket(Math.Abs(position));
			}
		}
		else if (position < 0m)
		{
			var staticStop = EnableHiddenStopLoss && HiddenStopLossPoints > 0m
				? PositionAvgPrice + HiddenStopLossPoints * point
				: (decimal?)null;

			var dynamicStop = EnableHiddenDynamicStopLoss && _lastMinuteOpen is decimal open
				? open + HiddenDynamicStopLossPoints * point
				: (decimal?)null;

			var stop = CombineShortStops(staticStop, dynamicStop);
			if (stop is decimal level && level > 0m && bid > 0m && bid >= level)
			{
				LogInfo($"Hidden stop triggered at {level:F5}. Bid={bid:F5}.");
				BuyMarket(Math.Abs(position));
			}
		}
	}

	private void CheckTrailingStop(decimal bid, decimal ask)
	{
		var position = Position;
		if (position == 0m)
		{
			ResetTrailingStops();
			return;
		}

		var point = GetPointValue();
		if (point <= 0m)
			return;

		var spreadPoints = 0m;
		if (bid > 0m && ask > 0m && ask > bid)
			spreadPoints = (ask - bid) / point;

		var profit = CalculateUnrealizedPnL(position > 0m ? bid : ask);
		var target = Math.Max(0.2m, GetPortfolioBalance() * TrailingTargetPercent / 100m);
		if (profit < target)
			return;

		if (position > 0m)
		{
			var entry = PositionAvgPrice;
			var requiredMove = (TrailingStopPoints + SureProfitPoints + spreadPoints) * point;
			if (bid <= 0m || entry <= 0m || bid - entry <= requiredMove)
				return;

			var newStop = bid - (TrailingStopPoints + spreadPoints) * point;
			if (_longTrailingStop is null || newStop > _longTrailingStop.Value)
			{
				_longTrailingStop = newStop;
				LogInfo($"Updated long trailing stop to {newStop:F5}.");
			}

			if (_longTrailingStop is decimal stop && bid <= stop)
			{
				LogInfo($"Long trailing stop hit at {stop:F5}. Bid={bid:F5}.");
				SellMarket(Math.Abs(position));
				_longTrailingStop = null;
			}
		}
		else if (position < 0m)
		{
			var entry = PositionAvgPrice;
			var requiredMove = (TrailingStopPoints + SureProfitPoints + spreadPoints) * point;
			if (ask <= 0m || entry <= 0m || entry - ask <= requiredMove)
				return;

			var newStop = ask + (TrailingStopPoints + spreadPoints) * point;
			if (_shortTrailingStop is null || newStop < _shortTrailingStop.Value)
			{
				_shortTrailingStop = newStop;
				LogInfo($"Updated short trailing stop to {newStop:F5}.");
			}

			if (_shortTrailingStop is decimal stop && ask >= stop)
			{
				LogInfo($"Short trailing stop hit at {stop:F5}. Ask={ask:F5}.");
				BuyMarket(Math.Abs(position));
				_shortTrailingStop = null;
			}
		}
	}

	private void ResetTrailingStops()
	{
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private void TrailPendingOrders(decimal bid, decimal ask)
	{
		var point = GetPointValue();
		if (point <= 0m)
			return;

		var spreadPoints = 0m;
		if (bid > 0m && ask > 0m && ask > bid)
			spreadPoints = (ask - bid) / point;

		var levelPoints = TrailPendingOrderPoints + Math.Max(spreadPoints, GetStopLevelPoints());
		if (levelPoints <= 0m)
			return;

		var buyStopPrice = ask > 0m ? ask + levelPoints * point : 0m;
		var sellStopPrice = bid > 0m ? bid - levelPoints * point : 0m;

		foreach (var order in ActiveOrders)
		{
			if (order.Type != OrderTypes.Stop || order.Price is not decimal price)
				continue;

			if (order.Direction == Sides.Buy && buyStopPrice > 0m && price > buyStopPrice)
			{
				ReRegisterOrder(order, buyStopPrice, order.Volume ?? 0m);
				LogInfo($"Trailing buy-stop order to {buyStopPrice:F5}.");
			}
			else if (order.Direction == Sides.Sell && sellStopPrice > 0m && price < sellStopPrice)
			{
				ReRegisterOrder(order, sellStopPrice, order.Volume ?? 0m);
				LogInfo($"Trailing sell-stop order to {sellStopPrice:F5}.");
			}
		}
	}

	private decimal CalculateNextEquityTarget()
	{
		if (!EnableTargetEquity)
			return 0m;

		var balance = GetPortfolioBalance();
		if (balance <= 0m)
			return 0m;

		return balance + balance * TargetEquityPercent / 100m;
	}

	private decimal GetPortfolioBalance()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		return portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
	}

	private decimal GetPointValue()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
				step = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (step <= 0m)
			return 0m;

		if (step == 0.00001m || step == 0.001m)
			return step * 10m;

		return step;
	}

	private decimal? CombineLongStops(decimal? first, decimal? second)
	{
		decimal? result = null;
		if (first is decimal value && value > 0m)
			result = value;

		if (second is decimal value2 && value2 > 0m)
			result = result is null ? value2 : Math.Max(result.Value, value2);

		return result;
	}

	private decimal? CombineShortStops(decimal? first, decimal? second)
	{
		decimal? result = null;
		if (first is decimal value && value > 0m)
			result = value;

		if (second is decimal value2 && value2 > 0m)
			result = result is null ? value2 : Math.Min(result.Value, value2);

		return result;
	}

	private decimal CalculateUnrealizedPnL(decimal price)
	{
		if (Position == 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return 0m;

		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
			return 0m;

		var diff = price - PositionAvgPrice;
		var steps = diff / priceStep;
		return steps * stepPrice * Position;
	}

	private decimal GetStopLevelPoints()
	{
		var security = Security;
		if (security?.ExtensionInfo == null)
			return 0m;

		if (security.ExtensionInfo.TryGetValue("StopLevel", out var raw))
		{
			if (raw is decimal dec)
				return dec;

			if (raw is double dbl)
				return (decimal)dbl;

			if (raw is string str && decimal.TryParse(str, out var parsed))
				return parsed;
		}

		return 0m;
	}

	private void CloseAllPositions()
	{
		CancelActiveOrders();

		var position = Position;
		if (position > 0m)
		{
			SellMarket(position);
		}
		else if (position < 0m)
		{
			BuyMarket(Math.Abs(position));
		}

		ResetTrailingStops();
	}
}
