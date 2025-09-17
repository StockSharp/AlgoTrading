using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert "Macd diver rsi mt4".
/// Combines RSI filters with MACD divergence detection to time long and short entries with fixed risk targets.
/// </summary>
public class MacdDivergenceRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _lowerRsiPeriod;
	private readonly StrategyParam<decimal> _lowerRsiThreshold;
	private readonly StrategyParam<int> _bullishFastEma;
	private readonly StrategyParam<int> _bullishSlowEma;
	private readonly StrategyParam<int> _bullishSignalSma;
	private readonly StrategyParam<decimal> _bullishVolume;
	private readonly StrategyParam<decimal> _bullishStopLossPips;
	private readonly StrategyParam<decimal> _bullishTakeProfitPips;
	private readonly StrategyParam<int> _upperRsiPeriod;
	private readonly StrategyParam<decimal> _upperRsiThreshold;
	private readonly StrategyParam<int> _bearishFastEma;
	private readonly StrategyParam<int> _bearishSlowEma;
	private readonly StrategyParam<int> _bearishSignalSma;
	private readonly StrategyParam<decimal> _bearishVolume;
	private readonly StrategyParam<decimal> _bearishStopLossPips;
	private readonly StrategyParam<decimal> _bearishTakeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _lowerRsi = null!;
	private RelativeStrengthIndex _upperRsi = null!;
	private MovingAverageConvergenceDivergence _bullMacd = null!;
	private MovingAverageConvergenceDivergence _bearMacd = null!;

	private decimal? _previousLowerRsi;
	private decimal? _previousUpperRsi;

	private readonly List<CandleSnapshot> _candles = new();
	private readonly List<decimal> _bullMacdHistory = new();
	private readonly List<decimal> _bearMacdHistory = new();

	private decimal _pipSize;
	private decimal _macdThreshold;

	private const int MaxHistory = 600;

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdDivergenceRsiStrategy"/> class.
	/// </summary>
	public MacdDivergenceRsiStrategy()
	{
		_lowerRsiPeriod = Param(nameof(LowerRsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Lower RSI Period", "Period for the oversold RSI filter (shifted by one bar).", "Signals")
			.SetCanOptimize(true);

		_lowerRsiThreshold = Param(nameof(LowerRsiThreshold), 30m)
			.SetDisplay("Lower RSI Threshold", "Oversold threshold used before checking for bullish divergence.", "Signals")
			.SetCanOptimize(true);

		_bullishFastEma = Param(nameof(BullishFastEma), 12)
			.SetGreaterThanZero()
			.SetDisplay("Bullish MACD Fast EMA", "Fast EMA length for the bullish MACD divergence detector.", "Signals")
			.SetCanOptimize(true);

		_bullishSlowEma = Param(nameof(BullishSlowEma), 26)
			.SetGreaterThanZero()
			.SetDisplay("Bullish MACD Slow EMA", "Slow EMA length for the bullish MACD divergence detector.", "Signals")
			.SetCanOptimize(true);

		_bullishSignalSma = Param(nameof(BullishSignalSma), 9)
			.SetGreaterThanZero()
			.SetDisplay("Bullish MACD Signal", "Signal smoothing period for the bullish MACD divergence detector.", "Signals")
			.SetCanOptimize(true);

		_bullishVolume = Param(nameof(BullishVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Bullish Volume", "Trade volume used when a bullish divergence signal appears.", "Trading");

		_bullishStopLossPips = Param(nameof(BullishStopLossPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Bullish Stop Loss (pips)", "Protective stop distance in pips for long entries.", "Risk");

		_bullishTakeProfitPips = Param(nameof(BullishTakeProfitPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Bullish Take Profit (pips)", "Target distance in pips for long entries.", "Risk");

		_upperRsiPeriod = Param(nameof(UpperRsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Upper RSI Period", "Period for the overbought RSI filter (shifted by one bar).", "Signals")
			.SetCanOptimize(true);

		_upperRsiThreshold = Param(nameof(UpperRsiThreshold), 70m)
			.SetDisplay("Upper RSI Threshold", "Overbought threshold used before checking for bearish divergence.", "Signals")
			.SetCanOptimize(true);

		_bearishFastEma = Param(nameof(BearishFastEma), 12)
			.SetGreaterThanZero()
			.SetDisplay("Bearish MACD Fast EMA", "Fast EMA length for the bearish MACD divergence detector.", "Signals")
			.SetCanOptimize(true);

		_bearishSlowEma = Param(nameof(BearishSlowEma), 26)
			.SetGreaterThanZero()
			.SetDisplay("Bearish MACD Slow EMA", "Slow EMA length for the bearish MACD divergence detector.", "Signals")
			.SetCanOptimize(true);

		_bearishSignalSma = Param(nameof(BearishSignalSma), 9)
			.SetGreaterThanZero()
			.SetDisplay("Bearish MACD Signal", "Signal smoothing period for the bearish MACD divergence detector.", "Signals")
			.SetCanOptimize(true);

		_bearishVolume = Param(nameof(BearishVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Bearish Volume", "Trade volume used when a bearish divergence signal appears.", "Trading");

		_bearishStopLossPips = Param(nameof(BearishStopLossPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Bearish Stop Loss (pips)", "Protective stop distance in pips for short entries.", "Risk");

		_bearishTakeProfitPips = Param(nameof(BearishTakeProfitPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Bearish Take Profit (pips)", "Target distance in pips for short entries.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for all indicator calculations.", "General");
	}

	/// <summary>
	/// RSI period used for the bullish (oversold) filter.
	/// </summary>
	public int LowerRsiPeriod
	{
		get => _lowerRsiPeriod.Value;
		set => _lowerRsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI oversold threshold checked one bar before the current signal.
	/// </summary>
	public decimal LowerRsiThreshold
	{
		get => _lowerRsiThreshold.Value;
		set => _lowerRsiThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the bullish MACD calculation.
	/// </summary>
	public int BullishFastEma
	{
		get => _bullishFastEma.Value;
		set => _bullishFastEma.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the bullish MACD calculation.
	/// </summary>
	public int BullishSlowEma
	{
		get => _bullishSlowEma.Value;
		set => _bullishSlowEma.Value = value;
	}

	/// <summary>
	/// Signal smoothing length for the bullish MACD calculation.
	/// </summary>
	public int BullishSignalSma
	{
		get => _bullishSignalSma.Value;
		set => _bullishSignalSma.Value = value;
	}

	/// <summary>
	/// Trade volume used for bullish divergence entries.
	/// </summary>
	public decimal BullishVolume
	{
		get => _bullishVolume.Value;
		set => _bullishVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips applied to bullish positions.
	/// </summary>
	public decimal BullishStopLossPips
	{
		get => _bullishStopLossPips.Value;
		set => _bullishStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips applied to bullish positions.
	/// </summary>
	public decimal BullishTakeProfitPips
	{
		get => _bullishTakeProfitPips.Value;
		set => _bullishTakeProfitPips.Value = value;
	}

	/// <summary>
	/// RSI period used for the bearish (overbought) filter.
	/// </summary>
	public int UpperRsiPeriod
	{
		get => _upperRsiPeriod.Value;
		set => _upperRsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought threshold checked one bar before the current signal.
	/// </summary>
	public decimal UpperRsiThreshold
	{
		get => _upperRsiThreshold.Value;
		set => _upperRsiThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the bearish MACD calculation.
	/// </summary>
	public int BearishFastEma
	{
		get => _bearishFastEma.Value;
		set => _bearishFastEma.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the bearish MACD calculation.
	/// </summary>
	public int BearishSlowEma
	{
		get => _bearishSlowEma.Value;
		set => _bearishSlowEma.Value = value;
	}

	/// <summary>
	/// Signal smoothing length for the bearish MACD calculation.
	/// </summary>
	public int BearishSignalSma
	{
		get => _bearishSignalSma.Value;
		set => _bearishSignalSma.Value = value;
	}

	/// <summary>
	/// Trade volume used for bearish divergence entries.
	/// </summary>
	public decimal BearishVolume
	{
		get => _bearishVolume.Value;
		set => _bearishVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips applied to bearish positions.
	/// </summary>
	public decimal BearishStopLossPips
	{
		get => _bearishStopLossPips.Value;
		set => _bearishStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips applied to bearish positions.
	/// </summary>
	public decimal BearishTakeProfitPips
	{
		get => _bearishTakeProfitPips.Value;
		set => _bearishTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type feeding the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lowerRsi?.Reset();
		_upperRsi?.Reset();
		_bullMacd?.Reset();
		_bearMacd?.Reset();

		_previousLowerRsi = null;
		_previousUpperRsi = null;

		_candles.Clear();
		_bullMacdHistory.Clear();
		_bearMacdHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		_macdThreshold = 3m * _pipSize;

		_lowerRsi = new RelativeStrengthIndex { Length = LowerRsiPeriod };
		_upperRsi = new RelativeStrengthIndex { Length = UpperRsiPeriod };
		_bullMacd = new MovingAverageConvergenceDivergence
		{
			Fast = BullishFastEma,
			Slow = BullishSlowEma,
			Signal = BullishSignalSma,
		};
		_bearMacd = new MovingAverageConvergenceDivergence
		{
			Fast = BearishFastEma,
			Slow = BearishSlowEma,
			Signal = BearishSignalSma,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bullMacd, _lowerRsi, _bearMacd, _upperRsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(
	ICandleMessage candle,
	decimal bullMacd,
	decimal bullSignal,
	decimal bullHistogram,
	decimal lowerRsi,
	decimal bearMacd,
	decimal bearSignal,
	decimal bearHistogram,
	decimal upperRsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		AddHistory(candle, bullMacd, bearMacd);

		var previousLower = _previousLowerRsi;
		var previousUpper = _previousUpperRsi;
		_previousLowerRsi = lowerRsi;
		_previousUpperRsi = upperRsi;

		if (!_bullMacd.IsFormed || !_bearMacd.IsFormed || previousLower is null || previousUpper is null)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var allowLong = previousLower < LowerRsiThreshold && Position == 0m && HasBullishDivergence(_macdThreshold);
		var allowShort = previousUpper > UpperRsiThreshold && Position == 0m && HasBearishDivergence(_macdThreshold);

		if (allowLong)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (allowShort)
		{
			EnterShort(candle.ClosePrice);
		}
	}

	private void EnterLong(decimal referencePrice)
	{
		var volume = BullishVolume;
		if (volume <= 0m)
		return;

		var resultingPosition = Position + volume;

		BuyMarket(volume);

		var stopDistance = ConvertPipsToPrice(BullishStopLossPips);
		if (stopDistance > 0m)
		{
			SetStopLoss(stopDistance, referencePrice, resultingPosition);
		}

		var takeDistance = ConvertPipsToPrice(BullishTakeProfitPips);
		if (takeDistance > 0m)
		{
			SetTakeProfit(takeDistance, referencePrice, resultingPosition);
		}
	}

	private void EnterShort(decimal referencePrice)
	{
		var volume = BearishVolume;
		if (volume <= 0m)
		return;

		var resultingPosition = Position - volume;

		SellMarket(volume);

		var stopDistance = ConvertPipsToPrice(BearishStopLossPips);
		if (stopDistance > 0m)
		{
			SetStopLoss(stopDistance, referencePrice, resultingPosition);
		}

		var takeDistance = ConvertPipsToPrice(BearishTakeProfitPips);
		if (takeDistance > 0m)
		{
			SetTakeProfit(takeDistance, referencePrice, resultingPosition);
		}
	}

	private bool HasBullishDivergence(decimal threshold)
	{
		if (threshold <= 0m)
		return false;

		if (_bullMacdHistory.Count < 6 || _candles.Count < 6)
		return false;

		if (!IsCurrentBullishSetup(threshold))
		return false;

		if (!TryFindLocalLow(0, 15, out var recentLow, out _))
		return false;

		var maxShift = Math.Min(_bullMacdHistory.Count - 1, _candles.Count - 1);
		decimal macd0 = 0m;
		var depth = 0;

		for (var shift = 6; shift <= maxShift; shift++)
		{
			if (!IsHistoricalBullishDip(shift, threshold))
			continue;

			if (!TryGetMacd(_bullMacdHistory, shift - 2, out var candidate))
			break;

			if (macd0 == 0m || (candidate < macd0 && depth >= 1))
			{
				macd0 = candidate;
			}
			else
			{
				break;
			}

			depth++;

			if (!TryFindLocalLow(shift - 4, shift + 15, out var previousLow, out _))
			continue;

			if (depth > 18)
			continue;

			if (!TryGetMacd(_bullMacdHistory, 2, out var recentMacd))
			break;

			var regularDivergence = candidate > recentMacd && previousLow < recentLow;
			var hiddenDivergence = candidate < recentMacd && previousLow > recentLow;

			if (regularDivergence || hiddenDivergence)
			return true;
		}

		return false;
	}

	private bool HasBearishDivergence(decimal threshold)
	{
		if (threshold <= 0m)
		return false;

		if (_bearMacdHistory.Count < 6 || _candles.Count < 6)
		return false;

		if (!IsCurrentBearishSetup(threshold))
		return false;

		if (!TryFindLocalHigh(0, 15, out var recentHigh, out _))
		return false;

		var maxShift = Math.Min(_bearMacdHistory.Count - 1, _candles.Count - 1);
		decimal macd0 = 0m;
		var depth = 0;

		for (var shift = 6; shift <= maxShift; shift++)
		{
			if (!IsHistoricalBearishPeak(shift, threshold))
			continue;

			if (!TryGetMacd(_bearMacdHistory, shift - 2, out var candidate))
			break;

			if (macd0 == 0m || (candidate > macd0 && depth >= 1))
			{
				macd0 = candidate;
			}
			else
			{
				break;
			}

			depth++;

			if (!TryFindLocalHigh(shift - 4, shift + 15, out var previousHigh, out _))
			continue;

			if (depth > 18)
			continue;

			if (!TryGetMacd(_bearMacdHistory, 2, out var recentMacd))
			break;

			var regularDivergence = candidate < recentMacd && previousHigh > recentHigh;
			var hiddenDivergence = candidate > recentMacd && previousHigh < recentHigh;

			if (regularDivergence || hiddenDivergence)
			return true;
		}

		return false;
	}

	private bool IsCurrentBullishSetup(decimal threshold)
	{
		return TryGetMacd(_bullMacdHistory, 1, out var macd1)
			&& TryGetMacd(_bullMacdHistory, 2, out var macd2)
			&& TryGetMacd(_bullMacdHistory, 3, out var macd3)
			&& TryGetMacd(_bullMacdHistory, 4, out var macd4)
			&& macd1 < -threshold
			&& macd2 < -threshold
			&& macd3 < -threshold
			&& macd4 < -threshold
			&& macd1 > macd2
			&& macd3 > macd2
			&& macd4 > macd2;
	}

	private bool IsCurrentBearishSetup(decimal threshold)
	{
		return TryGetMacd(_bearMacdHistory, 1, out var macd1)
			&& TryGetMacd(_bearMacdHistory, 2, out var macd2)
			&& TryGetMacd(_bearMacdHistory, 3, out var macd3)
			&& TryGetMacd(_bearMacdHistory, 4, out var macd4)
			&& macd1 > threshold
			&& macd2 > threshold
			&& macd3 > threshold
			&& macd4 > threshold
			&& macd1 < macd2
			&& macd3 < macd2
			&& macd4 < macd2;
	}

	private bool IsHistoricalBullishDip(int shift, decimal threshold)
	{
		return shift >= 4
			&& TryGetMacd(_bullMacdHistory, shift, out var macd0)
			&& TryGetMacd(_bullMacdHistory, shift - 1, out var macd1)
			&& TryGetMacd(_bullMacdHistory, shift - 2, out var macd2)
			&& TryGetMacd(_bullMacdHistory, shift - 3, out var macd3)
			&& TryGetMacd(_bullMacdHistory, shift - 4, out var macd4)
			&& macd0 < -threshold
			&& macd1 < -threshold
			&& macd2 < -threshold
			&& macd3 < -threshold
			&& macd4 < -threshold
			&& macd0 > macd2
			&& macd1 > macd2
			&& macd2 < macd3
			&& macd2 < macd4;
	}

	private bool IsHistoricalBearishPeak(int shift, decimal threshold)
	{
		return shift >= 4
			&& TryGetMacd(_bearMacdHistory, shift, out var macd0)
			&& TryGetMacd(_bearMacdHistory, shift - 1, out var macd1)
			&& TryGetMacd(_bearMacdHistory, shift - 2, out var macd2)
			&& TryGetMacd(_bearMacdHistory, shift - 3, out var macd3)
			&& TryGetMacd(_bearMacdHistory, shift - 4, out var macd4)
			&& macd0 > threshold
			&& macd1 > threshold
			&& macd2 > threshold
			&& macd3 > threshold
			&& macd4 > threshold
			&& macd0 < macd2
			&& macd1 < macd2
			&& macd2 > macd3
			&& macd2 > macd4;
	}

	private void AddHistory(ICandleMessage candle, decimal bullMacd, decimal bearMacd)
	{
		_candles.Add(new CandleSnapshot(candle));
		_bullMacdHistory.Add(bullMacd);
		_bearMacdHistory.Add(bearMacd);

		if (_candles.Count > MaxHistory)
		{
			_candles.RemoveAt(0);
			_bullMacdHistory.RemoveAt(0);
			_bearMacdHistory.RemoveAt(0);
		}
	}

	private bool TryGetMacd(IReadOnlyList<decimal> values, int shift, out decimal value)
	{
		var index = values.Count - 1 - shift;
		if (index < 0 || index >= values.Count)
		{
			value = 0m;
			return false;
		}

		value = values[index];
		return true;
	}

	private bool TryGetCandle(int shift, out CandleSnapshot candle)
	{
		var index = _candles.Count - 1 - shift;
		if (index < 0 || index >= _candles.Count)
		{
			candle = default;
			return false;
		}

		candle = _candles[index];
		return true;
	}

	private bool TryGetLow(int shift, out decimal low)
	{
		if (TryGetCandle(shift, out var candle))
		{
			low = candle.Low;
			return true;
		}

		low = 0m;
		return false;
	}

	private bool TryGetHigh(int shift, out decimal high)
	{
		if (TryGetCandle(shift, out var candle))
		{
			high = candle.High;
			return true;
		}

		high = 0m;
		return false;
	}

	private bool TryFindLocalLow(int startShift, int endShift, out decimal low, out int offset)
	{
		low = 0m;
		offset = -1;

		if (_candles.Count < 5)
		return false;

		startShift = Math.Max(startShift, 0);
		var maxShift = Math.Min(endShift, _candles.Count - 5);

		for (var shift = startShift; shift <= maxShift; shift++)
		{
			if (TryGetLow(shift, out var current)
				&& TryGetLow(shift + 1, out var low1)
				&& TryGetLow(shift + 2, out var low2)
				&& TryGetLow(shift + 3, out var low3)
				&& TryGetLow(shift + 4, out var low4)
				&& current < low1
				&& current < low2
				&& current < low3
				&& current < low4)
			{
				low = current;
				offset = shift;
				return true;
			}
		}

		return false;
	}

	private bool TryFindLocalHigh(int startShift, int endShift, out decimal high, out int offset)
	{
		high = 0m;
		offset = -1;

		if (_candles.Count < 5)
		return false;

		startShift = Math.Max(startShift, 0);
		var maxShift = Math.Min(endShift, _candles.Count - 5);

		for (var shift = startShift; shift <= maxShift; shift++)
		{
			if (TryGetHigh(shift, out var current)
				&& TryGetHigh(shift + 1, out var high1)
				&& TryGetHigh(shift + 2, out var high2)
				&& TryGetHigh(shift + 3, out var high3)
				&& TryGetHigh(shift + 4, out var high4)
				&& current > high1
				&& current > high2
				&& current > high3
				&& current > high4)
			{
				high = current;
				offset = shift;
				return true;
			}
		}

		return false;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m || _pipSize <= 0m)
		return 0m;

		return pips * _pipSize;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 1m;

		var decimals = GetDecimalPlaces(priceStep);

		return decimals switch
		{
			3 => priceStep * 10m,
			5 => priceStep * 10m,
			6 => priceStep * 100m,
			_ => priceStep,
		};
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);
		if (value == 0m)
		return 0;

		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(ICandleMessage candle)
		{
			Time = candle.OpenTime;
			High = candle.HighPrice;
			Low = candle.LowPrice;
			Close = candle.ClosePrice;
		}

		public DateTimeOffset Time { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}
}
