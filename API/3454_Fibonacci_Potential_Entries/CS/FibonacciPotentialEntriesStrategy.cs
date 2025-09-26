namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// StockSharp port of the EA_PUB_FibonacciPotentialEntries expert.
/// Places two pending orders around predefined Fibonacci levels and manages partial exits.
/// </summary>
public class FibonacciPotentialEntriesStrategy : Strategy
{
	private const decimal FirstTradeRiskPercent = 0.7m;

	private readonly StrategyParam<decimal> _p50Level;
	private readonly StrategyParam<decimal> _p61Level;
	private readonly StrategyParam<decimal> _p100Level;
	private readonly StrategyParam<decimal> _targetLevel;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<MarketBiasType> _marketBias;

	private decimal? _bestAsk;
	private decimal? _bestBid;
	private bool _ordersPlaced;

	private Order _firstEntryOrder;
	private Order _secondEntryOrder;
	private Order _firstStopOrder;
	private Order _secondStopOrder;
	private Order _firstPartialCloseOrder;
	private Order _secondPartialCloseOrder;

	private decimal _firstOpenVolume;
	private decimal _secondOpenVolume;
	private decimal? _firstEntryPrice;
	private decimal? _secondEntryPrice;
	private decimal? _firstInitialStop;
	private decimal? _secondInitialStop;

	private bool _firstPartialCloseCompleted;
	private bool _secondPartialCloseCompleted;
	private decimal? _pendingStopPriceAfterFirstPartial;
	private decimal? _pendingStopPriceAfterSecondPartial;

	/// <summary>
	/// Defines the direction bias of the strategy.
	/// </summary>
	public enum MarketBiasType
	{
		Bull = 1,
		Bear = 2,
	}

	/// <summary>
	/// Price level associated with the 50% Fibonacci retracement.
	/// </summary>
	public decimal P50Level
	{
		get => _p50Level.Value;
		set => _p50Level.Value = value;
	}

	/// <summary>
	/// Price level associated with the 61.8% Fibonacci retracement.
	/// </summary>
	public decimal P61Level
	{
		get => _p61Level.Value;
		set => _p61Level.Value = value;
	}

	/// <summary>
	/// Price level associated with the 100% Fibonacci retracement.
	/// </summary>
	public decimal P100Level
	{
		get => _p100Level.Value;
		set => _p100Level.Value = value;
	}

	/// <summary>
	/// Common profit taking level for both trades.
	/// </summary>
	public decimal TargetLevel
	{
		get => _targetLevel.Value;
		set => _targetLevel.Value = value;
	}

	/// <summary>
	/// Total risk allocated across the two trades (percent of equity).
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Selected market bias (long or short campaign).
	/// </summary>
	public MarketBiasType MarketBias
	{
		get => _marketBias.Value;
		set => _marketBias.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FibonacciPotentialEntriesStrategy()
	{
		_p50Level = Param(nameof(P50Level), 1.08261m)
			.SetDisplay("50% Level", "Price level corresponding to the 50% Fibonacci retracement.", "Levels");

		_p61Level = Param(nameof(P61Level), 1.07811m)
			.SetDisplay("61% Level", "Price level corresponding to the 61.8% Fibonacci retracement.", "Levels");

		_p100Level = Param(nameof(P100Level), 1.06370m)
			.SetDisplay("100% Level", "Price level corresponding to the 100% Fibonacci retracement.", "Levels");

		_targetLevel = Param(nameof(TargetLevel), 1.10178m)
			.SetDisplay("Target", "Shared take-profit level used by both entries.", "Levels");

		_riskPercent = Param(nameof(RiskPercent), 1.4m)
			.SetDisplay("Risk Percent", "Total risk percentage allocated to both entries.", "Risk")
			.SetGreaterOrEqual(FirstTradeRiskPercent);

		_marketBias = Param(nameof(MarketBias), MarketBiasType.Bull)
			.SetDisplay("Market Bias", "Long (Bull) or short (Bear) execution mode.", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestAsk = null;
		_bestBid = null;
		_ordersPlaced = false;
		_firstEntryOrder = null;
		_secondEntryOrder = null;
		CancelStopOrder(ref _firstStopOrder);
		CancelStopOrder(ref _secondStopOrder);
		_firstPartialCloseOrder = null;
		_secondPartialCloseOrder = null;
		_firstOpenVolume = 0m;
		_secondOpenVolume = 0m;
		_firstEntryPrice = null;
		_secondEntryPrice = null;
		_firstInitialStop = null;
		_secondInitialStop = null;
		_firstPartialCloseCompleted = false;
		_secondPartialCloseCompleted = false;
		_pendingStopPriceAfterFirstPartial = null;
		_pendingStopPriceAfterSecondPartial = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelStopOrder(ref _firstStopOrder);
		CancelStopOrder(ref _secondStopOrder);

		base.OnStopped();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		{
			var ask = (decimal)askObj;
			if (ask > 0m)
				_bestAsk = ask;
		}

		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
		{
			var bid = (decimal)bidObj;
			if (bid > 0m)
				_bestBid = bid;
		}

		TryPlaceOrders();
		TryHandlePartialClose();
	}

	private void TryPlaceOrders()
	{
		if (_ordersPlaced)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_bestAsk is not decimal ask || _bestBid is not decimal bid)
			return;

		var spread = Math.Max(0m, ask - bid);
		var riskSecondTrade = Math.Max(0m, RiskPercent - FirstTradeRiskPercent);

		var firstStop = MarketBias == MarketBiasType.Bull
			? P61Level - 3m * spread
			: P61Level + 3m * spread;

		var midpoint = (P61Level + P100Level) / 2m;
		var secondStop = MarketBias == MarketBiasType.Bull
			? midpoint - 3m * spread
			: midpoint + 3m * spread;

		if (firstStop <= 0m || secondStop <= 0m)
			return;

		if (MarketBias == MarketBiasType.Bull)
		{
			if (firstStop >= P50Level || secondStop >= P61Level)
				return;
		}
		else
		{
			if (firstStop <= P50Level || secondStop <= P61Level)
				return;
		}

		var firstVolume = CalculateVolume(P50Level, firstStop, FirstTradeRiskPercent);
		var secondVolume = CalculateVolume(P61Level, secondStop, riskSecondTrade);

		if (firstVolume <= 0m && secondVolume <= 0m)
			return;

		if (firstVolume > 0m)
		{
			_firstInitialStop = firstStop;
			_firstEntryOrder = MarketBias == MarketBiasType.Bull
				? BuyLimit(price: P50Level, volume: firstVolume)
				: SellLimit(price: P50Level, volume: firstVolume);
		}

		if (secondVolume > 0m)
		{
			_secondInitialStop = secondStop;
			_secondEntryOrder = MarketBias == MarketBiasType.Bull
				? BuyLimit(price: P61Level, volume: secondVolume)
				: SellLimit(price: P61Level, volume: secondVolume);
		}

		_ordersPlaced = _firstEntryOrder != null || _secondEntryOrder != null;
	}

	private void TryHandlePartialClose()
	{
		if (Position > 0m && _bestAsk is decimal ask && ask >= TargetLevel)
		{
			TryStartPartialClose(1, ref _firstPartialCloseOrder, ref _firstPartialCloseCompleted, _firstOpenVolume, _firstEntryPrice);
			TryStartPartialClose(2, ref _secondPartialCloseOrder, ref _secondPartialCloseCompleted, _secondOpenVolume, _secondEntryPrice);
		}
		else if (Position < 0m && _bestBid is decimal bid && bid <= TargetLevel)
		{
			TryStartPartialClose(1, ref _firstPartialCloseOrder, ref _firstPartialCloseCompleted, _firstOpenVolume, _firstEntryPrice);
			TryStartPartialClose(2, ref _secondPartialCloseOrder, ref _secondPartialCloseCompleted, _secondOpenVolume, _secondEntryPrice);
		}
	}

	private void TryStartPartialClose(int index, ref Order partialOrder, ref bool completed, decimal openVolume, decimal? entryPrice)
	{
		if (completed || partialOrder != null)
			return;

		if (openVolume <= 0m || entryPrice is null)
			return;

		var halfVolume = NormalizeVolume(openVolume / 2m);
		if (halfVolume <= 0m)
			return;

		var maxClosable = Math.Min(halfVolume, Math.Abs(Position));
		maxClosable = Math.Min(maxClosable, openVolume);
		if (maxClosable <= 0m)
			return;

		partialOrder = MarketBias == MarketBiasType.Bull
			? SellMarket(maxClosable)
			: BuyMarket(maxClosable);

		if (partialOrder != null)
		{
			if (index == 1)
				_pendingStopPriceAfterFirstPartial = entryPrice;
			else
				_pendingStopPriceAfterSecondPartial = entryPrice;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
			return;

		var order = trade.Order;

		if (order == _firstEntryOrder)
		{
			UpdateEntryState(ref _firstOpenVolume, ref _firstEntryPrice, volume, trade.Trade.Price);
			if (_firstInitialStop is decimal stop)
				MoveStopToPrice(1, stop, _firstOpenVolume);
		}
		else if (order == _secondEntryOrder)
		{
			UpdateEntryState(ref _secondOpenVolume, ref _secondEntryPrice, volume, trade.Trade.Price);
			if (_secondInitialStop is decimal stop)
				MoveStopToPrice(2, stop, _secondOpenVolume);
		}
		else if (order == _firstPartialCloseOrder || order == _firstStopOrder)
		{
			_firstOpenVolume = Math.Max(0m, _firstOpenVolume - volume);
			if (_firstOpenVolume <= 0m)
				ClearTradeState(1);
		}
		else if (order == _secondPartialCloseOrder || order == _secondStopOrder)
		{
			_secondOpenVolume = Math.Max(0m, _secondOpenVolume - volume);
			if (_secondOpenVolume <= 0m)
				ClearTradeState(2);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.Security != Security)
			return;

		if (order == _firstPartialCloseOrder)
		{
			HandlePartialCloseCompletion(order, 1, ref _firstPartialCloseOrder, ref _firstPartialCloseCompleted, ref _pendingStopPriceAfterFirstPartial, _firstOpenVolume);
		}
		else if (order == _secondPartialCloseOrder)
		{
			HandlePartialCloseCompletion(order, 2, ref _secondPartialCloseOrder, ref _secondPartialCloseCompleted, ref _pendingStopPriceAfterSecondPartial, _secondOpenVolume);
		}
		else if (order == _firstStopOrder)
		{
			HandleStopCompletion(order, 1);
		}
		else if (order == _secondStopOrder)
		{
			HandleStopCompletion(order, 2);
		}
	}

	private void HandlePartialCloseCompletion(Order order, int index, ref Order partialOrder, ref bool completed, ref decimal? pendingStopPrice, decimal openVolume)
	{
		if (order.State == OrderStates.Done)
		{
			partialOrder = null;
			completed = true;

			if (pendingStopPrice is decimal stop && openVolume > 0m)
			{
				MoveStopToPrice(index, stop, openVolume);
			}

			pendingStopPrice = null;
		}
		else if (order.State == OrderStates.Failed || order.State == OrderStates.Cancelled)
		{
			partialOrder = null;
			pendingStopPrice = null;
			completed = false;
		}
	}

	private void HandleStopCompletion(Order order, int index)
	{
		if (order.State == OrderStates.Done || order.State == OrderStates.Failed || order.State == OrderStates.Cancelled)
		{
			if (index == 1)
				_firstStopOrder = null;
			else
				_secondStopOrder = null;

			if (order.State == OrderStates.Done)
				ClearTradeState(index);
		}
	}

	private void UpdateEntryState(ref decimal openVolume, ref decimal? entryPrice, decimal tradeVolume, decimal tradePrice)
	{
		var previousVolume = openVolume;
		openVolume += tradeVolume;

		if (openVolume <= 0m)
		{
			entryPrice = null;
			return;
		}

		if (entryPrice is decimal existing && previousVolume > 0m)
		{
			entryPrice = (existing * previousVolume + tradePrice * tradeVolume) / openVolume;
		}
		else
		{
			entryPrice = tradePrice;
		}
	}

	private void MoveStopToPrice(int index, decimal price, decimal volume)
	{
		if (price <= 0m || volume <= 0m)
		{
			if (index == 1)
				CancelStopOrder(ref _firstStopOrder);
			else
				CancelStopOrder(ref _secondStopOrder);

			return;
		}

		var cappedVolume = Math.Min(volume, Math.Abs(Position));
		if (cappedVolume <= 0m)
		{
			if (index == 1)
				CancelStopOrder(ref _firstStopOrder);
			else
				CancelStopOrder(ref _secondStopOrder);

			return;
		}

		cappedVolume = NormalizeVolume(cappedVolume);
		if (cappedVolume <= 0m)
			return;

		var stopOrder = index == 1 ? _firstStopOrder : _secondStopOrder;
		if (stopOrder != null)
			CancelOrder(stopOrder);

		stopOrder = MarketBias == MarketBiasType.Bull
			? SellStop(cappedVolume, price)
			: BuyStop(cappedVolume, price);

		if (index == 1)
			_firstStopOrder = stopOrder;
		else
			_secondStopOrder = stopOrder;
	}

	private void CancelStopOrder(ref Order order)
	{
		if (order != null)
		{
			if (order.State == OrderStates.Active || order.State == OrderStates.Pending)
				CancelOrder(order);
		}

		order = null;
	}

	private void ClearTradeState(int index)
	{
		if (index == 1)
		{
			_firstOpenVolume = 0m;
			_firstEntryPrice = null;
			_firstInitialStop = null;
			_firstPartialCloseCompleted = false;
			_pendingStopPriceAfterFirstPartial = null;
			_firstPartialCloseOrder = null;
			CancelStopOrder(ref _firstStopOrder);
		}
		else
		{
			_secondOpenVolume = 0m;
			_secondEntryPrice = null;
			_secondInitialStop = null;
			_secondPartialCloseCompleted = false;
			_pendingStopPriceAfterSecondPartial = null;
			_secondPartialCloseOrder = null;
			CancelStopOrder(ref _secondStopOrder);
		}
	}

	private decimal CalculateVolume(decimal entryPrice, decimal stopPrice, decimal riskPercent)
	{
		if (riskPercent <= 0m)
			return 0m;

		var equity = Portfolio?.CurrentValue ?? Portfolio?.CurrentBalance ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
			return Volume;

		var stopDistance = Math.Abs(entryPrice - stopPrice);
		if (stopDistance <= 0m)
			return 0m;

		var step = Security?.PriceStep ?? 0m;
		var stepCost = Security?.PriceStepCost ?? 0m;
		var multiplier = Security?.Multiplier ?? 1m;

		decimal valuePerPriceUnit;
		if (step > 0m && stepCost > 0m)
			valuePerPriceUnit = stepCost / step;
		else if (multiplier > 0m)
			valuePerPriceUnit = multiplier;
		else
			valuePerPriceUnit = 1m;

		var riskAmount = equity * (riskPercent / 100m);
		if (riskAmount <= 0m)
			return 0m;

		var rawVolume = riskAmount / (stopDistance * valuePerPriceUnit);
		rawVolume = NormalizeVolume(rawVolume);

		return rawVolume;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
			volume = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;

		var minVolume = Security?.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security?.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}
}
