using System;
using System.Globalization;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 "Pivots" indicator and companion expert advisor located in MQL/8550.
/// The strategy calculates classic floor pivot levels from daily candles, keeps a pair of pending orders
/// around the central pivot, and manages protective stop/limit orders with a trailing stop.
/// </summary>
public class PivotsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _pivotCandleType;
	private readonly StrategyParam<bool> _logPivotUpdates;

	private PivotLevels? _activeLevels;
	private PivotLevels? _pendingLevels;
	private DateTime? _activeDate;
	private DateTime? _pendingDate;

	private Order? _buyLimitOrder;
	private Order? _sellStopOrder;
	private Order? _longStopLossOrder;
	private Order? _longTakeProfitOrder;
	private Order? _shortStopLossOrder;
	private Order? _shortTakeProfitOrder;

	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal _lastKnownPosition;
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="PivotsStrategy"/> class.
	/// </summary>
	public PivotsStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume of each pending order", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 30)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Trailing Stop (points)", "Trailing stop distance expressed in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 200, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Working Candle Type", "Intraday series used for trailing and scheduling", "Data");

		_pivotCandleType = Param(nameof(PivotCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Pivot Candle Type", "Higher timeframe used to derive classic pivot levels", "Data");

		_logPivotUpdates = Param(nameof(LogPivotUpdates), true)
			.SetDisplay("Log Pivot Updates", "Write pivot levels to the log whenever they change", "Diagnostics");
	}

	/// <summary>
	/// Volume of each pending order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in indicator points.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Intraday candle series used for trailing updates and daily session boundaries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe series that supplies the OHLC data for pivot calculation.
	/// </summary>
	public DataType PivotCandleType
	{
		get => _pivotCandleType.Value;
		set => _pivotCandleType.Value = value;
	}

	/// <summary>
	/// Toggle logging of pivot level changes.
	/// </summary>
	public bool LogPivotUpdates
	{
		get => _logPivotUpdates.Value;
		set => _logPivotUpdates.Value = value;
	}

	/// <summary>
	/// Current pivot value.
	/// </summary>
	public decimal? Pivot => _activeLevels?.Pivot;

	/// <summary>
	/// First resistance level (R1).
	/// </summary>
	public decimal? Resistance1 => _activeLevels?.R1;

	/// <summary>
	/// Second resistance level (R2).
	/// </summary>
	public decimal? Resistance2 => _activeLevels?.R2;

	/// <summary>
	/// Third resistance level (R3).
	/// </summary>
	public decimal? Resistance3 => _activeLevels?.R3;

	/// <summary>
	/// First support level (S1).
	/// </summary>
	public decimal? Support1 => _activeLevels?.S1;

	/// <summary>
	/// Second support level (S2).
	/// </summary>
	public decimal? Support2 => _activeLevels?.S2;

	/// <summary>
	/// Third support level (S3).
	/// </summary>
	public decimal? Support3 => _activeLevels?.S3;

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_activeLevels = null;
		_pendingLevels = null;
		_activeDate = null;
		_pendingDate = null;

		_buyLimitOrder = null;
		_sellStopOrder = null;
		_longStopLossOrder = null;
		_longTakeProfitOrder = null;
		_shortStopLossOrder = null;
		_shortTakeProfitOrder = null;

		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_lastKnownPosition = 0m;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPriceStep();

		var intradaySubscription = SubscribeCandles(CandleType);
		intradaySubscription
			.Bind(ProcessIntradayCandle)
			.Start();

		var dailySubscription = SubscribeCandles(PivotCandleType);
		dailySubscription
			.Bind(ProcessDailyCandle)
			.Start();
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_pendingLevels = CalculatePivotLevels(candle);
		_pendingDate = candle.OpenTime.Date.AddDays(1);
	}

	private void ProcessIntradayCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var candleDate = candle.OpenTime.Date;

		if (_pendingLevels is PivotLevels && _pendingDate is DateTime pendingDate && candleDate >= pendingDate)
			ActivatePendingLevels(pendingDate);

		UpdateTrailingStops(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_activeLevels is not PivotLevels levels)
			return;

		EnsurePendingOrders(levels);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null)
			return;

		var previousPosition = _lastKnownPosition;
		var currentPosition = Position;
		_lastKnownPosition = currentPosition;

		if (order == _buyLimitOrder)
		{
			_buyLimitOrder = null;

			if (currentPosition > 0m)
			{
				var previousLongVolume = Math.Max(previousPosition, 0m);
				_longEntryPrice = CalculateAveragePrice(_longEntryPrice, previousLongVolume, trade.Trade.Price, trade.Trade.Volume);
				UpdateLongProtection(Math.Max(currentPosition, 0m));
			}
		}
		else if (order == _sellStopOrder)
		{
			_sellStopOrder = null;

			if (currentPosition < 0m)
			{
				var previousShortVolume = Math.Abs(Math.Min(previousPosition, 0m));
				_shortEntryPrice = CalculateAveragePrice(_shortEntryPrice, previousShortVolume, trade.Trade.Price, trade.Trade.Volume);
				UpdateShortProtection(Math.Abs(Math.Min(currentPosition, 0m)));
			}
		}
		else if (order == _longStopLossOrder || order == _longTakeProfitOrder)
		{
			CancelOrderIfActive(ref _longStopLossOrder);
			CancelOrderIfActive(ref _longTakeProfitOrder);

			if (currentPosition <= 0m)
				_longEntryPrice = 0m;
		}
		else if (order == _shortStopLossOrder || order == _shortTakeProfitOrder)
		{
			CancelOrderIfActive(ref _shortStopLossOrder);
			CancelOrderIfActive(ref _shortTakeProfitOrder);

			if (currentPosition >= 0m)
				_shortEntryPrice = 0m;
		}
	}

	private void ActivatePendingLevels(DateTime applyDate)
	{
		if (_pendingLevels is not PivotLevels levels)
			return;

		_activeLevels = levels;
		_activeDate = applyDate;
		_pendingLevels = null;
		_pendingDate = null;

		CancelOrderIfActive(ref _buyLimitOrder);
		CancelOrderIfActive(ref _sellStopOrder);

		if (LogPivotUpdates)
		{
			LogInfo(FormattableString.Invariant($"Pivot levels for {applyDate:yyyy-MM-dd}: " +
				$"P={FormatPrice(levels.Pivot)}, R1={FormatPrice(levels.R1)}, R2={FormatPrice(levels.R2)}, R3={FormatPrice(levels.R3)}, " +
				$"S1={FormatPrice(levels.S1)}, S2={FormatPrice(levels.S2)}, S3={FormatPrice(levels.S3)}"));
		}
	}

	private void EnsurePendingOrders(PivotLevels levels)
	{
		if (OrderVolume <= 0m)
			return;

		var volume = OrderVolume;

		if (!IsOrderAlive(_buyLimitOrder) || _buyLimitOrder!.Price != levels.Pivot)
		{
			CancelOrderIfActive(ref _buyLimitOrder);
			_buyLimitOrder = BuyLimit(volume, levels.Pivot);
		}

		if (!IsOrderAlive(_sellStopOrder) || _sellStopOrder!.Price != levels.Pivot)
		{
			CancelOrderIfActive(ref _sellStopOrder);
			_sellStopOrder = SellStop(volume, levels.Pivot);
		}
	}

	private void UpdateLongProtection(decimal volume)
	{
		CancelOrderIfActive(ref _longStopLossOrder);
		CancelOrderIfActive(ref _longTakeProfitOrder);

		if (volume <= 0m || _activeLevels is not PivotLevels levels)
			return;

		_longStopLossOrder = SellStop(volume, levels.S2);
		_longTakeProfitOrder = SellLimit(volume, levels.R2);
	}

	private void UpdateShortProtection(decimal volume)
	{
		CancelOrderIfActive(ref _shortStopLossOrder);
		CancelOrderIfActive(ref _shortTakeProfitOrder);

		if (volume <= 0m || _activeLevels is not PivotLevels levels)
			return;

		_shortStopLossOrder = BuyStop(volume, levels.R2);
		_shortTakeProfitOrder = BuyLimit(volume, levels.S2);
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0)
			return;

		var distance = ConvertPoints(TrailingStopPoints);
		if (distance <= 0m)
			return;

		var closePrice = candle.ClosePrice;

		if (Position > 0m && _longEntryPrice > 0m && closePrice - _longEntryPrice > distance)
		{
			var newStop = closePrice - distance;

			if (_longStopLossOrder == null)
			{
				_longStopLossOrder = SellStop(Position, newStop);
			}
			else if (IsOrderAlive(_longStopLossOrder) && newStop > _longStopLossOrder.Price + _pipSize / 2m)
			{
				ReplaceStopOrder(ref _longStopLossOrder, Sides.Sell, newStop, Position);
			}
		}
		else if (Position < 0m && _shortEntryPrice > 0m && _shortEntryPrice - closePrice > distance)
		{
			var newStop = closePrice + distance;
			var volume = Math.Abs(Position);

			if (_shortStopLossOrder == null)
			{
				_shortStopLossOrder = BuyStop(volume, newStop);
			}
			else if (IsOrderAlive(_shortStopLossOrder) && newStop < _shortStopLossOrder.Price - _pipSize / 2m)
			{
				ReplaceStopOrder(ref _shortStopLossOrder, Sides.Buy, newStop, volume);
			}
		}
	}

	private void ReplaceStopOrder(ref Order? target, Sides side, decimal price, decimal volume)
	{
		if (volume <= 0m)
			return;

		if (IsOrderAlive(target))
			CancelOrder(target!);

		target = side == Sides.Sell
			? SellStop(volume, price)
			: BuyStop(volume, price);
	}

	private void CancelOrderIfActive(ref Order? order)
	{
		if (order == null)
			return;

		if (IsOrderAlive(order))
			CancelOrder(order);

		order = null;
	}

	private static bool IsOrderAlive(Order? order)
	{
		return order != null && order.State is OrderStates.Active or OrderStates.Pending;
	}

	private decimal ConvertPoints(int points)
	{
		return points * _pipSize;
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? security.MinPriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}

	private static PivotLevels CalculatePivotLevels(ICandleMessage candle)
	{
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		var pivot = (high + low + close) / 3m;
		var r1 = 2m * pivot - low;
		var s1 = 2m * pivot - high;
		var range = high - low;
		var r2 = pivot + range;
		var s2 = pivot - range;
		var r3 = 2m * pivot + high - 2m * low;
		var s3 = 2m * pivot - (2m * high - low);

		return new PivotLevels(pivot, r1, r2, r3, s1, s2, s3);
	}

	private static decimal CalculateAveragePrice(decimal currentAverage, decimal currentVolume, decimal newPrice, decimal newVolume)
	{
		if (newVolume <= 0m)
			return currentVolume > 0m ? currentAverage : newPrice;

		if (currentVolume <= 0m)
			return newPrice;

		var totalVolume = currentVolume + newVolume;
		return totalVolume <= 0m
			? 0m
			: ((currentAverage * currentVolume) + (newPrice * newVolume)) / totalVolume;
	}

	private string FormatPrice(decimal price)
	{
		var security = Security;
		if (security?.Decimals is int decimals && decimals > 0)
			return price.ToString("F" + decimals, CultureInfo.InvariantCulture);

		return price.ToString(CultureInfo.InvariantCulture);
	}

	private readonly record struct PivotLevels(decimal Pivot, decimal R1, decimal R2, decimal R3, decimal S1, decimal S2, decimal S3);
}
