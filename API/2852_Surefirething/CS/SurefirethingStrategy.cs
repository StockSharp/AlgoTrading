using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Surefirething breakout grid strategy.
/// Places symmetric limit orders around the latest candle close and manages trailing stops.
/// Cancels all exposure shortly before the end of the trading day.
/// </summary>
public class SurefirethingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private Order _buyLimitOrder;
	private Order _sellLimitOrder;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _entryPrice;
	private bool _isLongPosition;
	private DateTime? _lastCleanupDate;

	/// <summary>
	/// Base order volume used for the limit orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional move in pips required before updating the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle type used to compute breakout levels.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SurefirethingStrategy"/> class.
	/// </summary>
	public SurefirethingStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Base volume for limit orders", "Orders")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk")
			.SetGreaterThanOrEqual(0);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk")
			.SetGreaterThanOrEqual(0);

		_trailingStopPips = Param(nameof(TrailingStopPips), 25)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
			.SetGreaterThanOrEqual(0);

		_trailingStepPips = Param(nameof(TrailingStepPips), 1)
			.SetDisplay("Trailing Step (pips)", "Extra move before trailing stop is moved", "Risk")
			.SetGreaterThanOrEqual(0);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to calculate grid levels", "General");
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips == 0)
			throw new InvalidOperationException("Trailing is not possible: the parameter \"Trailing Step\" is zero!");

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	/// <summary>
	/// Calculates the pip size using the security price step.
	/// </summary>
	/// <returns>Pip size value.</returns>
	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			return 1m;

		var digits = 0;
		var tmp = step;

		while (tmp < 1m && digits < 10)
		{
			tmp *= 10m;
			digits++;
		}

		return digits is 3 or 5 ? step * 10m : step;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		HandleDailyCleanup(candle);
		ManageActivePosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		if (OrderVolume <= 0 || _pipSize <= 0m)
			return;

		PlaceGridOrders(candle);
	}

	/// <summary>
	/// Cancels entries and closes the position at the end of the trading day.
	/// </summary>
	/// <param name="candle">Latest candle message.</param>
	private void HandleDailyCleanup(ICandleMessage candle)
	{
		var openTime = candle.OpenTime;

		if (openTime.Hour == 23 && openTime.Minute == 50)
		{
			if (_lastCleanupDate == openTime.Date)
				return;

			_lastCleanupDate = openTime.Date;

			CancelEntryOrders();
			CloseOpenPosition();
			ResetProtection();
		}
	}

	/// <summary>
	/// Updates stop and take-profit levels for the active position.
	/// </summary>
	/// <param name="candle">Latest candle message.</param>
	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position == 0)
			return;

		if (_isLongPosition)
		{
			ApplyTrailingForLong(candle);

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Position);
				ResetProtection();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Position);
				ResetProtection();
			}
		}
		else
		{
			ApplyTrailingForShort(candle);

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
			}
		}
	}

	/// <summary>
	/// Places the buy and sell limit orders calculated from the latest candle.
	/// </summary>
	/// <param name="candle">Latest candle message.</param>
	private void PlaceGridOrders(ICandleMessage candle)
	{
		CancelEntryOrders();

		var range = (candle.HighPrice - candle.LowPrice) * 1.1m;
		var halfRange = range / 2m;

		var buyPrice = candle.ClosePrice - halfRange;
		var sellPrice = candle.ClosePrice + halfRange;

		_buyLimitOrder = BuyLimit(buyPrice, OrderVolume);
		_sellLimitOrder = SellLimit(sellPrice, OrderVolume);
	}

	/// <summary>
	/// Applies trailing logic for a long position.
	/// </summary>
	/// <param name="candle">Latest candle message.</param>
	private void ApplyTrailingForLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips * _pipSize;
		var threshold = trailingDistance + stepDistance;

		var current = candle.ClosePrice;
		var profit = current - _entryPrice;

		if (profit <= threshold)
			return;

		var minAllowed = current - threshold;
		var newStop = current - trailingDistance;

		if (!_stopPrice.HasValue || _stopPrice.Value < minAllowed)
			_stopPrice = newStop;
	}

	/// <summary>
	/// Applies trailing logic for a short position.
	/// </summary>
	/// <param name="candle">Latest candle message.</param>
	private void ApplyTrailingForShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips * _pipSize;
		var threshold = trailingDistance + stepDistance;

		var current = candle.ClosePrice;
		var profit = _entryPrice - current;

		if (profit <= threshold)
			return;

		var maxAllowed = current + threshold;
		var newStop = current + trailingDistance;

		if (!_stopPrice.HasValue || _stopPrice.Value > maxAllowed)
			_stopPrice = newStop;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			ResetProtection();
			return;
		}

		CancelEntryOrders();

		_entryPrice = Position.AveragePrice;
		_isLongPosition = Position > 0;

		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		_stopPrice = null;
		_takeProfitPrice = null;

		if (_isLongPosition)
		{
			if (StopLossPips > 0)
				_stopPrice = _entryPrice - stopDistance;

			if (TakeProfitPips > 0)
				_takeProfitPrice = _entryPrice + takeDistance;
		}
		else
		{
			if (StopLossPips > 0)
				_stopPrice = _entryPrice + stopDistance;

			if (TakeProfitPips > 0)
				_takeProfitPrice = _entryPrice - takeDistance;
		}
	}

	private void CancelEntryOrders()
	{
		if (_buyLimitOrder != null && _buyLimitOrder.State == OrderStates.Active)
			CancelOrder(_buyLimitOrder);

		if (_sellLimitOrder != null && _sellLimitOrder.State == OrderStates.Active)
			CancelOrder(_sellLimitOrder);

		_buyLimitOrder = null;
		_sellLimitOrder = null;
	}

	private void CloseOpenPosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
	}

	private void ResetProtection()
	{
		_stopPrice = null;
		_takeProfitPrice = null;
		_entryPrice = 0m;
		_isLongPosition = false;
	}

	private void ResetState()
	{
		CancelEntryOrders();
		ResetProtection();
		_lastCleanupDate = null;
		_pipSize = 0m;
	}
}
