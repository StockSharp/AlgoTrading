using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// USD/CHF stop breakout strategy converted from the MetaTrader 4 expert "UsdChf_new".
/// It relies on Commodity Channel Index crossings to deploy pending stop orders, mirrors the original pip based risk logic and maintains trailing plus break-even management.
/// </summary>
public class UsdChfNewStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciChannel;
	private readonly StrategyParam<decimal> _entryIndentPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _cancelDistancePips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _breakEvenPips;

	private CommodityChannelIndex _cci = null!;
	private decimal? _previousCci;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;

	private decimal? _plannedLongStop;
	private decimal? _plannedShortStop;
	private decimal? _activeStopPrice;
	private decimal? _entryPrice;

	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="UsdChfNewStrategy"/> class.
	/// </summary>
	public UsdChfNewStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for signals", "General");

		_cciPeriod = Param(nameof(CciPeriod), 73)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index averaging period", "Indicators")
			.SetCanOptimize(true);

		_cciChannel = Param(nameof(CciChannel), 120m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Channel", "Absolute threshold triggering breakout signals", "Indicators")
			.SetCanOptimize(true);

		_entryIndentPips = Param(nameof(EntryIndentPips), 30m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Entry Indent (pips)", "Distance between market price and pending stop", "Risk")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 95m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Initial stop loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_cancelDistancePips = Param(nameof(CancelDistancePips), 30m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Cancel Distance (pips)", "Maximum gap before pending stop is cancelled", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 110m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing distance maintained after activation", "Risk")
			.SetCanOptimize(true);

		_breakEvenPips = Param(nameof(BreakEvenPips), 60m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Break Even (pips)", "Profit needed before stop is moved to entry", "Risk")
			.SetCanOptimize(true);
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
	/// Commodity Channel Index averaging period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Absolute CCI level that forms the channel boundaries.
	/// </summary>
	public decimal CciChannel
	{
		get => _cciChannel.Value;
		set => _cciChannel.Value = value;
	}

	/// <summary>
	/// Pending order offset expressed in pips.
	/// </summary>
	public decimal EntryIndentPips
	{
		get => _entryIndentPips.Value;
		set => _entryIndentPips.Value = value;
	}

	/// <summary>
	/// Initial stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Maximum distance before pending orders are cancelled.
	/// </summary>
	public decimal CancelDistancePips
	{
		get => _cancelDistancePips.Value;
		set => _cancelDistancePips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Break-even activation distance in pips.
	/// </summary>
	public decimal BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousCci = null;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_plannedLongStop = null;
		_plannedShortStop = null;
		_activeStopPrice = null;
		_entryPrice = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		var previous = _previousCci;

		if (ManagePosition(candle))
		{
			_previousCci = cciValue;
			return;
		}

		UpdatePendingOrders(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCci = cciValue;
			return;
		}

		if (previous.HasValue)
		{
			var channel = CciChannel;
			var upper = channel;
			var lower = -channel;

			var crossUp = previous.Value < lower && cciValue > lower;
			var crossDown = previous.Value > upper && cciValue < upper;

			if (Position > 0m && crossDown)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				PlaceSellStop(candle.ClosePrice);
			}
			else if (Position < 0m && crossUp)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				PlaceBuyStop(candle.ClosePrice);
			}
			else if (Position == 0m && !HasActivePendingOrders())
			{
				if (crossUp)
					PlaceBuyStop(candle.ClosePrice);
				else if (crossDown)
					PlaceSellStop(candle.ClosePrice);
			}
		}

		_previousCci = cciValue;
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			ResetPositionState();
			return false;
		}

		if (_entryPrice == null)
			_entryPrice = PositionPrice != 0m ? PositionPrice : candle.ClosePrice;

		if (_activeStopPrice == null)
		{
			if (Position > 0m && _plannedLongStop.HasValue)
			{
				_activeStopPrice = _plannedLongStop;
				_plannedLongStop = null;
			}
			else if (Position < 0m && _plannedShortStop.HasValue)
			{
				_activeStopPrice = _plannedShortStop;
				_plannedShortStop = null;
			}
		}

		if (_activeStopPrice.HasValue)
		{
			if (Position > 0m && candle.LowPrice <= _activeStopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}

			if (Position < 0m && candle.HighPrice >= _activeStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}
		}

		var breakEvenDistance = BreakEvenPips * _pipSize;
		var trailingDistance = TrailingStopPips * _pipSize;
		var minimalBreakEvenBuffer = CalculateBreakEvenBuffer();

		if (Position > 0m && _entryPrice.HasValue)
		{
			var profit = candle.ClosePrice - _entryPrice.Value;

			if (BreakEvenPips > 0m && breakEvenDistance > 0m && profit > breakEvenDistance)
			{
				var breakEven = _entryPrice.Value;
				var stopReference = _activeStopPrice ?? (_plannedLongStop ?? breakEven);
				if (Math.Abs(breakEven - stopReference) > minimalBreakEvenBuffer)
				{
					if (!_activeStopPrice.HasValue || _activeStopPrice.Value < breakEven)
						_activeStopPrice = breakEven;
				}
			}

			if (TrailingStopPips > 0m && trailingDistance > 0m && profit > trailingDistance)
			{
				var desiredStop = candle.ClosePrice - trailingDistance;
				if (!_activeStopPrice.HasValue || desiredStop > _activeStopPrice.Value)
					_activeStopPrice = desiredStop;
			}
		}
		else if (Position < 0m && _entryPrice.HasValue)
		{
			var profit = _entryPrice.Value - candle.ClosePrice;

			if (BreakEvenPips > 0m && breakEvenDistance > 0m && profit > breakEvenDistance)
			{
				var breakEven = _entryPrice.Value;
				var stopReference = _activeStopPrice ?? (_plannedShortStop ?? breakEven);
				if (Math.Abs(breakEven - stopReference) > minimalBreakEvenBuffer)
				{
					if (!_activeStopPrice.HasValue || _activeStopPrice.Value > breakEven)
						_activeStopPrice = breakEven;
				}
			}

			if (TrailingStopPips > 0m && trailingDistance > 0m && profit > trailingDistance)
			{
				var desiredStop = candle.ClosePrice + trailingDistance;
				if (!_activeStopPrice.HasValue || desiredStop < _activeStopPrice.Value)
					_activeStopPrice = desiredStop;
			}
		}

		return false;
	}

	private void UpdatePendingOrders(ICandleMessage candle)
	{
		var cancelDistance = CancelDistancePips * _pipSize;

		if (_buyStopOrder != null)
		{
			if (_buyStopOrder.State != OrderStates.Active)
			{
				if (_buyStopOrder.State == OrderStates.Done || _buyStopOrder.State == OrderStates.Failed || _buyStopOrder.State == OrderStates.Canceled)
					_buyStopOrder = null;
			}
			else if (cancelDistance > 0m && _buyStopOrder.Price is decimal price)
			{
				var distance = price - candle.ClosePrice;
				if (distance > cancelDistance)
					CancelOrder(_buyStopOrder);
			}
		}

		if (_sellStopOrder != null)
		{
			if (_sellStopOrder.State != OrderStates.Active)
			{
				if (_sellStopOrder.State == OrderStates.Done || _sellStopOrder.State == OrderStates.Failed || _sellStopOrder.State == OrderStates.Canceled)
					_sellStopOrder = null;
			}
			else if (cancelDistance > 0m && _sellStopOrder.Price is decimal price)
			{
				var distance = candle.ClosePrice - price;
				if (distance > cancelDistance)
					CancelOrder(_sellStopOrder);
			}
		}
	}

	private bool HasActivePendingOrders()
	{
		return (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active) ||
			(_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active);
	}

	private void PlaceBuyStop(decimal referencePrice)
	{
		var indent = EntryIndentPips * _pipSize;
		if (indent <= 0m || Volume <= 0m)
			return;

		var entryPrice = RoundPrice(referencePrice + indent);
		if (entryPrice <= 0m)
			return;

		var stopDistance = StopLossPips * _pipSize;
		_plannedLongStop = stopDistance > 0m ? RoundPrice(entryPrice - stopDistance) : (decimal?)null;
		_plannedShortStop = null;
		_activeStopPrice = null;

		_buyStopOrder = BuyStop(Volume, entryPrice);

		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
			CancelOrder(_sellStopOrder);
	}

	private void PlaceSellStop(decimal referencePrice)
	{
		var indent = EntryIndentPips * _pipSize;
		if (indent <= 0m || Volume <= 0m)
			return;

		var entryPrice = RoundPrice(referencePrice - indent);
		if (entryPrice <= 0m)
			return;

		var stopDistance = StopLossPips * _pipSize;
		_plannedShortStop = stopDistance > 0m ? RoundPrice(entryPrice + stopDistance) : (decimal?)null;
		_plannedLongStop = null;
		_activeStopPrice = null;

		_sellStopOrder = SellStop(Volume, entryPrice);

		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
			CancelOrder(_buyStopOrder);
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_activeStopPrice = null;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;

		if (Security?.Decimals is 3 or 5)
			return step * 10m;

		return step;
	}

	private decimal CalculateBreakEvenBuffer()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return _pipSize > 0m ? _pipSize / 10m : 0.0001m;

		var buffer = step * 10m;
		if (buffer <= 0m)
			buffer = _pipSize > 0m ? _pipSize : step;

		return buffer;
	}

	private decimal RoundPrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
			return price;

		return Math.Round(price / step.Value, MidpointRounding.AwayFromZero) * step.Value;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null)
			return;

		var price = trade.Trade?.Price ?? order.Price;

		if (_buyStopOrder != null && order == _buyStopOrder)
		{
			_entryPrice = price ?? _entryPrice;
			_activeStopPrice = _plannedLongStop;
			_plannedLongStop = null;
			_buyStopOrder = null;

			if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
				CancelOrder(_sellStopOrder);
		}
		else if (_sellStopOrder != null && order == _sellStopOrder)
		{
			_entryPrice = price ?? _entryPrice;
			_activeStopPrice = _plannedShortStop;
			_plannedShortStop = null;
			_sellStopOrder = null;

			if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
				CancelOrder(_buyStopOrder);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
			ResetPositionState();
	}
}
