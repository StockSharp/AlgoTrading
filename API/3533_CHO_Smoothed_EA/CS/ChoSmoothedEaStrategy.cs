
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
/// Trades Chaikin Oscillator crossovers with optional zero level and time filters.
/// </summary>
public class ChoSmoothedEaStrategy : Strategy
{
	public enum MovingAverageTypes
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<MovingAverageTypes> _maType;
	private readonly StrategyParam<bool> _useZeroLevel;
	private readonly StrategyParam<ChoTradeModes> _tradeMode;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _useTimeControl;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;

	private decimal _previousCho;
	private decimal _previousSignal;
	private bool _isInitialized;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private decimal _trailingReferencePrice;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast period of the Chaikin oscillator.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow period of the Chaikin oscillator.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Period of the smoothing moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type for smoothing the Chaikin oscillator.
	/// </summary>
	public MovingAverageTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Use the zero level filter for trade entries.
	/// </summary>
	public bool UseZeroLevel
	{
		get => _useZeroLevel.Value;
		set => _useZeroLevel.Value = value;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public ChoTradeModes TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Reverse signals to trade the opposite direction.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Close opposite positions before opening a new one.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Allow only a single open position at a time.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Enable trading only inside the specified time window.
	/// </summary>
	public bool UseTimeControl
	{
		get => _useTimeControl.Value;
		set => _useTimeControl.Value = value;
	}

	/// <summary>
	/// Start time of the trading window (exchange time zone).
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End time of the trading window (exchange time zone).
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum advance in points before updating the trailing stop.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChoSmoothedEaStrategy"/> class.
	/// </summary>
	public ChoSmoothedEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signal calculations", "General");

		_fastPeriod = Param(nameof(FastPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast period for Chaikin oscillator", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow period for Chaikin oscillator", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_maPeriod = Param(nameof(MaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Signal MA Period", "Period of smoothing moving average", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_maType = Param(nameof(MaType), MovingAverageTypes.Simple)
			.SetDisplay("Signal MA Type", "Moving average type for smoothing", "Indicator");

		_useZeroLevel = Param(nameof(UseZeroLevel), true)
			.SetDisplay("Use Zero Level", "Require oscillator to be below/above zero", "Filters");

		_tradeMode = Param(nameof(TradeMode), ChoTradeModes.BuyAndSell)
			.SetDisplay("Trade Mode", "Allowed direction of trades", "Risk");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert long and short entries", "Filters");

		_closeOpposite = Param(nameof(CloseOpposite), false)
			.SetDisplay("Close Opposite", "Close opposite positions before entry", "Risk");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), false)
			.SetDisplay("Only One Position", "Allow only a single open position", "Risk");

		_useTimeControl = Param(nameof(UseTimeControl), true)
			.SetDisplay("Use Time Control", "Restrict trading hours", "Session");

		_startTime = Param(nameof(StartTime), new TimeSpan(10, 1, 0))
			.SetDisplay("Start Time", "Session start time", "Session");

		_endTime = Param(nameof(EndTime), new TimeSpan(15, 2, 0))
			.SetDisplay("End Time", "Session end time", "Session");

		_stopLossPoints = Param(nameof(StopLossPoints), 150m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pts)", "Stop-loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 460m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pts)", "Take-profit distance in points", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 250m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pts)", "Trailing stop distance in points", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pts)", "Minimum move to update trailing stop", "Risk");
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
		_previousCho = 0m;
		_previousSignal = 0m;
		_isInitialized = false;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_trailingReferencePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var cho = new ChaikinOscillator
		{
			ShortPeriod = FastPeriod,
			LongPeriod = SlowPeriod
		};

		var signalMa = new MovingAverage
		{
			Length = MaPeriod,
			Type = MaType
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cho, signalMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal choValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateTrailingStops(candle);
		CheckRiskExits(candle);

		if (!_isInitialized)
		{
			_previousCho = choValue;
			_previousSignal = signalValue;
			_isInitialized = true;
			return;
		}

		if (UseTimeControl && !IsWithinTradingWindow(candle.Time))
		{
			_previousCho = choValue;
			_previousSignal = signalValue;
			return;
		}

		var crossUp = _previousCho <= _previousSignal && choValue > signalValue;
		var crossDown = _previousCho >= _previousSignal && choValue < signalValue;

		var zeroFilterBuy = !UseZeroLevel || (choValue < 0m && signalValue < 0m);
		var zeroFilterSell = !UseZeroLevel || (choValue > 0m && signalValue > 0m);

		var buySignal = ReverseSignals ? crossDown : crossUp;
		var sellSignal = ReverseSignals ? crossUp : crossDown;

		buySignal &= zeroFilterBuy;
		sellSignal &= zeroFilterSell;

		if (OnlyOnePosition && Position != 0)
		{
			_previousCho = choValue;
			_previousSignal = signalValue;
			return;
		}

		if (buySignal && TradeMode != ChoTradeModes.SellOnly)
		{
			if (Position < 0)
			{
				if (CloseOpposite)
				{
					BuyMarket(Math.Abs(Position));
					_previousCho = choValue;
					_previousSignal = signalValue;
					return;
				}
				else
				{
					buySignal = false;
				}
			}

			if (buySignal && Position <= 0)
			{
				CancelActiveOrders();
				BuyMarket(Volume + Math.Abs(Position));
				SetupRiskLevels(candle.ClosePrice, true);
			}
		}
		else if (sellSignal && TradeMode != ChoTradeModes.BuyOnly)
		{
			if (Position > 0)
			{
				if (CloseOpposite)
				{
					SellMarket(Math.Abs(Position));
					_previousCho = choValue;
					_previousSignal = signalValue;
					return;
				}
				else
				{
					sellSignal = false;
				}
			}

			if (sellSignal && Position >= 0)
			{
				CancelActiveOrders();
				SellMarket(Volume + Math.Abs(Position));
				SetupRiskLevels(candle.ClosePrice, false);
			}
		}

		_previousCho = choValue;
		_previousSignal = signalValue;
	}

	private void SetupRiskLevels(decimal entryPrice, bool isLong)
	{
		var step = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints * step;
		var takeDistance = TakeProfitPoints * step;
		var trailingDistance = TrailingStopPoints * step;

		if (isLong)
		{
			_stopPrice = StopLossPoints > 0m ? entryPrice - stopDistance : 0m;
			_takeProfitPrice = TakeProfitPoints > 0m ? entryPrice + takeDistance : 0m;
			_trailingReferencePrice = TrailingStopPoints > 0m ? entryPrice : 0m;
		}
		else
		{
			_stopPrice = StopLossPoints > 0m ? entryPrice + stopDistance : 0m;
			_takeProfitPrice = TakeProfitPoints > 0m ? entryPrice - takeDistance : 0m;
			_trailingReferencePrice = TrailingStopPoints > 0m ? entryPrice : 0m;
		}
	}

	private void CheckRiskExits(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopPrice > 0m && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				ResetRiskLevels();
				return;
			}

			if (_takeProfitPrice > 0m && candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket(Position);
				ResetRiskLevels();
				return;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice > 0m && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetRiskLevels();
				return;
			}

			if (_takeProfitPrice > 0m && candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetRiskLevels();
				return;
			}
		}
		else if (Position == 0)
		{
			ResetRiskLevels();
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m || Position == 0)
			return;

		var step = Security?.PriceStep ?? 1m;
		var trailingDistance = TrailingStopPoints * step;
		var trailingStep = TrailingStepPoints * step;

		if (Position > 0)
		{
			if (_trailingReferencePrice == 0m)
				_trailingReferencePrice = candle.ClosePrice;

			if (candle.ClosePrice - _trailingReferencePrice >= trailingStep)
			{
				_trailingReferencePrice = candle.ClosePrice;
				var newStop = candle.ClosePrice - trailingDistance;
				if (newStop > _stopPrice)
					_stopPrice = newStop;
			}
		}
		else if (Position < 0)
		{
			if (_trailingReferencePrice == 0m)
				_trailingReferencePrice = candle.ClosePrice;

			if (_trailingReferencePrice - candle.ClosePrice >= trailingStep)
			{
				_trailingReferencePrice = candle.ClosePrice;
				var newStop = candle.ClosePrice + trailingDistance;
				if (_stopPrice == 0m || newStop < _stopPrice)
					_stopPrice = newStop;
			}
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var localTime = time.TimeOfDay;
		if (StartTime == EndTime)
			return false;

		if (StartTime < EndTime)
			return localTime >= StartTime && localTime < EndTime;

		return localTime >= StartTime || localTime < EndTime;
	}

	private void ResetRiskLevels()
	{
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_trailingReferencePrice = 0m;
	}

	/// <summary>
	/// Trade mode selection.
	/// </summary>
	public enum ChoTradeModes
	{
		/// <summary>
		/// Allow both long and short trades.
		/// </summary>
		BuyAndSell,

		/// <summary>
		/// Allow only long trades.
		/// </summary>
		BuyOnly,

		/// <summary>
		/// Allow only short trades.
		/// </summary>
		SellOnly
	}
}

