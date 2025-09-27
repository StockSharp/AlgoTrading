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
/// Port of the FORTRADER Bill Williams Awesome Oscillator expert advisor.
/// The strategy waits for a bullish or bearish fractal filtered by the Alligator teeth
/// and uses the Awesome Oscillator triple bar rule before entering on a breakout of the recent high or low.
/// Protective stop-loss and take-profit targets are expressed in MetaTrader points and an optional
/// Gragus trailing routine moves the stop to the Alligator lips or teeth depending on slope comparisons.
/// </summary>
public class FtBillWilliamsAoStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fractalPeriod;
	private readonly StrategyParam<decimal> _indentPoints;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<CloseDropTeethMode> _closeDropTeethMode;
	private readonly StrategyParam<CloseReverseSignalMode> _closeReverseSignalMode;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<int> _trendSmaPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _signalShift;

	private readonly List<decimal> _aoHistory = new();
	private readonly List<decimal> _jawHistory = new();
	private readonly List<decimal> _teethHistory = new();
	private readonly List<decimal> _lipsHistory = new();
	private readonly List<decimal> _smaHistory = new();
	private readonly List<decimal> _highHistory = new();
	private readonly List<decimal> _lowHistory = new();
	private readonly List<decimal> _closeHistory = new();
	private readonly List<DateTimeOffset> _timeHistory = new();

	private decimal? _pendingLongLevel;
	private decimal? _pendingShortLevel;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private bool _longFractalReady;
	private bool _shortFractalReady;
	private decimal? _lastUpFractal;
	private decimal? _lastDownFractal;
	private DateTimeOffset? _lastUpFractalTime;
	private DateTimeOffset? _lastDownFractalTime;
	private int _historyLimit;

	/// <summary>
	/// Trading volume expressed in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of bars used to identify Bill Williams fractals.
	/// </summary>
	public int FractalPeriod
	{
		get => _fractalPeriod.Value;
		set => _fractalPeriod.Value = value;
	}

	/// <summary>
	/// Additional breakout offset expressed in MetaTrader points.
	/// </summary>
	public decimal IndentPoints
	{
		get => _indentPoints.Value;
		set => _indentPoints.Value = value;
	}

	/// <summary>
	/// Length of the Alligator jaw moving average.
	/// </summary>
	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the Alligator jaw.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Length of the Alligator teeth moving average.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the Alligator teeth.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Length of the Alligator lips moving average.
	/// </summary>
	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the Alligator lips.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Defines how long positions are closed when price touches the jaw line.
	/// </summary>
	public CloseDropTeethMode CloseDropTeeth
	{
		get => _closeDropTeethMode.Value;
		set => _closeDropTeethMode.Value = value;
	}

	/// <summary>
	/// Defines when the strategy exits on an opposite reversal signal.
	/// </summary>
	public CloseReverseSignalMode CloseReverseSignal
	{
		get => _closeReverseSignalMode.Value;
		set => _closeReverseSignalMode.Value = value;
	}

	/// <summary>
	/// Enables the Gragus trailing stop routine.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Period of the auxiliary SMA used to compare slopes in the trailing logic.
	/// </summary>
	public int TrendSmaPeriod
	{
		get => _trendSmaPeriod.Value;
		set => _trendSmaPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Number of completed candles skipped when reading Awesome Oscillator values.
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FtBillWilliamsAoStrategy"/> class.
	/// </summary>
	public FtBillWilliamsAoStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Volume", "Order volume in lots", "General")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Market data source", "General");

		_fractalPeriod = Param(nameof(FractalPeriod), 5)
			.SetDisplay("Fractal Period", "Number of bars required to confirm a fractal", "Signals")
			.SetRange(3, 21)
			.SetCanOptimize(true);

		_indentPoints = Param(nameof(IndentPoints), 1m)
			.SetDisplay("Indent Points", "Additional offset in MetaTrader points above the trigger candle", "Signals")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_jawPeriod = Param(nameof(JawPeriod), 13)
			.SetDisplay("Jaw Period", "Alligator jaw moving average length", "Alligator")
			.SetGreaterThanZero();

		_jawShift = Param(nameof(JawShift), 8)
			.SetDisplay("Jaw Shift", "Forward shift for the jaw line", "Alligator")
			.SetRange(0, 30);

		_teethPeriod = Param(nameof(TeethPeriod), 8)
			.SetDisplay("Teeth Period", "Alligator teeth moving average length", "Alligator")
			.SetGreaterThanZero();

		_teethShift = Param(nameof(TeethShift), 5)
			.SetDisplay("Teeth Shift", "Forward shift for the teeth line", "Alligator")
			.SetRange(0, 30);

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
			.SetDisplay("Lips Period", "Alligator lips moving average length", "Alligator")
			.SetGreaterThanZero();

		_lipsShift = Param(nameof(LipsShift), 3)
			.SetDisplay("Lips Shift", "Forward shift for the lips line", "Alligator")
			.SetRange(0, 30);

		_closeDropTeethMode = Param(nameof(CloseDropTeeth), CloseDropTeethMode.PreviousCloseBelowJaw)
			.SetDisplay("Close Drop Teeth", "How long positions close relative to the jaw", "Risk");

		_closeReverseSignalMode = Param(nameof(CloseReverseSignal), CloseReverseSignalMode.OnShortSignal)
			.SetDisplay("Close Reverse", "When to close on opposite signals", "Risk");

		_useTrailing = Param(nameof(UseTrailing), true)
			.SetDisplay("Use Trailing", "Enable the Gragus trailing rules", "Risk");

		_trendSmaPeriod = Param(nameof(TrendSmaPeriod), 5)
			.SetDisplay("Trend SMA", "Length of the auxiliary SMA used in the trailing check", "Risk")
			.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetDisplay("Stop-Loss Points", "MetaTrader points for stop-loss", "Risk")
			.SetRange(0m, 2000m)
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetDisplay("Take-Profit Points", "MetaTrader points for take-profit", "Risk")
			.SetRange(0m, 2000m)
			.SetCanOptimize(true);

		_signalShift = Param(nameof(SignalShift), 1)
			.SetDisplay("Signal Shift", "Number of candles skipped when reading Awesome Oscillator", "Signals")
			.SetRange(0, 10);

		Volume = _tradeVolume.Value;
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
		Volume = _tradeVolume.Value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateHistoryLimit();

		var ao = new AwesomeOscillator();
		var jaw = new SmoothedMovingAverage { Length = JawPeriod, CandlePrice = CandlePrice.Median };
		var teeth = new SmoothedMovingAverage { Length = TeethPeriod, CandlePrice = CandlePrice.Median };
		var lips = new SmoothedMovingAverage { Length = LipsPeriod, CandlePrice = CandlePrice.Median };
		var trendSma = new SimpleMovingAverage { Length = TrendSmaPeriod, CandlePrice = CandlePrice.Close };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ao, jaw, teeth, lips, trendSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ao);
			DrawIndicator(area, jaw);
			DrawIndicator(area, teeth);
			DrawIndicator(area, lips);
			DrawIndicator(area, trendSma);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal aoValue, decimal jawValue, decimal teethValue, decimal lipsValue, decimal trendSmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		AddHistory(_aoHistory, aoValue);
		AddHistory(_jawHistory, jawValue);
		AddHistory(_teethHistory, teethValue);
		AddHistory(_lipsHistory, lipsValue);
		AddHistory(_smaHistory, trendSmaValue);
		AddHistory(_highHistory, candle.HighPrice);
		AddHistory(_lowHistory, candle.LowPrice);
		AddHistory(_closeHistory, candle.ClosePrice);
		AddHistory(_timeHistory, candle.OpenTime);

		var (upDetected, upValue, downDetected, downValue) = DetectFractals();
		if (upDetected)
			_lastUpFractal = upValue;
		if (downDetected)
			_lastDownFractal = downValue;

		UpdateSignals(upDetected, upValue, downDetected, downValue);

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		ManageLong(candle, downDetected, canTrade);
		ManageShort(candle, upDetected, canTrade);

		if (!canTrade)
			return;

		TryEnterLong(candle);
		TryEnterShort(candle);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var price = trade.Trade?.Price;
		var direction = trade.Order.Direction;

		if (price is null || direction is null)
			return;

		if (direction == Sides.Buy)
		{
			if (Position > 0m)
			{
				ResetLongSignal();
				var stopDistance = GetPriceOffset(StopLossPoints);
				var takeDistance = GetPriceOffset(TakeProfitPoints);
				_longStopPrice = stopDistance > 0m ? price.Value - stopDistance : null;
				_longTakePrice = takeDistance > 0m ? price.Value + takeDistance : null;
				ResetShortState();
			}
			else if (Position <= 0m)
			{
				ResetShortState();
			}
		}
		else if (direction == Sides.Sell)
		{
			if (Position < 0m)
			{
				ResetShortSignal();
				var stopDistance = GetPriceOffset(StopLossPoints);
				var takeDistance = GetPriceOffset(TakeProfitPoints);
				_shortStopPrice = stopDistance > 0m ? price.Value + stopDistance : null;
				_shortTakePrice = takeDistance > 0m ? price.Value - takeDistance : null;
				ResetLongState();
			}
			else if (Position >= 0m)
			{
				ResetLongState();
			}
		}

		if (Position == 0m)
		{
			ResetLongState();
			ResetShortState();
		}
	}

	private void UpdateSignals(bool upDetected, decimal upValue, bool downDetected, decimal downValue)
	{
		var shift = SignalShift;
		var aoC = GetValueBarsAgo(_aoHistory, shift);

		if (aoC is decimal aoCValue && aoCValue < 0m)
			ResetLongSignal();

		if (_longFractalReady && aoC is decimal cValue && cValue < 0m)
			ResetLongSignal();

		if (upDetected && !_longFractalReady && _pendingLongLevel is null && Position <= 0m)
		{
			var teethFilter = GetShiftedValue(_teethHistory, TeethShift, 1);
			if (teethFilter is decimal teethValue && upValue > teethValue)
				_longFractalReady = true;
		}

		if (_longFractalReady)
		{
			var aoA = GetValueBarsAgo(_aoHistory, shift + 2);
			var aoB = GetValueBarsAgo(_aoHistory, shift + 1);
			if (aoA is decimal aValue && aoB is decimal bValue && aoC is decimal cValue &&
				aValue > bValue && bValue < cValue && aValue > 0m && bValue > 0m && cValue > 0m)
			{
				var baseHigh = GetValueBarsAgo(_highHistory, shift);
				if (baseHigh is decimal triggerHigh)
				{
					var point = GetPoint();
					if (point <= 0m)
						point = 1m;
					_pendingLongLevel = triggerHigh + IndentPoints * point;
					_longFractalReady = false;
				}
			}
		}

		if (_shortFractalReady && aoC is decimal cShort && cShort > 0m)
			_shortFractalReady = false;

		if (downDetected && !_shortFractalReady && _pendingShortLevel is null && Position >= 0m)
		{
			var teethFilter = GetShiftedValue(_teethHistory, TeethShift, 1);
			if (teethFilter is decimal teethValue && downValue < teethValue)
				_shortFractalReady = true;
		}

		if (_shortFractalReady)
		{
			var aoA = GetValueBarsAgo(_aoHistory, shift + 2);
			var aoB = GetValueBarsAgo(_aoHistory, shift + 1);
			if (aoA is decimal aValue && aoB is decimal bValue && aoC is decimal cValue &&
				aValue < bValue && bValue > cValue && aValue < 0m && bValue < 0m && cValue < 0m)
			{
				var baseLow = GetValueBarsAgo(_lowHistory, shift);
				if (baseLow is decimal triggerLow)
				{
					var point = GetPoint();
					if (point <= 0m)
						point = 1m;
					_pendingShortLevel = triggerLow - IndentPoints * point;
					_shortFractalReady = false;
				}
			}
		}
	}

	private void ManageLong(ICandleMessage candle, bool downFractalTriggered, bool canTrade)
	{
		if (Position <= 0m)
		{
			ResetLongState();
			return;
		}

		UpdateLongTrailing(candle);

		if (!canTrade)
			return;

		var volume = Position;

		if (_longTakePrice is decimal take && candle.HighPrice >= take)
		{
			SellMarket(volume);
			ResetLongState();
			return;
		}

		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(volume);
			ResetLongState();
			return;
		}

		var jaw = GetShiftedValue(_jawHistory, JawShift, 1);
		if (CloseDropTeeth == CloseDropTeethMode.BidBelowJaw && jaw is decimal jawValue && candle.ClosePrice <= jawValue)
		{
			SellMarket(volume);
			ResetLongState();
			return;
		}

		if (CloseDropTeeth == CloseDropTeethMode.PreviousCloseBelowJaw && jaw is decimal jawPrev)
		{
			var prevClose = GetValueBarsAgo(_closeHistory, 1);
			if (prevClose is decimal closeValue && closeValue <= jawPrev)
			{
				SellMarket(volume);
				ResetLongState();
				return;
			}
		}

		if (CloseReverseSignal == CloseReverseSignalMode.OnOppositeFractal && downFractalTriggered)
		{
			SellMarket(volume);
			ResetLongState();
			return;
		}

		if (CloseReverseSignal == CloseReverseSignalMode.OnShortSignal && _pendingShortLevel is not null)
		{
			SellMarket(volume);
			ResetLongState();
		}
	}

	private void ManageShort(ICandleMessage candle, bool upFractalTriggered, bool canTrade)
	{
		if (Position >= 0m)
		{
			ResetShortState();
			return;
		}

		UpdateShortTrailing(candle);

		if (!canTrade)
			return;

		var volume = Math.Abs(Position);

		if (_shortTakePrice is decimal take && candle.LowPrice <= take)
		{
			BuyMarket(volume);
			ResetShortState();
			return;
		}

		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(volume);
			ResetShortState();
			return;
		}

		var jaw = GetShiftedValue(_jawHistory, JawShift, 1);
		if (CloseDropTeeth == CloseDropTeethMode.BidBelowJaw && jaw is decimal jawValue && candle.ClosePrice >= jawValue)
		{
			BuyMarket(volume);
			ResetShortState();
			return;
		}

		if (CloseDropTeeth == CloseDropTeethMode.PreviousCloseBelowJaw && jaw is decimal jawPrev)
		{
			var prevClose = GetValueBarsAgo(_closeHistory, 1);
			if (prevClose is decimal closeValue && closeValue >= jawPrev)
			{
				BuyMarket(volume);
				ResetShortState();
				return;
			}
		}

		if (CloseReverseSignal == CloseReverseSignalMode.OnOppositeFractal && upFractalTriggered)
		{
			BuyMarket(volume);
			ResetShortState();
			return;
		}

		if (CloseReverseSignal == CloseReverseSignalMode.OnShortSignal && _pendingLongLevel is not null)
		{
			BuyMarket(volume);
			ResetShortState();
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
	if (_pendingLongLevel is not decimal triggerLevel)
	return;

	var shift = SignalShift;
	var aoC = GetValueBarsAgo(_aoHistory, shift);
	var aoB = GetValueBarsAgo(_aoHistory, shift + 1);

	if (aoC is not decimal cValue || aoB is not decimal bValue)
	return;

	if (cValue <= bValue)
	return;

	if (candle.HighPrice < triggerLevel)
	return;

	var volume = Volume + Math.Max(0m, -Position);
	if (volume <= 0m)
	return;

	BuyMarket(volume);
	_pendingLongLevel = null;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
	if (_pendingShortLevel is not decimal triggerLevel)
	return;

	var shift = SignalShift;
	var aoC = GetValueBarsAgo(_aoHistory, shift);
	var aoB = GetValueBarsAgo(_aoHistory, shift + 1);

	if (aoC is not decimal cValue || aoB is not decimal bValue)
	return;

	if (cValue >= bValue)
	return;

	if (candle.LowPrice > triggerLevel)
	return;

	var volume = Volume + Math.Max(0m, Position);
	if (volume <= 0m)
	return;

	SellMarket(volume);
	_pendingShortLevel = null;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
	if (!UseTrailing)
	return;

	var entryPrice = Position.AveragePrice;
	if (entryPrice <= 0m)
	return;

	if (candle.ClosePrice <= entryPrice)
	return;

	var lips = GetShiftedValue(_lipsHistory, LipsShift, 1);
	var lipsPrev = GetShiftedValue(_lipsHistory, LipsShift, 2);
	var teeth = GetShiftedValue(_teethHistory, TeethShift, 1);
	var sma = GetShiftedValue(_smaHistory, 0, 1);
	var smaPrev = GetShiftedValue(_smaHistory, 0, 2);

	if (lips is not decimal lipsValue || lipsPrev is not decimal lipsPrevValue ||
	sma is not decimal smaValue || smaPrev is not decimal smaPrevValue)
	return;

	var point = GetPoint();
	if (point <= 0m)
	point = 0.0001m;

	if (lipsValue - lipsPrevValue > smaValue - smaPrevValue)
	{
	if (Math.Abs(candle.ClosePrice - lipsValue) > 12m * point)
	{
	if (_longStopPrice is not decimal currentStop || lipsValue > currentStop)
	_longStopPrice = lipsValue;
	}
	}
	else if (teeth is decimal teethValue)
	{
	if (Math.Abs(candle.ClosePrice - teethValue) > 12m * point)
	{
	var lipsNow = GetShiftedValue(_lipsHistory, LipsShift, 1);
	if (!_longStopPrice.HasValue || _longStopPrice.Value < teethValue || (lipsNow is decimal lipsCurrent && lipsCurrent > teethValue))
	_longStopPrice = teethValue;
	}
	}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
	if (!UseTrailing)
	return;

	var entryPrice = Position.AveragePrice;
	if (entryPrice >= 0m)
	return;

	if (candle.ClosePrice >= entryPrice)
	return;

	var lips = GetShiftedValue(_lipsHistory, LipsShift, 1);
	var lipsPrev = GetShiftedValue(_lipsHistory, LipsShift, 2);
	var teeth = GetShiftedValue(_teethHistory, TeethShift, 1);
	var sma = GetShiftedValue(_smaHistory, 0, 1);
	var smaPrev = GetShiftedValue(_smaHistory, 0, 2);

	if (lips is not decimal lipsValue || lipsPrev is not decimal lipsPrevValue ||
	sma is not decimal smaValue || smaPrev is not decimal smaPrevValue)
	return;

	var point = GetPoint();
	if (point <= 0m)
	point = 0.0001m;

	if (lipsPrevValue - lipsValue > smaPrevValue - smaValue)
	{
	if (Math.Abs(candle.ClosePrice - lipsValue) > 12m * point)
	{
	if (_shortStopPrice is not decimal currentStop || lipsValue < currentStop)
	_shortStopPrice = lipsValue;
	}
	}
	else if (teeth is decimal teethValue)
	{
	if (Math.Abs(candle.ClosePrice - teethValue) > 12m * point)
	{
	var lipsNow = GetShiftedValue(_lipsHistory, LipsShift, 1);
	if (!_shortStopPrice.HasValue || _shortStopPrice.Value > teethValue || (lipsNow is decimal lipsCurrent && lipsCurrent < teethValue))
	_shortStopPrice = teethValue;
	}
	}
	}

	private (bool upDetected, decimal upValue, bool downDetected, decimal downValue) DetectFractals()
	{
	var upDetected = false;
	var upValue = 0m;
	var downDetected = false;
	var downValue = 0m;

	var period = FractalPeriod;
	if (period < 3 || period % 2 == 0)
	return (false, 0m, false, 0m);

	var windowStart = _highHistory.Count - period;
	if (windowStart >= 0)
	{
	var centerOffset = (period - 1) / 2;
	var centerIndex = windowStart + centerOffset;
	var centerValue = _highHistory[centerIndex];
	var centerTime = _timeHistory.Count > centerIndex ? _timeHistory[centerIndex] : (DateTimeOffset?)null;

	var isFractal = true;
	for (var i = 0; i < period; i++)
	{
	var idx = windowStart + i;
	if (idx == centerIndex)
	continue;
	if (_highHistory[idx] >= centerValue)
	{
	isFractal = false;
	break;
	}
	}

	if (isFractal && centerTime.HasValue && _lastUpFractalTime != centerTime)
	{
	upDetected = true;
	upValue = centerValue;
	_lastUpFractalTime = centerTime;
	}
	}

	windowStart = _lowHistory.Count - period;
	if (windowStart >= 0)
	{
	var centerOffset = (period - 1) / 2;
	var centerIndex = windowStart + centerOffset;
	var centerValue = _lowHistory[centerIndex];
	var centerTime = _timeHistory.Count > centerIndex ? _timeHistory[centerIndex] : (DateTimeOffset?)null;

	var isFractal = true;
	for (var i = 0; i < period; i++)
	{
	var idx = windowStart + i;
	if (idx == centerIndex)
	continue;
	if (_lowHistory[idx] <= centerValue)
	{
	isFractal = false;
	break;
	}
	}

	if (isFractal && centerTime.HasValue && _lastDownFractalTime != centerTime)
	{
	downDetected = true;
	downValue = centerValue;
	_lastDownFractalTime = centerTime;
	}
	}

	return (upDetected, upValue, downDetected, downValue);
	}

	private void ResetState()
	{
	_aoHistory.Clear();
	_jawHistory.Clear();
	_teethHistory.Clear();
	_lipsHistory.Clear();
	_smaHistory.Clear();
	_highHistory.Clear();
	_lowHistory.Clear();
	_closeHistory.Clear();
	_timeHistory.Clear();

	ResetLongSignal();
	ResetShortSignal();
	ResetLongState();
	ResetShortState();
	_lastUpFractal = null;
	_lastDownFractal = null;
	_lastUpFractalTime = null;
	_lastDownFractalTime = null;
	}

	private void ResetLongSignal()
	{
	_longFractalReady = false;
	_pendingLongLevel = null;
	}

	private void ResetShortSignal()
	{
	_shortFractalReady = false;
	_pendingShortLevel = null;
	}

	private void ResetLongState()
	{
	_longStopPrice = null;
	_longTakePrice = null;
	}

	private void ResetShortState()
	{
	_shortStopPrice = null;
	_shortTakePrice = null;
	}

	private void UpdateHistoryLimit()
	{
	var maxShift = Math.Max(JawShift, Math.Max(TeethShift, LipsShift));
	var requirement = Math.Max(FractalPeriod + SignalShift + maxShift + 5, TrendSmaPeriod + maxShift + 5);
	_historyLimit = Math.Max(requirement, 50);
	}

	private void AddHistory(List<decimal> list, decimal value)
	{
	list.Add(value);
	if (list.Count > _historyLimit)
	list.RemoveAt(0);
	}

	private void AddHistory(List<DateTimeOffset> list, DateTimeOffset value)
	{
	list.Add(value);
	if (list.Count > _historyLimit)
	list.RemoveAt(0);
	}

	private decimal? GetValueBarsAgo(List<decimal> history, int barsAgo)
	{
	if (barsAgo < 0)
	return null;

	var index = history.Count - 1 - barsAgo;
	if (index < 0 || index >= history.Count)
	return null;

	return history[index];
	}

	private decimal? GetShiftedValue(List<decimal> history, int shift, int barsAgo)
	{
	return GetValueBarsAgo(history, shift + barsAgo);
	}

	private decimal GetPriceOffset(decimal points)
	{
	if (points <= 0m)
	return 0m;

	var point = GetPoint();
	if (point <= 0m)
	point = 1m;

	return point * points;
	}

	private decimal GetPoint()
	{
	var security = Security;
	if (security == null)
	return 0.0001m;

	var step = security.PriceStep ?? security.MinPriceStep ?? 0m;
	if (step <= 0m)
	step = 0.0001m;

	return step;
	}

	/// <summary>
	/// Defines how long trades are closed relative to the Alligator jaw.
	/// </summary>
	public enum CloseDropTeethMode
	{
	/// <summary>
	/// Do not close positions based on the jaw line.
	/// </summary>
	Disabled = 0,

	/// <summary>
	/// Close when the current close price crosses the jaw.
	/// </summary>
	BidBelowJaw = 1,

	/// <summary>
	/// Close when the previous close price crosses the jaw.
	/// </summary>
	PreviousCloseBelowJaw = 2,
	}

	/// <summary>
	/// Defines when to exit an existing trade on opposite signals.
	/// </summary>
	public enum CloseReverseSignalMode
	{
	/// <summary>
	/// Keep the position despite opposite signals.
	/// </summary>
	Disabled = 0,

	/// <summary>
	/// Close when an opposite fractal forms.
	/// </summary>
	OnOppositeFractal = 1,

	/// <summary>
	/// Close when the opposite entry signal becomes active.
	/// </summary>
	OnShortSignal = 2,
	}
}
