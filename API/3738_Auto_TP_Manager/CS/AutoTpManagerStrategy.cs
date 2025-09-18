using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Protective stop/take-profit helper converted from the "Auto Tp" MetaTrader 5 expert advisor.
/// The strategy does not open positions and only manages risk for manually opened trades.
/// </summary>
public class AutoTpManagerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useEquityProtection;
	private readonly StrategyParam<decimal> _minEquityPercent;
	private readonly StrategyParam<int> _slippage;

	private Order _stopOrder;
	private Order _takeProfitOrder;
	private decimal _pipSize;
	private decimal? _currentStopPrice;
	private decimal? _currentTakeProfitPrice;
	private bool _protectionInitialized;
	private decimal? _currentBid;
	private decimal? _currentAsk;
	private decimal? _lastTradePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="AutoTpManagerStrategy"/> class.
	/// </summary>
	public AutoTpManagerStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 25m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pips)", "Distance to the take-profit target expressed in pips.", "Risk Management")
		.SetCanOptimize(true);

		_useStopLoss = Param(nameof(UseStopLoss), false)
		.SetDisplay("Use Stop Loss", "Enable stop-loss placement for manual positions.", "Risk Management");

		_stopLossPips = Param(nameof(StopLossPips), 12m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pips)", "Distance to the protective stop in pips.", "Risk Management")
		.SetCanOptimize(true);

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
		.SetDisplay("Use Trailing Stop", "Activate stop adjustments when the trade moves into profit.", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop (pips)", "Trailing distance maintained once the position is profitable.", "Risk Management")
		.SetCanOptimize(true);

		_useEquityProtection = Param(nameof(UseEquityProtection), false)
		.SetDisplay("Use Equity Protection", "Close all exposure when account equity drops below a threshold.", "Risk Management");

		_minEquityPercent = Param(nameof(MinEquityPercent), 20m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Min Equity (%)", "Minimum equity as a percentage of the starting balance.", "Risk Management");

		_slippage = Param(nameof(Slippage), 3)
		.SetGreaterOrEqualZero()
		.SetDisplay("Slippage", "Reserved for compatibility with the original EA; no direct effect.", "Misc");
	}

	/// <summary>
	/// Distance to the take-profit target expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable stop-loss placement for manual positions.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Distance to the protective stop in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Activate stop adjustments when the trade moves into profit.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing distance maintained once the position is profitable.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Close all exposure when account equity drops below a threshold.
	/// </summary>
	public bool UseEquityProtection
	{
		get => _useEquityProtection.Value;
		set => _useEquityProtection.Value = value;
	}

	/// <summary>
	/// Minimum equity as a percentage of the starting balance.
	/// </summary>
	public decimal MinEquityPercent
	{
		get => _minEquityPercent.Value;
		set => _minEquityPercent.Value = value;
	}

	/// <summary>
	/// Reserved for compatibility with the original EA; no direct effect.
	/// </summary>
	public int Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
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

		ResetProtectionOrders();
		_protectionInitialized = false;
		_pipSize = 0m;
		_currentBid = null;
		_currentAsk = null;
		_lastTradePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetProtectionOrders();
			_protectionInitialized = false;
			return;
		}

		_protectionInitialized = false;
		EnsureProtection();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		// Store the last trade price to estimate the entry when necessary.
		_lastTradePrice = trade.Trade?.Price ?? _lastTradePrice;
		EnsureProtection();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
		_currentBid = bidPrice;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
		_currentAsk = askPrice;

		if (message.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last) && last is decimal lastPrice)
		_lastTradePrice = lastPrice;

		EnsureProtection();
		ApplyTrailingStop();
		CheckEquityLimit();
	}

	private void EnsureProtection()
	{
		if (Position == 0m)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		EnsurePipSize();

		if (_pipSize <= 0m)
		return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		var entryPrice = PositionPrice;
		if (entryPrice == 0m)
		entryPrice = _lastTradePrice ?? _currentBid ?? _currentAsk ?? 0m;

		if (entryPrice == 0m)
		return;

		var isLong = Position > 0m;

		decimal? stopPrice = null;
		if (UseStopLoss && StopLossPips > 0m)
		{
			var stopDistance = StopLossPips * _pipSize;
			stopPrice = isLong ? entryPrice - stopDistance : entryPrice + stopDistance;
		}

		decimal? takeProfitPrice = null;
		if (TakeProfitPips > 0m)
		{
			var takeProfitDistance = TakeProfitPips * _pipSize;
			takeProfitPrice = isLong ? entryPrice + takeProfitDistance : entryPrice - takeProfitDistance;
		}

		if (stopPrice != null)
		UpdateStopOrder(isLong, stopPrice.Value, volume);
		else
		ResetStopOrder();

		if (takeProfitPrice != null)
		UpdateTakeProfitOrder(isLong, takeProfitPrice.Value, volume);
		else
		ResetTakeProfitOrder();

		_protectionInitialized = stopPrice != null || takeProfitPrice != null;
	}

	private void ApplyTrailingStop()
	{
		if (!UseStopLoss || !UseTrailingStop)
		return;

		if (Position == 0m)
		return;

		EnsurePipSize();

		var trailingDistance = TrailingStopPips * _pipSize;
		if (trailingDistance <= 0m)
		return;

		var entryPrice = PositionPrice;
		if (entryPrice == 0m)
		return;

		var isLong = Position > 0m;
		var marketPrice = isLong ? _currentBid ?? _lastTradePrice : _currentAsk ?? _lastTradePrice;
		if (marketPrice == null || marketPrice.Value == 0m)
		return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		if (isLong)
		{
			var profit = marketPrice.Value - entryPrice;
			if (profit <= trailingDistance)
			return;

			var newStop = marketPrice.Value - trailingDistance;
			if (_currentStopPrice.HasValue && _currentStopPrice.Value >= newStop)
			return;

			UpdateStopOrder(true, newStop, volume);
		}
		else
		{
			var profit = entryPrice - marketPrice.Value;
			if (profit <= trailingDistance)
			return;

			var newStop = marketPrice.Value + trailingDistance;
			if (_currentStopPrice.HasValue && _currentStopPrice.Value <= newStop)
			return;

			UpdateStopOrder(false, newStop, volume);
		}
	}

	private void CheckEquityLimit()
	{
		if (!UseEquityProtection)
		return;

		if (Portfolio == null)
		return;

		var balance = Portfolio.BeginValue ?? 0m;
		if (balance <= 0m)
		return;

		var equity = Portfolio.CurrentValue ?? balance;
		var minEquity = balance * MinEquityPercent / 100m;

		if (equity > minEquity)
		return;

		if (Position != 0m)
		{
			// ClosePosition sends a market order that liquidates the current exposure.
			ClosePosition();
		}
	}

	private void UpdateStopOrder(bool isLong, decimal stopPrice, decimal volume)
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active && _stopOrder.Price == stopPrice && _stopOrder.Volume == volume)
		return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		CancelOrder(_stopOrder);

		_stopOrder = isLong
		? SellStop(price: stopPrice, volume: volume)
		: BuyStop(price: stopPrice, volume: volume);

		_currentStopPrice = stopPrice;
	}

	private void UpdateTakeProfitOrder(bool isLong, decimal takeProfitPrice, decimal volume)
	{
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active && _takeProfitOrder.Price == takeProfitPrice && _takeProfitOrder.Volume == volume)
		return;

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
		CancelOrder(_takeProfitOrder);

		_takeProfitOrder = isLong
		? SellLimit(price: takeProfitPrice, volume: volume)
		: BuyLimit(price: takeProfitPrice, volume: volume);

		_currentTakeProfitPrice = takeProfitPrice;
	}

	private void ResetProtectionOrders()
	{
		ResetStopOrder();
		ResetTakeProfitOrder();
	}

	private void ResetStopOrder()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		CancelOrder(_stopOrder);

		_stopOrder = null;
		_currentStopPrice = null;
	}

	private void ResetTakeProfitOrder()
	{
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
		CancelOrder(_takeProfitOrder);

		_takeProfitOrder = null;
		_currentTakeProfitPrice = null;
	}

	private void EnsurePipSize()
	{
		if (_pipSize > 0m)
		return;

		var step = Security?.PriceStep ?? 0m;
		_pipSize = step > 0m ? step * 10m : 1m;
	}
}
