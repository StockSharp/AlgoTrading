using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Probe CCI stop order breakout strategy converted from the original MetaTrader 5 expert advisor.
/// The strategy listens for Commodity Channel Index threshold breakouts and places pending stop orders with pip based offsets.
/// </summary>
public class ProbeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<decimal> _cciChannelLevel;
	private readonly StrategyParam<decimal> _indentPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;

	private CommodityChannelIndex _cci = null!;
	private decimal? _previousCci;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;

	private decimal? _pendingLongStop;
	private decimal? _pendingShortStop;

	private decimal? _entryPrice;
	private decimal? _stopPrice;

	private bool _buyCancelRequested;
	private bool _sellCancelRequested;

	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="ProbeStrategy"/> class.
	/// </summary>
	public ProbeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for indicator calculations", "General");

		_cciLength = Param(nameof(CciLength), 60)
		.SetGreaterThanZero()
		.SetDisplay("CCI Length", "Averaging period of the Commodity Channel Index", "Indicators")
		.SetCanOptimize(true);

		_cciChannelLevel = Param(nameof(CciChannelLevel), 120m)
		.SetGreaterThanZero()
		.SetDisplay("CCI Channel", "Absolute CCI level used as the channel boundary", "Indicators")
		.SetCanOptimize(true);

		_indentPips = Param(nameof(IndentPips), 30m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Indent (pips)", "Distance from the market price to the stop order", "Risk")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pips)", "Protective stop loss distance expressed in pips", "Risk")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop (pips)", "Minimum profit required before trailing activates", "Risk")
		.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Step (pips)", "Additional profit required before the stop is moved again", "Risk")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type used for CCI calculations and candle processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Averaging period for the Commodity Channel Index indicator.
	/// </summary>
	public int CciLength
	{
		get => _cciLength.Value;
		set => _cciLength.Value = value;
	}

	/// <summary>
	/// Absolute level defining the CCI channel boundaries.
	/// </summary>
	public decimal CciChannelLevel
	{
		get => _cciChannelLevel.Value;
		set => _cciChannelLevel.Value = value;
	}

	/// <summary>
	/// Indent distance expressed in pips between the market price and the stop order.
	/// </summary>
	public decimal IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop activation threshold in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional pips required before the trailing stop advances.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
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
		_pendingLongStop = null;
		_pendingShortStop = null;
		_entryPrice = null;
		_stopPrice = null;
		_buyCancelRequested = false;
		_sellCancelRequested = false;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_cci = new CommodityChannelIndex
		{
			Length = CciLength
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

		var exited = ManagePosition(candle);
		if (exited)
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

		if (HasActivePendingOrders())
		{
			_previousCci = cciValue;
			return;
		}

		if (_previousCci.HasValue)
		{
			var channel = CciChannelLevel;
			var lower = -channel;
			var indent = GetIndentDistance();

			if (indent <= 0m || Volume <= 0m)
			{
				_previousCci = cciValue;
				return;
			}

			var crossUp = _previousCci.Value < lower && cciValue > lower;
			var crossDown = _previousCci.Value > channel && cciValue < channel;

			if (crossUp)
			PlaceBuyStop(candle.ClosePrice, indent);
			else if (crossDown)
			PlaceSellStop(candle.ClosePrice, indent);
		}

		_previousCci = cciValue;
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			_entryPrice = null;
			_stopPrice = null;
			_pendingLongStop = null;
			_pendingShortStop = null;
			return false;
		}

		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0m)
		{
			if (_entryPrice == null)
			_entryPrice = PositionPrice != 0m ? PositionPrice : candle.ClosePrice;

			if (_stopPrice == null && _pendingLongStop.HasValue)
			_stopPrice = _pendingLongStop;

			_pendingLongStop = null;

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}

			if (TrailingStopPips > 0m && trailingStop > 0m && _entryPrice.HasValue)
			{
				var profit = candle.ClosePrice - _entryPrice.Value;
				var threshold = trailingStop + trailingStep;

				if (profit > threshold)
				{
					var desiredStop = candle.ClosePrice - trailingStop;
					if (!_stopPrice.HasValue || _stopPrice.Value < candle.ClosePrice - threshold)
					_stopPrice = desiredStop;
				}
			}
		}
		else if (Position < 0m)
		{
			if (_entryPrice == null)
			_entryPrice = PositionPrice != 0m ? PositionPrice : candle.ClosePrice;

			if (_stopPrice == null && _pendingShortStop.HasValue)
			_stopPrice = _pendingShortStop;

			_pendingShortStop = null;

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}

			if (TrailingStopPips > 0m && trailingStop > 0m && _entryPrice.HasValue)
			{
				var profit = _entryPrice.Value - candle.ClosePrice;
				var threshold = trailingStop + trailingStep;

				if (profit > threshold)
				{
					var desiredStop = candle.ClosePrice + trailingStop;
					if (!_stopPrice.HasValue || _stopPrice.Value > candle.ClosePrice + threshold)
					_stopPrice = desiredStop;
				}
			}
		}

		return false;
	}

	private void UpdatePendingOrders(ICandleMessage candle)
	{
		var indent = GetIndentDistance();

		if (_buyStopOrder != null)
		{
			if (_buyStopOrder.State != OrderStates.Active)
			{
				if (_buyStopOrder.State == OrderStates.Done || _buyStopOrder.State == OrderStates.Failed || _buyStopOrder.State == OrderStates.Canceled)
				{
					_buyStopOrder = null;
					_buyCancelRequested = false;
				}
			}
			else if (indent > 0m && !_buyCancelRequested)
			{
				var entry = _buyStopOrder.Price;
				if (entry != null)
				{
					var distance = entry.Value - candle.ClosePrice;
					if (distance > indent * 1.5m)
					{
						CancelOrder(_buyStopOrder);
						_buyCancelRequested = true;
					}
				}
			}
		}

		if (_sellStopOrder != null)
		{
			if (_sellStopOrder.State != OrderStates.Active)
			{
				if (_sellStopOrder.State == OrderStates.Done || _sellStopOrder.State == OrderStates.Failed || _sellStopOrder.State == OrderStates.Canceled)
				{
					_sellStopOrder = null;
					_sellCancelRequested = false;
				}
			}
			else if (indent > 0m && !_sellCancelRequested)
			{
				var entry = _sellStopOrder.Price;
				if (entry != null)
				{
					var distance = candle.ClosePrice - entry.Value;
					if (distance > indent * 1.5m)
					{
						CancelOrder(_sellStopOrder);
						_sellCancelRequested = true;
					}
				}
			}
		}
	}

	private bool HasActivePendingOrders()
	{
		return (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active) ||
		(_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active);
	}

	private void PlaceBuyStop(decimal referencePrice, decimal indent)
	{
		var entryPrice = RoundPrice(referencePrice + indent);
		if (entryPrice <= 0m)
		return;

		var stopDistance = StopLossPips * _pipSize;
		_pendingLongStop = stopDistance > 0m ? RoundPrice(entryPrice - stopDistance) : (decimal?)null;
		_pendingShortStop = null;

		_buyStopOrder = BuyStop(Volume, entryPrice);
		_buyCancelRequested = false;

		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
		{
			CancelOrder(_sellStopOrder);
			_sellCancelRequested = true;
		}
	}

	private void PlaceSellStop(decimal referencePrice, decimal indent)
	{
		var entryPrice = RoundPrice(referencePrice - indent);
		if (entryPrice <= 0m)
		return;

		var stopDistance = StopLossPips * _pipSize;
		_pendingShortStop = stopDistance > 0m ? RoundPrice(entryPrice + stopDistance) : (decimal?)null;
		_pendingLongStop = null;

		_sellStopOrder = SellStop(Volume, entryPrice);
		_sellCancelRequested = false;

		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
		{
			CancelOrder(_buyStopOrder);
			_buyCancelRequested = true;
		}
	}

	private void ResetTradeState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_pendingLongStop = null;
		_pendingShortStop = null;
	}

	private decimal GetIndentDistance()
	{
		var indent = IndentPips * _pipSize;
		return indent > 0m ? indent : 0m;
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

	private decimal RoundPrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
		return price;

		var rounded = Math.Round(price / step.Value, MidpointRounding.AwayFromZero) * step.Value;
		return rounded;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade?.Order;
		if (order == null)
		return;

		var tradePrice = trade.Trade?.Price ?? order.Price;

		if (_buyStopOrder != null && order == _buyStopOrder)
		{
			_entryPrice = tradePrice ?? _entryPrice;
			_stopPrice = _pendingLongStop;
			_pendingLongStop = null;
			_buyStopOrder = null;
			_buyCancelRequested = false;

			if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
			{
				CancelOrder(_sellStopOrder);
				_sellCancelRequested = true;
			}
		}
		else if (_sellStopOrder != null && order == _sellStopOrder)
		{
			_entryPrice = tradePrice ?? _entryPrice;
			_stopPrice = _pendingShortStop;
			_pendingShortStop = null;
			_sellStopOrder = null;
			_sellCancelRequested = false;

			if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
			{
				CancelOrder(_buyStopOrder);
				_buyCancelRequested = true;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		ResetTradeState();
	}
}
