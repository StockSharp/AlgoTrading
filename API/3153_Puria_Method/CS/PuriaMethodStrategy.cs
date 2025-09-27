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
/// Puria method trend-following strategy converted from MetaTrader.
/// It combines three moving averages with a MACD trend confirmation and optional risk management features.
/// </summary>
public class PuriaMethodStrategy : Strategy
{
	public enum CandlePrices
	{
		/// <summary>
		/// Open price.
		/// </summary>
		Open,
		/// <summary>
		/// High price.
		/// </summary>
		High,
		/// <summary>
		/// Low price.
		/// </summary>
		Low,
		/// <summary>
		/// Close price.
		/// </summary>
		Close,
		/// <summary>
		/// Median price (HL/2).
		/// </summary>
		Median,
		/// <summary>
		/// Typical price (HLC/3).
		/// </summary>
		Typical,
		/// <summary>
		/// Weighted close price (HLCC/4).
		/// </summary>
		WClose
	}

	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _minProfitStepPips;
	private readonly StrategyParam<decimal> _minProfitFraction;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _epsilon;

	private readonly StrategyParam<int> _ma0Period;
	private readonly StrategyParam<int> _ma0Shift;
	private readonly StrategyParam<MaMethods> _ma0Method;
	private readonly StrategyParam<CandlePrices> _ma0Price;

	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma1Shift;
	private readonly StrategyParam<MaMethods> _ma1Method;
	private readonly StrategyParam<CandlePrices> _ma1Price;

	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<int> _ma2Shift;
	private readonly StrategyParam<MaMethods> _ma2Method;
	private readonly StrategyParam<CandlePrices> _ma2Price;

	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _macdTrendBars;
	private readonly StrategyParam<CandlePrices> _macdPrice;

	private LengthIndicator<decimal> _ma0 = null!;
	private LengthIndicator<decimal> _ma1 = null!;
	private LengthIndicator<decimal> _ma2 = null!;
	private Shift? _ma0ShiftIndicator;
	private Shift? _ma1ShiftIndicator;
	private Shift? _ma2ShiftIndicator;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal? _previousMa0;
	private decimal? _previousMa1;
	private decimal? _previousMa2;
	private decimal? _previousMacd;

	private readonly LinkedList<decimal> _macdHistory = new();

	private decimal _pipSize;
	private decimal _pointSize;

	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal? _longHighestPrice;
	private decimal? _shortLowestPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="PuriaMethodStrategy"/>.
	/// </summary>
	public PuriaMethodStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 150m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 45m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step", "Minimum advance before trailing moves", "Risk");

		_minProfitStepPips = Param(nameof(MinProfitStepPips), 100m)
			.SetNotNegative()
			.SetDisplay("Min Profit Step", "Distance in pips before partial exit", "Risk");

		_minProfitFraction = Param(nameof(MinProfitFraction), 0.5m)
			.SetNotNegative()
			.SetDisplay("Partial Exit Ratio", "Fraction of position to reduce on profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "Data");

		_epsilon = Param(nameof(Epsilon), 0.0000001m)
			.SetGreaterThanZero()
			.SetDisplay("Comparison Tolerance", "Minimum difference considered significant in price/volume checks", "Advanced");

		_ma0Period = Param(nameof(Ma0Period), 69)
			.SetGreaterThanZero()
			.SetDisplay("MA 0 Period", "First MA period", "Indicators");

		_ma0Shift = Param(nameof(Ma0Shift), 0)
			.SetNotNegative()
			.SetDisplay("MA 0 Shift", "Bars to shift the first MA", "Indicators");

		_ma0Method = Param(nameof(Ma0Method), MaMethods.Smoothed)
			.SetDisplay("MA 0 Method", "Smoothing for first MA", "Indicators");

		_ma0Price = Param(nameof(Ma0Price), CandlePrices.High)
			.SetDisplay("MA 0 Price", "Price source for first MA", "Indicators");

		_ma1Period = Param(nameof(Ma1Period), 74)
			.SetGreaterThanZero()
			.SetDisplay("MA 1 Period", "Second MA period", "Indicators");

		_ma1Shift = Param(nameof(Ma1Shift), 0)
			.SetNotNegative()
			.SetDisplay("MA 1 Shift", "Bars to shift the second MA", "Indicators");

		_ma1Method = Param(nameof(Ma1Method), MaMethods.Smoothed)
			.SetDisplay("MA 1 Method", "Smoothing for second MA", "Indicators");

		_ma1Price = Param(nameof(Ma1Price), CandlePrices.High)
			.SetDisplay("MA 1 Price", "Price source for second MA", "Indicators");

		_ma2Period = Param(nameof(Ma2Period), 19)
			.SetGreaterThanZero()
			.SetDisplay("MA 2 Period", "Third MA period", "Indicators");

		_ma2Shift = Param(nameof(Ma2Shift), 0)
			.SetNotNegative()
			.SetDisplay("MA 2 Shift", "Bars to shift the third MA", "Indicators");

		_ma2Method = Param(nameof(Ma2Method), MaMethods.Exponential)
			.SetDisplay("MA 2 Method", "Smoothing for third MA", "Indicators");

		_ma2Price = Param(nameof(Ma2Price), CandlePrices.Open)
			.SetDisplay("MA 2 Price", "Price source for third MA", "Indicators");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 17)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 38)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal smoothing period", "Indicators");

		_macdTrendBars = Param(nameof(MacdTrendBars), 8)
			.SetNotNegative()
			.SetDisplay("MACD Trend Bars", "Number of bars for MACD trend check", "Indicators");

		_macdPrice = Param(nameof(MacdPrice), CandlePrices.Open)
			.SetDisplay("MACD Price", "Price source for MACD", "Indicators");
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
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal advance in pips before the trailing stop is shifted.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Minimum profit distance before performing a partial exit.
	/// </summary>
	public decimal MinProfitStepPips
	{
		get => _minProfitStepPips.Value;
		set => _minProfitStepPips.Value = value;
	}

	/// <summary>
	/// Fraction of the current position to close when the minimum profit condition is met.
	/// </summary>
	public decimal MinProfitFraction
	{
		get => _minProfitFraction.Value;
		set => _minProfitFraction.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Numerical tolerance applied when comparing prices and volumes.
	/// </summary>
	public decimal Epsilon
	{
		get => _epsilon.Value;
		set => _epsilon.Value = value;
	}

	/// <summary>
	/// Period for the first moving average.
	/// </summary>
	public int Ma0Period
	{
		get => _ma0Period.Value;
		set => _ma0Period.Value = value;
	}

	/// <summary>
	/// Shift in bars for the first moving average.
	/// </summary>
	public int Ma0Shift
	{
		get => _ma0Shift.Value;
		set => _ma0Shift.Value = value;
	}

	/// <summary>
	/// Method used to smooth the first moving average.
	/// </summary>
	public MaMethods Ma0Method
	{
		get => _ma0Method.Value;
		set => _ma0Method.Value = value;
	}

	/// <summary>
	/// Price source for the first moving average.
	/// </summary>
	public CandlePrices Ma0Price
	{
		get => _ma0Price.Value;
		set => _ma0Price.Value = value;
	}

	/// <summary>
	/// Period for the second moving average.
	/// </summary>
	public int Ma1Period
	{
		get => _ma1Period.Value;
		set => _ma1Period.Value = value;
	}

	/// <summary>
	/// Shift in bars for the second moving average.
	/// </summary>
	public int Ma1Shift
	{
		get => _ma1Shift.Value;
		set => _ma1Shift.Value = value;
	}

	/// <summary>
	/// Method used to smooth the second moving average.
	/// </summary>
	public MaMethods Ma1Method
	{
		get => _ma1Method.Value;
		set => _ma1Method.Value = value;
	}

	/// <summary>
	/// Price source for the second moving average.
	/// </summary>
	public CandlePrices Ma1Price
	{
		get => _ma1Price.Value;
		set => _ma1Price.Value = value;
	}

	/// <summary>
	/// Period for the third moving average.
	/// </summary>
	public int Ma2Period
	{
		get => _ma2Period.Value;
		set => _ma2Period.Value = value;
	}

	/// <summary>
	/// Shift in bars for the third moving average.
	/// </summary>
	public int Ma2Shift
	{
		get => _ma2Shift.Value;
		set => _ma2Shift.Value = value;
	}

	/// <summary>
	/// Method used to smooth the third moving average.
	/// </summary>
	public MaMethods Ma2Method
	{
		get => _ma2Method.Value;
		set => _ma2Method.Value = value;
	}

	/// <summary>
	/// Price source for the third moving average.
	/// </summary>
	public CandlePrices Ma2Price
	{
		get => _ma2Price.Value;
		set => _ma2Price.Value = value;
	}

	/// <summary>
	/// Fast period for the MACD indicator.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow period for the MACD indicator.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for the MACD indicator.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars used to confirm a MACD trend.
	/// </summary>
	public int MacdTrendBars
	{
		get => _macdTrendBars.Value;
		set => _macdTrendBars.Value = value;
	}

	/// <summary>
	/// Price source for the MACD calculation.
	/// </summary>
	public CandlePrices MacdPrice
	{
		get => _macdPrice.Value;
		set => _macdPrice.Value = value;
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

		_previousMa0 = null;
		_previousMa1 = null;
		_previousMa2 = null;
		_previousMacd = null;
		_macdHistory.Clear();

		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfitPrice = null;
		_shortTakeProfitPrice = null;
		_longHighestPrice = null;
		_shortLowestPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma0 = CreateMovingAverage(Ma0Method, Ma0Period, Ma0Price);
		_ma1 = CreateMovingAverage(Ma1Method, Ma1Period, Ma1Price);
		_ma2 = CreateMovingAverage(Ma2Method, Ma2Period, Ma2Price);

		_ma0ShiftIndicator = Ma0Shift > 0 ? new Shift { Length = Ma0Shift } : null;
		_ma1ShiftIndicator = Ma1Shift > 0 ? new Shift { Length = Ma1Shift } : null;
		_ma2ShiftIndicator = Ma2Shift > 0 ? new Shift { Length = Ma2Shift } : null;

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = Math.Max(1, MacdFastPeriod), CandlePrice = MacdPrice },
				LongMa = { Length = Math.Max(1, MacdSlowPeriod), CandlePrice = MacdPrice },
			},
			SignalMa = { Length = Math.Max(1, MacdSignalPeriod) }
		};

		_pipSize = CalculatePipSize();
		_pointSize = CalculatePointSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma0);
			DrawIndicator(area, _ma1);
			DrawIndicator(area, _ma2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Process moving averages for the current candle.
		var ma0 = ProcessMovingAverage(_ma0, _ma0ShiftIndicator, candle);
		var ma1 = ProcessMovingAverage(_ma1, _ma1ShiftIndicator, candle);
		var ma2 = ProcessMovingAverage(_ma2, _ma2ShiftIndicator, candle);

		// Evaluate MACD and keep the auxiliary history in sync.
		var macdValue = _macd.Process(candle);
		if (!macdValue.IsFinal)
		{
			StorePreviousValues(ma0, ma1, ma2, null);
			return;
		}

		var macdSignal = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdCurrent = macdSignal.Macd;

		// Store the latest MACD value to validate monotonic trends.
		UpdateMacdHistory(macdCurrent);

		var previousMacd = _previousMacd;
		_previousMacd = macdCurrent;

		// Skip trading until all three averages provide valid data.
		if (ma0 == null || ma1 == null || ma2 == null)
		{
			StorePreviousValues(ma0, ma1, ma2, previousMacd);
			return;
		}

		if (_previousMa0 == null || _previousMa1 == null || _previousMa2 == null || previousMacd == null)
		{
			StorePreviousValues(ma0, ma1, ma2, previousMacd);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			StorePreviousValues(ma0, ma1, ma2, previousMacd);
			return;
		}

		// Apply trailing stops and partial exits before making new decisions.
		ManageOpenPosition(candle);

		var prevMa0 = _previousMa0.Value;
		var prevMa1 = _previousMa1.Value;
		var prevMa2 = _previousMa2.Value;

		var maPoint = _pointSize > 0m ? _pointSize : CalculatePointSize();
		if (maPoint <= 0m)
		{
			StorePreviousValues(ma0, ma1, ma2, previousMacd);
			return;
		}

		var maBuy = (prevMa1 - prevMa0) / maPoint > 0.5m && (prevMa2 - prevMa0) / maPoint > 0.5m;
		var maSell = (prevMa0 - prevMa1) / maPoint > 0.5m && (prevMa0 - prevMa2) / maPoint > 0.5m;

		var macdTrendLength = GetRequiredMacdCount();
		var macdUp = previousMacd > 0m && HasMacdTrend(upward: true, macdTrendLength);
		var macdDown = previousMacd < 0m && HasMacdTrend(upward: false, macdTrendLength);

		var buySignal = maBuy && macdUp;
		var sellSignal = maSell && macdDown;

		// Ignore ambiguous situations where both directions trigger simultaneously.
		if (buySignal && sellSignal)
		{
			StorePreviousValues(ma0, ma1, ma2, previousMacd);
			return;
		}

		if (buySignal && Position <= 0m)
		{
			var volume = Volume;
			if (Position < 0m)
				volume += Math.Abs(Position);

			if (volume > 0m)
				BuyMarket(volume);
		}
		else if (sellSignal && Position >= 0m)
		{
			var volume = Volume;
			if (Position > 0m)
				volume += Position;

			if (volume > 0m)
				SellMarket(volume);
		}

		// Persist indicator values for the next candle.
		StorePreviousValues(ma0, ma1, ma2, previousMacd);
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		// Manage a long position by trailing and locking profits.
		if (Position > 0m)
		{
			_longHighestPrice = _longHighestPrice.HasValue ? Math.Max(_longHighestPrice.Value, candle.HighPrice) : candle.HighPrice;
			_longStopPrice ??= StopLossPips > 0m ? NormalizePrice(entryPrice - StopLossPips * _pipSize) : null;
			_longTakeProfitPrice ??= TakeProfitPips > 0m ? NormalizePrice(entryPrice + TakeProfitPips * _pipSize) : null;

			ApplyTrailingForLong(candle, entryPrice);
			TryPartialExitForLong(candle, volume, entryPrice);

			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(volume);
				ResetLongState();
				return;
			}

			if (_longTakeProfitPrice.HasValue && candle.HighPrice >= _longTakeProfitPrice.Value)
			{
				SellMarket(volume);
				ResetLongState();
			}
		}
		// Manage a short position using mirrored logic.
		else
		{
			_shortLowestPrice = _shortLowestPrice.HasValue ? Math.Min(_shortLowestPrice.Value, candle.LowPrice) : candle.LowPrice;
			_shortStopPrice ??= StopLossPips > 0m ? NormalizePrice(entryPrice + StopLossPips * _pipSize) : null;
			_shortTakeProfitPrice ??= TakeProfitPips > 0m ? NormalizePrice(entryPrice - TakeProfitPips * _pipSize) : null;

			ApplyTrailingForShort(candle, entryPrice);
			TryPartialExitForShort(candle, volume, entryPrice);

			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(volume);
				ResetShortState();
				return;
			}

			if (_shortTakeProfitPrice.HasValue && candle.LowPrice <= _shortTakeProfitPrice.Value)
			{
				BuyMarket(volume);
				ResetShortState();
			}
		}
	}

	private void ApplyTrailingForLong(ICandleMessage candle, decimal entryPrice)
	{
		// Trailing is disabled unless both distance and step are positive.
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || _pipSize <= 0m)
			return;

		// Convert pip settings to absolute prices.
		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var requiredMove = trailingDistance + trailingStep;

		// Wait until the price advances enough before shifting the stop.
		var profit = candle.ClosePrice - entryPrice;
		if (profit < requiredMove)
			return;

		var desiredStop = NormalizePrice(candle.ClosePrice - trailingDistance);

		if (!_longStopPrice.HasValue || desiredStop - _longStopPrice.Value > trailingStep - Epsilon)
			_longStopPrice = desiredStop;
	}

	private void ApplyTrailingForShort(ICandleMessage candle, decimal entryPrice)
	{
		// Trailing is disabled unless both distance and step are positive.
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || _pipSize <= 0m)
			return;

		// Convert pip settings to absolute prices.
		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var requiredMove = trailingDistance + trailingStep;

		// Wait until the price advances enough before shifting the stop.
		var profit = entryPrice - candle.ClosePrice;
		if (profit < requiredMove)
			return;

		var desiredStop = NormalizePrice(candle.ClosePrice + trailingDistance);

		if (!_shortStopPrice.HasValue || _shortStopPrice.Value - desiredStop > trailingStep - Epsilon)
			_shortStopPrice = desiredStop;
	}

	private void TryPartialExitForLong(ICandleMessage candle, decimal volume, decimal entryPrice)
	{
		if (MinProfitStepPips <= 0m || MinProfitFraction <= 0m || _pipSize <= 0m)
			return;

		var minDistance = MinProfitStepPips * _pipSize;
		if (candle.ClosePrice - entryPrice < minDistance)
			return;

		// Adjust the partial exit volume to exchange constraints.
		var exitVolume = AdjustVolume(volume * MinProfitFraction);
		if (exitVolume <= 0m || exitVolume >= volume - Epsilon)
			return;

		SellMarket(exitVolume);
	}

	private void TryPartialExitForShort(ICandleMessage candle, decimal volume, decimal entryPrice)
	{
		if (MinProfitStepPips <= 0m || MinProfitFraction <= 0m || _pipSize <= 0m)
			return;

		var minDistance = MinProfitStepPips * _pipSize;
		if (entryPrice - candle.ClosePrice < minDistance)
			return;

		// Adjust the partial exit volume to exchange constraints.
		var exitVolume = AdjustVolume(volume * MinProfitFraction);
		if (exitVolume <= 0m || exitVolume >= volume - Epsilon)
			return;

		BuyMarket(exitVolume);
	}

	private void StorePreviousValues(decimal? ma0, decimal? ma1, decimal? ma2, decimal? macd)
	{
		if (ma0.HasValue)
			_previousMa0 = ma0.Value;

		if (ma1.HasValue)
			_previousMa1 = ma1.Value;

		if (ma2.HasValue)
			_previousMa2 = ma2.Value;

		if (macd.HasValue)
			_previousMacd = macd.Value;
	}

	private decimal? ProcessMovingAverage(LengthIndicator<decimal> indicator, Shift? shift, ICandleMessage candle)
	{
		var value = indicator.Process(candle);
		if (!value.IsFinal)
			return null;

		var result = value.ToDecimal();

		if (shift == null)
			return result;

		var shifted = shift.Process(value, candle.OpenTime, true);
		if (!shift.IsFormed || !shifted.IsFinal)
			return null;

		return shifted.ToDecimal();
	}

	private void UpdateMacdHistory(decimal macdValue)
	{
		_macdHistory.AddFirst(macdValue);

		var required = GetRequiredMacdCount();
		while (_macdHistory.Count > required)
			_macdHistory.RemoveLast();
	}

	private bool HasMacdTrend(bool upward, int requiredCount)
	{
		if (_macdHistory.Count < requiredCount)
			return false;

		var iterator = _macdHistory.GetEnumerator();
		if (!iterator.MoveNext())
			return false;

		var current = iterator.Current;
		var comparisons = 0;

		while (iterator.MoveNext())
		{
			var next = iterator.Current;

			if (upward)
			{
				if (next > current + Epsilon)
					return false;
			}
			else
			{
				if (next < current - Epsilon)
					return false;
			}

			current = next;
			comparisons++;

			if (comparisons >= requiredCount - 1)
				break;
		}

		return comparisons >= requiredCount - 1;
	}

	private int GetRequiredMacdCount()
	{
		return MacdTrendBars < 3 ? 3 : MacdTrendBars + 1;
	}

	private decimal AdjustVolume(decimal volume)
	{
		// Without security information use the raw volume (clamped to zero).
		if (Security is null)
			return Math.Max(0m, volume);

		var step = Security.VolumeStep ?? 0m;
		var min = Security.VolumeMin ?? step;
		var max = Security.VolumeMax;

		var adjusted = step > 0m ? Math.Floor(volume / step) * step : volume;
		if (adjusted < min)
			return 0m;

		if (max.HasValue && adjusted > max.Value)
			adjusted = max.Value;

		return adjusted;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (Security is null)
			return price;

		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
			return price;

		return Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
	}

	private decimal CalculatePipSize()
	{
		if (Security is null)
			return 0.0001m;

		var step = Security.PriceStep ?? 0.0001m;
		var decimals = Security.Decimals;
		var multiplier = (decimals == 3 || decimals == 5) ? 10m : 1m;
		return step * multiplier;
	}

	private decimal CalculatePointSize()
	{
		if (Security is null)
			return 0.0001m;

		var step = Security.PriceStep ?? 0.0001m;
		return step;
	}

	private void ResetLongState()
	{
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_longHighestPrice = null;
	}

	private void ResetShortState()
	{
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_shortLowestPrice = null;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MaMethods method, int length, CandlePrices price)
	{
		var maLength = Math.Max(1, length);

		return method switch
		{
			MaMethods.Simple => new SimpleMovingAverage { Length = maLength, CandlePrice = price },
			MaMethods.Exponential => new ExponentialMovingAverage { Length = maLength, CandlePrice = price },
			MaMethods.Smoothed => new SmoothedMovingAverage { Length = maLength, CandlePrice = price },
			MaMethods.LinearWeighted => new WeightedMovingAverage { Length = maLength, CandlePrice = price },
			_ => new SimpleMovingAverage { Length = maLength, CandlePrice = price }
		};
	}

	/// <summary>
	/// Moving average smoothing modes that mirror the original MetaTrader inputs.
	/// </summary>
	public enum MaMethods
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		LinearWeighted
	}
}

