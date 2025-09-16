using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Places a straddle around the 9 AM London session using configurable pip distances.
/// </summary>
public class Gbp9AmBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
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
	private bool _ordersPlaced;
	private DateTime _currentDate;
	private decimal? _longStopLoss;
	private decimal? _longTakeProfit;
	private decimal? _shortStopLoss;
	private decimal? _shortTakeProfit;
	private decimal? _activeStopLoss;
	private decimal? _activeTakeProfit;
	private int _positionDirection;

	/// <summary>
	/// Order volume used for both stop orders.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Hour that corresponds to 9 AM London in the data feed time zone.
	/// </summary>
	public int LookHour
	{
		get => _lookHour.Value;
		set => _lookHour.Value = value;
	}

	/// <summary>
	/// Minute offset within the look hour when orders are placed.
	/// </summary>
	public int LookMinute
	{
		get => _lookMinute.Value;
		set => _lookMinute.Value = value;
	}

	/// <summary>
	/// Hour when the strategy forcefully closes positions.
	/// </summary>
	public int CloseHour
	{
		get => _closeHour.Value;
		set => _closeHour.Value = value;
	}

	/// <summary>
	/// Enables or disables the automatic close hour logic.
	/// </summary>
	public bool UseCloseHour
	{
		get => _useCloseHour.Value;
		set => _useCloseHour.Value = value;
	}

	/// <summary>
	/// Profit target in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance from the current price to the buy stop entry in pips.
	/// </summary>
	public int BuyDistancePips
	{
		get => _buyDistancePips.Value;
		set => _buyDistancePips.Value = value;
	}

	/// <summary>
	/// Distance from the current price to the sell stop entry in pips.
	/// </summary>
	public int SellDistancePips
	{
		get => _sellDistancePips.Value;
		set => _sellDistancePips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long positions in pips.
	/// </summary>
	public int BuyStopLossPips
	{
		get => _buyStopLossPips.Value;
		set => _buyStopLossPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short positions in pips.
	/// </summary>
	public int SellStopLossPips
	{
		get => _sellStopLossPips.Value;
		set => _sellStopLossPips.Value = value;
	}

	/// <summary>
	/// Candle type used for timing and trade management.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="Gbp9AmBreakoutStrategy"/>.
	/// </summary>
	public Gbp9AmBreakoutStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume used for both stop orders", "General")
			.SetCanOptimize(true);

		_lookHour = Param(nameof(LookHour), 9)
			.SetDisplay("Look hour", "Hour that represents 9 AM London", "Timing")
			.SetCanOptimize(true);

		_lookMinute = Param(nameof(LookMinute), 0)
			.SetDisplay("Look minute", "Minute offset for order placement", "Timing")
			.SetCanOptimize(true);

		_closeHour = Param(nameof(CloseHour), 18)
			.SetDisplay("Close hour", "Hour to flatten positions", "Timing")
			.SetCanOptimize(true);

		_useCloseHour = Param(nameof(UseCloseHour), true)
			.SetDisplay("Use close hour", "Enable the daily close logic", "Timing");

		_takeProfitPips = Param(nameof(TakeProfitPips), 40)
			.SetGreaterThanZero()
			.SetDisplay("Take profit (pips)", "Profit target distance in pips", "Risk management")
			.SetCanOptimize(true);

		_buyDistancePips = Param(nameof(BuyDistancePips), 18)
			.SetGreaterThanZero()
			.SetDisplay("Buy distance (pips)", "Buy stop distance from the current price", "Entries")
			.SetCanOptimize(true);

		_sellDistancePips = Param(nameof(SellDistancePips), 22)
			.SetGreaterThanZero()
			.SetDisplay("Sell distance (pips)", "Sell stop distance from the current price", "Entries")
			.SetCanOptimize(true);

		_buyStopLossPips = Param(nameof(BuyStopLossPips), 22)
			.SetGreaterThanZero()
			.SetDisplay("Buy stop-loss (pips)", "Stop-loss distance for long trades", "Risk management")
			.SetCanOptimize(true);

		_sellStopLossPips = Param(nameof(SellStopLossPips), 18)
			.SetGreaterThanZero()
			.SetDisplay("Sell stop-loss (pips)", "Stop-loss distance for short trades", "Risk management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle series used for timing", "General");
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

		_ordersPlaced = false;
		_currentDate = default;
		_positionDirection = 0;
		_activeStopLoss = null;
		_activeTakeProfit = null;
		_longStopLoss = null;
		_longTakeProfit = null;
		_shortStopLoss = null;
		_shortTakeProfit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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
		if (candle.State != CandleStates.Finished)
			return;

		var candleTime = candle.CloseTime;
		if (_currentDate != candleTime.Date)
		{
			_currentDate = candleTime.Date;
			_ordersPlaced = false;
		}

		UpdatePositionState();

		if (UseCloseHour && candleTime.Hour >= CloseHour)
		{
			CancelActiveOrders();

			if (Position != 0)
			{
				ClosePosition();
				ResetActiveTargets();
			}

			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_positionDirection != 0 && TryExitPosition(candle))
			return;

		if (_ordersPlaced)
			return;

		if (Position != 0)
			return;

		if (candleTime.Hour == LookHour && candleTime.Minute >= LookMinute)
			PlaceEntryOrders(candle.ClosePrice);
	}

	private void PlaceEntryOrders(decimal referencePrice)
	{
		if (Volume <= 0m || referencePrice <= 0m)
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		_longStopLoss = null;
		_longTakeProfit = null;
		_shortStopLoss = null;
		_shortTakeProfit = null;

		CancelActiveOrders();

		if (Position != 0)
		{
			ClosePosition();
			ResetActiveTargets();
		}

		if (BuyDistancePips > 0)
		{
			var buyEntry = RoundPrice(referencePrice + (decimal)BuyDistancePips * _pipSize);

			if (buyEntry > 0m)
			{
				BuyStop(Volume, buyEntry);
				_longStopLoss = BuyStopLossPips <= 0 ? null : RoundPrice(referencePrice - (decimal)BuyStopLossPips * _pipSize);
				_longTakeProfit = TakeProfitPips <= 0 ? null : RoundPrice(referencePrice + (decimal)TakeProfitPips * _pipSize);
			}
		}

		if (SellDistancePips > 0)
		{
			var sellEntry = RoundPrice(referencePrice - (decimal)SellDistancePips * _pipSize);

			if (sellEntry > 0m)
			{
				SellStop(Volume, sellEntry);
				_shortStopLoss = SellStopLossPips <= 0 ? null : RoundPrice(referencePrice + (decimal)SellStopLossPips * _pipSize);
				_shortTakeProfit = TakeProfitPips <= 0 ? null : RoundPrice(referencePrice - (decimal)TakeProfitPips * _pipSize);
			}
		}

		_ordersPlaced = true;
	}

	private void UpdatePositionState()
	{
		if (Position > 0m && _positionDirection <= 0)
		{
			_positionDirection = 1;
			_activeStopLoss = _longStopLoss;
			_activeTakeProfit = _longTakeProfit;
			CancelActiveOrders();
		}
		else if (Position < 0m && _positionDirection >= 0)
		{
			_positionDirection = -1;
			_activeStopLoss = _shortStopLoss;
			_activeTakeProfit = _shortTakeProfit;
			CancelActiveOrders();
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

		if (_positionDirection > 0)
		{
			if (_activeStopLoss.HasValue && candle.LowPrice <= _activeStopLoss.Value)
			{
				SellMarket(volume);
				CancelActiveOrders();
				ResetActiveTargets();
				return true;
			}

			if (_activeTakeProfit.HasValue && candle.HighPrice >= _activeTakeProfit.Value)
			{
				SellMarket(volume);
				CancelActiveOrders();
				ResetActiveTargets();
				return true;
			}
		}
		else if (_positionDirection < 0)
		{
			if (_activeStopLoss.HasValue && candle.HighPrice >= _activeStopLoss.Value)
			{
				BuyMarket(volume);
				CancelActiveOrders();
				ResetActiveTargets();
				return true;
			}

			if (_activeTakeProfit.HasValue && candle.LowPrice <= _activeTakeProfit.Value)
			{
				BuyMarket(volume);
				CancelActiveOrders();
				ResetActiveTargets();
				return true;
			}
		}

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

	private void ResetActiveTargets()
	{
		_positionDirection = 0;
		_activeStopLoss = null;
		_activeTakeProfit = null;
	}
}
