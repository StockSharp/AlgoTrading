using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Protective trade manager inspired by the MetaTrader expert advisor "ProfitLossTrailEA".
/// Applies stop-loss, take-profit, trailing stop, and break-even logic to the current position.
/// </summary>
public class ProfitLossTrailStrategy : Strategy
{
	private readonly StrategyParam<bool> _manageAsBasket;
	private readonly StrategyParam<bool> _enableTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<decimal> _activateTrailingAfterPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _removeTakeProfit;
	private readonly StrategyParam<bool> _removeStopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _signedPosition;
	private decimal? _longAveragePrice;
	private decimal? _shortAveragePrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;
	private bool _longBreakEvenActive;
	private bool _shortBreakEvenActive;

	/// <summary>
	/// Manage all trades of the same direction as a single basket when enabled.
	/// </summary>
	public bool ManageAsBasket
	{
		get => _manageAsBasket.Value;
		set => _manageAsBasket.Value = value;
	}

	/// <summary>
	/// Enable automatic take-profit handling.
	/// </summary>
	public bool EnableTakeProfit
	{
		get => _enableTakeProfit.Value;
		set => _enableTakeProfit.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable automatic stop-loss handling.
	/// </summary>
	public bool EnableStopLoss
	{
		get => _enableStopLoss.Value;
		set => _enableStopLoss.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Enable trailing stop management once the position is in profit.
	/// </summary>
	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	/// <summary>
	/// Profit distance (in pips) required before trailing becomes active.
	/// </summary>
	public decimal ActivateTrailingAfterPips
	{
		get => _activateTrailingAfterPips.Value;
		set => _activateTrailingAfterPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum incremental gain (in pips) required before tightening the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enable break-even stop logic.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Profit distance (in pips) required before break-even activates.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional offset (in pips) applied to the break-even stop once triggered.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Remove any active take-profit levels when enabled.
	/// </summary>
	public bool RemoveTakeProfit
	{
		get => _removeTakeProfit.Value;
		set => _removeTakeProfit.Value = value;
	}

	/// <summary>
	/// Remove any active stop-loss levels when enabled.
	/// </summary>
	public bool RemoveStopLoss
	{
		get => _removeStopLoss.Value;
		set => _removeStopLoss.Value = value;
	}

	/// <summary>
	/// Candle type used to monitor the market when applying risk rules.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ProfitLossTrailStrategy"/> class.
	/// </summary>
	public ProfitLossTrailStrategy()
	{
		_manageAsBasket = Param(nameof(ManageAsBasket), true)
			.SetDisplay("Manage As Basket", "Treat all trades per side as a single basket.", "Risk");

		_enableTakeProfit = Param(nameof(EnableTakeProfit), true)
			.SetDisplay("Enable Take Profit", "Control take-profit exits automatically.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips.", "Risk")
			.SetCanOptimize(true);

		_enableStopLoss = Param(nameof(EnableStopLoss), true)
			.SetDisplay("Enable Stop Loss", "Control stop-loss exits automatically.", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips.", "Risk")
			.SetCanOptimize(true);

		_enableTrailingStop = Param(nameof(EnableTrailingStop), true)
			.SetDisplay("Enable Trailing Stop", "Activate trailing stop management.", "Risk");

		_activateTrailingAfterPips = Param(nameof(ActivateTrailingAfterPips), 0m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Trailing Activation (pips)", "Profit required before trailing starts.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Trailing Stop (pips)", "Trailing distance applied after activation.", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 1m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Trailing Step (pips)", "Extra profit needed before tightening the trailing stop.", "Risk");

		_enableBreakEven = Param(nameof(EnableBreakEven), false)
			.SetDisplay("Enable Break-Even", "Protect the trade once a specified profit is reached.", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 10m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Break-Even Trigger (pips)", "Profit distance required to activate break-even.", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 5m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Break-Even Offset (pips)", "Offset added to the entry price when moving the stop to break-even.", "Risk");

		_removeTakeProfit = Param(nameof(RemoveTakeProfit), false)
			.SetDisplay("Remove Take Profit", "Ignore any configured take-profit levels.", "Risk");

		_removeStopLoss = Param(nameof(RemoveStopLoss), false)
			.SetDisplay("Remove Stop Loss", "Ignore any configured stop-loss levels.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used to supervise open positions.", "Data");

		Volume = 1m;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_signedPosition = 0m;
		_longAveragePrice = null;
		_shortAveragePrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_longBreakEvenActive = false;
		_shortBreakEvenActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageLongSide(candle);
		ManageShortSide(candle);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null || order.Security != Security)
			return;

		var tradePrice = trade.Trade?.Price ?? order.Price;
		var delta = trade.Volume * (order.Side == Sides.Buy ? 1m : -1m);
		var previousPosition = _signedPosition;
		_signedPosition += delta;

		if (order.Side == Sides.Buy)
		{
			if (_signedPosition > 0m)
			{
				if (previousPosition >= 0m && ManageAsBasket)
				{
					_longAveragePrice = CalculateNewAverage(_longAveragePrice, Math.Max(previousPosition, 0m), tradePrice, trade.Volume);
				}
				else if (previousPosition <= 0m)
				{
					_longAveragePrice = tradePrice;
				}

				if (previousPosition <= 0m || ManageAsBasket)
				{
					InitializeLongProtection();
				}
			}
			else if (_signedPosition <= 0m)
			{
				_shortAveragePrice = previousPosition < 0m ? _shortAveragePrice : null;
			}
		}
		else
		{
			if (_signedPosition < 0m)
			{
				if (previousPosition <= 0m && ManageAsBasket)
				{
					_shortAveragePrice = CalculateNewAverage(_shortAveragePrice, Math.Max(-previousPosition, 0m), tradePrice, trade.Volume);
				}
				else if (previousPosition >= 0m)
				{
					_shortAveragePrice = tradePrice;
				}

				if (previousPosition >= 0m || ManageAsBasket)
				{
					InitializeShortProtection();
				}
			}
			else if (_signedPosition >= 0m)
			{
				_longAveragePrice = previousPosition > 0m ? _longAveragePrice : null;
			}
		}

		if (previousPosition > 0m && _signedPosition <= 0m)
		{
			ResetLongState();
		}

		if (previousPosition < 0m && _signedPosition >= 0m)
		{
			ResetShortState();
		}
	}

	private void ManageLongSide(ICandleMessage candle)
	{
		if (_signedPosition <= 0m)
			return;

		var volume = Position > 0m ? Position : _signedPosition;
		if (volume <= 0m)
			return;

		var entryPrice = _longAveragePrice ?? Position.AveragePrice ?? candle.ClosePrice;

		if (RemoveTakeProfit)
		{
			_longTakePrice = null;
		}
		else if (EnableTakeProfit && TakeProfitPips > 0m)
		{
			var desired = NormalizePrice(entryPrice + PointsToPrice(TakeProfitPips));
			if (!_longTakePrice.HasValue || ManageAsBasket)
				_longTakePrice = desired;
		}
		else
		{
			_longTakePrice = null;
		}

		if (RemoveStopLoss)
		{
			_longStopPrice = null;
			_longBreakEvenActive = false;
		}
		else
		{
			if (EnableStopLoss && StopLossPips > 0m && !_longBreakEvenActive && (!EnableTrailingStop || !_longStopPrice.HasValue || ManageAsBasket))
			{
				var desired = NormalizePrice(entryPrice - PointsToPrice(StopLossPips));
				if (!_longStopPrice.HasValue || ManageAsBasket)
					_longStopPrice = desired;
			}

			if (EnableBreakEven && BreakEvenTriggerPips > 0m && entryPrice > 0m)
			{
				var trigger = entryPrice + PointsToPrice(BreakEvenTriggerPips);
				if (!_longBreakEvenActive && candle.ClosePrice >= trigger)
				{
					var offset = PointsToPrice(BreakEvenOffsetPips);
					_longStopPrice = NormalizePrice(entryPrice + offset);
					_longBreakEvenActive = true;
				}
			}
			else if (!EnableBreakEven)
			{
				_longBreakEvenActive = false;
			}

			if (EnableTrailingStop && TrailingStopPips > 0m)
			{
				var activation = entryPrice + PointsToPrice(ActivateTrailingAfterPips);
				if (ActivateTrailingAfterPips <= 0m)
					activation = entryPrice;

				if (candle.ClosePrice >= activation)
				{
					var candidate = NormalizePrice(candle.ClosePrice - PointsToPrice(TrailingStopPips));
					if (!_longStopPrice.HasValue)
					{
						_longStopPrice = candidate;
					}
					else
					{
						var minImprovement = PointsToPrice(TrailingStepPips);
						if (candidate > _longStopPrice.Value + minImprovement)
							_longStopPrice = candidate;
					}
				}
			}
		}

		if (_longTakePrice is decimal takePrice && candle.HighPrice >= takePrice)
		{
			SellMarket(volume);
			return;
		}

		if (_longStopPrice is decimal stopPrice && candle.LowPrice <= stopPrice)
		{
			SellMarket(volume);
		}
	}

	private void ManageShortSide(ICandleMessage candle)
	{
		if (_signedPosition >= 0m)
			return;

		var volume = Position < 0m ? -Position : -_signedPosition;
		if (volume <= 0m)
			return;

		var entryPrice = _shortAveragePrice ?? Position.AveragePrice ?? candle.ClosePrice;

		if (RemoveTakeProfit)
		{
			_shortTakePrice = null;
		}
		else if (EnableTakeProfit && TakeProfitPips > 0m)
		{
			var desired = NormalizePrice(entryPrice - PointsToPrice(TakeProfitPips));
			if (!_shortTakePrice.HasValue || ManageAsBasket)
				_shortTakePrice = desired;
		}
		else
		{
			_shortTakePrice = null;
		}

		if (RemoveStopLoss)
		{
			_shortStopPrice = null;
			_shortBreakEvenActive = false;
		}
		else
		{
			if (EnableStopLoss && StopLossPips > 0m && !_shortBreakEvenActive && (!EnableTrailingStop || !_shortStopPrice.HasValue || ManageAsBasket))
			{
				var desired = NormalizePrice(entryPrice + PointsToPrice(StopLossPips));
				if (!_shortStopPrice.HasValue || ManageAsBasket)
					_shortStopPrice = desired;
			}

			if (EnableBreakEven && BreakEvenTriggerPips > 0m && entryPrice > 0m)
			{
				var trigger = entryPrice - PointsToPrice(BreakEvenTriggerPips);
				if (!_shortBreakEvenActive && candle.ClosePrice <= trigger)
				{
					var offset = PointsToPrice(BreakEvenOffsetPips);
					_shortStopPrice = NormalizePrice(entryPrice - offset);
					_shortBreakEvenActive = true;
				}
			}
			else if (!EnableBreakEven)
			{
				_shortBreakEvenActive = false;
			}

			if (EnableTrailingStop && TrailingStopPips > 0m)
			{
				var activation = entryPrice - PointsToPrice(ActivateTrailingAfterPips);
				if (ActivateTrailingAfterPips <= 0m)
					activation = entryPrice;

				if (candle.ClosePrice <= activation)
				{
					var candidate = NormalizePrice(candle.ClosePrice + PointsToPrice(TrailingStopPips));
					if (!_shortStopPrice.HasValue)
					{
						_shortStopPrice = candidate;
					}
					else
					{
						var minImprovement = PointsToPrice(TrailingStepPips);
						if (candidate < _shortStopPrice.Value - minImprovement)
							_shortStopPrice = candidate;
					}
				}
			}
		}

		if (_shortTakePrice is decimal takePrice && candle.LowPrice <= takePrice)
		{
			BuyMarket(volume);
			return;
		}

		if (_shortStopPrice is decimal stopPrice && candle.HighPrice >= stopPrice)
		{
			BuyMarket(volume);
		}
	}

	private void InitializeLongProtection()
	{
		if (_longAveragePrice is not decimal entry)
			return;

		_longBreakEvenActive = false;

		if (RemoveTakeProfit)
		{
			_longTakePrice = null;
		}
		else if (EnableTakeProfit && TakeProfitPips > 0m)
		{
			_longTakePrice = NormalizePrice(entry + PointsToPrice(TakeProfitPips));
		}
		else
		{
			_longTakePrice = null;
		}

		if (RemoveStopLoss)
		{
			_longStopPrice = null;
		}
		else if (EnableStopLoss && StopLossPips > 0m)
		{
			_longStopPrice = NormalizePrice(entry - PointsToPrice(StopLossPips));
		}
		else
		{
			_longStopPrice = null;
		}
	}

	private void InitializeShortProtection()
	{
		if (_shortAveragePrice is not decimal entry)
			return;

		_shortBreakEvenActive = false;

		if (RemoveTakeProfit)
		{
			_shortTakePrice = null;
		}
		else if (EnableTakeProfit && TakeProfitPips > 0m)
		{
			_shortTakePrice = NormalizePrice(entry - PointsToPrice(TakeProfitPips));
		}
		else
		{
			_shortTakePrice = null;
		}

		if (RemoveStopLoss)
		{
			_shortStopPrice = null;
		}
		else if (EnableStopLoss && StopLossPips > 0m)
		{
			_shortStopPrice = NormalizePrice(entry + PointsToPrice(StopLossPips));
		}
		else
		{
			_shortStopPrice = null;
		}
	}

	private void ResetLongState()
	{
		_longAveragePrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_longBreakEvenActive = false;
	}

	private void ResetShortState()
	{
		_shortAveragePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_shortBreakEvenActive = false;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}

	private decimal PointsToPrice(decimal points)
	{
		if (points <= 0m)
			return 0m;

		return points * (_pipSize > 0m ? _pipSize : 1m);
	}

	private decimal NormalizePrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private static decimal CalculateNewAverage(decimal? currentAverage, decimal currentVolume, decimal tradePrice, decimal tradeVolume)
	{
		if (tradeVolume <= 0m)
			return currentAverage ?? tradePrice;

		if (currentVolume <= 0m || currentAverage is null)
			return tradePrice;

		var totalVolume = currentVolume + tradeVolume;
		if (totalVolume <= 0m)
			return tradePrice;

		return ((currentAverage.Value * currentVolume) + (tradePrice * tradeVolume)) / totalVolume;
	}
}
