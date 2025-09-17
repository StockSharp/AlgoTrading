using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fractals and Alligator breakout strategy converted from the MetaTrader expert.
/// Combines Bill Williams Alligator alignment, fractal breakouts, and a momentum filter.
/// Optional ATR or fixed range filter prevents trades inside narrow consolidations.
/// </summary>
public class FractalsAlligatorStrategy : Strategy
{
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _rangeLookback;
	private readonly StrategyParam<bool> _useAtrFilter;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _rangeBoxSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw = null!;
	private SmoothedMovingAverage _teeth = null!;
	private SmoothedMovingAverage _lips = null!;
	private AverageTrueRange _atr = null!;

	private readonly Queue<decimal> _jawShiftBuffer = new();
	private readonly Queue<decimal> _teethShiftBuffer = new();
	private readonly Queue<decimal> _lipsShiftBuffer = new();
	private readonly decimal[] _highBuffer = new decimal[5];
	private readonly decimal[] _lowBuffer = new decimal[5];
	private readonly Queue<decimal> _closeHistory = new();
	private readonly decimal[] _momentumAbs = new decimal[3];
	private int _momentumFillCount;
	private int _fractalBufferFillCount;
	private decimal? _lastUpFractal;
	private decimal? _lastDownFractal;
	private readonly Queue<decimal> _rangeHighs = new();
	private readonly Queue<decimal> _rangeLows = new();

	/// <summary>
	/// Period for the Alligator jaw (blue) moving average.
	/// </summary>
	public int JawLength
	{
		get => _jawLength.Value;
		set => _jawLength.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the jaw line in bars.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Period for the Alligator teeth (red) moving average.
	/// </summary>
	public int TeethLength
	{
		get => _teethLength.Value;
		set => _teethLength.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the teeth line in bars.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Period for the Alligator lips (green) moving average.
	/// </summary>
	public int LipsLength
	{
		get => _lipsLength.Value;
		set => _lipsLength.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the lips line in bars.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Lookback length for the momentum percentage calculation.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute momentum change required for buy signals.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum absolute momentum change required for sell signals.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Number of candles used to compute the breakout range filter.
	/// </summary>
	public int RangeLookback
	{
		get => _rangeLookback.Value;
		set => _rangeLookback.Value = value;
	}

	/// <summary>
	/// Enable ATR-based confirmation instead of the fixed box size.
	/// </summary>
	public bool UseAtrFilter
	{
		get => _useAtrFilter.Value;
		set => _useAtrFilter.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the ATR value when the ATR filter is enabled.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Box size in price steps when the fixed-range filter is active.
	/// </summary>
	public decimal RangeBoxSteps
	{
		get => _rangeBoxSteps.Value;
		set => _rangeBoxSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
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
	/// Initializes a new instance of the <see cref="FractalsAlligatorStrategy"/> class.
	/// </summary>
	public FractalsAlligatorStrategy()
	{
		_jawLength = Param(nameof(JawLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Length", "Alligator jaw period", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 1);

		_jawShift = Param(nameof(JawShift), 8)
			.SetGreaterOrEqual(0)
			.SetDisplay("Jaw Shift", "Forward shift for the jaw line", "Alligator")
			.SetCanOptimize(false);

		_teethLength = Param(nameof(TeethLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Length", "Alligator teeth period", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_teethShift = Param(nameof(TeethShift), 5)
			.SetGreaterOrEqual(0)
			.SetDisplay("Teeth Shift", "Forward shift for the teeth line", "Alligator");

		_lipsLength = Param(nameof(LipsLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Length", "Alligator lips period", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_lipsShift = Param(nameof(LipsShift), 3)
			.SetGreaterOrEqual(0)
			.SetDisplay("Lips Shift", "Forward shift for the lips line", "Alligator");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Lookback for the momentum calculation", "Momentum");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Momentum Buy Threshold", "Minimum percentage change for long trades", "Momentum");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Momentum Sell Threshold", "Minimum percentage change for short trades", "Momentum");

		_rangeLookback = Param(nameof(RangeLookback), 10)
			.SetGreaterThanZero()
			.SetDisplay("Range Lookback", "Candles used for the breakout range filter", "Filters");

		_useAtrFilter = Param(nameof(UseAtrFilter), true)
			.SetDisplay("Use ATR Filter", "Use ATR instead of fixed box size", "Filters");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for the range filter", "Filters");

		_rangeBoxSteps = Param(nameof(RangeBoxSteps), 20m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Range Box Steps", "Fixed box size in price steps", "Filters");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Take Profit Steps", "Take-profit distance in steps", "Risk Management");

		_stopLossSteps = Param(nameof(StopLossSteps), 20m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Stop Loss Steps", "Stop-loss distance in steps", "Risk Management");

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 40m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Trailing Stop Steps", "Trailing stop distance in steps", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");
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
		_closeHistory.Clear();
		_rangeHighs.Clear();
		_rangeLows.Clear();
		Array.Clear(_highBuffer);
		Array.Clear(_lowBuffer);
		Array.Clear(_momentumAbs);
		_momentumFillCount = 0;
		_fractalBufferFillCount = 0;
		_lastUpFractal = null;
		_lastDownFractal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jaw = new SmoothedMovingAverage { Length = JawLength };
		_teeth = new SmoothedMovingAverage { Length = TeethLength };
		_lips = new SmoothedMovingAverage { Length = LipsLength };
		_atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_jaw, _teeth, _lips, _atr, ProcessCandle)
			.Start();

		var takeProfitUnit = TakeProfitSteps > 0m ? new Unit(TakeProfitSteps, UnitTypes.PriceStep) : null;
		var stopLossUnit = StopLossSteps > 0m ? new Unit(StopLossSteps, UnitTypes.PriceStep) : null;
		var trailingUnit = TrailingStopSteps > 0m ? new Unit(TrailingStopSteps, UnitTypes.PriceStep) : null;

		StartProtection(
			takeProfit: takeProfitUnit,
			stopLoss: stopLossUnit,
			trailingStop: trailingUnit,
			isStopTrailing: TrailingStopSteps > 0m,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal jawValue, decimal teethValue, decimal lipsValue, decimal atrValue)
	{
		UpdateFractalBuffers(candle);
		UpdateMomentum(candle.ClosePrice);
		UpdateRangeBuffers(candle);

		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var jawShifted = UpdateShiftValue(_jawShiftBuffer, jawValue, JawShift);
		var teethShifted = UpdateShiftValue(_teethShiftBuffer, teethValue, TeethShift);
		var lipsShifted = UpdateShiftValue(_lipsShiftBuffer, lipsValue, LipsShift);

		if (!_jaw.IsFormed || !_teeth.IsFormed || !_lips.IsFormed)
			return;

		if (jawShifted is null || teethShifted is null || lipsShifted is null)
			return;

		if (UseAtrFilter && !_atr.IsFormed)
			return;

		if (_momentumFillCount < 3 || _fractalBufferFillCount < 5)
			return;

		var jaw = jawShifted.Value;
		var teeth = teethShifted.Value;
		var lips = lipsShifted.Value;

		DetectFractals();

		var hasUpTrend = lips > teeth && teeth > jaw;
		var hasDownTrend = lips < teeth && teeth < jaw;

		var (momentumUp, momentumDown) = EvaluateMomentum();

		var rangeThreshold = UseAtrFilter ? atrValue * AtrMultiplier : GetStepValue(RangeBoxSteps);
		var rangeOkBuy = rangeThreshold <= 0m || candle.ClosePrice - GetRangeMin() >= rangeThreshold;
		var rangeOkSell = rangeThreshold <= 0m || GetRangeMax() - candle.ClosePrice >= rangeThreshold;

		var buySignal = Position <= 0 && hasUpTrend && momentumUp && rangeOkBuy &&
			_lastUpFractal is decimal upFractal && candle.ClosePrice > upFractal && upFractal > teeth;

		var sellSignal = Position >= 0 && hasDownTrend && momentumDown && rangeOkSell &&
			_lastDownFractal is decimal downFractal && candle.ClosePrice < downFractal && downFractal < teeth;

		if (buySignal)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal at {candle.ClosePrice:F4} (fractal {_lastUpFractal:F4})");
			_lastUpFractal = null;
		}
		else if (sellSignal)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal at {candle.ClosePrice:F4} (fractal {_lastDownFractal:F4})");
			_lastDownFractal = null;
		}

		if (Position > 0 && (!hasUpTrend || (_lastDownFractal is decimal down && candle.LowPrice <= down)))
		{
			SellMarket(Position);
			LogInfo("Exit long due to opposite conditions.");
		}
		else if (Position < 0 && (!hasDownTrend || (_lastUpFractal is decimal up && candle.HighPrice >= up)))
		{
			BuyMarket(-Position);
			LogInfo("Exit short due to opposite conditions.");
		}
	}

	private void UpdateFractalBuffers(ICandleMessage candle)
	{
		for (var i = 0; i < 4; i++)
		{
			_highBuffer[i] = _highBuffer[i + 1];
			_lowBuffer[i] = _lowBuffer[i + 1];
		}

		_highBuffer[4] = candle.HighPrice;
		_lowBuffer[4] = candle.LowPrice;

		if (_fractalBufferFillCount < 5)
			_fractalBufferFillCount++;
	}

	private void UpdateMomentum(decimal closePrice)
	{
		_closeHistory.Enqueue(closePrice);

		while (_closeHistory.Count > MomentumPeriod + 1)
			_closeHistory.Dequeue();

		if (_closeHistory.Count == MomentumPeriod + 1)
		{
			var previous = _closeHistory.Peek();
			if (previous != 0m)
			{
				var ratio = (closePrice / previous) * 100m;
				var diff = Math.Abs(ratio - 100m);
				_momentumAbs[0] = _momentumAbs[1];
				_momentumAbs[1] = _momentumAbs[2];
				_momentumAbs[2] = diff;
				if (_momentumFillCount < 3)
					_momentumFillCount++;
			}
		}
	}

	private void UpdateRangeBuffers(ICandleMessage candle)
	{
		_rangeHighs.Enqueue(candle.HighPrice);
		_rangeLows.Enqueue(candle.LowPrice);

		while (_rangeHighs.Count > RangeLookback)
			_rangeHighs.Dequeue();

		while (_rangeLows.Count > RangeLookback)
			_rangeLows.Dequeue();
	}

	private void DetectFractals()
	{
		if (_fractalBufferFillCount < 5)
			return;

		var middleHigh = _highBuffer[2];
		var middleLow = _lowBuffer[2];

		var isUpFractal = middleHigh > _highBuffer[0] && middleHigh > _highBuffer[1] &&
			middleHigh > _highBuffer[3] && middleHigh > _highBuffer[4];

		if (isUpFractal)
			_lastUpFractal = middleHigh;

		var isDownFractal = middleLow < _lowBuffer[0] && middleLow < _lowBuffer[1] &&
			middleLow < _lowBuffer[3] && middleLow < _lowBuffer[4];

		if (isDownFractal)
			_lastDownFractal = middleLow;
	}

	private (bool momentumUp, bool momentumDown) EvaluateMomentum()
	{
		var momentumUp = false;
		var momentumDown = false;

		for (var i = 0; i < _momentumAbs.Length; i++)
		{
			var value = _momentumAbs[i];
			if (value >= MomentumBuyThreshold)
				momentumUp = true;
			if (value >= MomentumSellThreshold)
				momentumDown = true;
		}

		return (momentumUp, momentumDown);
	}

	private decimal? UpdateShiftValue(Queue<decimal> buffer, decimal value, int shift)
	{
		buffer.Enqueue(value);
		var maxCount = shift + 1;
		while (buffer.Count > maxCount)
			buffer.Dequeue();

		if (buffer.Count < maxCount)
			return null;

		return buffer.Peek();
	}

	private decimal GetRangeMin()
	{
		var min = decimal.MaxValue;
		foreach (var value in _rangeLows)
		{
			if (value < min)
				min = value;
		}
		return min == decimal.MaxValue ? 0m : min;
	}

	private decimal GetRangeMax()
	{
		var max = decimal.MinValue;
		foreach (var value in _rangeHighs)
		{
			if (value > max)
				max = value;
		}
		return max == decimal.MinValue ? 0m : max;
	}

	private decimal GetStepValue(decimal steps)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return steps;
		return steps * step;
	}
}
