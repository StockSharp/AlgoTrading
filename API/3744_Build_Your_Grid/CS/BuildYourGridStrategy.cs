using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the BuildYourGrid MetaTrader expert advisor to the StockSharp high level API.
/// The strategy places a configurable grid of market orders in both directions, manages dynamic lot sizing,
/// optional hedge balancing and profit / loss exits measured in pips or account currency.
/// </summary>
public class BuildYourGridStrategy : Strategy
{
	/// <summary>
	/// Describes which directions the strategy is allowed to trade.
	/// </summary>
	public enum OrderPlacementType
	{
		/// <summary>
		/// Allow both buy and sell orders.
		/// </summary>
		OpenBuyAndSell,

		/// <summary>
		/// Allow only buy orders.
		/// </summary>
		OpenOnlyBuy,

		/// <summary>
		/// Allow only sell orders.
		/// </summary>
		OpenOnlySell
	}

	/// <summary>
	/// Controls the direction of the grid when additional orders are added.
	/// </summary>
	public enum NextOrderDirection
	{
		/// <summary>
		/// Add new orders in the trend direction (price must advance beyond the previous entry).
		/// </summary>
		GridAccordingTrend,

		/// <summary>
		/// Add new orders against the trend (price must retrace against the previous entry).
		/// </summary>
		GridContraryTrend
	}

	/// <summary>
	/// Defines how the distance between consecutive grid orders is calculated.
	/// </summary>
	public enum GridStepMode
	{
		/// <summary>
		/// Use the same distance for every grid order.
		/// </summary>
		StaticalStep,

		/// <summary>
		/// Multiply the distance by the current order count (1 * step, 2 * step, ...).
		/// </summary>
		GeometricalStep,

		/// <summary>
		/// Double the distance for every additional order (1, 2, 4, 8 ... steps).
		/// </summary>
		ExponentialStep
	}

	/// <summary>
	/// Target evaluation metric for closing all grid orders in profit.
	/// </summary>
	public enum ProfitTargetMode
	{
		/// <summary>
		/// Compare accumulated unrealized pips with the target.
		/// </summary>
		TargetInPips,

		/// <summary>
		/// Compare unrealized profit in account currency with the target.
		/// </summary>
		TargetInCurrency
	}

	/// <summary>
	/// Defines how the strategy closes orders when the floating loss reaches the threshold.
	/// </summary>
	public enum LossCloseMode
	{
		/// <summary>
		/// Keep positions open.
		/// </summary>
		NotClose,

		/// <summary>
		/// Close only the very first order in each direction.
		/// </summary>
		CloseFirstOrders,

		/// <summary>
		/// Close the entire grid in both directions.
		/// </summary>
		CloseAllOrders
	}

	/// <summary>
	/// Controls the lot sizing progression across the grid.
	/// </summary>
	public enum LotProgressionMode
	{
		/// <summary>
		/// Always reuse the first lot size.
		/// </summary>
		StaticalLot,

		/// <summary>
		/// Double the second order and then add the last and the first lot afterwards.
		/// </summary>
		GeometricalLot,

		/// <summary>
		/// Double the lot size for every new order.
		/// </summary>
		ExponentialLot
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<OrderPlacementType> _orderPlacementType;
	private readonly StrategyParam<NextOrderDirection> _nextOrderDirection;
	private readonly StrategyParam<decimal> _pipsForNextOrder;
	private readonly StrategyParam<GridStepMode> _stepMode;
	private readonly StrategyParam<ProfitTargetMode> _profitTargetMode;
	private readonly StrategyParam<decimal> _pipsCloseInProfit;
	private readonly StrategyParam<decimal> _currencyCloseInProfit;
	private readonly StrategyParam<LossCloseMode> _lossMode;
	private readonly StrategyParam<decimal> _pipsForCloseInLoss;
	private readonly StrategyParam<bool> _placeHedgeOrder;
	private readonly StrategyParam<decimal> _levelLossForHedge;
	private readonly StrategyParam<decimal> _hedgeLotMultiplier;
	private readonly StrategyParam<bool> _autoLotSize;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualLotSize;
	private readonly StrategyParam<LotProgressionMode> _lotProgressionMode;
	private readonly StrategyParam<decimal> _maxMultiplierLot;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _maxSpread;

	private readonly List<PositionEntry> _buyEntries = new();
	private readonly List<PositionEntry> _sellEntries = new();

	private decimal _pipSize;
	private decimal _priceStep;
	private decimal _stepPrice;
	private decimal? _lastBid;
	private decimal? _lastAsk;
	private bool _hedgeArmed;
	private decimal _firstBuyVolume;
	private decimal _firstSellVolume;
	private decimal _lastBuyVolume;
	private decimal _lastSellVolume;
	private decimal _lastBuyPrice;
	private decimal _lastSellPrice;

	private const decimal VolumeTolerance = 0.0000001m;

	/// <summary>
	/// Initializes default parameters mirroring the MetaTrader expert advisor inputs.
	/// </summary>
	public BuildYourGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe that drives grid decisions.", "Data");

		_orderPlacementType = Param(nameof(OrderPlacement), OrderPlacementType.OpenBuyAndSell)
		.SetDisplay("Order Placement", "Allowed trade directions for the grid.", "Trading")
		.SetCanOptimize(true);

		_nextOrderDirection = Param(nameof(NextOrder), NextOrderDirection.GridContraryTrend)
		.SetDisplay("Next Order Direction", "Decides if new grid trades follow or fade the last entry.", "Trading")
		.SetCanOptimize(true);

		_pipsForNextOrder = Param(nameof(PipsForNextOrder), 25m)
		.SetGreaterThanZero()
		.SetDisplay("Grid Step (pips)", "Base distance between consecutive grid entries.", "Trading")
		.SetCanOptimize(true);

		_stepMode = Param(nameof(StepMode), GridStepMode.StaticalStep)
		.SetDisplay("Step Mode", "Progression applied to the grid distance.", "Trading")
		.SetCanOptimize(true);

		_profitTargetMode = Param(nameof(ProfitTarget), ProfitTargetMode.TargetInPips)
		.SetDisplay("Profit Target", "Metric used when closing the grid in profit.", "Risk management")
		.SetCanOptimize(true);

		_pipsCloseInProfit = Param(nameof(PipsCloseInProfit), 5m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Target Pips", "Total unrealized pips required to close the grid.", "Risk management")
		.SetCanOptimize(true);

		_currencyCloseInProfit = Param(nameof(CurrencyCloseInProfit), 5m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Target Currency", "Unrealized profit that triggers a full close.", "Risk management")
		.SetCanOptimize(true);

		_lossMode = Param(nameof(LossMode), LossCloseMode.NotClose)
		.SetDisplay("Loss Close Mode", "Behaviour when floating loss exceeds the threshold.", "Risk management")
		.SetCanOptimize(true);

		_pipsForCloseInLoss = Param(nameof(PipsForCloseInLoss), 100m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Loss Threshold (pips)", "Negative pips that activate loss handling.", "Risk management")
		.SetCanOptimize(true);

		_placeHedgeOrder = Param(nameof(PlaceHedgeOrder), false)
		.SetDisplay("Use Hedge", "Enable balancing hedge orders when drawdown grows.", "Risk management");

		_levelLossForHedge = Param(nameof(LevelLossForHedge), 10m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Hedge Loss Level (%)", "Drawdown percentage of balance before hedge balancing starts.", "Risk management");

		_hedgeLotMultiplier = Param(nameof(MuliplierHedgeLot), 1m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Hedge Multiplier", "Multiplier applied to the volume difference when hedging.", "Risk management");

		_autoLotSize = Param(nameof(AutoLotSize), false)
		.SetDisplay("Auto Lot", "Recalculate the base lot size from account balance and risk factor.", "Money management");

		_riskFactor = Param(nameof(RiskFactor), 1m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Risk Factor", "Risk percentage used for automatic lot sizing.", "Money management");

		_manualLotSize = Param(nameof(ManualLotSize), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Manual Lot", "Lot size for the first order when auto lot is disabled.", "Money management");

		_lotProgressionMode = Param(nameof(LotProgression), LotProgressionMode.StaticalLot)
		.SetDisplay("Lot Progression", "Controls how grid volume increases.", "Money management")
		.SetCanOptimize(true);

		_maxMultiplierLot = Param(nameof(MaxMultiplierLot), 50m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Max Lot Multiplier", "Safety cap applied to the first lot size.", "Money management");

		_maxOrders = Param(nameof(MaxOrders), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("Max Orders", "Maximum simultaneous grid trades (0 = unlimited).", "Trading");

		_maxSpread = Param(nameof(MaxSpread), 0m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Max Spread", "Maximum accepted bid/ask spread in pips (0 = ignore).", "Filters");
	}

	/// <summary>
	/// Primary candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public OrderPlacementType OrderPlacement
	{
		get => _orderPlacementType.Value;
		set => _orderPlacementType.Value = value;
	}

	/// <summary>
	/// Determines whether the grid follows or fades the current trend.
	/// </summary>
	public NextOrderDirection NextOrder
	{
		get => _nextOrderDirection.Value;
		set => _nextOrderDirection.Value = value;
	}

	/// <summary>
	/// Base distance between orders expressed in pips.
	/// </summary>
	public decimal PipsForNextOrder
	{
		get => _pipsForNextOrder.Value;
		set => _pipsForNextOrder.Value = value;
	}

	/// <summary>
	/// Step progression mode.
	/// </summary>
	public GridStepMode StepMode
	{
		get => _stepMode.Value;
		set => _stepMode.Value = value;
	}

	/// <summary>
	/// Target type used to close the grid in profit.
	/// </summary>
	public ProfitTargetMode ProfitTarget
	{
		get => _profitTargetMode.Value;
		set => _profitTargetMode.Value = value;
	}

	/// <summary>
	/// Target pips for closing all positions.
	/// </summary>
	public decimal PipsCloseInProfit
	{
		get => _pipsCloseInProfit.Value;
		set => _pipsCloseInProfit.Value = value;
	}

	/// <summary>
	/// Target profit in currency for closing all positions.
	/// </summary>
	public decimal CurrencyCloseInProfit
	{
		get => _currencyCloseInProfit.Value;
		set => _currencyCloseInProfit.Value = value;
	}

	/// <summary>
	/// Selected loss handling behaviour.
	/// </summary>
	public LossCloseMode LossMode
	{
		get => _lossMode.Value;
		set => _lossMode.Value = value;
	}

	/// <summary>
	/// Loss threshold in pips.
	/// </summary>
	public decimal PipsForCloseInLoss
	{
		get => _pipsForCloseInLoss.Value;
		set => _pipsForCloseInLoss.Value = value;
	}

	/// <summary>
	/// Enables hedge balancing when the drawdown reaches the configured level.
	/// </summary>
	public bool PlaceHedgeOrder
	{
		get => _placeHedgeOrder.Value;
		set => _placeHedgeOrder.Value = value;
	}

	/// <summary>
	/// Drawdown percentage of the account balance required to trigger hedge balancing.
	/// </summary>
	public decimal LevelLossForHedge
	{
		get => _levelLossForHedge.Value;
		set => _levelLossForHedge.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the volume difference when hedge orders are placed.
	/// </summary>
	public decimal MuliplierHedgeLot
	{
		get => _hedgeLotMultiplier.Value;
		set => _hedgeLotMultiplier.Value = value;
	}

	/// <summary>
	/// Enable automatic lot calculation.
	/// </summary>
	public bool AutoLotSize
	{
		get => _autoLotSize.Value;
		set => _autoLotSize.Value = value;
	}

	/// <summary>
	/// Risk factor used in automatic lot sizing.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Manual lot size used when auto lot sizing is disabled.
	/// </summary>
	public decimal ManualLotSize
	{
		get => _manualLotSize.Value;
		set => _manualLotSize.Value = value;
	}

	/// <summary>
	/// Lot progression mode applied to the grid.
	/// </summary>
	public LotProgressionMode LotProgression
	{
		get => _lotProgressionMode.Value;
		set => _lotProgressionMode.Value = value;
	}

	/// <summary>
	/// Upper bound for the lot multiplier.
	/// </summary>
	public decimal MaxMultiplierLot
	{
		get => _maxMultiplierLot.Value;
		set => _maxMultiplierLot.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open orders.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Maximum accepted spread measured in pips.
	/// </summary>
	public decimal MaxSpread
	{
		get => _maxSpread.Value;
		set => _maxSpread.Value = value;
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
		_lastBid = null;
		_lastAsk = null;
		_hedgeArmed = false;
		_firstBuyVolume = 0m;
		_firstSellVolume = 0m;
		_lastBuyVolume = 0m;
		_lastSellVolume = 0m;
		_lastBuyPrice = 0m;
		_lastSellPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0.0001m;
		_priceStep = Security?.PriceStep ?? 0.0001m;
		_stepPrice = Security?.StepPrice ?? 1m;

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		SubscribeCandles(CandleType)
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		_lastBid = Convert.ToDecimal(bidValue);

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		_lastAsk = Convert.ToDecimal(askValue);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var closePrice = candle.ClosePrice;

		var buyStats = CalculateDirectionalStats(_buyEntries, closePrice, true);
		var sellStats = CalculateDirectionalStats(_sellEntries, closePrice, false);

		var buyOrders = _buyEntries.Count;
		var sellOrders = _sellEntries.Count;
		var totalOrders = buyOrders + sellOrders;

		if (totalOrders == 0)
		{
			_hedgeArmed = false;
		}
		else if (buyOrders > 1 && sellOrders > 1)
		{
			var diff = Math.Abs(GetTotalVolume(_buyEntries) - GetTotalVolume(_sellEntries));
			if (diff <= VolumeTolerance)
			_hedgeArmed = true;
		}

		if (totalOrders > 0)
		{
			var totalPips = buyStats.Pips + sellStats.Pips;
			var totalProfit = buyStats.Currency + sellStats.Currency;

			if (ShouldCloseInProfit(totalPips, totalProfit))
			{
				CloseAllPositions();
				return;
			}

			if (LossMode != LossCloseMode.NotClose && PipsForCloseInLoss > 0m && totalPips <= -PipsForCloseInLoss)
			{
				if (LossMode == LossCloseMode.CloseFirstOrders)
				CloseFirstPositions();
				else
				CloseAllPositions();

				return;
			}

			if (PlaceHedgeOrder && !_hedgeArmed && LevelLossForHedge > 0m && totalProfit < 0m)
			{
				var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
				if (balance > 0m)
				{
					var drawdownPercent = Math.Abs(totalProfit) * 100m / balance;
					if (drawdownPercent >= LevelLossForHedge)
					{
						_hedgeArmed = true;
						OpenHedgePosition();
						return;
					}
				}
			}
		}

		if (!IsSpreadAccepted())
		return;

		var expectedBuyOrders = buyOrders;
		var expectedSellOrders = sellOrders;
		var openedInitial = false;

		if (ShouldOpenInitialBuy(buyOrders))
		{
			OpenOrder(Sides.Buy);
			expectedBuyOrders++;
			openedInitial = true;
		}

		if (ShouldOpenInitialSell(sellOrders))
		{
			OpenOrder(Sides.Sell);
			expectedSellOrders++;
			openedInitial = true;
		}

		if (openedInitial)
		return;

		if (MaxOrders > 0 && expectedBuyOrders + expectedSellOrders >= MaxOrders)
		return;

		if (ShouldOpenNextBuy(closePrice, buyOrders))
		{
			if (MaxOrders == 0 || expectedBuyOrders + expectedSellOrders < MaxOrders)
			{
				OpenOrder(Sides.Buy);
				expectedBuyOrders++;
			}
		}

		if (ShouldOpenNextSell(closePrice, sellOrders))
		{
			if (MaxOrders == 0 || expectedBuyOrders + expectedSellOrders < MaxOrders)
			{
				OpenOrder(Sides.Sell);
				expectedSellOrders++;
			}
		}
	}

	private bool ShouldCloseInProfit(decimal totalPips, decimal totalProfit)
	{
		return ProfitTarget switch
		{
			ProfitTargetMode.TargetInPips => PipsCloseInProfit > 0m && totalPips >= PipsCloseInProfit,
			ProfitTargetMode.TargetInCurrency => CurrencyCloseInProfit > 0m && totalProfit >= CurrencyCloseInProfit,
			_ => false
		};
	}

	private bool ShouldOpenInitialBuy(int buyOrders)
	{
		if (buyOrders > 0)
		return false;

		switch (OrderPlacement)
		{
			case OrderPlacementType.OpenBuyAndSell:
			case OrderPlacementType.OpenOnlyBuy:
			return true;
			default:
			return false;
		}
	}

	private bool ShouldOpenInitialSell(int sellOrders)
	{
		if (sellOrders > 0)
		return false;

		switch (OrderPlacement)
		{
			case OrderPlacementType.OpenBuyAndSell:
			case OrderPlacementType.OpenOnlySell:
			return true;
			default:
			return false;
		}
	}

	private bool ShouldOpenNextBuy(decimal closePrice, int buyOrders)
	{
		if (OrderPlacement == OrderPlacementType.OpenOnlySell || buyOrders <= 0)
		return false;

		var requiredMove = CalculateStepDistance(buyOrders);
		if (requiredMove <= 0m)
		return false;

		return NextOrder switch
		{
			NextOrderDirection.GridAccordingTrend => closePrice >= _lastBuyPrice + requiredMove,
			NextOrderDirection.GridContraryTrend => closePrice <= _lastBuyPrice - requiredMove,
			_ => false
		};
	}

	private bool ShouldOpenNextSell(decimal closePrice, int sellOrders)
	{
		if (OrderPlacement == OrderPlacementType.OpenOnlyBuy || sellOrders <= 0)
		return false;

		var requiredMove = CalculateStepDistance(sellOrders);
		if (requiredMove <= 0m)
		return false;

		return NextOrder switch
		{
			NextOrderDirection.GridAccordingTrend => closePrice <= _lastSellPrice - requiredMove,
			NextOrderDirection.GridContraryTrend => closePrice >= _lastSellPrice + requiredMove,
			_ => false
		};
	}

	private decimal CalculateStepDistance(int orderCount)
	{
		var baseStep = PipsForNextOrder;
		if (baseStep <= 0m)
		return 0m;

		decimal multiplier = StepMode switch
		{
			GridStepMode.StaticalStep => 1m,
			GridStepMode.GeometricalStep => Math.Max(1, orderCount),
			GridStepMode.ExponentialStep => (decimal)Math.Max(1d, Math.Pow(2d, Math.Max(0, orderCount - 1))),
			_ => 1m
		};

		return baseStep * multiplier * _pipSize;
	}

	private void OpenOrder(Sides side, bool hedgeMode = false)
	{
		var volume = CalculateOrderVolume(side, hedgeMode);
		if (volume <= 0m)
		return;

		if (side == Sides.Buy)
		BuyMarket(volume);
		else
		SellMarket(volume);
	}

	private decimal CalculateOrderVolume(Sides side, bool hedgeMode)
	{
		var baseVolume = AutoLotSize ? CalculateAutoLot() : ManualLotSize;
		if (baseVolume <= 0m)
		return 0m;

		var entries = side == Sides.Buy ? _buyEntries : _sellEntries;
		var orderCount = entries.Count;
		var firstVolume = side == Sides.Buy ? _firstBuyVolume : _firstSellVolume;
		var lastVolume = side == Sides.Buy ? _lastBuyVolume : _lastSellVolume;

		var result = baseVolume;

		switch (LotProgression)
		{
			case LotProgressionMode.StaticalLot:
			if (orderCount > 0 && firstVolume > 0m)
			result = firstVolume;
			break;
			case LotProgressionMode.GeometricalLot:
			if (orderCount == 0)
			result = baseVolume;
			else if (orderCount == 1 && lastVolume > 0m)
			result = lastVolume * 2m;
			else if (orderCount >= 2 && lastVolume > 0m && firstVolume > 0m)
			result = lastVolume + firstVolume;
			break;
			case LotProgressionMode.ExponentialLot:
			if (orderCount == 0)
			result = baseVolume;
			else if (lastVolume > 0m)
			result = lastVolume * 2m;
			break;
		}

		if (hedgeMode)
		{
			var buyVolume = GetTotalVolume(_buyEntries);
			var sellVolume = GetTotalVolume(_sellEntries);
			var difference = side == Sides.Buy ? sellVolume - buyVolume : buyVolume - sellVolume;
			if (difference <= 0m)
			return 0m;

			result = difference * MuliplierHedgeLot;
		}

		if (MaxMultiplierLot > 0m && firstVolume > 0m && orderCount > 0)
		{
			var cap = firstVolume * MaxMultiplierLot;
			if (result > cap)
			result = cap;
		}

		return NormalizeVolume(result);
	}

	private decimal CalculateAutoLot()
	{
		if (!AutoLotSize)
		return ManualLotSize;

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (balance <= 0m)
		return ManualLotSize;

		return balance * RiskFactor / 100000m;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var minVolume = Security?.MinVolume ?? 0.01m;
		var maxVolume = Security?.MaxVolume ?? 0m;
		var step = Security?.VolumeStep ?? minVolume;

		if (step > 0m)
		volume = Math.Round(volume / step) * step;

		if (volume < minVolume)
		volume = minVolume;

		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private DirectionalStats CalculateDirectionalStats(List<PositionEntry> entries, decimal price, bool isBuy)
	{
		var pips = 0m;
		var currency = 0m;

		if (entries.Count == 0)
		return new DirectionalStats(0m, 0m);

		var pipSize = _pipSize > 0m ? _pipSize : 0.0001m;
		var priceStep = _priceStep > 0m ? _priceStep : pipSize;
		var stepPrice = _stepPrice > 0m ? _stepPrice : 1m;

		foreach (var entry in entries)
		{
			var diff = isBuy ? price - entry.Price : entry.Price - price;
			pips += diff / pipSize;
			currency += diff / priceStep * stepPrice * entry.Volume;
		}

		return new DirectionalStats(pips, currency);
	}

	private bool IsSpreadAccepted()
	{
		if (MaxSpread <= 0m)
		return true;

		if (_lastBid is not decimal bid || _lastAsk is not decimal ask || bid <= 0m || ask <= 0m)
		return true;

		var pipSize = _pipSize > 0m ? _pipSize : 0.0001m;
		var spread = (ask - bid) / pipSize;
		return spread <= MaxSpread;
	}

	private void CloseAllPositions()
	{
		var buyVolume = GetTotalVolume(_buyEntries);
		if (buyVolume > 0m)
		SellMarket(buyVolume);

		var sellVolume = GetTotalVolume(_sellEntries);
		if (sellVolume > 0m)
		BuyMarket(sellVolume);
	}

	private void CloseFirstPositions()
	{
		CloseFirst(_buyEntries, Sides.Sell);
		CloseFirst(_sellEntries, Sides.Buy);
	}

	private void CloseFirst(List<PositionEntry> entries, Sides closingSide)
	{
		if (entries.Count == 0)
		return;

		var volume = entries[0].Volume;
		if (volume <= 0m)
		return;

		if (closingSide == Sides.Buy)
		BuyMarket(volume);
		else
		SellMarket(volume);
	}

	private void OpenHedgePosition()
	{
		var buyVolume = GetTotalVolume(_buyEntries);
		var sellVolume = GetTotalVolume(_sellEntries);

		if (buyVolume < sellVolume)
		OpenOrder(Sides.Buy, true);
		else if (sellVolume < buyVolume)
		OpenOrder(Sides.Sell, true);
	}

	private decimal GetTotalVolume(List<PositionEntry> entries)
	{
		decimal total = 0m;
		foreach (var entry in entries)
		total += entry.Volume;
		return total;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;
		var side = trade.Order.Direction;

		if (volume <= 0m)
		return;

		if (side == Sides.Buy)
		{
			MatchOpposite(_sellEntries, ref volume);
			if (volume > 0m)
			{
				_buyEntries.Add(new PositionEntry(price, volume));
			}
		}
		else
		{
			MatchOpposite(_buyEntries, ref volume);
			if (volume > 0m)
			{
				_sellEntries.Add(new PositionEntry(price, volume));
			}
		}

		RefreshDirectionalState(_buyEntries, true);
		RefreshDirectionalState(_sellEntries, false);
	}

	private void MatchOpposite(List<PositionEntry> entries, ref decimal volume)
	{
		var remaining = volume;
		for (var i = 0; i < entries.Count && remaining > 0m; i++)
		{
			var entry = entries[i];
			var used = Math.Min(entry.Volume, remaining);
			entry = entry with { Volume = entry.Volume - used };

			if (entry.Volume <= VolumeTolerance)
			{
				entries.RemoveAt(i);
				i--;
			}
			else
			{
				entries[i] = entry;
			}

			remaining -= used;
		}

		volume = remaining;
	}

	private void RefreshDirectionalState(List<PositionEntry> entries, bool isBuy)
	{
		if (entries.Count == 0)
		{
			if (isBuy)
			{
				_firstBuyVolume = 0m;
				_lastBuyVolume = 0m;
				_lastBuyPrice = 0m;
			}
			else
			{
				_firstSellVolume = 0m;
				_lastSellVolume = 0m;
				_lastSellPrice = 0m;
			}

			return;
		}

		var first = entries[0];
		var last = entries[^1];

		if (isBuy)
		{
			_firstBuyVolume = first.Volume;
			_lastBuyVolume = last.Volume;
			_lastBuyPrice = last.Price;
		}
		else
		{
			_firstSellVolume = first.Volume;
			_lastSellVolume = last.Volume;
			_lastSellPrice = last.Price;
		}
	}

	private readonly record struct PositionEntry(decimal Price, decimal Volume);

	private readonly record struct DirectionalStats(decimal Pips, decimal Currency);
}
