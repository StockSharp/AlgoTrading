namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that automates the core ideas of the Manual Position Tracking Panel EA.
/// It maintains protective take-profit orders for the active position and can push them to break-even.
/// </summary>
public class ManualPositionTrackingPanelStrategy : Strategy
{
	private static readonly Level1Fields? StopLevelField = TryGetField("StopLevel")
		?? TryGetField("MinStopPrice")
		?? TryGetField("StopPrice")
		?? TryGetField("StopDistance");

	private static readonly Level1Fields? FreezeLevelField = TryGetField("FreezeLevel")
		?? TryGetField("FreezePrice")
		?? TryGetField("FreezeDistance");

	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableTakeProfitFromOpen;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<int> _breakEvenActivationPoints;
	private readonly StrategyParam<bool> _manageBuyPositions;
	private readonly StrategyParam<bool> _manageSellPositions;
	private readonly StrategyParam<bool> _removeTakeProfitWhenDisabled;
	private readonly StrategyParam<bool> _logActions;
	private readonly StrategyParam<int> _freezeDistanceMultiplier;

	private decimal _pointValue;
	private decimal _priceStep;
	private Order? _takeProfitOrder;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _stopLevel;
	private decimal? _freezeLevel;

	/// <summary>
	/// Initializes a new instance of the <see cref="ManualPositionTrackingPanelStrategy"/> class.
	/// </summary>
	public ManualPositionTrackingPanelStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 460)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take profit distance (pips)", "MetaTrader pips added to the entry price when creating a take-profit.", "Risk")
			.SetCanOptimize(true);

		_enableTakeProfitFromOpen = Param(nameof(EnableTakeProfitFromOpen), true)
			.SetDisplay("Enable entry-based take profit", "Automatically register a take-profit order at entry price ± distance.", "Automation");

		_enableBreakEven = Param(nameof(EnableBreakEven), false)
			.SetDisplay("Enable break-even", "Move the take-profit level to the average entry when profit reaches the trigger.", "Automation");

		_breakEvenActivationPoints = Param(nameof(BreakEvenActivationPoints), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Break-even trigger (pips)", "Minimum favorable distance in MetaTrader pips before moving to break-even.", "Automation")
			.SetCanOptimize(true);

		_manageBuyPositions = Param(nameof(ManageBuyPositions), true)
			.SetDisplay("Manage long positions", "Apply the management rules to buy positions.", "Filters");

		_manageSellPositions = Param(nameof(ManageSellPositions), true)
			.SetDisplay("Manage short positions", "Apply the management rules to sell positions.", "Filters");

		_removeTakeProfitWhenDisabled = Param(nameof(RemoveTakeProfitWhenDisabled), true)
			.SetDisplay("Remove take profit when disabled", "Cancel existing take-profit orders if management conditions are not met.", "Automation");

		_logActions = Param(nameof(LogActions), true)
			.SetDisplay("Log management actions", "Write informational messages whenever the protection is changed.", "Diagnostics");

		_freezeDistanceMultiplier = Param(nameof(FreezeDistanceMultiplier), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Freeze distance multiplier", "Fallback multiplier applied to the current spread when broker levels are unknown.", "Risk");
	}

	/// <summary>
	/// Distance from the entry price that will be used when placing take-profit orders (MetaTrader pips).
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set
		{
			_takeProfitPoints.Value = value;
			TryUpdateTakeProfit();
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether the strategy should automatically place a take-profit from the entry price.
	/// </summary>
	public bool EnableTakeProfitFromOpen
	{
		get => _enableTakeProfitFromOpen.Value;
		set
		{
			_enableTakeProfitFromOpen.Value = value;
			TryUpdateTakeProfit();
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether the strategy should push the take-profit to break-even.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set
		{
			_enableBreakEven.Value = value;
			TryUpdateTakeProfit();
		}
	}

	/// <summary>
	/// Minimum favorable distance in MetaTrader pips before the break-even adjustment is applied.
	/// </summary>
	public int BreakEvenActivationPoints
	{
		get => _breakEvenActivationPoints.Value;
		set
		{
			_breakEvenActivationPoints.Value = value;
			TryUpdateTakeProfit();
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether long positions should be managed.
	/// </summary>
	public bool ManageBuyPositions
	{
		get => _manageBuyPositions.Value;
		set
		{
			_manageBuyPositions.Value = value;
			TryUpdateTakeProfit();
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether short positions should be managed.
	/// </summary>
	public bool ManageSellPositions
	{
		get => _manageSellPositions.Value;
		set
		{
			_manageSellPositions.Value = value;
			TryUpdateTakeProfit();
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether existing take-profit orders should be removed when management is disabled.
	/// </summary>
	public bool RemoveTakeProfitWhenDisabled
	{
		get => _removeTakeProfitWhenDisabled.Value;
		set
		{
			_removeTakeProfitWhenDisabled.Value = value;
			TryUpdateTakeProfit();
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether informational messages should be written to the log.
	/// </summary>
	public bool LogActions
	{
		get => _logActions.Value;
		set => _logActions.Value = value;
	}

	/// <summary>
	/// Fallback multiplier used when the broker does not provide freeze or stop levels.
	/// </summary>
	public int FreezeDistanceMultiplier
	{
		get => _freezeDistanceMultiplier.Value;
		set
		{
			_freezeDistanceMultiplier.Value = value;
			TryUpdateTakeProfit();
		}
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = CalculatePointValue();
		_priceStep = GetPriceStep();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		TryUpdateTakeProfit();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelTakeProfitOrder();

		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		TryUpdateTakeProfit();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		TryUpdateTakeProfit();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
		_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
		_bestAsk = ask;

		if (StopLevelField is Level1Fields stopField && message.Changes.TryGetValue(stopField, out var stopValue))
		_stopLevel = ToDecimal(stopValue);

		if (FreezeLevelField is Level1Fields freezeField && message.Changes.TryGetValue(freezeField, out var freezeValue))
		_freezeLevel = ToDecimal(freezeValue);

		TryUpdateTakeProfit();
	}

	private void TryUpdateTakeProfit()
	{
		if (ProcessState != ProcessStates.Started)
		return;

		var security = Security;
		var portfolio = Portfolio;
		if (security == null || portfolio == null)
		{
			CancelTakeProfitOrder();
			return;
		}

		var position = portfolio.Positions.FirstOrDefault(p => p.Security == security);
		if (position == null || position.CurrentValue == 0m)
		{
			CancelTakeProfitOrder();
			return;
		}

		var isLong = position.CurrentValue > 0m;
		if ((isLong && !ManageBuyPositions) || (!isLong && !ManageSellPositions))
		{
			if (RemoveTakeProfitWhenDisabled)
			CancelTakeProfitOrder();
			return;
		}

		if (position.AveragePrice is not decimal averagePrice || averagePrice <= 0m)
		{
			CancelTakeProfitOrder();
			return;
		}

		decimal? targetPrice = null;
		var distanceFromEntry = TakeProfitPoints * _pointValue;

		if (EnableTakeProfitFromOpen && distanceFromEntry > 0m)
		{
			targetPrice = isLong
			? averagePrice + distanceFromEntry
			: averagePrice - distanceFromEntry;
		}

		var referencePrice = isLong ? _bestBid : _bestAsk;
		if (EnableBreakEven && referencePrice is decimal marketPrice)
		{
			var requiredDistance = BreakEvenActivationPoints * _pointValue;
			var favorableMove = Math.Abs(marketPrice - averagePrice);

			if (requiredDistance <= 0m || favorableMove >= requiredDistance)
			{
				var breakEvenPrice = averagePrice;
				targetPrice = targetPrice == null
				? breakEvenPrice
				: isLong
				? Math.Min(targetPrice.Value, breakEvenPrice)
				: Math.Max(targetPrice.Value, breakEvenPrice);
			}
		}

		if (targetPrice == null)
		{
			if (RemoveTakeProfitWhenDisabled)
			CancelTakeProfitOrder();
			return;
		}

		if (referencePrice is decimal price)
		{
			var minimalDistance = Math.Max(GetMinimalDistance(), _pointValue);
			if (isLong && targetPrice.Value <= price)
			targetPrice = price + minimalDistance;
			else if (!isLong && targetPrice.Value >= price)
			targetPrice = price - minimalDistance;
		}

		var normalizedPrice = NormalizePrice(targetPrice.Value);
		if (normalizedPrice <= 0m)
		{
			CancelTakeProfitOrder();
			return;
		}

		var volume = NormalizeVolume(Math.Abs(position.CurrentValue));
		if (volume <= 0m)
		{
			CancelTakeProfitOrder();
			return;
		}

		UpdateTakeProfitOrder(normalizedPrice, volume, isLong);
	}

	private void UpdateTakeProfitOrder(decimal price, decimal volume, bool isLong)
	{
		if (_takeProfitOrder == null)
		{
			_takeProfitOrder = isLong
			? SellLimit(volume, price)
			: BuyLimit(volume, price);
			LogAction($"Registered {(isLong ? "SELL" : "BUY")} take profit at {price:0.#####} for volume {volume:0.#####}.");
			return;
		}

		if (_takeProfitOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_takeProfitOrder = null;
			UpdateTakeProfitOrder(price, volume, isLong);
			return;
		}

		if (_takeProfitOrder.Price != price || _takeProfitOrder.Volume != volume)
		{
			ReRegisterOrder(_takeProfitOrder, price, volume);
			LogAction($"Adjusted take profit to {price:0.#####} with volume {volume:0.#####}.");
		}
	}

	private void CancelTakeProfitOrder()
	{
		if (_takeProfitOrder == null)
		return;

		if (_takeProfitOrder.State is OrderStates.Active or OrderStates.Pending)
		{
			CancelOrder(_takeProfitOrder);
			LogAction("Canceled active take-profit order.");
		}

		_takeProfitOrder = null;
	}

	private decimal GetMinimalDistance()
	{
		var level = Math.Max(_stopLevel ?? 0m, _freezeLevel ?? 0m);
		if (level > 0m)
		return level;

		if (_bestBid is decimal bid && _bestAsk is decimal ask)
		{
			var spread = Math.Abs(ask - bid);
			if (spread > 0m)
			{
				var multiplier = Math.Max(1m, FreezeDistanceMultiplier);
				return spread * multiplier;
			}
		}

		return 0m;
	}

	private decimal CalculatePointValue()
	{
		var security = Security;
		if (security == null)
		return 0.0001m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
			step = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (step <= 0m)
		step = 0.0001m;

		var multiplier = 1m;
		var digits = security.Decimals;
		if (digits != null && (digits.Value == 3 || digits.Value == 5))
		multiplier = 10m;

		return step * multiplier;
	}

	private decimal GetPriceStep()
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

		return step;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
		return price;

		var steps = Math.Round(price / _priceStep, MidpointRounding.AwayFromZero);
		return steps * _priceStep;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			volume = steps * step;
		}

		var min = security.MinVolume;
		if (min != null && volume < min.Value)
		volume = min.Value;

		var max = security.MaxVolume;
		if (max != null && volume > max.Value)
		volume = max.Value;

		return volume;
	}

	private void LogAction(string message)
	{
		if (!LogActions || string.IsNullOrWhiteSpace(message))
		return;

		LogInfo(message);
	}

	private static Level1Fields? TryGetField(string name)
	{
		return Enum.TryParse(name, out Level1Fields field) ? field : null;
	}

	private static decimal? ToDecimal(object value)
	{
		return value switch
		{
			double dbl => (decimal)dbl,
			float flt => (decimal)flt,
			decimal dec => dec,
			int i => i,
			long l => l,
			string str when decimal.TryParse(str, out var parsed) => parsed,
			_ => null,
		};
	}
}
