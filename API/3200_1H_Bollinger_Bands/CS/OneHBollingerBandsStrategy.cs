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
/// Higher timeframe Bollinger Bands bounce strategy for the H1 chart.
/// Combines trend filters from linear weighted moving averages with
/// momentum confirmation, daily Bollinger Bands bounces, and a
/// monthly MACD direction filter.
/// Includes protective stop-loss, take-profit, trailing stop and
/// break-even management expressed in pips.
/// </summary>
public class OneHBollingerBandsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherTimeFrame;
	private readonly StrategyParam<DataType> _macdTimeFrame;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _trendFastPeriod;
	private readonly StrategyParam<int> _trendSlowPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private WeightedMovingAverage _trendFastMa = null!;
	private WeightedMovingAverage _trendSlowMa = null!;
	private BollingerBands _bollinger = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal _fastMaValue;
	private decimal _slowMaValue;
	private decimal _trendFastValue;
	private decimal _trendSlowValue;

	private readonly Queue<(decimal Open, decimal High, decimal Low)> _higherHistory = new();
	private readonly Queue<decimal> _momentumDeviationHistory = new();

	private decimal _lowerBand;
	private decimal _upperBand;
	private decimal _middleBand;
	private bool _hasBands;

	private bool _macdReady;
	private decimal _macdValue;
	private decimal _macdSignal;

	private decimal? _entryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private decimal? _longBreakEvenPrice;
	private decimal? _shortBreakEvenPrice;

	/// <summary>
	/// Initializes strategy parameters with defaults taken from the original MQL implementation.
	/// </summary>
	public OneHBollingerBandsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Base Timeframe", "Primary timeframe for signal evaluation", "General");

		_higherTimeFrame = Param(nameof(HigherTimeFrame), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Higher Timeframe", "Higher timeframe used for Bollinger Bands and momentum", "Trend");

		_macdTimeFrame = Param(nameof(MacdTimeFrame), TimeSpan.FromDays(30).TimeFrame())
		.SetDisplay("MACD Timeframe", "Macro timeframe for MACD confirmation", "Trend");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Length of the fast LWMA", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(4, 12, 2);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Length of the slow LWMA", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(50, 120, 5);

		_trendFastPeriod = Param(nameof(TrendFastPeriod), 250)
		.SetGreaterThanZero()
		.SetDisplay("Trend Fast LWMA", "Fast trend filter period", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(150, 300, 25);

		_trendSlowPeriod = Param(nameof(TrendSlowPeriod), 500)
		.SetGreaterThanZero()
		.SetDisplay("Trend Slow LWMA", "Slow trend filter period", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(300, 600, 25);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Length", "Period for momentum deviation", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(10, 20, 2);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Threshold", "Minimum deviation from 100 required", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(0.2m, 0.8m, 0.1m);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Length of the Bollinger Bands", "Volatility")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 2);

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Width", "Standard deviation multiplier", "Volatility")
		.SetCanOptimize(true)
		.SetOptimize(1.5m, 3m, 0.25m);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Default trade volume per entry", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 2m, 0.1m);

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Target profit distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(30m, 80m, 5m);

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Use Trailing Stop", "Enable trailing stop management", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop (pips)", "Distance of the trailing stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20m, 60m, 5m);

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Use Break-Even", "Enable automatic break-even move", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Break-Even Trigger", "Profit in pips required to arm break-even", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20m, 50m, 5m);

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Break-Even Offset", "Offset applied when moving to break-even", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);
	}

	/// <summary>
	/// Base timeframe used for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for Bollinger Bands and momentum filters.
	/// </summary>
	public DataType HigherTimeFrame
	{
		get => _higherTimeFrame.Value;
		set => _higherTimeFrame.Value = value;
	}

	/// <summary>
	/// Macro timeframe used for MACD direction confirmation.
	/// </summary>
	public DataType MacdTimeFrame
	{
		get => _macdTimeFrame.Value;
		set => _macdTimeFrame.Value = value;
	}

	/// <summary>
	/// Length of the fast LWMA applied to typical price.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow LWMA applied to typical price.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the fast trend LWMA filter.
	/// </summary>
	public int TrendFastPeriod
	{
		get => _trendFastPeriod.Value;
		set => _trendFastPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slow trend LWMA filter.
	/// </summary>
	public int TrendSlowPeriod
	{
		get => _trendSlowPeriod.Value;
		set => _trendSlowPeriod.Value = value;
	}

	/// <summary>
	/// Momentum lookback length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Required absolute momentum deviation from 100.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Length of the Bollinger Bands.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Width (standard deviation multiplier) of the Bollinger Bands.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// Default trade volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
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
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables or disables trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enables or disables break-even logic.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Profit in pips required before the stop is moved to break-even.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Offset applied when moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherTimeFrame), (Security, MacdTimeFrame)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_higherHistory.Clear();
		_momentumDeviationHistory.Clear();
		_hasBands = false;
		_macdReady = false;
		_entryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_longBreakEvenPrice = null;
		_shortBreakEvenPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };
		_trendFastMa = new WeightedMovingAverage { Length = TrendFastPeriod };
		_trendSlowMa = new WeightedMovingAverage { Length = TrendSlowPeriod };
		_bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerWidth };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription
		.Bind(ProcessBaseCandle)
		.Start();

		var higherSubscription = SubscribeCandles(HigherTimeFrame);
		higherSubscription
		.Bind(_bollinger, _momentum, ProcessHigherCandle)
		.Start();

		var macdSubscription = SubscribeCandles(MacdTimeFrame);
		macdSubscription
		.Bind(_macd, ProcessMacdCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawOwnTrades(area);
		}

		Volume = TradeVolume;
	}

	private void ProcessBaseCandle(ICandleMessage candle)
	{
		var isFinal = candle.State == CandleStates.Finished;
		// Calculate typical price (H+L+C)/3 just like PRICE_TYPICAL in MQL.
		var typical = GetTypicalPrice(candle);

		// Update base timeframe indicators and wait for closed candles.
		_fastMaValue = _fastMa.Process(typical, candle.OpenTime, isFinal).ToDecimal();
		_slowMaValue = _slowMa.Process(typical, candle.OpenTime, isFinal).ToDecimal();
		_trendFastValue = _trendFastMa.Process(typical, candle.OpenTime, isFinal).ToDecimal();
		_trendSlowValue = _trendSlowMa.Process(typical, candle.OpenTime, isFinal).ToDecimal();

		if (!isFinal)
		return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_trendFastMa.IsFormed || !_trendSlowMa.IsFormed)
		return;

		ManageOpenPosition(candle);

		if (!CanGenerateSignal())
		return;

		TryEnter(candle);
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_middleBand = middle;
		_upperBand = upper;
		_lowerBand = lower;
		_hasBands = _bollinger.IsFormed;

		// Keep only the latest higher timeframe candles for the bounce pattern.
		if (_higherHistory.Count == 3)
		_higherHistory.Dequeue();

		_higherHistory.Enqueue((candle.OpenPrice, candle.HighPrice, candle.LowPrice));

		var deviation = Math.Abs(momentumValue - 100m);
		// Maintain a rolling window of momentum deviations to mimic the MQL logic.
		if (_momentumDeviationHistory.Count == 3)
		_momentumDeviationHistory.Dequeue();

		_momentumDeviationHistory.Enqueue(deviation);
	}

	private void ProcessMacdCandle(ICandleMessage candle, decimal macd, decimal signal, decimal histogram)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Store the MACD and signal values from the macro timeframe.
		_macdValue = macd;
		_macdSignal = signal;
		_macdReady = _macd.IsFormed;

		_ = histogram; // Histogram value is not used directly but required by the delegate signature.
	}

	private void TryEnter(ICandleMessage candle)
	{
		// Snapshot higher timeframe candles to evaluate the Bollinger bounce pattern.
		var higherArray = _higherHistory.ToArray();
		var previous = higherArray[higherArray.Length - 1];
		var twoAgo = higherArray.Length >= 2 ? higherArray[higherArray.Length - 2] : previous;

		var longBandSignal = twoAgo.Low < _lowerBand && previous.Open > _lowerBand;
		var shortBandSignal = twoAgo.High > _upperBand && previous.Open < _upperBand;

		var momentumConfirmed = _momentumDeviationHistory.Any(v => v >= MomentumThreshold);
		var macdBullish = _macdValue > _macdSignal;
		var macdBearish = _macdValue < _macdSignal;

		// Combine trend, Bollinger, momentum and MACD for the long setup.
		var bullish = _trendFastValue > _trendSlowValue
		&& longBandSignal
		&& _fastMaValue > _slowMaValue
		&& momentumConfirmed
		&& macdBullish;

		// Mirror conditions define the short setup.
		var bearish = _trendFastValue < _trendSlowValue
		&& shortBandSignal
		&& _fastMaValue < _slowMaValue
		&& momentumConfirmed
		&& macdBearish;

		if (bullish && Position <= 0m)
		{
			var volume = TradeVolume + Math.Abs(Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				RegisterEntry(candle.ClosePrice);
			}
		}
		else if (bearish && Position >= 0m)
		{
			var volume = TradeVolume + Math.Abs(Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				RegisterEntry(candle.ClosePrice);
			}
		}
	}

	private bool CanGenerateSignal()
	{
		// Ensure higher timeframe indicators are ready before trading.
		if (!_hasBands || !_macdReady)
		return false;

		if (_higherHistory.Count < 2)
		return false;

		if (_momentumDeviationHistory.Count < 3)
		return false;

		return TradeVolume > 0m;
	}

	private void RegisterEntry(decimal price)
	{
		_entryPrice = price;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_longBreakEvenPrice = null;
		_shortBreakEvenPrice = null;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_longBreakEvenPrice = null;
		_shortBreakEvenPrice = null;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			ManageLongPosition(candle);
		}
		else if (Position < 0m)
		{
			ManageShortPosition(candle);
		}
		else
		{
			ResetPositionState();
		}
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		// Protect the long position only after a valid entry price is stored.
		if (_entryPrice is not decimal entryPrice)
		return;

		// Convert pip distances into prices using the instrument step.
		var priceStep = Security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var positionVolume = Position;

		if (TakeProfitPips > 0m)
		{
			var target = entryPrice + priceStep * TakeProfitPips;
			if (candle.HighPrice >= target)
			{
				SellMarket(positionVolume);
				ResetPositionState();
				return;
			}
		}

		if (StopLossPips > 0m)
		{
			var stop = entryPrice - priceStep * StopLossPips;
			if (candle.LowPrice <= stop)
			{
				SellMarket(positionVolume);
				ResetPositionState();
				return;
			}
		}

		if (EnableBreakEven && BreakEvenTriggerPips > 0m && !_longBreakEvenPrice.HasValue)
		{
			var trigger = entryPrice + priceStep * BreakEvenTriggerPips;
			if (candle.HighPrice >= trigger)
			_longBreakEvenPrice = entryPrice + priceStep * BreakEvenOffsetPips;
		}

		if (_longBreakEvenPrice.HasValue && candle.LowPrice <= _longBreakEvenPrice.Value)
		{
			SellMarket(positionVolume);
			ResetPositionState();
			return;
		}

		if (EnableTrailing && TrailingStopPips > 0m)
		{
			// Update the trailing stop only when price has moved far enough.
			var distance = priceStep * TrailingStopPips;
			if (distance > 0m && candle.ClosePrice - entryPrice >= distance)
			{
				var candidate = candle.ClosePrice - distance;
				if (!_longTrailingStop.HasValue || candidate > _longTrailingStop.Value)
				_longTrailingStop = candidate;
			}
		}

		if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
		{
			SellMarket(positionVolume);
			ResetPositionState();
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		// Protect the short position only after a valid entry price is stored.
		if (_entryPrice is not decimal entryPrice)
		return;

		// Convert pip distances into prices using the instrument step.
		var priceStep = Security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var positionVolume = Math.Abs(Position);

		if (TakeProfitPips > 0m)
		{
			var target = entryPrice - priceStep * TakeProfitPips;
			if (candle.LowPrice <= target)
			{
				BuyMarket(positionVolume);
				ResetPositionState();
				return;
			}
		}

		if (StopLossPips > 0m)
		{
			var stop = entryPrice + priceStep * StopLossPips;
			if (candle.HighPrice >= stop)
			{
				BuyMarket(positionVolume);
				ResetPositionState();
				return;
			}
		}

		if (EnableBreakEven && BreakEvenTriggerPips > 0m && !_shortBreakEvenPrice.HasValue)
		{
			var trigger = entryPrice - priceStep * BreakEvenTriggerPips;
			if (candle.LowPrice <= trigger)
			_shortBreakEvenPrice = entryPrice - priceStep * BreakEvenOffsetPips;
		}

		if (_shortBreakEvenPrice.HasValue && candle.HighPrice >= _shortBreakEvenPrice.Value)
		{
			BuyMarket(positionVolume);
			ResetPositionState();
			return;
		}

		if (EnableTrailing && TrailingStopPips > 0m)
		{
			// Update the trailing stop only when price has moved far enough.
			var distance = priceStep * TrailingStopPips;
			if (distance > 0m && entryPrice - candle.ClosePrice >= distance)
			{
				var candidate = candle.ClosePrice + distance;
				if (!_shortTrailingStop.HasValue || candidate < _shortTrailingStop.Value)
				_shortTrailingStop = candidate;
			}
		}

		if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
		{
			BuyMarket(positionVolume);
			ResetPositionState();
		}
	}

	private static decimal GetTypicalPrice(ICandleMessage candle)
	{
		// PRICE_TYPICAL = (High + Low + Close) / 3.
		return (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
	}
}

