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
/// Explosion breakout strategy ported from MetaTrader 5.
/// Detects wide range candles compared to the previous one and enters in the direction of the candle body.
/// Includes optional stop-loss, take-profit, trailing stop, trading hours filter, pause between trades, and daily trade limit.
/// </summary>
public class ExplosionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _ratio;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _pauseSeconds;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _oneTradePerDay;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;

	private decimal _priceStep;
	private decimal? _previousRange;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTake;
	private decimal? _shortTake;
	private decimal _lastKnownPosition;
	private DateTimeOffset? _lastEntryTime;
	private DateTime? _lastEntryDate;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Range expansion ratio comparing current candle to the previous one.
	/// </summary>
	public decimal Ratio
	{
		get => _ratio.Value;
		set => _ratio.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous positions expressed in lots.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Pause between trades in seconds.
	/// </summary>
	public int PauseSeconds
	{
		get => _pauseSeconds.Value;
		set => _pauseSeconds.Value = value;
	}

	/// <summary>
	/// Hour (0-23) when trading is allowed to start.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour (0-23) when trading is no longer allowed.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Allow only one entry per day.
	/// </summary>
	public bool OneTradePerDay
	{
		get => _oneTradePerDay.Value;
		set => _oneTradePerDay.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum additional price movement required before the trailing stop is moved.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExplosionStrategy"/> class.
	/// </summary>
	public ExplosionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for calculations", "General")
			.SetCanOptimize(true);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Trade volume in lots", "General")
			.SetCanOptimize(true);

		_ratio = Param(nameof(Ratio), 1.6m)
			.SetGreaterThanZero()
			.SetDisplay("Range Ratio", "Current candle range must exceed previous range multiplied by this value", "Signals")
			.SetCanOptimize(true);

		_maxPositions = Param(nameof(MaxPositions), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of simultaneous positions", "Risk Management")
			.SetCanOptimize(true);

		_pauseSeconds = Param(nameof(PauseSeconds), 36000)
			.SetNotNegative()
			.SetDisplay("Pause (sec)", "Minimum time in seconds between entries", "Risk Management")
			.SetCanOptimize(true);

		_startHour = Param(nameof(StartHour), 1)
			.SetNotNegative()
			.SetDisplay("Start Hour", "Hour when trading becomes allowed", "Schedule")
			.SetCanOptimize(true);

		_endHour = Param(nameof(EndHour), 23)
			.SetNotNegative()
			.SetDisplay("End Hour", "Hour when trading is no longer allowed", "Schedule")
			.SetCanOptimize(true);

		_oneTradePerDay = Param(nameof(OneTradePerDay), true)
			.SetDisplay("One Trade Per Day", "Allow only one entry per calendar day", "Risk Management")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance expressed in price steps", "Protection")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance expressed in price steps", "Protection")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop", "Trailing stop distance in price steps", "Protection")
			.SetCanOptimize(true);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 1m)
			.SetNotNegative()
			.SetDisplay("Trailing Step", "Extra price movement required before trailing updates", "Protection")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (StartHour >= EndHour)
			throw new InvalidOperationException("Trading window is invalid: start hour must be less than end hour.");

		if (TrailingStopPoints > 0m && TrailingStepPoints <= 0m)
			throw new InvalidOperationException("Trailing stop requires a positive trailing step.");

		Volume = OrderVolume;

		_priceStep = GetPriceStep();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousRange = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStop = null;
		_shortStop = null;
		_longTake = null;
		_shortTake = null;
		_lastKnownPosition = 0m;
		_lastEntryTime = null;
		_lastEntryDate = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			ResetPositionTracking();
			_lastKnownPosition = 0m;
			return;
		}

		if (_lastKnownPosition == 0m)
		{
			var now = CurrentTime ?? DateTimeOffset.Now;
			_lastEntryTime = now;
			_lastEntryDate = now.Date;
		}

		var point = GetPointValue();

		if (Position > 0)
		{
			_longEntryPrice = Position.AveragePrice;
			_longStop = StopLossPoints > 0m ? _longEntryPrice - StopLossPoints * point : null;
			_longTake = TakeProfitPoints > 0m ? _longEntryPrice + TakeProfitPoints * point : null;
			_shortEntryPrice = null;
			_shortStop = null;
			_shortTake = null;
		}
		else
		{
			_shortEntryPrice = Position.AveragePrice;
			_shortStop = StopLossPoints > 0m ? _shortEntryPrice + StopLossPoints * point : null;
			_shortTake = TakeProfitPoints > 0m ? _shortEntryPrice - TakeProfitPoints * point : null;
			_longEntryPrice = null;
			_longStop = null;
			_longTake = null;
		}

		_lastKnownPosition = Position;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (HandleActivePosition(candle))
		{
			_previousRange = candle.HighPrice - candle.LowPrice;
			return;
		}

		if (!CanOpenNewTrade(candle))
		{
			_previousRange = candle.HighPrice - candle.LowPrice;
			return;
		}


		var currentRange = candle.HighPrice - candle.LowPrice;

		if (_previousRange is not decimal previousRange || previousRange <= 0m)
		{
			_previousRange = currentRange;
			return;
		}

		var hasRangeExpansion = currentRange > previousRange * Ratio;

		if (!hasRangeExpansion)
		{
			_previousRange = currentRange;
			return;
		}

		var volume = OrderVolume;

		if (candle.ClosePrice > candle.OpenPrice && Position <= 0 && CanOpenLong())
		{
			BuyMarket(volume);
		}
		else if (candle.ClosePrice < candle.OpenPrice && Position >= 0 && CanOpenShort())
		{
			SellMarket(volume);
		}

		_previousRange = currentRange;
	}

	private bool HandleActivePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			UpdateLongTrailing(candle);

			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Position);
				return true;
			}

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Position);
				return true;
			}
		}
		else if (Position < 0)
		{
			UpdateShortTrailing(candle);

			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}
		}

		return false;
	}

	private bool CanOpenNewTrade(ICandleMessage candle)
	{
		var hour = candle.CloseTime.Hour;

		if (hour < StartHour || hour > EndHour)
			return false;

		if (OneTradePerDay && _lastEntryDate.HasValue && _lastEntryDate.Value == candle.CloseTime.Date)
			return false;

		if (_lastEntryTime.HasValue)
		{
			var elapsed = candle.CloseTime - _lastEntryTime.Value;
			if (elapsed.TotalSeconds < PauseSeconds)
				return false;
		}

		return true;
	}

	private bool CanOpenLong()
	{
		if (OrderVolume <= 0m)
			return false;

		if (MaxPositions <= 0)
			return true;

		var projectedPosition = Position + OrderVolume;
		var limit = MaxPositions * OrderVolume;

		return Math.Abs(projectedPosition) <= limit + 1e-9m;
	}

	private bool CanOpenShort()
	{
		if (OrderVolume <= 0m)
			return false;

		if (MaxPositions <= 0)
			return true;

		var projectedPosition = Position - OrderVolume;
		var limit = MaxPositions * OrderVolume;

		return Math.Abs(projectedPosition) <= limit + 1e-9m;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_longEntryPrice is not decimal entry)
			return;

		if (TrailingStopPoints <= 0m)
			return;

		var point = GetPointValue();
		var trailingDistance = TrailingStopPoints * point;
		var stepDistance = TrailingStepPoints > 0m ? TrailingStepPoints * point : 0m;
		var threshold = trailingDistance + stepDistance;

		var current = candle.ClosePrice;
		var profit = current - entry;

		if (profit <= threshold)
			return;

		var minAllowedStop = current - threshold;
		var desiredStop = current - trailingDistance;

		if (!_longStop.HasValue || _longStop.Value < minAllowedStop)
			_longStop = desiredStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_shortEntryPrice is not decimal entry)
			return;

		if (TrailingStopPoints <= 0m)
			return;

		var point = GetPointValue();
		var trailingDistance = TrailingStopPoints * point;
		var stepDistance = TrailingStepPoints > 0m ? TrailingStepPoints * point : 0m;
		var threshold = trailingDistance + stepDistance;

		var current = candle.ClosePrice;
		var profit = entry - current;

		if (profit <= threshold)
			return;

		var maxAllowedStop = current + threshold;
		var desiredStop = current + trailingDistance;

		if (!_shortStop.HasValue || _shortStop.Value > maxAllowedStop)
			_shortStop = desiredStop;
	}

	private decimal GetPriceStep()
	{
		if (Security?.PriceStep is decimal step && step > 0m)
			return step;

		if (Security?.Decimals is int decimals && decimals > 0)
			return (decimal)Math.Pow(10, -decimals);

		return 0.0001m;
	}

	private decimal GetPointValue()
	{
		return _priceStep > 0m ? _priceStep : GetPriceStep();
	}

	private void ResetPositionTracking()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStop = null;
		_shortStop = null;
		_longTake = null;
		_shortTake = null;
	}
}


