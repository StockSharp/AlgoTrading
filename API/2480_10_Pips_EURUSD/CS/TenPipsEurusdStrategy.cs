namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Breakout strategy based on pending stop orders around the previous candle range.
/// </summary>
public class TenPipsEurusdStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousHigh;
	private decimal _previousLow;
	private DateTimeOffset _currentBarOpenTime;
	private bool _ordersEvaluated;
	private decimal _pipSize;
	private decimal _currentBid;
	private decimal _currentAsk;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private bool _buyOrderActive;
	private bool _sellOrderActive;

	private decimal _longStopPrice;
	private decimal _longTakePrice;
	private bool _longTrailActive;
	private decimal _longTrailingStop;

	private decimal _shortStopPrice;
	private decimal _shortTakePrice;
	private bool _shortTrailActive;
	private decimal _shortTrailingStop;

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
	/// Enable trailing stop management.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Initial trailing distance in pips.
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
	/// Candle type used for the breakout detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TenPipsEurusdStrategy()
	{
		_volume = Param(nameof(Volume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_useTrailing = Param(nameof(UseTrailing), false)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing activation distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 25m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step (pips)", "Step for updating trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for calculations", "General");
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
		ResetInternalState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Calculate pip size based on the current security settings.
		_pipSize = CalculatePipSize();
		if (_pipSize <= 0m)
		{
			var step = Security?.PriceStep ?? 0m;
			_pipSize = step > 0m ? step : 0.0001m;
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		SubscribeOrderBook()
			.Bind(depth =>
			{
				var bestBid = depth.GetBestBid();
				if (bestBid != null)
					_currentBid = bestBid.Price;

				var bestAsk = depth.GetBestAsk();
				if (bestAsk != null)
					_currentAsk = bestAsk.Price;

				// Check trailing or protective exits on each quote update.
				ManageActivePosition();
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
		{
			_buyOrderActive = false;
			_buyStopOrder = null;
			InitializeLongPosition();
		}
		else if (Position < 0m)
		{
			_sellOrderActive = false;
			_sellStopOrder = null;
			InitializeShortPosition();
		}
		else
		{
			ResetPositionState();
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State == CandleStates.Finished)
		{
			// Store the last finished candle for the breakout levels.
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			_currentBarOpenTime = default;
			_ordersEvaluated = false;

			_currentBid = candle.ClosePrice;
			_currentAsk = candle.ClosePrice;
			ManageActivePosition();

			// Pending orders are only valid for a single bar.
			CancelPendingOrders();
			return;
		}

		if (candle.State != CandleStates.Active)
			return;

		_currentBid = candle.ClosePrice;
		_currentAsk = candle.ClosePrice;
		ManageActivePosition();

		if (_previousHigh <= 0m || _previousLow <= 0m)
			return;

		if (_currentBarOpenTime != candle.OpenTime)
		{
			_currentBarOpenTime = candle.OpenTime;
			_ordersEvaluated = false;
		}

		if (_ordersEvaluated)
			return;

		// Skip if there are active pending orders from the same bar.
		if (_buyOrderActive || _sellOrderActive)
		{
			_ordersEvaluated = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_ordersEvaluated = true;
			return;
		}

		var stopLevel = GetStopLevel();
		if (stopLevel <= 0m)
		{
			_ordersEvaluated = true;
			return;
		}

		var currentClose = candle.ClosePrice;
		var currentOpen = candle.OpenPrice;

		var highDistance = _previousHigh - currentClose;
		var lowDistance = currentClose - _previousLow;

		if (highDistance >= stopLevel && lowDistance >= stopLevel)
		{
			var spread = GetSpread();

			if (currentOpen < _previousHigh)
			{
				var activation = _previousHigh + 2m * spread;
				if (activation > 0m)
				{
					var order = BuyStop(Volume, activation);
					if (order != null)
					{
						_buyStopOrder = order;
						_buyOrderActive = true;
					}
				}
			}

			if (currentOpen > _previousLow)
			{
				var activation = _previousLow - spread;
				var order = SellStop(Volume, activation);
				if (order != null)
				{
					_sellStopOrder = order;
					_sellOrderActive = true;
				}
			}
		}

		_ordersEvaluated = true;
	}

	private void ManageActivePosition()
	{
		if (Position > 0m)
		{
			var bid = _currentBid;
			if (bid <= 0m)
				return;

			var stopPrice = _longTrailActive ? _longTrailingStop : _longStopPrice;
			if (!_longTrailActive)
			{
				if (UseTrailing)
				{
					TryActivateLongTrailing(bid);
					stopPrice = _longTrailActive ? _longTrailingStop : _longStopPrice;
				}
			}
			else
			{
				TryUpdateLongTrailing(bid);
				stopPrice = _longTrailingStop;
			}

			if (stopPrice > 0m && bid <= stopPrice)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
					SellMarket(volume);

				ResetPositionState();
				return;
			}

			if (!UseTrailing && _longTakePrice > 0m && bid >= _longTakePrice)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
					SellMarket(volume);

				ResetPositionState();
			}
		}
		else if (Position < 0m)
		{
			var ask = _currentAsk;
			if (ask <= 0m)
				return;

			var stopPrice = _shortTrailActive ? _shortTrailingStop : _shortStopPrice;
			if (!_shortTrailActive)
			{
				if (UseTrailing)
				{
					TryActivateShortTrailing(ask);
					stopPrice = _shortTrailActive ? _shortTrailingStop : _shortStopPrice;
				}
			}
			else
			{
				TryUpdateShortTrailing(ask);
				stopPrice = _shortTrailingStop;
			}

			if (stopPrice > 0m && ask >= stopPrice)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
					BuyMarket(volume);

				ResetPositionState();
				return;
			}

			if (!UseTrailing && _shortTakePrice > 0m && ask <= _shortTakePrice)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
					BuyMarket(volume);

				ResetPositionState();
			}
		}
	}

	private void TryActivateLongTrailing(decimal bid)
	{
		var trailingDistance = TrailingStopPips * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var move = bid - PositionPrice;
		if (move <= trailingDistance)
			return;

		var newStop = bid - trailingDistance;
		if (newStop <= 0m)
			return;

		_longTrailActive = true;
		_longTrailingStop = newStop;
	}

	private void TryUpdateLongTrailing(decimal bid)
	{
		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips * _pipSize;
		if (trailingDistance <= 0m || stepDistance <= 0m)
			return;

		var newStop = bid - trailingDistance;
		if (newStop <= _longTrailingStop)
			return;

		if (newStop - _longTrailingStop >= stepDistance)
			_longTrailingStop = newStop;
	}

	private void TryActivateShortTrailing(decimal ask)
	{
		var trailingDistance = TrailingStopPips * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var move = PositionPrice - ask;
		if (move <= trailingDistance)
			return;

		var newStop = ask + trailingDistance;
		if (newStop <= 0m)
			return;

		_shortTrailActive = true;
		_shortTrailingStop = newStop;
	}

	private void TryUpdateShortTrailing(decimal ask)
	{
		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips * _pipSize;
		if (trailingDistance <= 0m || stepDistance <= 0m)
			return;

		var newStop = ask + trailingDistance;
		if (newStop >= _shortTrailingStop)
			return;

		if (_shortTrailingStop - newStop >= stepDistance)
			_shortTrailingStop = newStop;
	}

	private void InitializeLongPosition()
	{
		var entryPrice = PositionPrice;
		var stopDistance = StopLossPips * _pipSize;
		_longStopPrice = stopDistance > 0m ? entryPrice - stopDistance : 0m;
		_longTakePrice = UseTrailing ? 0m : entryPrice + TakeProfitPips * _pipSize;
		_longTrailActive = false;
		_longTrailingStop = _longStopPrice;
	}

	private void InitializeShortPosition()
	{
		var entryPrice = PositionPrice;
		var stopDistance = StopLossPips * _pipSize;
		_shortStopPrice = stopDistance > 0m ? entryPrice + stopDistance : 0m;
		_shortTakePrice = UseTrailing ? 0m : entryPrice - TakeProfitPips * _pipSize;
		_shortTrailActive = false;
		_shortTrailingStop = _shortStopPrice;
	}

	private void ResetPositionState()
	{
		_longStopPrice = 0m;
		_longTakePrice = 0m;
		_longTrailActive = false;
		_longTrailingStop = 0m;

		_shortStopPrice = 0m;
		_shortTakePrice = 0m;
		_shortTrailActive = false;
		_shortTrailingStop = 0m;
	}

	private void CancelPendingOrders()
	{
		if (_buyStopOrder != null)
		{
			CancelOrder(_buyStopOrder);
			_buyStopOrder = null;
			_buyOrderActive = false;
		}

		if (_sellStopOrder != null)
		{
			CancelOrder(_sellStopOrder);
			_sellStopOrder = null;
			_sellOrderActive = false;
		}
	}

	private void ResetInternalState()
	{
		_previousHigh = 0m;
		_previousLow = 0m;
		_currentBarOpenTime = default;
		_ordersEvaluated = false;
		_currentBid = 0m;
		_currentAsk = 0m;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_buyOrderActive = false;
		_sellOrderActive = false;
		ResetPositionState();
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var digits = 0;
		var value = step;
		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		var multiplier = (digits == 3 || digits == 5) ? 10m : 1m;
		return step * multiplier;
	}

	private decimal GetStopLevel()
	{
		var baseLevel = _pipSize;
		if (baseLevel <= 0m)
			baseLevel = Security?.PriceStep ?? 0m;

		return baseLevel * 3m;
	}

	private decimal GetSpread()
	{
		var spread = _currentAsk - _currentBid;
		if (spread > 0m)
			return spread;

		var fallback = _pipSize;
		if (fallback <= 0m)
			fallback = Security?.PriceStep ?? 0.0001m;

		return fallback;
	}
}
