namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Pin bar reversal strategy converted from the original MQL expert.
/// Combines pin bar detection with higher timeframe momentum and MACD confirmation.
/// </summary>
public class PinbarReversal2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _bodyToRangeRatio;
	private readonly StrategyParam<decimal> _wickRatio;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _breakEvenTriggerPercent;
	private readonly StrategyParam<decimal> _breakEvenOffsetPercent;
	private readonly StrategyParam<decimal> _trailingActivationPercent;
	private readonly StrategyParam<decimal> _trailingDistancePercent;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;
	private Momentum _momentum;
	private MovingAverageConvergenceDivergenceSignal _macd;

	private decimal _latestMomentum;
	private decimal _prevMomentum;
	private decimal _prevMomentum2;

	private decimal _macdValue;
	private decimal _macdSignal;

	private decimal _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private bool _trailingActive;
	private bool _breakEvenActive;
	private decimal _breakEvenPrice;

	private decimal _high0;
	private decimal _high1;
	private decimal _high2;
	private decimal _high3;
	private decimal _high4;
	private decimal _low0;
	private decimal _low1;
	private decimal _low2;
	private decimal _low3;
	private decimal _low4;
	private bool _hasBullishFractal;
	private bool _hasBearishFractal;
	private decimal _bullishFractalPrice;
	private decimal _bearishFractalPrice;
	private int _barCount;

	/// <summary>
	/// Initialize <see cref="PinbarReversal2Strategy"/>.
	/// </summary>
	public PinbarReversal2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Primary Candle", "Candle type used for entries", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Trend Candle", "Higher timeframe candle for momentum", "General");

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA", "Fast moving average length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA", "Slow moving average length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(40, 150, 5);

		_momentumLength = Param(nameof(MomentumLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Length", "Bars for momentum calculation", "Indicators");

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.1m)
		.SetDisplay("Momentum Threshold", "Minimum momentum for confirmation", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 0.5m, 0.05m);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal length for MACD", "Indicators");

		_bodyToRangeRatio = Param(nameof(BodyToRangeRatio), 0.3m)
		.SetDisplay("Body Ratio", "Maximum body size relative to range", "Pattern")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 0.5m, 0.05m);

		_wickRatio = Param(nameof(WickRatio), 0.6m)
		.SetDisplay("Wick Ratio", "Minimum dominant wick size", "Pattern")
		.SetCanOptimize(true)
		.SetOptimize(0.4m, 0.8m, 0.05m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
		.SetDisplay("Stop Loss %", "Protective stop in percent", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
		.SetDisplay("Take Profit %", "Target profit in percent", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 8m, 0.5m);

		_breakEvenTriggerPercent = Param(nameof(BreakEvenTriggerPercent), 1.5m)
		.SetDisplay("Break-even Trigger %", "Profit required to move stop to break-even", "Risk");

		_breakEvenOffsetPercent = Param(nameof(BreakEvenOffsetPercent), 0.2m)
		.SetDisplay("Break-even Offset %", "Extra margin added to break-even stop", "Risk");

		_trailingActivationPercent = Param(nameof(TrailingActivationPercent), 2.5m)
		.SetDisplay("Trailing Activation %", "Profit required before enabling trailing", "Risk");

		_trailingDistancePercent = Param(nameof(TrailingDistancePercent), 1m)
		.SetDisplay("Trailing Distance %", "Distance of trailing stop", "Risk");

		Volume = 1;
	}

	/// <summary>
	/// Primary candle type used for entries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle used for momentum and MACD filters.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum lookback length.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Minimum absolute momentum required for entries.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal smoothing length for MACD.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Maximum body size relative to candle range.
	/// </summary>
	public decimal BodyToRangeRatio
	{
		get => _bodyToRangeRatio.Value;
		set => _bodyToRangeRatio.Value = value;
	}

	/// <summary>
	/// Minimum wick size that defines the pin bar tail.
	/// </summary>
	public decimal WickRatio
	{
		get => _wickRatio.Value;
		set => _wickRatio.Value = value;
	}

	/// <summary>
	/// Stop loss size expressed in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit size expressed in percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Profit threshold to enable break-even stop.
	/// </summary>
	public decimal BreakEvenTriggerPercent
	{
		get => _breakEvenTriggerPercent.Value;
		set => _breakEvenTriggerPercent.Value = value;
	}

	/// <summary>
	/// Additional offset applied to break-even stop level.
	/// </summary>
	public decimal BreakEvenOffsetPercent
	{
		get => _breakEvenOffsetPercent.Value;
		set => _breakEvenOffsetPercent.Value = value;
	}

	/// <summary>
	/// Profit threshold to activate trailing stop.
	/// </summary>
	public decimal TrailingActivationPercent
	{
		get => _trailingActivationPercent.Value;
		set => _trailingActivationPercent.Value = value;
	}

	/// <summary>
	/// Distance of trailing stop in percent.
	/// </summary>
	public decimal TrailingDistancePercent
	{
		get => _trailingDistancePercent.Value;
		set => _trailingDistancePercent.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (!Equals(CandleType, TrendCandleType))
		yield return (Security, TrendCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMa = null;
		_slowMa = null;
		_momentum = null;
		_macd = null;

		_latestMomentum = 0m;
		_prevMomentum = 0m;
		_prevMomentum2 = 0m;

		_macdValue = 0m;
		_macdSignal = 0m;

		ResetPositionState();

		_high0 = _high1 = _high2 = _high3 = _high4 = 0m;
		_low0 = _low1 = _low2 = _low3 = _low4 = 0m;
		_hasBullishFractal = false;
		_hasBearishFractal = false;
		_bullishFractalPrice = 0m;
		_bearishFractalPrice = 0m;
		_barCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create primary timeframe indicators.
		_fastMa = new ExponentialMovingAverage { Length = FastMaLength };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaLength };

		// Subscribe to primary candles.
		var primarySubscription = SubscribeCandles(CandleType);
		primarySubscription
		.Bind(_fastMa, _slowMa, ProcessPrimaryCandle)
		.Start();

		// Create higher timeframe indicators.
		_momentum = new Momentum { Length = MomentumLength };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength }
			},
			SignalMa = { Length = MacdSignalLength }
		};

		// Subscribe to higher timeframe candles.
		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription
		.Bind(_momentum, ProcessTrendMomentum)
		.BindEx(_macd, ProcessTrendMacd)
		.Start();

		// Optional chart visualization.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
		}
	}

	private void ProcessTrendMomentum(ICandleMessage candle, decimal momentumValue)
	{
		// Store momentum values from the higher timeframe.
		if (candle.State != CandleStates.Finished)
		return;

		_prevMomentum2 = _prevMomentum;
		_prevMomentum = _latestMomentum;
		_latestMomentum = momentumValue;
	}

	private void ProcessTrendMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		// Update MACD state once the higher timeframe candle is closed.
		if (candle.State != CandleStates.Finished || !macdValue.IsFinal)
		return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		_macdValue = typed.Macd;
		_macdSignal = typed.Signal;
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal fastMa, decimal slowMa)
	{
		// Work only with finished candles.
		if (candle.State != CandleStates.Finished)
		return;

		UpdateFractals(candle);

		// Manage active positions before looking for new signals.
		if (ManagePosition(candle))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_fastMa == null || _slowMa == null || _momentum == null || _macd == null)
		return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_momentum.IsFormed || !_macd.IsFormed)
		return;

		if (_barCount < 5)
		return;

		var bullishPinbar = IsBullishPinbar(candle);
		var bearishPinbar = IsBearishPinbar(candle);

		var bullishTrend = fastMa > slowMa;
		var bearishTrend = fastMa < slowMa;

		var momentumThreshold = MomentumThreshold;
		var bullishMomentum = _latestMomentum >= momentumThreshold || _prevMomentum >= momentumThreshold || _prevMomentum2 >= momentumThreshold;
		var bearishMomentum = _latestMomentum <= -momentumThreshold || _prevMomentum <= -momentumThreshold || _prevMomentum2 <= -momentumThreshold;

		var macdAbove = _macdValue > _macdSignal;
		var macdBelow = _macdValue < _macdSignal;

		if (Position <= 0 && bullishPinbar && bullishTrend && bullishMomentum && macdAbove && _hasBullishFractal)
		{
			EnterLong(candle.ClosePrice);
			return;
		}

		if (Position >= 0 && bearishPinbar && bearishTrend && bearishMomentum && macdBelow && _hasBearishFractal)
		{
			EnterShort(candle.ClosePrice);
		}
	}

	private void EnterLong(decimal price)
	{
		// Open or reverse into a long position.
		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);

		_entryPrice = price;
		_highestSinceEntry = price;
		_lowestSinceEntry = price;
		_trailingActive = false;
		_breakEvenActive = false;
		_breakEvenPrice = 0m;
		_hasBullishFractal = false;
	}

	private void EnterShort(decimal price)
	{
		// Open or reverse into a short position.
		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);

		_entryPrice = price;
		_highestSinceEntry = price;
		_lowestSinceEntry = price;
		_trailingActive = false;
		_breakEvenActive = false;
		_breakEvenPrice = 0m;
		_hasBearishFractal = false;
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = _lowestSinceEntry == 0m ? candle.LowPrice : Math.Min(_lowestSinceEntry, candle.LowPrice);

			var stopLossPrice = StopLossPercent > 0m ? _entryPrice * (1m - StopLossPercent / 100m) : decimal.MinValue;
			if (StopLossPercent > 0m && candle.LowPrice <= stopLossPrice)
			return ExitLong();

			var takeProfitPrice = TakeProfitPercent > 0m ? _entryPrice * (1m + TakeProfitPercent / 100m) : decimal.MaxValue;
			if (TakeProfitPercent > 0m && candle.HighPrice >= takeProfitPrice)
			return ExitLong();

			if (!_breakEvenActive && BreakEvenTriggerPercent > 0m && candle.HighPrice >= _entryPrice * (1m + BreakEvenTriggerPercent / 100m))
			{
				_breakEvenActive = true;
				_breakEvenPrice = _entryPrice * (1m + BreakEvenOffsetPercent / 100m);
			}

			if (_breakEvenActive && candle.LowPrice <= _breakEvenPrice)
			return ExitLong();

			if (!_trailingActive && TrailingActivationPercent > 0m && _highestSinceEntry >= _entryPrice * (1m + TrailingActivationPercent / 100m))
			_trailingActive = true;

			if (_trailingActive && TrailingDistancePercent > 0m)
			{
				var trailingPrice = _highestSinceEntry * (1m - TrailingDistancePercent / 100m);
				if (candle.ClosePrice <= trailingPrice)
				return ExitLong();
			}
		}
		else if (Position < 0)
		{
			_highestSinceEntry = _highestSinceEntry == 0m ? candle.HighPrice : Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry == 0m ? candle.LowPrice : _lowestSinceEntry, candle.LowPrice);

			var stopLossPrice = StopLossPercent > 0m ? _entryPrice * (1m + StopLossPercent / 100m) : decimal.MaxValue;
			if (StopLossPercent > 0m && candle.HighPrice >= stopLossPrice)
			return ExitShort();

			var takeProfitPrice = TakeProfitPercent > 0m ? _entryPrice * (1m - TakeProfitPercent / 100m) : decimal.MinValue;
			if (TakeProfitPercent > 0m && candle.LowPrice <= takeProfitPrice)
			return ExitShort();

			if (!_breakEvenActive && BreakEvenTriggerPercent > 0m && candle.LowPrice <= _entryPrice * (1m - BreakEvenTriggerPercent / 100m))
			{
				_breakEvenActive = true;
				_breakEvenPrice = _entryPrice * (1m - BreakEvenOffsetPercent / 100m);
			}

			if (_breakEvenActive && candle.HighPrice >= _breakEvenPrice)
			return ExitShort();

			if (!_trailingActive && TrailingActivationPercent > 0m && _lowestSinceEntry <= _entryPrice * (1m - TrailingActivationPercent / 100m))
			_trailingActive = true;

			if (_trailingActive && TrailingDistancePercent > 0m)
			{
				var trailingPrice = _lowestSinceEntry * (1m + TrailingDistancePercent / 100m);
				if (candle.ClosePrice >= trailingPrice)
				return ExitShort();
			}
		}
		else
		{
			ResetPositionState();
		}

		return false;
	}

	private bool ExitLong()
	{
		var volume = Position;
		if (volume <= 0m)
		return false;

		SellMarket(volume);
		ResetPositionState();
		return true;
	}

	private bool ExitShort()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return false;

		BuyMarket(volume);
		ResetPositionState();
		return true;
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_trailingActive = false;
		_breakEvenActive = false;
		_breakEvenPrice = 0m;
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		// Shift historical buffers to keep the last five highs and lows.
		_high4 = _high3;
		_high3 = _high2;
		_high2 = _high1;
		_high1 = _high0;
		_high0 = candle.HighPrice;

		_low4 = _low3;
		_low3 = _low2;
		_low2 = _low1;
		_low1 = _low0;
		_low0 = candle.LowPrice;

		if (_barCount >= 4)
		{
			var middleLow = _low2;
			var isBullishFractal = middleLow < _low0 && middleLow < _low1 && middleLow < _low3 && middleLow < _low4;
			if (isBullishFractal)
			{
				_hasBullishFractal = true;
				_bullishFractalPrice = middleLow;
			}

			var middleHigh = _high2;
			var isBearishFractal = middleHigh > _high0 && middleHigh > _high1 && middleHigh > _high3 && middleHigh > _high4;
			if (isBearishFractal)
			{
				_hasBearishFractal = true;
				_bearishFractalPrice = middleHigh;
			}
		}

		// Reset fractal confirmation if price moves through the level.
		if (_hasBullishFractal && candle.LowPrice < _bullishFractalPrice)
		_hasBullishFractal = false;

		if (_hasBearishFractal && candle.HighPrice > _bearishFractalPrice)
		_hasBearishFractal = false;

		_barCount++;
	}

	private bool IsBullishPinbar(ICandleMessage candle)
	{
		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0m)
		return false;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		if (body > range * BodyToRangeRatio)
		return false;

		var bodyHigh = Math.Max(candle.OpenPrice, candle.ClosePrice);
		var bodyLow = Math.Min(candle.OpenPrice, candle.ClosePrice);
		var lowerWick = bodyLow - candle.LowPrice;
		var upperWick = candle.HighPrice - bodyHigh;

		return lowerWick >= range * WickRatio && upperWick <= range * (1m - WickRatio);
	}

	private bool IsBearishPinbar(ICandleMessage candle)
	{
		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0m)
		return false;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		if (body > range * BodyToRangeRatio)
		return false;

		var bodyHigh = Math.Max(candle.OpenPrice, candle.ClosePrice);
		var bodyLow = Math.Min(candle.OpenPrice, candle.ClosePrice);
		var upperWick = candle.HighPrice - bodyHigh;
		var lowerWick = bodyLow - candle.LowPrice;

		return upperWick >= range * WickRatio && lowerWick <= range * (1m - WickRatio);
	}
}
