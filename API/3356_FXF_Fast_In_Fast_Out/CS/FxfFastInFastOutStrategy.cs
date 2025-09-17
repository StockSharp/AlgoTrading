using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility breakout strategy converted from the FXF Fast in Fast out MT4 expert advisor.
/// Places pending stop orders after large candles and manages risk with optional trailing stops.
/// </summary>
public class FxfFastInFastOutStrategy : Strategy
{
	private readonly StrategyParam<int> _enterOffsetPoints;
	private readonly StrategyParam<int> _maxSpreadPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _volatilitySizePoints;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _maxOrdersPerBar;
	private readonly StrategyParam<DataType> _candleType;

	private Sides? _currentSignal;
	private DateTimeOffset? _currentBarTime;
	private int _ordersPlacedThisBar;
	private decimal _bestBid;
	private decimal _bestAsk;
	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Offset in price steps between the best quote and the pending order price.
	/// </summary>
	public int EnterOffsetPoints
	{
		get => _enterOffsetPoints.Value;
		set => _enterOffsetPoints.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in price steps.
	/// </summary>
	public int MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Minimum candle range required to generate a signal.
	/// </summary>
	public int VolatilitySizePoints
	{
		get => _volatilitySizePoints.Value;
		set => _volatilitySizePoints.Value = value;
	}

	/// <summary>
	/// Enables or disables trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Base trailing stop distance in price steps.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Enables or disables money management sizing.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Risk percentage per trade used for position sizing.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Maximum number of pending orders allowed during a single bar.
	/// </summary>
	public int MaxOrdersPerBar
	{
		get => _maxOrdersPerBar.Value;
		set => _maxOrdersPerBar.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters with defaults matching the original expert advisor.
	/// </summary>
	public FxfFastInFastOutStrategy()
	{
		_enterOffsetPoints = Param(nameof(EnterOffsetPoints), 22)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Entry Offset (points)", "Distance between quote and pending stop order.", "Orders")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 15)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Max Spread", "Maximum spread allowed for new entries (price steps).", "Orders");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 250)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Take Profit", "Take-profit distance in price steps.", "Orders");

		_stopLossPoints = Param(nameof(StopLossPoints), 500)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Stop Loss", "Stop-loss distance in price steps.", "Orders");

		_volatilitySizePoints = Param(nameof(VolatilitySizePoints), 220)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Volatility Threshold", "Minimum candle size required to trade (price steps).", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(150, 400, 50);

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Toggle trailing stop management.", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 1)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Trailing Stop", "Base trailing distance in price steps.", "Risk");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), true)
		.SetDisplay("Money Management", "Enable risk-based position sizing.", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Risk %", "Risk percentage per trade.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 10m, 1m);

		_maxOrdersPerBar = Param(nameof(MaxOrdersPerBar), 1)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Orders per Bar", "Maximum number of pending orders allowed per candle.", "Orders");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for volatility calculations.", "Signals");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new List<(Security, DataType)>
		{
			(Security, CandleType),
			(Security, DataType.Level1)
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentSignal = null;
		_currentBarTime = null;
		_ordersPlacedThisBar = 0;
		_bestBid = 0m;
		_bestAsk = 0m;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.Bind(ProcessCandle)
		.Start();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var isNewBar = !_currentBarTime.HasValue || candle.OpenTime > _currentBarTime.Value;
		if (isNewBar)
		{
			_currentBarTime = candle.OpenTime;
			_ordersPlacedThisBar = 0;
		}

		_currentSignal = null;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			LogWarning("PriceStep is not configured. Unable to evaluate volatility.");
			return;
		}

		var rangePoints = (candle.HighPrice - candle.LowPrice) / priceStep;
		if (VolatilitySizePoints > 0 && rangePoints < VolatilitySizePoints)
		return;

		var midPrice = (_bestBid > 0m && _bestAsk > 0m)
		? (_bestBid + _bestAsk) / 2m
		: candle.ClosePrice;

		if (midPrice > candle.OpenPrice)
		{
			_currentSignal = Sides.Buy;
		}
		else if (midPrice < candle.OpenPrice)
		{
			_currentSignal = Sides.Sell;
		}

		if (_currentSignal != null)
		TryPlacePendingOrders();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		_bestAsk = (decimal)ask;

		CleanupOrder(ref _buyStopOrder);
		CleanupOrder(ref _sellStopOrder);

		if (_bestBid <= 0m || _bestAsk <= 0m)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		EnforceSpreadLimit();
		TryPlacePendingOrders();
		UpdateTrailingStops();
	}

	private void TryPlacePendingOrders()
	{
		if (_currentSignal is null)
		return;

		if (_bestBid <= 0m || _bestAsk <= 0m)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0m)
		return;

		if (HasActivePendingOrders())
		return;

		if (MaxOrdersPerBar > 0 && _ordersPlacedThisBar >= MaxOrdersPerBar)
		return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			LogWarning("PriceStep is not configured. Pending orders cannot be placed.");
			return;
		}

		var spreadPoints = GetSpreadPoints();
		if (MaxSpreadPoints > 0 && spreadPoints > MaxSpreadPoints)
		return;

		var entryOffset = Math.Max(0, EnterOffsetPoints) * priceStep;
		if (entryOffset <= 0m)
		entryOffset = priceStep;

		var volume = CalculateOrderVolume(spreadPoints);
		if (volume <= 0m)
		{
			LogWarning("Calculated order volume is zero. Skipping entry.");
			return;
		}

		if (_currentSignal == Sides.Buy)
		{
			var entryPrice = NormalizePrice(_bestAsk + entryOffset);
			var stopLoss = StopLossPoints > 0
			? NormalizePrice(entryPrice - (StopLossPoints + spreadPoints) * priceStep)
			: (decimal?)null;
			var takeProfit = TakeProfitPoints > 0
			? NormalizePrice(entryPrice + TakeProfitPoints * priceStep)
			: (decimal?)null;

			_buyStopOrder = BuyStop(volume, entryPrice, stopLoss, takeProfit);
			_ordersPlacedThisBar++;
			LogInfo($"Buy stop placed at {entryPrice} with volume {volume}.");
		}
		else if (_currentSignal == Sides.Sell)
		{
			var entryPrice = NormalizePrice(_bestBid - entryOffset);
			var stopLoss = StopLossPoints > 0
			? NormalizePrice(entryPrice + (StopLossPoints + spreadPoints) * priceStep)
			: (decimal?)null;
			var takeProfit = TakeProfitPoints > 0
			? NormalizePrice(entryPrice - TakeProfitPoints * priceStep)
			: (decimal?)null;

			_sellStopOrder = SellStop(volume, entryPrice, stopLoss, takeProfit);
			_ordersPlacedThisBar++;
			LogInfo($"Sell stop placed at {entryPrice} with volume {volume}.");
		}
	}

	private void EnforceSpreadLimit()
	{
		if (MaxSpreadPoints <= 0)
		return;

		var spreadPoints = GetSpreadPoints();
		if (spreadPoints <= MaxSpreadPoints)
		return;

		if (HasActivePendingOrders())
		{
			LogInfo($"Spread {spreadPoints:F2} exceeds limit {MaxSpreadPoints}. Cancelling pending orders.");
			CancelPendingOrders();
		}
	}

	private void UpdateTrailingStops()
	{
		if (!EnableTrailing)
		return;

		if (Position > 0m)
		{
			if (_longTrailingStop.HasValue && _bestBid <= _longTrailingStop.Value)
			{
				SellMarket(Math.Abs(Position));
				LogInfo("Long trailing stop triggered.");
				_longTrailingStop = null;
				_shortTrailingStop = null;
				return;
			}

			var newStop = GetTrailingStopPrice(true);
			if (newStop.HasValue && (!_longTrailingStop.HasValue || newStop.Value > _longTrailingStop.Value))
			{
				_longTrailingStop = newStop;
				LogInfo($"Long trailing stop updated to {_longTrailingStop.Value}.");
			}
		}
		else if (Position < 0m)
		{
			if (_shortTrailingStop.HasValue && _bestAsk >= _shortTrailingStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo("Short trailing stop triggered.");
				_longTrailingStop = null;
				_shortTrailingStop = null;
				return;
			}

			var newStop = GetTrailingStopPrice(false);
			if (newStop.HasValue && (!_shortTrailingStop.HasValue || newStop.Value < _shortTrailingStop.Value))
			{
				_shortTrailingStop = newStop;
				LogInfo($"Short trailing stop updated to {_shortTrailingStop.Value}.");
			}
		}
		else
		{
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}
	}

	private decimal? GetTrailingStopPrice(bool isLong)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return null;

		var spreadPoints = GetSpreadPoints();
		if (spreadPoints < 0m)
		spreadPoints = 0m;

		var trailingPoints = Math.Max(0, TrailingStopPoints) + spreadPoints;
		if (trailingPoints <= 0m)
		return null;

		var offset = trailingPoints * priceStep;
		var price = isLong
		? _bestBid - offset
		: _bestAsk + offset;

		if (price <= 0m)
		return null;

		return NormalizePrice(price);
	}

	private decimal CalculateOrderVolume(decimal spreadPoints)
	{
		if (!UseMoneyManagement)
		return Volume;

		if (StopLossPoints <= 0)
		return Volume;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
		{
			LogWarning("Missing price step or step price. Falling back to fixed volume.");
			return Volume;
		}

		var stopPoints = StopLossPoints + spreadPoints;
		if (stopPoints <= 0m)
		return Volume;

		var riskAmount = (Portfolio?.CurrentValue ?? 0m) * RiskPercent / 100m;
		if (riskAmount <= 0m)
		return Volume;

		var riskPerUnit = stopPoints * stepPrice;
		if (riskPerUnit <= 0m)
		return Volume;

		var volume = riskAmount / riskPerUnit;

		var volumeStep = Security?.VolumeStep;
		if (volumeStep is { } step && step > 0m)
		{
			volume = Math.Floor(volume / step) * step;
			if (volume <= 0m)
			volume = step;
		}

		var minVolume = Security?.MinVolume;
		if (minVolume is { } min && min > 0m && volume < min)
		volume = min;

		var maxVolume = Security?.MaxVolume;
		if (maxVolume is { } max && max > 0m && volume > max)
		volume = max;

		return volume;
	}

	private void CancelPendingOrders()
	{
		CancelManagedOrder(ref _buyStopOrder);
		CancelManagedOrder(ref _sellStopOrder);
	}

	private void CancelManagedOrder(ref Order? order)
	{
		if (order == null)
		return;

		switch (order.State)
		{
			case OrderStates.Done:
			case OrderStates.Canceled:
			case OrderStates.Failed:
				order = null;
				return;
		}

		CancelOrder(order);
		order = null;
	}

	private static void CleanupOrder(ref Order? order)
	{
		if (order == null)
		return;

		switch (order.State)
		{
			case OrderStates.Done:
			case OrderStates.Canceled:
			case OrderStates.Failed:
				order = null;
				break;
		}
	}

	private bool HasActivePendingOrders()
	{
		return IsOrderActive(_buyStopOrder) || IsOrderActive(_sellStopOrder);
	}

	private static bool IsOrderActive(Order? order)
	{
		return order is { State: OrderStates.Active or OrderStates.Pending or OrderStates.Placed or OrderStates.InProcess or OrderStates.None };
	}

	private decimal GetSpreadPoints()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 0m;

		var spread = _bestAsk - _bestBid;
		if (spread <= 0m)
		return 0m;

		return spread / priceStep;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step is { } s && s > 0m)
		{
			var steps = Math.Round(price / s, MidpointRounding.AwayFromZero);
			return steps * s;
		}

		return price;
	}
}
