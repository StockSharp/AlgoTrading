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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Translation of the MetaTrader 5 expert advisor "TDSGlobal".
/// The strategy monitors MACD and OsMA momentum on four-hour candles and combines them with Force Index direction.
/// When conditions align it places pending limit orders around the previous candle extremums and manages protective levels and trailing stops manually.
/// </summary>
public class TdsGlobalPendingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _useRiskSizing;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _forceLength;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _entryOffsetPips;
	private readonly StrategyParam<decimal> _minDistancePips;
	private readonly StrategyParam<decimal> _pipSize;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private ForceIndex _forceIndex = null!;

	private Order _pendingBuyLimit;
	private Order _pendingSellLimit;

	private decimal? _prevMacd;
	private decimal? _prevPrevMacd;
	private decimal? _prevOsma;
	private decimal? _prevPrevOsma;
	private decimal? _prevForce;
	private decimal? _previousHigh;
	private decimal? _previousLow;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal? _plannedLongStop;
	private decimal? _plannedShortStop;
	private decimal? _plannedLongTake;
	private decimal? _plannedShortTake;

	private decimal _pipSizeValue;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;
	private decimal _entryOffsetDistance;
	private decimal _minDistance;

	/// <summary>
	/// Initializes default parameters for the strategy.
	/// </summary>
	public TdsGlobalPendingStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Fixed trade volume used when risk sizing is disabled", "Trading")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_useRiskSizing = Param(nameof(UseRiskSizing), true)
			.SetDisplay("Use Risk Sizing", "Switch between fixed lot and risk-based position sizing", "Trading");

		_riskPercent = Param(nameof(RiskPercent), 3m)
			.SetDisplay("Risk Percent", "Risk percentage applied when calculating dynamic volume", "Trading")
			.SetCanOptimize(true);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast EMA", "Fast EMA length for the MACD line", "Indicators")
			.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow EMA", "Slow EMA length for the MACD line", "Indicators")
			.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal EMA", "Signal EMA length for the histogram", "Indicators")
			.SetCanOptimize(true);

		_forceLength = Param(nameof(ForceLength), 24)
			.SetDisplay("Force Index Length", "EMA length for the Force Index indicator", "Indicators")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Minimum step for trailing stop updates in pips", "Risk")
			.SetCanOptimize(true);

		_entryOffsetPips = Param(nameof(EntryOffsetPips), 16m)
			.SetDisplay("Entry Offset (pips)", "Buffer added to previous highs/lows when placing limit orders", "Trading")
			.SetCanOptimize(true);

		_minDistancePips = Param(nameof(MinDistancePips), 3m)
			.SetDisplay("Minimum Distance (pips)", "Minimum allowed distance between price and protective orders", "Risk")
			.SetCanOptimize(true);

		_pipSize = Param(nameof(PipSize), 0.0001m)
			.SetDisplay("Pip Size", "Size of one pip used for price conversions", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General");
	}

	/// <summary>
	/// Fixed order volume used when <see cref="UseRiskSizing"/> is disabled.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Enables risk-based position sizing.
	/// </summary>
	public bool UseRiskSizing
	{
		get => _useRiskSizing.Value;
		set => _useRiskSizing.Value = value;
	}

	/// <summary>
	/// Risk percentage applied to the portfolio when calculating volume.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA length for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Force Index lookback length.
	/// </summary>
	public int ForceLength
	{
		get => _forceLength.Value;
		set => _forceLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Trailing step distance in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Entry buffer applied to previous highs and lows.
	/// </summary>
	public decimal EntryOffsetPips
	{
		get => _entryOffsetPips.Value;
		set => _entryOffsetPips.Value = value;
	}

	/// <summary>
	/// Minimum allowed distance between price and stop/take levels.
	/// </summary>
	public decimal MinDistancePips
	{
		get => _minDistancePips.Value;
		set => _minDistancePips.Value = value;
	}

	/// <summary>
	/// Pip size used for point conversions.
	/// </summary>
	public decimal PipSize
	{
		get => _pipSize.Value;
		set => _pipSize.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security security, DataType dataType)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevMacd = null;
		_prevPrevMacd = null;
		_prevOsma = null;
		_prevPrevOsma = null;
		_prevForce = null;
		_previousHigh = null;
		_previousLow = null;

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfitPrice = null;
		_shortTakeProfitPrice = null;
		_plannedLongStop = null;
		_plannedShortStop = null;
		_plannedLongTake = null;
		_plannedShortTake = null;

		_pendingBuyLimit = null;
		_pendingSellLimit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSizeValue = ResolvePipSize();
		_stopLossDistance = StopLossPips > 0m ? StopLossPips * _pipSizeValue : 0m;
		_takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * _pipSizeValue : 0m;
		_trailingStopDistance = TrailingStopPips > 0m ? TrailingStopPips * _pipSizeValue : 0m;
		_trailingStepDistance = TrailingStepPips > 0m ? TrailingStepPips * _pipSizeValue : 0m;
		_entryOffsetDistance = EntryOffsetPips > 0m ? EntryOffsetPips * _pipSizeValue : 0m;
		_minDistance = MinDistancePips > 0m ? MinDistancePips * _pipSizeValue : 0m;

		Volume = OrderVolume;

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod },
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		_forceIndex = new ForceIndex { Length = ForceLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _forceIndex, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _forceIndex);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue forceValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed || !_forceIndex.IsFormed)
			goto UpdateHistory;

		if (!IsFormedAndOnlineAndAllowTrading())
			goto UpdateHistory;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			goto UpdateHistory;

		var osmaValue = macdLine - signalLine;
		var currentForce = forceValue.ToDecimal();

		var bid = Security?.BestBid?.Price ?? candle.ClosePrice;
		var ask = Security?.BestAsk?.Price ?? candle.ClosePrice;
		if (ask < bid)
			ask = bid;

		var spread = ask - bid;
		var freezeLevel = spread > 0m ? spread * 3m * 1.1m : 0m;
		var stopLevel = freezeLevel;
		var level = Math.Max(_minDistance, Math.Max(freezeLevel, stopLevel));
		if (level <= 0m)
			level = _minDistance;

		var canEvaluate = _prevMacd.HasValue && _prevPrevMacd.HasValue &&
			_prevOsma.HasValue && _prevPrevOsma.HasValue &&
			_prevForce.HasValue && _previousHigh.HasValue && _previousLow.HasValue;

		if (canEvaluate)
		{
			var macdDirection = Compare(_prevMacd.Value, _prevPrevMacd.Value);
			var osmaDirection = Compare(_prevOsma!.Value, _prevPrevOsma!.Value);
			var forcePositive = _prevForce!.Value > 0m;
			var forceNegative = _prevForce.Value < 0m;

			ManageOpenPositions(candle);
			HandlePendingOrderCancellation(osmaDirection);

			if (Position == 0m && _pendingBuyLimit == null && _pendingSellLimit == null)
			{
				if (osmaDirection == 1 && forceNegative)
				{
					TryPlaceSellLimit(_previousHigh!.Value, bid, level);
				}

				if (osmaDirection == -1 && forcePositive)
				{
					TryPlaceBuyLimit(_previousLow!.Value, ask, level);
				}
			}
		}

UpdateHistory:
		_prevPrevMacd = _prevMacd;
		_prevMacd = macdValue.IsFinal ? macdTyped.Macd : _prevMacd;

		_prevPrevOsma = _prevOsma;
		_prevOsma = macdValue.IsFinal ? osmaValue : _prevOsma;

		_prevForce = forceValue.IsFinal ? currentForce : _prevForce;

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}

	private void TryPlaceSellLimit(decimal previousHigh, decimal bid, decimal level)
	{
		var entryCandidate = previousHigh + _pipSizeValue;
		var targetPrice = entryCandidate > bid - _entryOffsetDistance ? entryCandidate : bid + _entryOffsetDistance;
		targetPrice = AlignPrice(targetPrice);

		if (targetPrice - bid < level)
			return;

		var stopPrice = _stopLossDistance > 0m ? AlignPrice(targetPrice + _stopLossDistance) : (decimal?)null;
		var takePrice = _takeProfitDistance > 0m ? AlignPrice(targetPrice - _takeProfitDistance) : (decimal?)null;

		if (stopPrice.HasValue && _stopLossDistance < level)
			stopPrice = null;
		if (takePrice.HasValue && _takeProfitDistance < level)
			takePrice = null;

		var volume = GetOrderVolume(_stopLossDistance);
		if (volume <= 0m)
			return;

		CancelOrder(ref _pendingSellLimit);
		_pendingSellLimit = SellLimit(volume, targetPrice);
		_plannedShortStop = stopPrice;
		_plannedShortTake = takePrice;
	}

	private void TryPlaceBuyLimit(decimal previousLow, decimal ask, decimal level)
	{
		var entryCandidate = previousLow - _pipSizeValue;
		var targetPrice = entryCandidate < ask + _entryOffsetDistance ? entryCandidate : ask + _entryOffsetDistance;
		targetPrice = AlignPrice(targetPrice);

		if (ask - targetPrice < level)
			return;

		var stopPrice = _stopLossDistance > 0m ? AlignPrice(targetPrice - _stopLossDistance) : (decimal?)null;
		var takePrice = _takeProfitDistance > 0m ? AlignPrice(targetPrice + _takeProfitDistance) : (decimal?)null;

		if (stopPrice.HasValue && _stopLossDistance < level)
			stopPrice = null;
		if (takePrice.HasValue && _takeProfitDistance < level)
			takePrice = null;

		var volume = GetOrderVolume(_stopLossDistance);
		if (volume <= 0m)
			return;

		CancelOrder(ref _pendingBuyLimit);
		_pendingBuyLimit = BuyLimit(volume, targetPrice);
		_plannedLongStop = stopPrice;
		_plannedLongTake = takePrice;
	}

	private void HandlePendingOrderCancellation(int osmaDirection)
	{
		if (_pendingBuyLimit != null && osmaDirection == -1)
		{
			CancelOrder(ref _pendingBuyLimit);
			_plannedLongStop = null;
			_plannedLongTake = null;
		}

		if (_pendingSellLimit != null && osmaDirection == 1)
		{
			CancelOrder(ref _pendingSellLimit);
			_plannedShortStop = null;
			_plannedShortTake = null;
		}
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			UpdateLongTrailing(candle);

			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				return;
			}

			if (_longTakeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0m)
		{
			UpdateShortTrailing(candle);

			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (_shortTakeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_longEntryPrice is not decimal entryPrice || _trailingStopDistance <= 0m)
			return;

		var trigger = entryPrice + _trailingStopDistance + _trailingStepDistance;
		if (candle.ClosePrice <= trigger)
			return;

		var newStop = AlignPrice(candle.ClosePrice - _trailingStopDistance);
		if (_longStopPrice is not decimal currentStop || newStop - currentStop >= _trailingStepDistance)
			_longStopPrice = newStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_shortEntryPrice is not decimal entryPrice || _trailingStopDistance <= 0m)
			return;

		var trigger = entryPrice - _trailingStopDistance - _trailingStepDistance;
		if (candle.ClosePrice >= trigger)
			return;

		var newStop = AlignPrice(candle.ClosePrice + _trailingStopDistance);
		if (_shortStopPrice is not decimal currentStop || currentStop - newStop >= _trailingStepDistance)
			_shortStopPrice = newStop;
	}

	private decimal ResolvePipSize()
	{
		if (PipSize > 0m)
			return PipSize;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;

		var decimals = Security?.Decimals;
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private decimal AlignPrice(decimal price)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return price;

		return Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
	}

	private decimal AlignVolume(decimal volume)
	{
		var step = Security?.VolumeStep ?? 0m;
		if (step <= 0m)
			return volume;

		var steps = Math.Max(1m, Math.Floor(volume / step));
		return steps * step;
	}

	private decimal GetOrderVolume(decimal stopDistance)
	{
		if (!UseRiskSizing)
			return AlignVolume(OrderVolume);

		if (stopDistance <= 0m || RiskPercent <= 0m)
			return AlignVolume(OrderVolume);

		var portfolio = Portfolio;
		if (portfolio is null)
			return AlignVolume(OrderVolume);

		var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
			return AlignVolume(OrderVolume);

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
			return AlignVolume(OrderVolume);

		var perUnitRisk = stopDistance / priceStep * stepPrice;
		if (perUnitRisk <= 0m)
			return AlignVolume(OrderVolume);

		var riskAmount = equity * RiskPercent / 100m;
		var rawVolume = riskAmount / perUnitRisk;
		return AlignVolume(Math.Max(rawVolume, 0m));
	}

	private static int Compare(decimal first, decimal second)
	{
		if (first > second)
			return 1;
		if (first < second)
			return -1;
		return 0;
	}

	private void CancelOrder(ref Order orderField)
	{
		var order = orderField;
		if (order == null)
			return;

		if (order.State is OrderStates.Active or OrderStates.Pending)
			CancelOrder(order);

		orderField = null;
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (_pendingBuyLimit != null && order == _pendingBuyLimit && order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			if (order.State != OrderStates.Done)
			{
				_plannedLongStop = null;
				_plannedLongTake = null;
			}

			_pendingBuyLimit = null;
		}

		if (_pendingSellLimit != null && order == _pendingSellLimit && order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			if (order.State != OrderStates.Done)
			{
				_plannedShortStop = null;
				_plannedShortTake = null;
			}

			_pendingSellLimit = null;
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		if (_pendingBuyLimit != null && trade.Order == _pendingBuyLimit)
		{
			_pendingBuyLimit = null;
			_longEntryPrice = trade.Trade.Price;
			_longStopPrice = _plannedLongStop;
			_longTakeProfitPrice = _plannedLongTake;
			_plannedLongStop = null;
			_plannedLongTake = null;

			CancelOrder(ref _pendingSellLimit);
		}
		else if (_pendingSellLimit != null && trade.Order == _pendingSellLimit)
		{
			_pendingSellLimit = null;
			_shortEntryPrice = trade.Trade.Price;
			_shortStopPrice = _plannedShortStop;
			_shortTakeProfitPrice = _plannedShortTake;
			_plannedShortStop = null;
			_plannedShortTake = null;

			CancelOrder(ref _pendingBuyLimit);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_longStopPrice = null;
			_shortStopPrice = null;
			_longTakeProfitPrice = null;
			_shortTakeProfitPrice = null;
		}
		else if (Position > 0m)
		{
			_shortEntryPrice = null;
			_shortStopPrice = null;
			_shortTakeProfitPrice = null;
		}
		else if (Position < 0m)
		{
			_longEntryPrice = null;
			_longStopPrice = null;
			_longTakeProfitPrice = null;
		}
	}
}

