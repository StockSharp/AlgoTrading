using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Places opposing stop orders around the 9 AM London fix and manages the breakout position.
/// </summary>
public class Gbp9AmStrategy : Strategy
{
	private readonly StrategyParam<int> _lookHour;
	private readonly StrategyParam<int> _lookMinute;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<bool> _useCloseHour;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _buyDistancePips;
	private readonly StrategyParam<int> _sellDistancePips;
	private readonly StrategyParam<int> _buyStopLossPips;
	private readonly StrategyParam<int> _sellStopLossPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private bool _canSendOrders = true;
	private DateTime _currentDate;
	private Order _longOrder;
	private Order _shortOrder;
	private bool _longOrderActive;
	private bool _shortOrderActive;
	private decimal? _longStopLoss;
	private decimal? _longTakeProfit;
	private decimal? _shortStopLoss;
	private decimal? _shortTakeProfit;
	private decimal? _activeStopLoss;
	private decimal? _activeTakeProfit;
	private int _positionDirection;


	/// <summary>
	/// Hour corresponding to the London 9 AM observation.
	/// </summary>
	public int LookHour
	{
		get => _lookHour.Value;
		set => _lookHour.Value = value;
	}

	/// <summary>
	/// Minute offset inside the look hour when the price snapshot is taken.
	/// </summary>
	public int LookMinute
	{
		get => _lookMinute.Value;
		set => _lookMinute.Value = value;
	}

	/// <summary>
	/// Hour when open trades are forcefully flattened.
	/// </summary>
	public int CloseHour
	{
		get => _closeHour.Value;
		set => _closeHour.Value = value;
	}

	/// <summary>
	/// Enables the daily close hour logic.
	/// </summary>
	public bool UseCloseHour
	{
		get => _useCloseHour.Value;
		set => _useCloseHour.Value = value;
	}

	/// <summary>
	/// Profit target distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance to the buy stop entry in pips.
	/// </summary>
	public int BuyDistancePips
	{
		get => _buyDistancePips.Value;
		set => _buyDistancePips.Value = value;
	}

	/// <summary>
	/// Distance to the sell stop entry in pips.
	/// </summary>
	public int SellDistancePips
	{
		get => _sellDistancePips.Value;
		set => _sellDistancePips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades in pips.
	/// </summary>
	public int BuyStopLossPips
	{
		get => _buyStopLossPips.Value;
		set => _buyStopLossPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades in pips.
	/// </summary>
	public int SellStopLossPips
	{
		get => _sellStopLossPips.Value;
		set => _sellStopLossPips.Value = value;
	}

	/// <summary>
	/// Candle type used for timing decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="Gbp9AmStrategy"/>.
	/// </summary>
	public Gbp9AmStrategy()
	{

		_lookHour = Param(nameof(LookHour), 9)
			.SetDisplay("Look hour", "Hour that represents the London 9 AM fix", "Timing")
			.SetCanOptimize(true);

		_lookMinute = Param(nameof(LookMinute), 0)
			.SetDisplay("Look minute", "Minute offset for the morning snapshot", "Timing")
			.SetCanOptimize(true);

		_closeHour = Param(nameof(CloseHour), 18)
			.SetDisplay("Close hour", "Hour for forced daily exit", "Timing")
			.SetCanOptimize(true);

		_useCloseHour = Param(nameof(UseCloseHour), true)
			.SetDisplay("Use close hour", "Toggle the forced exit logic", "Timing");

		_takeProfitPips = Param(nameof(TakeProfitPips), 40)
			.SetGreaterThanZero()
			.SetDisplay("Take profit (pips)", "Target distance applied to triggered trades", "Risk management")
			.SetCanOptimize(true);

		_buyDistancePips = Param(nameof(BuyDistancePips), 18)
			.SetGreaterThanZero()
			.SetDisplay("Buy distance (pips)", "Offset for the buy stop order", "Entries")
			.SetCanOptimize(true);

		_sellDistancePips = Param(nameof(SellDistancePips), 22)
			.SetGreaterThanZero()
			.SetDisplay("Sell distance (pips)", "Offset for the sell stop order", "Entries")
			.SetCanOptimize(true);

		_buyStopLossPips = Param(nameof(BuyStopLossPips), 22)
			.SetGreaterThanZero()
			.SetDisplay("Buy stop-loss (pips)", "Stop-loss distance for long positions", "Risk management")
			.SetCanOptimize(true);

		_sellStopLossPips = Param(nameof(SellStopLossPips), 18)
			.SetGreaterThanZero()
			.SetDisplay("Sell stop-loss (pips)", "Stop-loss distance for short positions", "Risk management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle series that drives the schedule", "General");
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

		_canSendOrders = true;
		_currentDate = default;
		_longOrder = null;
		_shortOrder = null;
		_longOrderActive = false;
		_shortOrderActive = false;
		_longStopLoss = null;
		_longTakeProfit = null;
		_shortStopLoss = null;
		_shortTakeProfit = null;
		_activeStopLoss = null;
		_activeTakeProfit = null;
		_positionDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_pipSize = CalculatePipSize();

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
		// Ignore unfinished candles to avoid double processing.
		if (candle.State != CandleStates.Finished)
			return;

		var candleTime = candle.CloseTime;

		// Re-arm the daily logic when the calendar day changes.
		if (_currentDate != candleTime.Date)
		{
			_currentDate = candleTime.Date;
			_canSendOrders = true;
		}

		UpdatePendingOrdersState();
		UpdatePositionState();

		// Enforce daily close-out if configured.
		if (UseCloseHour && candleTime.Hour >= CloseHour)
		{
			CancelActiveOrders();
			ResetPendingOrders();

			if (Position != 0)
			{
				ClosePosition();
				ResetActiveTargets();
			}

			_canSendOrders = false;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Manage active trades before checking for new entries.
		if (_positionDirection != 0 && TryExitPosition(candle))
			return;

		// Replicate the MQL clear_to_send gate.
		if (!_canSendOrders)
		{
			if (ShouldRearmOrders(candleTime))
				_canSendOrders = true;
			else
				return;
		}

		if (Position != 0)
			return;

		// Schedule both stop orders once the look time is reached.
		if (candleTime.Hour == LookHour && candleTime.Minute >= LookMinute)
			PlaceEntryOrders(candle.ClosePrice);
	}

	private void PlaceEntryOrders(decimal referencePrice)
	{
		if (Volume <= 0m || referencePrice <= 0m)
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		// Approximate the MQL price offsets using the instrument pip size.
		var longEntry = RoundPrice(referencePrice + (decimal)BuyDistancePips * _pipSize);
		var longStop = BuyStopLossPips <= 0 ? null : RoundPrice(longEntry - (decimal)BuyStopLossPips * _pipSize);
		var longTake = TakeProfitPips <= 0 ? null : RoundPrice(longEntry + (decimal)TakeProfitPips * _pipSize);

		var shortEntry = RoundPrice(referencePrice - (decimal)SellDistancePips * _pipSize);
		var shortStop = SellStopLossPips <= 0 ? null : RoundPrice(shortEntry + (decimal)SellStopLossPips * _pipSize);
		var shortTake = TakeProfitPips <= 0 ? null : RoundPrice(shortEntry - (decimal)TakeProfitPips * _pipSize);

		// Register the stop orders and store references for later state checks.
		var buyOrder = BuyStop(Volume, longEntry);
		if (buyOrder != null)
		{
			_longOrder = buyOrder;
			_longOrderActive = true;
			_longStopLoss = longStop;
			_longTakeProfit = longTake;
		}

		var sellOrder = SellStop(Volume, shortEntry);
		if (sellOrder != null)
		{
			_shortOrder = sellOrder;
			_shortOrderActive = true;
			_shortStopLoss = shortStop;
			_shortTakeProfit = shortTake;
		}

		_canSendOrders = false;
	}

	private void UpdatePendingOrdersState()
	{
		// Track whether the stored orders are still active.
		if (_longOrder != null && !_longOrder.State.IsActive())
		{
			_longOrderActive = false;

			if (_longOrder.State != OrderStates.Done)
				_longOrder = null;
		}
		else if (_longOrder != null)
		{
			_longOrderActive = true;
		}
		else
		{
			_longOrderActive = false;
		}

		if (_shortOrder != null && !_shortOrder.State.IsActive())
		{
			_shortOrderActive = false;

			if (_shortOrder.State != OrderStates.Done)
				_shortOrder = null;
		}
		else if (_shortOrder != null)
		{
			_shortOrderActive = true;
		}
		else
		{
			_shortOrderActive = false;
		}
	}

	private void UpdatePositionState()
	{
		if (Position > 0m && _positionDirection <= 0)
		{
			_positionDirection = 1;
			_activeStopLoss = _longStopLoss;
			_activeTakeProfit = _longTakeProfit;
			CancelActiveOrders();
			ResetPendingOrders();
		}
		else if (Position < 0m && _positionDirection >= 0)
		{
			_positionDirection = -1;
			_activeStopLoss = _shortStopLoss;
			_activeTakeProfit = _shortTakeProfit;
			CancelActiveOrders();
			ResetPendingOrders();
		}
		else if (Position == 0m && _positionDirection != 0)
		{
			_positionDirection = 0;
			_activeStopLoss = null;
			_activeTakeProfit = null;
		}
	}

	private bool TryExitPosition(ICandleMessage candle)
	{
		var volume = Math.Abs(Position);

		if (volume == 0m)
			return false;

		// Close the long position if either stop-loss or target is touched.
		if (_positionDirection > 0)
		{
			if (_activeStopLoss.HasValue && candle.LowPrice <= _activeStopLoss.Value)
			{
				SellMarket(volume);
				ResetPendingOrders();
				ResetActiveTargets();
				return true;
			}

			if (_activeTakeProfit.HasValue && candle.HighPrice >= _activeTakeProfit.Value)
			{
				SellMarket(volume);
				ResetPendingOrders();
				ResetActiveTargets();
				return true;
			}
		}
		// Close the short position if either stop-loss or target is touched.
		else if (_positionDirection < 0)
		{
			if (_activeStopLoss.HasValue && candle.HighPrice >= _activeStopLoss.Value)
			{
				BuyMarket(volume);
				ResetPendingOrders();
				ResetActiveTargets();
				return true;
			}

			if (_activeTakeProfit.HasValue && candle.LowPrice <= _activeTakeProfit.Value)
			{
				BuyMarket(volume);
				ResetPendingOrders();
				ResetActiveTargets();
				return true;
			}
		}

		return false;
	}

	private bool ShouldRearmOrders(DateTime candleTime)
	{
		// Pending orders must be absent before we can prepare another straddle.
		if (_longOrderActive || _shortOrderActive)
			return false;

		if (Position != 0m)
			return false;

		var minute = candleTime.Minute;
		var hour = candleTime.Hour;
		var previousHour = (LookHour + 23) % 24;

		// Allow re-arming about one hour before the next look time, replicating the MQL behavior.
		if (hour == previousHour && Math.Abs(minute - LookMinute) < 10)
			return true;

		// Enable the flag once the market moves away from the look hour.
		if (hour != LookHour && minute >= LookMinute)
			return true;

		return false;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			return 0.0001m;

		var digits = 0;
		var value = step;

		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
			return step * 10m;

		return step;
	}

	private decimal RoundPrice(decimal price)
	{
		var step = Security?.PriceStep;

		if (step == null || step.Value <= 0m)
			return price;

		return Math.Round(price / step.Value, 0, MidpointRounding.AwayFromZero) * step.Value;
	}

	private void ResetPendingOrders()
	{
		_longOrder = null;
		_shortOrder = null;
		_longOrderActive = false;
		_shortOrderActive = false;
	}

	private void ResetActiveTargets()
	{
		_positionDirection = 0;
		_activeStopLoss = null;
		_activeTakeProfit = null;
		_longStopLoss = null;
		_longTakeProfit = null;
		_shortStopLoss = null;
		_shortTakeProfit = null;
	}
}
