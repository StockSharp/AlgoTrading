using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double moving average breakout strategy converted from MetaTrader DoubleMA_Breakout EA.
/// Places stop orders after a fast/slow moving average crossover and manages entries with trading windows.
/// Pending orders are cancelled and open positions are closed when the crossover flips.
/// </summary>
public class DoubleMaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<MovingAverageMode> _fastMaMode;
	private readonly StrategyParam<MovingAverageMode> _slowMaMode;
	private readonly StrategyParam<AppliedPriceMode> _fastAppliedPrice;
	private readonly StrategyParam<AppliedPriceMode> _slowAppliedPrice;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<decimal> _breakoutDistancePoints;
	private readonly StrategyParam<bool> _useTimeWindow;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<bool> _useFridayCloseAll;
	private readonly StrategyParam<TimeSpan> _fridayCloseTime;
	private readonly StrategyParam<bool> _useFridayStopTrading;
	private readonly StrategyParam<TimeSpan> _fridayStopTradingTime;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal>? _fastMa;
	private LengthIndicator<decimal>? _slowMa;
	private LinearRegression? _fastLsma;
	private LinearRegression? _slowLsma;
	private readonly Queue<decimal> _fastHistory = new();
	private readonly Queue<decimal> _slowHistory = new();
	private decimal _priceStep;
	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private bool _fridayTradingDisabled;

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Fast moving average calculation mode.
	/// </summary>
	public MovingAverageMode FastMaMode
	{
		get => _fastMaMode.Value;
		set => _fastMaMode.Value = value;
	}

	/// <summary>
	/// Slow moving average calculation mode.
	/// </summary>
	public MovingAverageMode SlowMaMode
	{
		get => _slowMaMode.Value;
		set => _slowMaMode.Value = value;
	}

	/// <summary>
	/// Price source for the fast moving average.
	/// </summary>
	public AppliedPriceMode FastAppliedPrice
	{
		get => _fastAppliedPrice.Value;
		set => _fastAppliedPrice.Value = value;
	}

	/// <summary>
	/// Price source for the slow moving average.
	/// </summary>
	public AppliedPriceMode SlowAppliedPrice
	{
		get => _slowAppliedPrice.Value;
		set => _slowAppliedPrice.Value = value;
	}

	/// <summary>
	/// Number of completed candles used for signal evaluation (0 = current).
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	/// <summary>
	/// Breakout distance in price steps for pending stop orders.
	/// </summary>
	public decimal BreakoutDistancePoints
	{
		get => _breakoutDistancePoints.Value;
		set => _breakoutDistancePoints.Value = value;
	}

	/// <summary>
	/// Enables trading only inside the configured time window.
	/// </summary>
	public bool UseTimeWindow
	{
		get => _useTimeWindow.Value;
		set => _useTimeWindow.Value = value;
	}

	/// <summary>
	/// Hour of day (inclusive) when new trades can start.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour of day (inclusive) when trading stops.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	/// <summary>
	/// Enables automatic position flattening late on Friday.
	/// </summary>
	public bool UseFridayCloseAll
	{
		get => _useFridayCloseAll.Value;
		set => _useFridayCloseAll.Value = value;
	}

	/// <summary>
	/// Time of day on Friday when all activity is stopped.
	/// </summary>
	public TimeSpan FridayCloseTime
	{
		get => _fridayCloseTime.Value;
		set => _fridayCloseTime.Value = value;
	}

	/// <summary>
	/// Enables halting new trades before the Friday close.
	/// </summary>
	public bool UseFridayStopTrading
	{
		get => _useFridayStopTrading.Value;
		set => _useFridayStopTrading.Value = value;
	}

	/// <summary>
	/// Time of day on Friday when new entries are blocked.
	/// </summary>
	public TimeSpan FridayStopTradingTime
	{
		get => _fridayStopTradingTime.Value;
		set => _fridayStopTradingTime.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DoubleMaBreakoutStrategy"/> class.
	/// </summary>
	public DoubleMaBreakoutStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Period of the fast moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(2, 30, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Period of the slow moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_fastMaMode = Param(nameof(FastMaMode), MovingAverageMode.Simple)
			.SetDisplay("Fast MA Mode", "Type of the fast moving average", "Moving Averages");

		_slowMaMode = Param(nameof(SlowMaMode), MovingAverageMode.Simple)
			.SetDisplay("Slow MA Mode", "Type of the slow moving average", "Moving Averages");

		_fastAppliedPrice = Param(nameof(FastAppliedPrice), AppliedPriceMode.Close)
			.SetDisplay("Fast Price", "Applied price for the fast MA", "Moving Averages");

		_slowAppliedPrice = Param(nameof(SlowAppliedPrice), AppliedPriceMode.Close)
			.SetDisplay("Slow Price", "Applied price for the slow MA", "Moving Averages");

		_signalShift = Param(nameof(SignalShift), 1)
			.SetNotNegative()
			.SetDisplay("Signal Shift", "Number of completed candles to look back", "Trading Rules");

		_breakoutDistancePoints = Param(nameof(BreakoutDistancePoints), 45m)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Distance", "Distance in points for stop entries", "Trading Rules")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 5m);

		_useTimeWindow = Param(nameof(UseTimeWindow), true)
			.SetDisplay("Use Time Window", "Enable Start/Stop hours", "Session");

		_startHour = Param(nameof(StartHour), 11)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Hour when trading can start", "Session");

		_stopHour = Param(nameof(StopHour), 16)
			.SetRange(0, 23)
			.SetDisplay("Stop Hour", "Hour when trading stops", "Session");

		_useFridayCloseAll = Param(nameof(UseFridayCloseAll), true)
			.SetDisplay("Friday Close All", "Close positions and cancel orders late on Friday", "Session");

		_fridayCloseTime = Param(nameof(FridayCloseTime), new TimeSpan(21, 30, 0))
			.SetDisplay("Friday Close Time", "Time of day to flatten on Friday", "Session");

		_useFridayStopTrading = Param(nameof(UseFridayStopTrading), false)
			.SetDisplay("Friday Stop Trading", "Block new entries before Friday close", "Session");

		_fridayStopTradingTime = Param(nameof(FridayStopTradingTime), new TimeSpan(19, 0, 0))
			.SetDisplay("Friday Stop Time", "Time to block new entries on Friday", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signal candles", "General");
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

		_fastMa = null;
		_slowMa = null;
		_fastLsma = null;
		_slowLsma = null;
		_fastHistory.Clear();
		_slowHistory.Clear();
		_priceStep = 0m;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_fridayTradingDisabled = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		_fastMa = CreateMovingAverage(FastMaMode, FastMaPeriod, out _fastLsma);
		_slowMa = CreateMovingAverage(SlowMaMode, SlowMaPeriod, out _slowLsma);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenCandlesFinished(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_fastLsma != null)
				DrawIndicator(area, _fastLsma);
			else if (_fastMa != null)
				DrawIndicator(area, _fastMa);

			if (_slowLsma != null)
				DrawIndicator(area, _slowLsma);
			else if (_slowMa != null)
				DrawIndicator(area, _slowMa);
		}

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_buyStopOrder != null && order == _buyStopOrder)
		{
			if (order.State == OrderStates.Done)
			{
				_buyStopOrder = null;
				CancelOppositeStop(Sides.Buy);
			}
			else if (order.State is OrderStates.Failed or OrderStates.Cancelled)
			{
				_buyStopOrder = null;
			}
		}

		if (_sellStopOrder != null && order == _sellStopOrder)
		{
			if (order.State == OrderStates.Done)
			{
				_sellStopOrder = null;
				CancelOppositeStop(Sides.Sell);
			}
			else if (order.State is OrderStates.Failed or OrderStates.Cancelled)
			{
				_sellStopOrder = null;
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!ValidateIndicators())
			return;

		var time = candle.CloseTime;

		if (UseFridayCloseAll && time.DayOfWeek == DayOfWeek.Friday && time.TimeOfDay >= FridayCloseTime)
		{
			_fridayTradingDisabled = true;
			CancelPendingOrders();
			if (Position != 0)
				ClosePosition();
			return;
		}

		if (UseFridayStopTrading && time.DayOfWeek == DayOfWeek.Friday && time.TimeOfDay >= FridayStopTradingTime)
			_fridayTradingDisabled = true;

		if (!IsWithinTradingWindow(time))
		{
			CancelPendingOrders();
			return;
		}

		var fastPrice = GetAppliedPrice(candle, FastAppliedPrice);
		var slowPrice = GetAppliedPrice(candle, SlowAppliedPrice);

		if (!TryProcessIndicator(_fastMa, _fastLsma, fastPrice, candle, out var fastValue))
			return;

		if (!TryProcessIndicator(_slowMa, _slowLsma, slowPrice, candle, out var slowValue))
			return;

		UpdateHistory(_fastHistory, fastValue);
		UpdateHistory(_slowHistory, slowValue);

		var fastSignal = GetShiftedValue(_fastHistory);
		var slowSignal = GetShiftedValue(_slowHistory);

		if (fastSignal is not decimal fastMaSignal || slowSignal is not decimal slowMaSignal)
			return;

		var difference = fastMaSignal - slowMaSignal;

		if (difference > 0m)
		{
			HandleBullishSignal(candle);
		}
		else if (difference < 0m)
		{
			HandleBearishSignal(candle);
		}
	}

	private bool ValidateIndicators()
	{
		if (FastMaMode == MovingAverageMode.LeastSquares && _fastLsma == null)
			return false;

		if (FastMaMode != MovingAverageMode.LeastSquares && _fastMa == null)
			return false;

		if (SlowMaMode == MovingAverageMode.LeastSquares && _slowLsma == null)
			return false;

		if (SlowMaMode != MovingAverageMode.LeastSquares && _slowMa == null)
			return false;

		return true;
	}

	private bool TryProcessIndicator(LengthIndicator<decimal>? ma, LinearRegression? lsma, decimal price, ICandleMessage candle, out decimal value)
	{
		IIndicatorValue? result = null;

		if (lsma != null)
		{
			result = lsma.Process(price, candle.OpenTime, true);
			if (!lsma.IsFormed)
			{
				value = 0m;
				return false;
			}
		}
		else if (ma != null)
		{
			result = ma.Process(price, candle.OpenTime, true);
			if (!ma.IsFormed)
			{
				value = 0m;
				return false;
			}
		}
		else
		{
			value = 0m;
			return false;
		}

		var converted = result.ToNullableDecimal();
		if (converted is not decimal decimalValue)
		{
			value = 0m;
			return false;
		}

		value = decimalValue;
		return true;
	}

	private void HandleBullishSignal(ICandleMessage candle)
	{
		if (Position < 0m)
			ClosePosition();

		if (_sellStopOrder != null)
			CancelOrder(_sellStopOrder);

		if (!CanPlaceNewOrder())
			return;

		var entryPrice = candle.ClosePrice + BreakoutDistancePoints * _priceStep;
		_buyStopOrder = BuyStop(Volume, entryPrice);
		if (_buyStopOrder != null)
			LogInfo($"Placed buy stop at {entryPrice:F5} due to bullish crossover.");
	}

	private void HandleBearishSignal(ICandleMessage candle)
	{
		if (Position > 0m)
			ClosePosition();

		if (_buyStopOrder != null)
			CancelOrder(_buyStopOrder);

		if (!CanPlaceNewOrder())
			return;

		var entryPrice = candle.ClosePrice - BreakoutDistancePoints * _priceStep;
		_sellStopOrder = SellStop(Volume, entryPrice);
		if (_sellStopOrder != null)
			LogInfo($"Placed sell stop at {entryPrice:F5} due to bearish crossover.");
	}

	private bool CanPlaceNewOrder()
	{
		if (_fridayTradingDisabled)
			return false;

		if (Position != 0m)
			return false;

		if (_buyStopOrder != null || _sellStopOrder != null)
			return false;

		return true;
	}

	private void CancelPendingOrders()
	{
		if (_buyStopOrder != null)
			CancelOrder(_buyStopOrder);

		if (_sellStopOrder != null)
			CancelOrder(_sellStopOrder);
	}

	private void CancelOppositeStop(Sides side)
	{
		if (side == Sides.Buy && _sellStopOrder != null)
		{
			CancelOrder(_sellStopOrder);
			_sellStopOrder = null;
		}
		else if (side == Sides.Sell && _buyStopOrder != null)
		{
			CancelOrder(_buyStopOrder);
			_buyStopOrder = null;
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (_fridayTradingDisabled)
			return false;

		if (!UseTimeWindow)
			return true;

		var hour = time.Hour;
		return hour >= StartHour && hour <= StopHour;
	}

	private void UpdateHistory(Queue<decimal> storage, decimal value)
	{
		storage.Enqueue(value);

		var maxCount = Math.Max(SignalShift + 1, 1);
		while (storage.Count > maxCount)
			storage.Dequeue();
	}

	private decimal? GetShiftedValue(Queue<decimal> storage)
	{
		var required = SignalShift + 1;
		if (storage.Count < required)
			return null;

		var values = storage.ToArray();
		return values[^required];
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceMode mode)
	{
		return mode switch
		{
			AppliedPriceMode.Open => candle.OpenPrice,
			AppliedPriceMode.High => candle.HighPrice,
			AppliedPriceMode.Low => candle.LowPrice,
			AppliedPriceMode.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceMode.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceMode.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private static LengthIndicator<decimal>? CreateMovingAverage(MovingAverageMode mode, int length, out LinearRegression? lsma)
	{
		lsma = null;
		return mode switch
		{
			MovingAverageMode.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMode.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMode.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMode.LinearWeighted => new WeightedMovingAverage { Length = length },
			MovingAverageMode.LeastSquares => lsma = new LinearRegression { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Available moving average types matching the original EA modes.
	/// </summary>
	public enum MovingAverageMode
	{
		Simple = 0,
		Exponential = 1,
		Smoothed = 2,
		LinearWeighted = 3,
		LeastSquares = 4
	}

	/// <summary>
	/// Applied price selection options.
	/// </summary>
	public enum AppliedPriceMode
	{
		Close = 0,
		Open = 1,
		High = 2,
		Low = 3,
		Median = 4,
		Typical = 5,
		Weighted = 6
	}
}
