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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scheduled breakout strategy that monitors price around a reference level and enters on breakout.
/// Converted from the original pending-order version to use market orders.
/// </summary>
public class ENewsLuckyStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<int> _placementHour;
	private readonly StrategyParam<int> _cancelHour;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal? _buyLevel;
	private decimal? _sellLevel;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private bool _pendingActive;
	private bool _lastWasPlacementDay;

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	public int PlacementHour
	{
		get => _placementHour.Value;
		set => _placementHour.Value = value;
	}

	public int CancelHour
	{
		get => _cancelHour.Value;
		set => _cancelHour.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ENewsLuckyStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss", "Stop loss in pips", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
			.SetDisplay("Take Profit", "Take profit in pips", "Trading");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetDisplay("Trailing Stop", "Trailing distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step", "Minimum trailing step in pips", "Risk");

		_distancePips = Param(nameof(DistancePips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Distance", "Distance from market in pips", "Trading");

		_placementHour = Param(nameof(PlacementHour), 2)
			.SetDisplay("Placement Hour", "Hour to set breakout levels", "General");

		_cancelHour = Param(nameof(CancelHour), 22)
			.SetDisplay("Cancel Hour", "Hour to cancel and close", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_buyLevel = null;
		_sellLevel = null;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_pendingActive = false;
		_lastWasPlacementDay = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var step = Security?.PriceStep ?? 0m;
		_pipSize = step > 0 ? step : 1m;

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.CloseTime.Hour;
		var price = candle.ClosePrice;

		// Set breakout levels at placement hour
		if (hour == PlacementHour && !_lastWasPlacementDay && Position == 0)
		{
			var distance = DistancePips * _pipSize;
			_buyLevel = price + distance;
			_sellLevel = price - distance;
			_pendingActive = true;
			_lastWasPlacementDay = true;
		}

		if (hour != PlacementHour)
			_lastWasPlacementDay = false;

		// Cancel at cancel hour
		if (hour == CancelHour && _pendingActive)
		{
			_pendingActive = false;
			_buyLevel = null;
			_sellLevel = null;

			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			_entryPrice = 0m;
			_stopPrice = null;
			_takePrice = null;
			return;
		}

		// Check breakout triggers
		if (_pendingActive && Position == 0)
		{
			if (_buyLevel.HasValue && candle.HighPrice >= _buyLevel.Value)
			{
				BuyMarket(Volume);
				_entryPrice = _buyLevel.Value;
				_stopPrice = StopLossPips > 0 ? _entryPrice - StopLossPips * _pipSize : null;
				_takePrice = TakeProfitPips > 0 ? _entryPrice + TakeProfitPips * _pipSize : null;
				_pendingActive = false;
				_buyLevel = null;
				_sellLevel = null;
			}
			else if (_sellLevel.HasValue && candle.LowPrice <= _sellLevel.Value)
			{
				SellMarket(Volume);
				_entryPrice = _sellLevel.Value;
				_stopPrice = StopLossPips > 0 ? _entryPrice + StopLossPips * _pipSize : null;
				_takePrice = TakeProfitPips > 0 ? _entryPrice - TakeProfitPips * _pipSize : null;
				_pendingActive = false;
				_buyLevel = null;
				_sellLevel = null;
			}
		}

		// Manage open position
		if (Position > 0)
		{
			// Trailing stop
			if (TrailingStopPips > 0 && _entryPrice > 0)
			{
				var trailDist = TrailingStopPips * _pipSize;
				var stepDist = TrailingStepPips * _pipSize;
				if (price - _entryPrice > trailDist + stepDist)
				{
					var newStop = price - trailDist;
					if (!_stopPrice.HasValue || newStop > _stopPrice.Value)
						_stopPrice = newStop;
				}
			}

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Position);
				ResetPosition();
				return;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Position);
				ResetPosition();
			}
		}
		else if (Position < 0)
		{
			// Trailing stop
			if (TrailingStopPips > 0 && _entryPrice > 0)
			{
				var trailDist = TrailingStopPips * _pipSize;
				var stepDist = TrailingStepPips * _pipSize;
				if (_entryPrice - price > trailDist + stepDist)
				{
					var newStop = price + trailDist;
					if (!_stopPrice.HasValue || newStop < _stopPrice.Value)
						_stopPrice = newStop;
				}
			}

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(-Position);
				ResetPosition();
				return;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(-Position);
				ResetPosition();
			}
		}
	}

	private void ResetPosition()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}
}
