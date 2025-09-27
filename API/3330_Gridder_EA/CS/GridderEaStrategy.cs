using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gridder EA strategy converted from the original MetaTrader expert advisor.
/// Implements a multi-stage grid with flexible step and lot progressions, basket management,
/// and emergency hedging when the number of averaging trades becomes excessive.
/// </summary>
public class GridderEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _gridStepPips;
	private readonly StrategyParam<decimal> _stepMultiplier;
	private readonly StrategyParam<decimal> _targetProfitPerLot;
	private readonly StrategyParam<decimal> _targetLossPerLot;
	private readonly StrategyParam<int> _maxOrdersPerSide;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<StepProgressions> _stepMode;
	private readonly StrategyParam<LotProgressions> _lotMode;
	private readonly StrategyParam<bool> _useEmergencyMode;
	private readonly StrategyParam<int> _emergencyOrdersTrigger;
	private readonly StrategyParam<decimal> _hedgeVolumeFactor;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<GridEntry> _buyEntries = new();
	private readonly List<GridEntry> _sellEntries = new();

	private bool _buyEmergencyActive;
	private bool _sellEmergencyActive;
	private decimal _lastBuyReferencePrice;
	private decimal _lastSellReferencePrice;
	private bool _hasBuyReference;
	private bool _hasSellReference;

	/// <summary>
	/// Initializes a new instance of the <see cref="GridderEaStrategy"/> class.
	/// </summary>
	public GridderEaStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Volume", "Base volume for the first grid order", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.4m)
		.SetGreaterThanZero()
		.SetDisplay("Volume Multiplier", "Factor applied to the previous order volume", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.1m);

		_gridStepPips = Param(nameof(GridStepPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Grid Step (pips)", "Base distance between consecutive entries", "Grid")
		.SetCanOptimize(true)
		.SetOptimize(10m, 150m, 10m);

		_stepMultiplier = Param(nameof(StepMultiplier), 1.2m)
		.SetGreaterThanZero()
		.SetDisplay("Step Multiplier", "Progression applied to the grid step", "Grid")
		.SetCanOptimize(true)
		.SetOptimize(1m, 2m, 0.1m);

		_targetProfitPerLot = Param(nameof(TargetProfitPerLot), 15m)
		.SetNotNegative()
		.SetDisplay("Target Profit / Lot", "Money profit per lot that closes all trades", "Risk");

		_targetLossPerLot = Param(nameof(TargetLossPerLot), 60m)
		.SetNotNegative()
		.SetDisplay("Target Loss / Lot", "Money loss per lot that liquidates the basket", "Risk");

		_maxOrdersPerSide = Param(nameof(MaxOrdersPerSide), 10)
		.SetNotNegative()
		.SetDisplay("Max Orders Per Side", "Maximum averaging trades on each side", "Risk");

		_allowLong = Param(nameof(AllowLong), true)
		.SetDisplay("Allow Long", "Enable long side trading", "Trading");

		_allowShort = Param(nameof(AllowShort), true)
		.SetDisplay("Allow Short", "Enable short side trading", "Trading");

		_stepMode = Param(nameof(StepMode), StepProgressions.Geometric)
		.SetDisplay("Step Mode", "Rule used to increase the grid spacing", "Grid");

		_lotMode = Param(nameof(LotMode), LotProgressions.Geometric)
		.SetDisplay("Lot Mode", "Rule used to increase the order volume", "Trading");

		_useEmergencyMode = Param(nameof(UseEmergencyMode), true)
		.SetDisplay("Use Emergency Mode", "Enable hedge protection when too many orders accumulate", "Protection");

		_emergencyOrdersTrigger = Param(nameof(EmergencyOrdersTrigger), 5)
		.SetNotNegative()
		.SetDisplay("Emergency Trigger", "Orders per side that activate the hedge", "Protection");

		_hedgeVolumeFactor = Param(nameof(HedgeVolumeFactor), 0.5m)
		.SetNotNegative()
		.SetDisplay("Hedge Volume Factor", "Fraction of total volume hedged in emergency mode", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for grid recalculation", "General");
	}

	/// <summary>
	/// Initial order volume.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Multiplier used to increase volume for subsequent orders.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Base distance between grid orders in pips.
	/// </summary>
	public decimal GridStepPips
	{
		get => _gridStepPips.Value;
		set => _gridStepPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the step when using geometric or exponential progressions.
	/// </summary>
	public decimal StepMultiplier
	{
		get => _stepMultiplier.Value;
		set => _stepMultiplier.Value = value;
	}

	/// <summary>
	/// Profit target in account currency per lot of open volume.
	/// </summary>
	public decimal TargetProfitPerLot
	{
		get => _targetProfitPerLot.Value;
		set => _targetProfitPerLot.Value = value;
	}

	/// <summary>
	/// Loss threshold in account currency per lot of open volume.
	/// </summary>
	public decimal TargetLossPerLot
	{
		get => _targetLossPerLot.Value;
		set => _targetLossPerLot.Value = value;
	}

	/// <summary>
	/// Maximum number of averaging trades allowed on each side.
	/// </summary>
	public int MaxOrdersPerSide
	{
		get => _maxOrdersPerSide.Value;
		set => _maxOrdersPerSide.Value = value;
	}

	/// <summary>
	/// Enable buying operations.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	/// <summary>
	/// Enable selling operations.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	/// <summary>
	/// Step progression mode.
	/// </summary>
	public StepProgressions StepMode
	{
		get => _stepMode.Value;
		set => _stepMode.Value = value;
	}

	/// <summary>
	/// Volume progression mode.
	/// </summary>
	public LotProgressions LotMode
	{
		get => _lotMode.Value;
		set => _lotMode.Value = value;
	}

	/// <summary>
	/// Enable emergency hedge logic.
	/// </summary>
	public bool UseEmergencyMode
	{
		get => _useEmergencyMode.Value;
		set => _useEmergencyMode.Value = value;
	}

	/// <summary>
	/// Number of trades that activates emergency hedging.
	/// </summary>
	public int EmergencyOrdersTrigger
	{
		get => _emergencyOrdersTrigger.Value;
		set => _emergencyOrdersTrigger.Value = value;
	}

	/// <summary>
	/// Fraction of the accumulated volume hedged when emergency mode triggers.
	/// </summary>
	public decimal HedgeVolumeFactor
	{
		get => _hedgeVolumeFactor.Value;
		set => _hedgeVolumeFactor.Value = value;
	}

	/// <summary>
	/// Candle type used for grid management.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyEntries.Clear();
		_sellEntries.Clear();
		_buyEmergencyActive = false;
		_sellEmergencyActive = false;
		_hasBuyReference = false;
		_hasSellReference = false;
		_lastBuyReferencePrice = 0m;
		_lastSellReferencePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var point = GetPointValue();
		if (point <= 0m)
		return;

		var price = candle.ClosePrice;
		var baseStep = GridStepPips * point;

		if (AllowLong)
		HandleSide(true, price, baseStep);
		else if (_buyEntries.Count > 0)
		CloseBuys();

		if (AllowShort)
		HandleSide(false, price, baseStep);
		else if (_sellEntries.Count > 0)
		CloseSells();

		if (TargetProfitPerLot > 0m || TargetLossPerLot > 0m)
		CheckBasketTargets(price, point);
	}

	private void HandleSide(bool isBuy, decimal price, decimal baseStep)
	{
		var entries = isBuy ? _buyEntries : _sellEntries;
		var reference = isBuy ? _lastBuyReferencePrice : _lastSellReferencePrice;
		var hasReference = isBuy ? _hasBuyReference : _hasSellReference;

		if (!hasReference)
		{
			reference = price;
			hasReference = true;
		}

		var directionMultiplier = entries.Count + 1;
		var requiredStep = GetProgressiveStep(baseStep, directionMultiplier);

		var shouldOpen = isBuy
		? reference - price >= requiredStep
		: price - reference >= requiredStep;

		if (shouldOpen)
		{
			var volume = CalculateNextVolume(entries);
			if (MaxOrdersPerSide == 0 || entries.Count < MaxOrdersPerSide)
			{
				if (isBuy)
				BuyMarket(volume);
				else
				SellMarket(volume);
			}

			reference = price;
		}

		if (isBuy)
		{
			_lastBuyReferencePrice = reference;
			_hasBuyReference = hasReference;
			if (UseEmergencyMode)
			CheckEmergency(entries, true, price);
		}
		else
		{
			_lastSellReferencePrice = reference;
			_hasSellReference = hasReference;
			if (UseEmergencyMode)
			CheckEmergency(entries, false, price);
		}
	}

	private void CheckEmergency(List<GridEntry> entries, bool isBuy, decimal price)
	{
		if (EmergencyOrdersTrigger <= 0)
		return;

		if (entries.Count < EmergencyOrdersTrigger)
		{
			if (isBuy)
			_buyEmergencyActive = false;
			else
			_sellEmergencyActive = false;
			return;
		}

		var totalVolume = GetTotalVolume(entries);
		var hedgeVolume = RoundVolume(totalVolume * HedgeVolumeFactor);
		if (hedgeVolume <= 0m)
		return;

		if (isBuy)
		{
			if (_buyEmergencyActive)
			return;

			_sellEmergencyActive = true;
			_buyEmergencyActive = true;
			SellMarket(hedgeVolume);
			_lastSellReferencePrice = price;
			_hasSellReference = true;
		}
		else
		{
			if (_sellEmergencyActive)
			return;

			_buyEmergencyActive = true;
			_sellEmergencyActive = true;
			BuyMarket(hedgeVolume);
			_lastBuyReferencePrice = price;
			_hasBuyReference = true;
		}
	}

	private void CheckBasketTargets(decimal price, decimal point)
	{
		var stepPrice = Security.StepPrice ?? 1m;

		var buyProfit = CalculateUnrealizedProfit(price, _buyEntries, true, point, stepPrice);
		var sellProfit = CalculateUnrealizedProfit(price, _sellEntries, false, point, stepPrice);

		var totalVolume = GetTotalVolume(_buyEntries) + GetTotalVolume(_sellEntries);
		if (totalVolume <= 0m)
		return;

		var profitTarget = TargetProfitPerLot * totalVolume;
		var lossTarget = TargetLossPerLot * totalVolume;
		var totalProfit = buyProfit + sellProfit;

		if (TargetProfitPerLot > 0m && totalProfit >= profitTarget)
		{
			CloseBuys();
			CloseSells();
			return;
		}

		if (TargetLossPerLot > 0m && totalProfit <= -lossTarget)
		{
			CloseBuys();
			CloseSells();
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		var tradeInfo = trade.Trade;
		if (order == null || tradeInfo == null)
		return;

		var volume = tradeInfo.Volume ?? order.Volume;
		if (volume == null)
		return;

		var price = tradeInfo.Price;

		if (order.Direction == Sides.Buy)
		{
			_buyEntries.Add(new GridEntry(price, volume.Value));
			_lastBuyReferencePrice = price;
			_hasBuyReference = true;
		}
		else if (order.Direction == Sides.Sell)
		{
			_sellEntries.Add(new GridEntry(price, volume.Value));
			_lastSellReferencePrice = price;
			_hasSellReference = true;
		}
	}

	/// <inheritdoc />
	protected override void OnOrderRegistered(Order order)
	{
		base.OnOrderRegistered(order);

		if (order.Direction == Sides.Buy)
		order.Volume = RoundVolume(order.Volume ?? 0m);
		else if (order.Direction == Sides.Sell)
		order.Volume = RoundVolume(order.Volume ?? 0m);
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order.State == OrderStates.Done)
		return;

		order.Volume = RoundVolume(order.Volume ?? 0m);
	}

	/// <inheritdoc />
	protected override void OnNewOrderFail(OrderFail fail)
	{
		base.OnNewOrderFail(fail);

		if (fail.Order.Direction == Sides.Buy)
		_hasBuyReference = false;
		else if (fail.Order.Direction == Sides.Sell)
		_hasSellReference = false;
	}

	private decimal CalculateNextVolume(List<GridEntry> entries)
	{
		var baseVolume = InitialVolume;
		if (entries.Count == 0)
		return RoundVolume(baseVolume);

		var level = entries.Count;
		var multiplier = GetProgressionMultiplier(level);
		var volume = baseVolume * multiplier;
		return RoundVolume(volume);
	}

	private decimal GetProgressionMultiplier(int level)
	{
		switch (LotMode)
		{
			case LotProgressions.Static:
				return 1m;
			case LotProgressions.Geometric:
				return (decimal)Math.Pow((double)VolumeMultiplier, level);
			case LotProgressions.Exponential:
				return (decimal)Math.Pow((double)VolumeMultiplier, level * (level + 1) / 2.0);
			default:
				return 1m;
		}
	}

	private decimal GetProgressiveStep(decimal baseStep, int level)
	{
		switch (StepMode)
		{
			case StepProgressions.Static:
				return baseStep;
			case StepProgressions.Geometric:
				return baseStep * (decimal)Math.Pow((double)StepMultiplier, level - 1);
			case StepProgressions.Exponential:
				return baseStep * (decimal)Math.Pow((double)StepMultiplier, level * (level - 1) / 2.0);
			default:
				return baseStep;
		}
	}

	private void CloseBuys()
	{
		var volume = GetTotalVolume(_buyEntries);
		if (volume <= 0m)
		return;

		SellMarket(volume);
		_buyEntries.Clear();
		_buyEmergencyActive = false;
		_hasBuyReference = false;
	}

	private void CloseSells()
	{
		var volume = GetTotalVolume(_sellEntries);
		if (volume <= 0m)
		return;

		BuyMarket(volume);
		_sellEntries.Clear();
		_sellEmergencyActive = false;
		_hasSellReference = false;
	}

	private decimal CalculateUnrealizedProfit(decimal price, List<GridEntry> entries, bool isBuy, decimal point, decimal stepPrice)
	{
		if (entries.Count == 0)
		return 0m;

		var total = 0m;
		for (var i = 0; i < entries.Count; i++)
		{
			var entry = entries[i];
			var difference = isBuy ? price - entry.Price : entry.Price - price;
			var steps = difference / point;
			total += steps * stepPrice * entry.Volume;
		}

		return total;
	}

	private decimal GetTotalVolume(List<GridEntry> entries)
	{
		var total = 0m;
		for (var i = 0; i < entries.Count; i++)
		total += entries[i].Volume;
		return RoundVolume(total);
	}

	private decimal RoundVolume(decimal volume)
	{
		var step = Security.VolumeStep;
		if (step == null || step == 0m)
		return Math.Max(0m, volume);

		var normalized = Math.Floor(volume / step.Value) * step.Value;
		if (normalized <= 0m)
		normalized = step.Value;
		return normalized;
	}

	private decimal GetPointValue()
	{
		var point = Security.PriceStep;
		if (point == null || point == 0m)
		return 0.0001m;
		return point.Value;
	}

	/// <summary>
	/// Step progression modes mirroring the MetaTrader implementation.
	/// </summary>
	public enum StepProgressions
	{
		Static,
		Geometric,
		Exponential,
	}

	/// <summary>
	/// Lot progression modes mirroring the MetaTrader implementation.
	/// </summary>
	public enum LotProgressions
	{
		Static,
		Geometric,
		Exponential,
	}

	private readonly struct GridEntry
	{
		public GridEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; }

		public decimal Volume { get; }
	}
}

