using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the 1-2-3 pattern expert advisor by Martes.
/// Detects the three point reversal structure combined with MACD confirmation
/// and trend length filters from the original MQL4 code.
/// </summary>
public class OneTwoThreePatternStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _volumeParam;
	private readonly StrategyParam<decimal> _trailingStopPips;
        private readonly StrategyParam<decimal> _trendRatio;
        private readonly StrategyParam<decimal> _macdEpsilon;
        private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _patternLookback;

	private readonly List<CandleSnapshot> _candles = new();
	private readonly List<decimal> _macdValues = new();
	private readonly List<decimal> _signalValues = new();

	private decimal? _longStop;
	private decimal? _longTakeProfit;
	private decimal? _longEntryPrice;
	private decimal? _shortStop;
	private decimal? _shortTakeProfit;
	private decimal? _shortEntryPrice;
        private decimal _pipSize;

        /// <summary>
	/// Take profit distance expressed in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Base trade volume (MetaTrader lots).
	/// </summary>
	public decimal TradeVolume
	{
		get => _volumeParam.Value;
		set => _volumeParam.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in MetaTrader points.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum ratio between the previous and current trend lengths.
	/// </summary>
        public decimal TrendRatio
        {
                get => _trendRatio.Value;
                set => _trendRatio.Value = value;
        }

        public decimal MacdEpsilon
        {
                get => _macdEpsilon.Value;
                set => _macdEpsilon.Value = value;
        }

	/// <summary>
	/// Candle type used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast MACD moving average period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow MACD moving average period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal line period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Number of historical bars inspected when validating the pattern.
	/// </summary>
	public int PatternLookback
	{
		get => _patternLookback.Value;
		set => _patternLookback.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public OneTwoThreePatternStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 60m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "MetaTrader style take profit distance.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 120m, 10m);

		_volumeParam = Param(nameof(TradeVolume), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Trade volume in lots.", "General");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in MetaTrader points.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 5m);

                _trendRatio = Param(nameof(TrendRatio), 4m)
                        .SetGreaterThanZero()
                        .SetDisplay("Trend Ratio", "Required ratio between previous and current trend lengths.", "Filters")
                        .SetCanOptimize(true)
                        .SetOptimize(2m, 6m, 0.5m);

                _macdEpsilon = Param(nameof(MacdEpsilon), 0.001m)
                        .SetGreaterThanZero()
                        .SetDisplay("MACD Epsilon", "Small offset used to avoid division by zero in ratio checks.", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay(LocalizedStrings.CandleTypeKey, "Candle series for pattern recognition.", "Data");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period.", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period.", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period.", "Indicators");

		_patternLookback = Param(nameof(PatternLookback), 100)
			.SetGreaterThanZero()
			.SetDisplay("Pattern Lookback", "Maximum amount of history inspected for pattern points.", "Filters");
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

		_candles.Clear();
		_macdValues.Clear();
		_signalValues.Clear();
		_longStop = null;
		_longTakeProfit = null;
		_longEntryPrice = null;
		_shortStop = null;
		_shortTakeProfit = null;
		_shortEntryPrice = null;
		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.Step ?? 0.0001m;
		Volume = TradeVolume;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				LongMa = { Length = MacdSlow },
				ShortMa = { Length = MacdFast }
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdSignal)
			return;

		if (macdSignal.Macd is not decimal macd || macdSignal.Signal is not decimal signal)
			return;

		// Store historical context for pattern calculations.
		AddCandle(candle);
		_macdValues.Add(macd);
		_signalValues.Add(signal);

		TrimHistory();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManageOpenPositions(candle);

		if (Position != 0)
			return;

		TryOpenPosition(candle);
	}

	private void TryOpenPosition(ICandleMessage candle)
	{
		if (TakeProfitPips < 10m)
			return;

		if (_candles.Count < PatternLookback + 5)
			return;

		if (_macdValues.Count < PatternLookback + 5 || _signalValues.Count < PatternLookback + 5)
			return;

		var currentPrice = candle.ClosePrice;
		var step = _pipSize;

		if (step <= 0m)
			return;

		if (TryEvaluateBuy(currentPrice, step, out var longSetup))
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_longEntryPrice = currentPrice;
			_longStop = longSetup.StopLoss;
			_longTakeProfit = longSetup.TakeProfit;
			_shortEntryPrice = null;
			_shortStop = null;
			_shortTakeProfit = null;
			LogInfo($"Opened long @ {currentPrice} with SL {longSetup.StopLoss} and TP {longSetup.TakeProfit}.");
			return;
		}

		if (TryEvaluateSell(currentPrice, step, out var shortSetup))
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_shortEntryPrice = currentPrice;
			_shortStop = shortSetup.StopLoss;
			_shortTakeProfit = shortSetup.TakeProfit;
			_longEntryPrice = null;
			_longStop = null;
			_longTakeProfit = null;
			LogInfo($"Opened short @ {currentPrice} with SL {shortSetup.StopLoss} and TP {shortSetup.TakeProfit}.");
		}
	}

	private bool TryEvaluateBuy(decimal currentPrice, decimal step, out TradeSetup setup)
	{
		setup = default;

		var point3Index = FindFirstValley(currentPrice, decimal.MinValue, 1, PatternLookback);
		if (point3Index == -1)
			return false;

		if (!TryGetCandle(point3Index, out var point3))
			return false;

		var point3Level = Math.Min(point3.Open, point3.Close);

		var point2Index = FindFirstPeak(currentPrice, point3Level, point3Index + 1, PatternLookback);
		if (point2Index == -1)
			return false;

		if (!TryGetCandle(point2Index, out var point2))
			return false;

		var point2Level = Math.Max(point2.Open, point2.Close);

		if (currentPrice - point2Level > 5m * step)
			return false;

		var point1Index = FindFirstValley(point2Level, decimal.MinValue, point2Index + 1, PatternLookback);
		if (point1Index == -1)
			return false;

		if (!ValidateMacdForBuy(point3Index))
			return false;

		var downLength = CalculateRelativeDownTrendLength(100, point1Index);
		var upLength = CalculateRelativeUpTrendLength(point1Index, 1);

		if (upLength <= 0m)
			return false;

		if (downLength / (upLength + MacdEpsilon) <= TrendRatio)
			return false;

		var stopDistance = point2Level - point3Level;
		if (stopDistance / step <= 13m)
			return false;

		var stop = point3Level - step;
		var takeProfit = currentPrice + stopDistance;

		setup = new TradeSetup(stop, takeProfit);
		return true;
	}

	private bool TryEvaluateSell(decimal currentPrice, decimal step, out TradeSetup setup)
	{
		setup = default;

		var point3Index = FindFirstPeak(decimal.MaxValue, currentPrice, 1, PatternLookback);
		if (point3Index == -1)
			return false;

		if (!TryGetCandle(point3Index, out var point3))
			return false;

		var point3Level = Math.Max(point3.Open, point3.Close);

		var point2Index = FindFirstValley(point3Level, currentPrice, point3Index + 1, PatternLookback);
		if (point2Index == -1)
			return false;

		if (!TryGetCandle(point2Index, out var point2))
			return false;

		var point2Level = Math.Min(point2.Open, point2.Close);

		if (Math.Abs(currentPrice - point2Level) > 5m * step)
			return false;

		var point1Index = FindFirstPeak(decimal.MaxValue, point2Level, point2Index + 1, PatternLookback);
		if (point1Index == -1)
			return false;

		if (!ValidateMacdForSell(point3Index))
			return false;

		var downLength = CalculateRelativeDownTrendLength(100, point1Index);
		var upLength = CalculateRelativeUpTrendLength(point1Index, 1);

		if (downLength <= 0m)
			return false;

		if (upLength / (downLength + MacdEpsilon) <= TrendRatio)
			return false;

		var stopDistance = Math.Abs(point2Level - point3Level);
		if (stopDistance / step <= 13m)
			return false;

		var stop = point3Level + step;
		var takeProfit = currentPrice - stopDistance;

		setup = new TradeSetup(stop, takeProfit);
		return true;
	}

	private bool ValidateMacdForBuy(int point3Index)
	{
		if (_macdValues.Count <= point3Index || _macdValues.Count < 2)
			return false;

		var macdCurrent = GetMacdValue(0);
		var macdPrevious = GetMacdValue(1);
		var signalCurrent = GetSignalValue(0);
		var signalPrevious = GetSignalValue(1);
		var macdAtPoint3 = GetMacdValue(point3Index);

		var crossover = macdCurrent > signalCurrent && macdPrevious < signalPrevious;
		var zeroCross = macdCurrent > 0m && macdPrevious < 0m;

		return (crossover || zeroCross) && macdAtPoint3 > 0m;
	}

	private bool ValidateMacdForSell(int point3Index)
	{
		if (_macdValues.Count <= point3Index || _macdValues.Count < 2)
			return false;

		var macdCurrent = GetMacdValue(0);
		var macdPrevious = GetMacdValue(1);
		var signalCurrent = GetSignalValue(0);
		var signalPrevious = GetSignalValue(1);
		var macdAtPoint3 = GetMacdValue(point3Index);

		var crossover = macdCurrent < signalCurrent && macdPrevious > signalPrevious;
		var zeroCross = macdCurrent < 0m && macdPrevious > 0m;

		return (crossover || zeroCross) && macdAtPoint3 < 0m;
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Position);
				ClearLongState();
				return;
			}

			if (_longTakeProfit.HasValue && candle.HighPrice >= _longTakeProfit.Value)
			{
				SellMarket(Position);
				ClearLongState();
				return;
			}

			ApplyTrailingForLong(candle);
		}
		else if (Position < 0)
		{
			var absPos = Math.Abs(Position);
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(absPos);
				ClearShortState();
				return;
			}

			if (_shortTakeProfit.HasValue && candle.LowPrice <= _shortTakeProfit.Value)
			{
				BuyMarket(absPos);
				ClearShortState();
				return;
			}

			ApplyTrailingForShort(candle);
		}
	}

	private void ApplyTrailingForLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || !_longEntryPrice.HasValue)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var price = candle.ClosePrice;
		var gain = price - _longEntryPrice.Value;
		if (gain <= trailingDistance)
			return;

		var candidate = price - trailingDistance;
		if (!_longStop.HasValue || candidate > _longStop.Value)
		{
			_longStop = candidate;
			LogInfo($"Updated long trailing stop to {candidate}.");
		}
	}

	private void ApplyTrailingForShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || !_shortEntryPrice.HasValue)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var price = candle.ClosePrice;
		var gain = _shortEntryPrice.Value - price;
		if (gain <= trailingDistance)
			return;

		var candidate = price + trailingDistance;
		if (!_shortStop.HasValue || candidate < _shortStop.Value)
		{
			_shortStop = candidate;
			LogInfo($"Updated short trailing stop to {candidate}.");
		}
	}

	private void ClearLongState()
	{
		_longStop = null;
		_longTakeProfit = null;
		_longEntryPrice = null;
	}

	private void ClearShortState()
	{
		_shortStop = null;
		_shortTakeProfit = null;
		_shortEntryPrice = null;
	}

	private void AddCandle(ICandleMessage candle)
	{
		var snapshot = new CandleSnapshot(
			candle.OpenPrice,
			candle.HighPrice,
			candle.LowPrice,
			candle.ClosePrice,
			candle.OpenTime);

		_candles.Add(snapshot);
	}

	private void TrimHistory()
	{
		const int maxSize = 600;

		if (_candles.Count > maxSize)
			_candles.RemoveRange(0, _candles.Count - maxSize);

		if (_macdValues.Count > maxSize)
			_macdValues.RemoveRange(0, _macdValues.Count - maxSize);

		if (_signalValues.Count > maxSize)
			_signalValues.RemoveRange(0, _signalValues.Count - maxSize);
	}

	private bool TryGetCandle(int barsAgo, out CandleSnapshot snapshot)
	{
		if (barsAgo < 0 || barsAgo >= _candles.Count)
		{
			snapshot = default;
			return false;
		}

		snapshot = _candles[^ (barsAgo + 1)];
		return true;
	}

	private decimal GetMacdValue(int barsAgo)
	{
		return _macdValues[^ (barsAgo + 1)];
	}

	private decimal GetSignalValue(int barsAgo)
	{
		return _signalValues[^ (barsAgo + 1)];
	}

	private int FindFirstValley(decimal maxLevel, decimal minLevel, int startIndex, int barsToProcess)
	{
		for (var i = startIndex; i < startIndex + barsToProcess; i++)
		{
			if (!TryGetCandle(i, out var candle))
				return -1;

			var value = Math.Min(candle.Open, candle.Close);
			var upper = Math.Max(candle.Open, candle.Close);

			if (upper > maxLevel || value < minLevel)
				return -1;

			if (i <= 0)
				continue;

			if (!TryGetCandle(i + 1, out var next) || !TryGetCandle(i - 1, out var previous))
				return -1;

			var nextValue = Math.Min(next.Open, next.Close);
			var previousValue = Math.Min(previous.Open, previous.Close);

			if (value < nextValue && value < previousValue)
				return i;
		}

		return -1;
	}

	private int FindFirstPeak(decimal maxLevel, decimal minLevel, int startIndex, int barsToProcess)
	{
		for (var i = startIndex; i < startIndex + barsToProcess; i++)
		{
			if (!TryGetCandle(i, out var candle))
				return -1;

			var value = Math.Max(candle.Open, candle.Close);
			var lower = Math.Min(candle.Open, candle.Close);

			if (value > maxLevel || lower < minLevel)
				return -1;

			if (i <= 0)
				continue;

			if (!TryGetCandle(i + 1, out var next) || !TryGetCandle(i - 1, out var previous))
				return -1;

			var nextValue = Math.Max(next.Open, next.Close);
			var previousValue = Math.Max(previous.Open, previous.Close);

			if (value > nextValue && value > previousValue)
				return i;
		}

			return -1;
	}

	private decimal CalculateRelativeDownTrendLength(int barsToProcess, int startingBarNumber)
	{
		if (barsToProcess <= 0)
			return 0m;

		if (!TryGetCandle(startingBarNumber + barsToProcess, out _))
			return 0m;

		if (barsToProcess <= 2)
			return 0m;

		var maxima = new decimal[barsToProcess];
		for (var i = 1; i <= barsToProcess; i++)
		{
			if (!TryGetCandle(startingBarNumber + i, out var candle))
				return 0m;

			maxima[i - 1] = Math.Max(candle.Open, candle.Close);
		}

		var hull = new int[barsToProcess];
		hull[0] = 0;
		var hullIdx = 1;
		hull[1] = 1;

		for (var i = 2; i < barsToProcess; i++)
		{
			hullIdx++;
			hull[hullIdx] = i;

			while (hullIdx >= 2)
			{
				var x0 = hull[hullIdx - 2];
				var y0 = maxima[hull[hullIdx - 2]];
				var x1 = hull[hullIdx - 1];
				var y1 = maxima[hull[hullIdx - 1]];
				var x2 = hull[hullIdx];
				var y2 = maxima[hull[hullIdx]];

				if ((x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0) >= 0)
				{
					hull[hullIdx - 1] = hull[hullIdx];
					hullIdx--;
				}
				else
				{
					break;
				}
			}
		}

		var bestLength = 0;
		for (var i = 0; i < hullIdx; i++)
		{
			var startIdx = hull[i];
			var endIdx = hull[i + 1];
			var length = endIdx - startIdx;
			if (length > bestLength && maxima[startIdx] < maxima[endIdx])
				bestLength = length;
		}

		if (bestLength == 0)
			return 0m;

		return bestLength / (decimal)barsToProcess;
	}

	private decimal CalculateRelativeUpTrendLength(int barsToProcess, int startingBarNumber)
	{
		if (barsToProcess <= 0)
			return 0m;

		if (!TryGetCandle(startingBarNumber + barsToProcess, out _))
			return 0m;

		if (barsToProcess <= 2)
			return 0m;

		var minima = new decimal[barsToProcess];
		for (var i = 1; i <= barsToProcess; i++)
		{
			if (!TryGetCandle(startingBarNumber + i, out var candle))
				return 0m;

			minima[i - 1] = Math.Min(candle.Open, candle.Close);
		}

		var hull = new int[barsToProcess];
		hull[0] = 0;
		var hullIdx = 1;
		hull[1] = 1;

		for (var i = 2; i < barsToProcess; i++)
		{
			hullIdx++;
			hull[hullIdx] = i;

			while (hullIdx >= 2)
			{
				var x0 = hull[hullIdx - 2];
				var y0 = minima[hull[hullIdx - 2]];
				var x1 = hull[hullIdx - 1];
				var y1 = minima[hull[hullIdx - 1]];
				var x2 = hull[hullIdx];
				var y2 = minima[hull[hullIdx]];

				if ((x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0) <= 0)
				{
					hull[hullIdx - 1] = hull[hullIdx];
					hullIdx--;
				}
				else
				{
					break;
				}
			}
		}

		var bestLength = 0;
		for (var i = 0; i < hullIdx; i++)
		{
			var startIdx = hull[i];
			var endIdx = hull[i + 1];
			var length = endIdx - startIdx;
			if (length > bestLength && minima[startIdx] > minima[endIdx])
				bestLength = length;
		}

		if (bestLength == 0)
			return 0m;

		return bestLength / (decimal)barsToProcess;
	}

	private readonly record struct CandleSnapshot(decimal Open, decimal High, decimal Low, decimal Close, DateTimeOffset Time);

	private readonly record struct TradeSetup(decimal StopLoss, decimal TakeProfit);
}
