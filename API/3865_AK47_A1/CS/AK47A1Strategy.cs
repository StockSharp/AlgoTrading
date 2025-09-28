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
/// AK47 A1 strategy combining Alligator, Williams %R, DeMarker and fractal filters.
/// </summary>
public class AK47A1Strategy : Strategy
{
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<int> _fractalWindow;
	private readonly StrategyParam<int> _fractalLookback;
	private readonly StrategyParam<decimal> _demarkerThreshold;
	private readonly StrategyParam<decimal> _wprLowerBound;
	private readonly StrategyParam<decimal> _wprUpperBound;

	private readonly StrategyParam<decimal> _spanGatorPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw = null!;
	private SmoothedMovingAverage _teeth = null!;
	private SmoothedMovingAverage _lips = null!;
	private DeMarker _demarker = null!;
	private WilliamsPercentRange _wpr = null!;

	private readonly Queue<decimal> _jawShiftBuffer = new();
	private readonly Queue<decimal> _teethShiftBuffer = new();
	private readonly Queue<decimal> _lipsShiftBuffer = new();

	private readonly Queue<decimal> _highWindow = new();
	private readonly Queue<decimal> _lowWindow = new();

	private decimal? _currentDemarker;
	private decimal? _currentWpr;
	private decimal? _previousWpr;

	private decimal? _upperFractal;
	private decimal? _lowerFractal;
	private int _upperFractalAge;
	private int _lowerFractalAge;

	private decimal? _longStopPrice;
	private decimal? _longTargetPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTargetPrice;

	/// <summary>
	/// Length of the Alligator jaw moving average.
	/// </summary>
	public int JawLength
	{
		get => _jawLength.Value;
		set => _jawLength.Value = value;
	}

	/// <summary>
	/// Shift applied to the Alligator jaw line.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Length of the Alligator teeth moving average.
	/// </summary>
	public int TeethLength
	{
		get => _teethLength.Value;
		set => _teethLength.Value = value;
	}

	/// <summary>
	/// Shift applied to the Alligator teeth line.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Length of the Alligator lips moving average.
	/// </summary>
	public int LipsLength
	{
		get => _lipsLength.Value;
		set => _lipsLength.Value = value;
	}

	/// <summary>
	/// Shift applied to the Alligator lips line.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Number of candles used to detect fractal formations.
	/// </summary>
	public int FractalWindow
	{
		get => _fractalWindow.Value;
		set => _fractalWindow.Value = value;
	}

	/// <summary>
	/// Maximum age for a detected fractal to remain valid.
	/// </summary>
	public int FractalLookback
	{
		get => _fractalLookback.Value;
		set => _fractalLookback.Value = value;
	}

	/// <summary>
	/// Minimum DeMarker value required for long entries.
	/// </summary>
	public decimal DemarkerThreshold
	{
		get => _demarkerThreshold.Value;
		set => _demarkerThreshold.Value = value;
	}

	/// <summary>
	/// Lower bound of the Williams %R filter range.
	/// </summary>
	public decimal WprLowerBound
	{
		get => _wprLowerBound.Value;
		set => _wprLowerBound.Value = value;
	}

	/// <summary>
	/// Upper bound of the Williams %R filter range.
	/// </summary>
	public decimal WprUpperBound
	{
		get => _wprUpperBound.Value;
		set => _wprUpperBound.Value = value;
	}

	/// <summary>
	/// Required Alligator mouth width in points.
	/// </summary>
	public decimal SpanGatorPoints
	{
		get => _spanGatorPoints.Value;
		set => _spanGatorPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
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
	/// Initialize <see cref="AK47A1Strategy"/>.
	/// </summary>
	public AK47A1Strategy()
	{
		_jawLength = Param(nameof(JawLength), 13)
		.SetGreaterThanZero()
		.SetDisplay("Jaw Length", "Length of the Alligator jaw line", "Alligator")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_jawShift = Param(nameof(JawShift), 8)
		.SetNotNegative()
		.SetDisplay("Jaw Shift", "Shift applied to the Alligator jaw line", "Alligator")
		.SetCanOptimize(true)
		.SetOptimize(0, 16, 1);

		_teethLength = Param(nameof(TeethLength), 8)
		.SetGreaterThanZero()
		.SetDisplay("Teeth Length", "Length of the Alligator teeth line", "Alligator")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);

		_teethShift = Param(nameof(TeethShift), 5)
		.SetNotNegative()
		.SetDisplay("Teeth Shift", "Shift applied to the Alligator teeth line", "Alligator")
		.SetCanOptimize(true)
		.SetOptimize(0, 16, 1);

		_lipsLength = Param(nameof(LipsLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Lips Length", "Length of the Alligator lips line", "Alligator")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_lipsShift = Param(nameof(LipsShift), 3)
		.SetNotNegative()
		.SetDisplay("Lips Shift", "Shift applied to the Alligator lips line", "Alligator")
		.SetCanOptimize(true)
		.SetOptimize(0, 10, 1);

		_fractalWindow = Param(nameof(FractalWindow), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fractal Window", "Number of candles for fractal detection", "Fractals")
		.SetCanOptimize(true)
		.SetOptimize(3, 9, 2);

		_fractalLookback = Param(nameof(FractalLookback), 3)
		.SetGreaterThanZero()
		.SetDisplay("Fractal Lookback", "Maximum age of a detected fractal", "Fractals")
		.SetCanOptimize(true)
		.SetOptimize(1, 6, 1);

		_demarkerThreshold = Param(nameof(DemarkerThreshold), 0.5m)
		.SetNotNegative()
		.SetDisplay("DeMarker Threshold", "Minimum value for long setups", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(0m, 1m, 0.05m);

		_wprLowerBound = Param(nameof(WprLowerBound), 0.25m)
		.SetNotNegative()
		.SetDisplay("WPR Lower", "Lower Williams %R filter bound", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(0m, 0.5m, 0.05m);

		_wprUpperBound = Param(nameof(WprUpperBound), 0.75m)
		.SetNotNegative()
		.SetDisplay("WPR Upper", "Upper Williams %R filter bound", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 1m, 0.05m);

		_spanGatorPoints = Param(nameof(SpanGatorPoints), 0.5m)
		.SetNotNegative()
		.SetDisplay("Alligator Span", "Required gap between Alligator lines", "Alligator")
		.SetCanOptimize(true)
		.SetOptimize(0m, 3m, 0.1m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take-profit distance in points", "Orders")
		.SetCanOptimize(true)
		.SetOptimize(0m, 200m, 10m);

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop-loss distance in points", "Orders")
		.SetCanOptimize(true)
		.SetOptimize(0m, 150m, 10m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 50m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop", "Trailing-stop distance in points", "Orders")
		.SetCanOptimize(true)
		.SetOptimize(0m, 150m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for trading", "General");
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

		_jawShiftBuffer.Clear();
		_teethShiftBuffer.Clear();
		_lipsShiftBuffer.Clear();
		_highWindow.Clear();
		_lowWindow.Clear();

		_currentDemarker = null;
		_currentWpr = null;
		_previousWpr = null;

		_upperFractal = null;
		_lowerFractal = null;
		_upperFractalAge = FractalLookback + 1;
		_lowerFractalAge = FractalLookback + 1;

		ResetPositionLevels();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jaw = new SmoothedMovingAverage { Length = JawLength };
		_teeth = new SmoothedMovingAverage { Length = TeethLength };
		_lips = new SmoothedMovingAverage { Length = LipsLength };
		_demarker = new DeMarker { Length = 13 };
		_wpr = new WilliamsPercentRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var time = candle.ServerTime;

		var jawValue = _jaw.Process(new DecimalIndicatorValue(_jaw, median, time));
		var teethValue = _teeth.Process(new DecimalIndicatorValue(_teeth, median, time));
		var lipsValue = _lips.Process(new DecimalIndicatorValue(_lips, median, time));

		var demarkerValue = _demarker.Process(new CandleIndicatorValue(_demarker, candle));
		if (demarkerValue.IsFinal)
			_currentDemarker = demarkerValue.ToDecimal();

		var wprValue = _wpr.Process(new CandleIndicatorValue(_wpr, candle));
		if (wprValue.IsFinal)
		{
			_previousWpr = _currentWpr;
			var normalized = NormalizeWpr(wprValue.ToDecimal());
			_currentWpr = normalized;
		}

		if (!jawValue.IsFinal || !teethValue.IsFinal || !lipsValue.IsFinal)
			return;

		var jaw = UpdateShiftValue(_jawShiftBuffer, jawValue.ToDecimal(), JawShift);
		var teeth = UpdateShiftValue(_teethShiftBuffer, teethValue.ToDecimal(), TeethShift);
		var lips = UpdateShiftValue(_lipsShiftBuffer, lipsValue.ToDecimal(), LipsShift);

		if (candle.State != CandleStates.Finished)
			return;

		UpdateFractals(candle);

		if (jaw is null || teeth is null || lips is null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			ManageOpenPosition(candle);
			return;
		}

		if (ManageOpenPosition(candle))
			return;

		var step = Security?.PriceStep ?? 1m;
		var threshold = SpanGatorPoints * step;

		var jawTeeth = Math.Abs(jaw.Value - teeth.Value);
		var lipsTeeth = Math.Abs(lips.Value - teeth.Value);
		var lipsJaw = Math.Abs(lips.Value - jaw.Value);
		var alligatorActive = jawTeeth >= threshold && lipsTeeth >= threshold && lipsJaw >= threshold;

		var wprFilter = _previousWpr is decimal prevWpr && prevWpr > WprLowerBound && prevWpr < WprUpperBound;
		var demarker = _currentDemarker;

		var lowerFresh = _lowerFractal is decimal lower && _lowerFractalAge <= FractalLookback;
		var upperFresh = _upperFractal is decimal upper && _upperFractalAge <= FractalLookback;

		var buySignal = Position <= 0m && alligatorActive && wprFilter && demarker is decimal deHigh && deHigh >= DemarkerThreshold && lowerFresh;
		var sellSignal = Position >= 0m && alligatorActive && wprFilter && demarker is decimal deLow && deLow <= DemarkerThreshold && upperFresh;

		if (buySignal)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (sellSignal)
		{
			EnterShort(candle.ClosePrice);
		}
	}

	private void EnterLong(decimal referencePrice)
	{
		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionLevels();
		}

		if (Position == 0m)
		{
			BuyMarket(Volume);
			InitializeLongLevels(referencePrice);
		}
	}

	private void EnterShort(decimal referencePrice)
	{
		if (Position > 0m)
		{
			SellMarket(Math.Abs(Position));
			ResetPositionLevels();
		}

		if (Position == 0m)
		{
			SellMarket(Volume);
			InitializeShortLevels(referencePrice);
		}
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 1m;

		if (Position > 0m)
		{
			var closePrice = candle.ClosePrice;

			if (_longStopPrice is decimal stop && (candle.LowPrice <= stop || closePrice <= stop))
			{
				SellMarket(Math.Abs(Position));
				ResetPositionLevels();
				return true;
			}

			if (_longTargetPrice is decimal target && (candle.HighPrice >= target || closePrice >= target))
			{
				SellMarket(Math.Abs(Position));
				ResetPositionLevels();
				return true;
			}

			if (TrailingStopPoints > 0m)
			{
				var trailing = closePrice - TrailingStopPoints * step;
				if (!_longStopPrice.HasValue || trailing > _longStopPrice.Value)
					_longStopPrice = trailing;
			}
		}
		else if (Position < 0m)
		{
			var closePrice = candle.ClosePrice;

			if (_shortStopPrice is decimal stop && (candle.HighPrice >= stop || closePrice >= stop))
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionLevels();
				return true;
			}

			if (_shortTargetPrice is decimal target && (candle.LowPrice <= target || closePrice <= target))
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionLevels();
				return true;
			}

			if (TrailingStopPoints > 0m)
			{
				var trailing = closePrice + TrailingStopPoints * step;
				if (!_shortStopPrice.HasValue || trailing < _shortStopPrice.Value)
					_shortStopPrice = trailing;
			}
		}

		return false;
	}

	private void InitializeLongLevels(decimal entryPrice)
	{
		var step = Security?.PriceStep ?? 1m;

		_longStopPrice = StopLossPoints > 0m ? entryPrice - StopLossPoints * step : null;
		_longTargetPrice = TakeProfitPoints > 0m ? entryPrice + TakeProfitPoints * step : null;
		_shortStopPrice = null;
		_shortTargetPrice = null;
	}

	private void InitializeShortLevels(decimal entryPrice)
	{
		var step = Security?.PriceStep ?? 1m;

		_shortStopPrice = StopLossPoints > 0m ? entryPrice + StopLossPoints * step : null;
		_shortTargetPrice = TakeProfitPoints > 0m ? entryPrice - TakeProfitPoints * step : null;
		_longStopPrice = null;
		_longTargetPrice = null;
	}

	private void ResetPositionLevels()
	{
		_longStopPrice = null;
		_longTargetPrice = null;
		_shortStopPrice = null;
		_shortTargetPrice = null;
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		_highWindow.Enqueue(candle.HighPrice);
		_lowWindow.Enqueue(candle.LowPrice);

		if (_highWindow.Count > FractalWindow)
			_highWindow.Dequeue();

		if (_lowWindow.Count > FractalWindow)
			_lowWindow.Dequeue();

		var upperDetected = false;
		var lowerDetected = false;

		if (_highWindow.Count == FractalWindow)
		{
			var highs = _highWindow.ToArray();
			if (highs[2] > highs[0] && highs[2] > highs[1] && highs[2] > highs[3] && highs[2] > highs[4])
			{
				_upperFractal = highs[2];
				_upperFractalAge = 2;
				upperDetected = true;
			}
		}

		if (_lowWindow.Count == FractalWindow)
		{
			var lows = _lowWindow.ToArray();
			if (lows[2] < lows[0] && lows[2] < lows[1] && lows[2] < lows[3] && lows[2] < lows[4])
			{
				_lowerFractal = lows[2];
				_lowerFractalAge = 2;
				lowerDetected = true;
			}
		}

		if (!upperDetected && _upperFractalAge <= FractalLookback)
			_upperFractalAge++;

		if (!lowerDetected && _lowerFractalAge <= FractalLookback)
			_lowerFractalAge++;

		if (_upperFractalAge > FractalLookback)
			_upperFractal = null;

		if (_lowerFractalAge > FractalLookback)
			_lowerFractal = null;
	}

	private static decimal? UpdateShiftValue(Queue<decimal> buffer, decimal value, int shift)
	{
		buffer.Enqueue(value);

		var max = shift + 1;
		if (buffer.Count > max)
			buffer.Dequeue();

		return buffer.Count < max ? null : buffer.Peek();
	}

	private static decimal NormalizeWpr(decimal value)
	{
		var normalized = -value / 100m;

		if (normalized < 0m)
			normalized = 0m;
		else if (normalized > 1m)
			normalized = 1m;

		return normalized;
	}
}

