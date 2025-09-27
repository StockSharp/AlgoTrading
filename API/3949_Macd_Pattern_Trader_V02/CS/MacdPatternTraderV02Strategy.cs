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
/// MACD pattern trader strategy converted from the "MacdPatternTraderv02" MetaTrader expert.
/// The strategy monitors the MACD main line and opens positions when the characteristic reversal pattern appears.
/// It also manages open positions using the original partial close logic based on moving averages.
/// </summary>
public class MacdPatternTraderV02Strategy : Strategy
{
	private readonly StrategyParam<decimal> _profitThresholdPoints;
	private readonly StrategyParam<int> _maxHistory;
	private readonly StrategyParam<int> _stopLossBars;
	private readonly StrategyParam<int> _takeProfitBars;
	private readonly StrategyParam<int> _offsetPoints;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<decimal> _maxThreshold;
	private readonly StrategyParam<decimal> _minThreshold;
	private readonly StrategyParam<int> _ema1Period;
	private readonly StrategyParam<int> _ema2Period;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _ema3Period;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd = null!;
	private ExponentialMovingAverage _ema1 = null!;
	private ExponentialMovingAverage _ema2 = null!;
	private SimpleMovingAverage _sma = null!;
	private ExponentialMovingAverage _ema3 = null!;

	private readonly List<ICandleMessage> _history = new();

	private decimal? _ema1Prev;
	private decimal? _ema2Prev;
	private decimal? _smaPrev;
	private decimal? _ema3Prev;
	private decimal? _ema1Last;
	private decimal? _ema2Last;
	private decimal? _smaLast;
	private decimal? _ema3Last;

	private decimal? _macdPrev1;
	private decimal? _macdPrev2;
	private decimal? _macdPrev3;

	private bool _maxThresholdReached;
	private bool _minThresholdReached;
	private bool _sellPatternReady;
	private bool _buyPatternReady;
	private decimal _patternMinValue;
	private decimal _patternMaxValue;

	private decimal _pointSize;

	private int _entryDirection;
	private decimal _entryPrice;
	private decimal _openVolume;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private int _longPartialStage;
	private int _shortPartialStage;

	/// <summary>
	/// Number of bars used to calculate protective stop-loss levels.
	/// </summary>
	public int StopLossBars
	{
		get => _stopLossBars.Value;
		set => _stopLossBars.Value = value;
	}

	/// <summary>
	/// Number of bars evaluated when searching for take-profit targets.
	/// </summary>
	public int TakeProfitBars
	{
		get => _takeProfitBars.Value;
		set => _takeProfitBars.Value = value;
	}

	/// <summary>
	/// Offset applied to stop-loss levels expressed in price points.
	/// </summary>
	public int OffsetPoints
	{
		get => _offsetPoints.Value;
		set => _offsetPoints.Value = value;
	}

	/// <summary>
	/// Minimal profit in points required before partial exits are considered.
	/// </summary>
	public decimal ProfitThresholdPoints
	{
		get => _profitThresholdPoints.Value;
		set => _profitThresholdPoints.Value = value;
	}

	/// <summary>
	/// Fast EMA period for the MACD main line.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the MACD main line.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Upper MACD threshold that arms the short pattern.
	/// </summary>
	public decimal MaxThreshold
	{
		get => _maxThreshold.Value;
		set => _maxThreshold.Value = value;
	}

	/// <summary>
	/// Lower MACD threshold that arms the long pattern.
	/// </summary>
	public decimal MinThreshold
	{
		get => _minThreshold.Value;
		set => _minThreshold.Value = value;
	}

	/// <summary>
	/// Period of the first EMA used in the partial close logic.
	/// </summary>
	public int Ema1Period
	{
		get => _ema1Period.Value;
		set => _ema1Period.Value = value;
	}

	/// <summary>
	/// Period of the second EMA used in the partial close logic.
	/// </summary>
	public int Ema2Period
	{
		get => _ema2Period.Value;
		set => _ema2Period.Value = value;
	}

	/// <summary>
	/// Period of the SMA used to detect profit taking levels.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slow EMA used in the partial close logic.
	/// </summary>
	public int Ema3Period
	{
		get => _ema3Period.Value;
		set => _ema3Period.Value = value;
	}

	/// <summary>
	/// Trading volume applied to market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum number of finished candles stored in the sliding history window.
	/// </summary>
	public int MaxHistory
	{
		get => _maxHistory.Value;
		set => _maxHistory.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdPatternTraderV02Strategy"/> class.
	/// </summary>
	public MacdPatternTraderV02Strategy()
	{
		_stopLossBars = Param(nameof(StopLossBars), 6)
			.SetGreaterThanZero()
			.SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk");

		_takeProfitBars = Param(nameof(TakeProfitBars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Take-Profit Bars", "Window used when scanning for take-profit", "Risk");

		_offsetPoints = Param(nameof(OffsetPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Offset Points", "Additional protective offset in points", "Risk");

		_profitThresholdPoints = Param(nameof(ProfitThresholdPoints), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Threshold Points", "Minimal profit in points before partial exits", "Risk");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period for MACD", "Indicators");

		_maxThreshold = Param(nameof(MaxThreshold), 0.0045m)
			.SetDisplay("Upper Threshold", "Maximum MACD threshold for longs", "Signals");

		_minThreshold = Param(nameof(MinThreshold), -0.0045m)
			.SetDisplay("Lower Threshold", "Minimum MACD threshold for shorts", "Signals");

		_ema1Period = Param(nameof(Ema1Period), 7)
			.SetGreaterThanZero()
			.SetDisplay("EMA 1", "First EMA period for management", "Management");

		_ema2Period = Param(nameof(Ema2Period), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA 2", "Second EMA period for management", "Management");

		_smaPeriod = Param(nameof(SmaPeriod), 98)
			.SetGreaterThanZero()
			.SetDisplay("SMA", "SMA period for management", "Management");

		_ema3Period = Param(nameof(Ema3Period), 365)
			.SetGreaterThanZero()
			.SetDisplay("EMA 3", "Slow EMA period for management", "Management");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Market order volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for indicators", "General");

		_maxHistory = Param(nameof(MaxHistory), 1024)
			.SetGreaterThanZero()
			.SetDisplay("History Limit", "Maximum candles stored for pattern recognition", "General");
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

		_history.Clear();
		_macdPrev1 = null;
		_macdPrev2 = null;
		_macdPrev3 = null;
		_ema1Prev = null;
		_ema2Prev = null;
		_smaPrev = null;
		_ema3Prev = null;
		_ema1Last = null;
		_ema2Last = null;
		_smaLast = null;
		_ema3Last = null;
		_maxThresholdReached = false;
		_minThresholdReached = false;
		_sellPatternReady = false;
		_buyPatternReady = false;
		_patternMinValue = 0m;
		_patternMaxValue = 0m;
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = Security?.PriceStep ?? 0m;
		if (_pointSize <= 0m)
		{
			var decimals = Security?.Decimals;
			if (decimals.HasValue)
				_pointSize = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (_pointSize <= 0m)
			_pointSize = 0.0001m;

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = FastEmaPeriod,
			LongPeriod = SlowEmaPeriod,
			SignalPeriod = 1
		};

		_ema1 = new ExponentialMovingAverage { Length = Ema1Period };
		_ema2 = new ExponentialMovingAverage { Length = Ema2Period };
		_sma = new SimpleMovingAverage { Length = SmaPeriod };
		_ema3 = new ExponentialMovingAverage { Length = Ema3Period };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_ema1, _ema2, _sma, _ema3, ProcessTrendIndicators)
			.BindEx(_macd, ProcessMacd)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _ema1);
			DrawIndicator(area, _ema2);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _ema3);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrendIndicators(ICandleMessage candle, decimal ema1Value, decimal ema2Value, decimal smaValue, decimal ema3Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_ema1Prev = _ema1Last;
		_ema2Prev = _ema2Last;
		_smaPrev = _smaLast;
		_ema3Prev = _ema3Last;

		_ema1Last = ema1Value;
		_ema2Last = ema2Value;
		_smaLast = smaValue;
		_ema3Last = ema3Value;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed || !indicatorValue.IsFinal)
			return;

		var macdValue = (MovingAverageConvergenceDivergenceValue)indicatorValue;
		if (macdValue.Macd is not decimal macdLine)
			return;

		var macdLast = _macdPrev1;
		var macdLast2 = _macdPrev2;
		var macdLast3 = _macdPrev3;

		if (macdLast is null || macdLast2 is null || macdLast3 is null)
		{
			_macdPrev3 = _macdPrev2;
			_macdPrev2 = _macdPrev1;
			_macdPrev1 = macdLine;
			AddCandle(candle);
			return;
		}

		AddCandle(candle);

		ExecutePatternLogic(candle, macdLine, macdLast.Value, macdLast2.Value, macdLast3.Value);

		_macdPrev3 = _macdPrev2;
		_macdPrev2 = _macdPrev1;
		_macdPrev1 = macdLine;
	}

	private void ExecutePatternLogic(ICandleMessage candle, decimal macdCurrent, decimal macdPrev1, decimal macdPrev2, decimal macdPrev3)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pointSize <= 0m)
			return;

		if (macdCurrent > 0m)
		{
			_maxThresholdReached = true;
			_sellPatternReady = false;
		}

		if (macdCurrent > macdPrev1 && macdPrev1 < macdPrev3 && _maxThresholdReached && macdCurrent > MinThreshold && macdCurrent < 0m && !_sellPatternReady)
		{
			_sellPatternReady = true;
			_patternMinValue = Math.Abs(macdPrev1 * 10000m);
		}

		var currentMagnitude = Math.Abs(macdCurrent * 10000m);

		if (_sellPatternReady && macdCurrent < macdPrev1 && macdPrev1 > macdPrev3 && macdCurrent < 0m && _patternMinValue <= currentMagnitude)
		{
			_maxThresholdReached = false;
		}

		if (_sellPatternReady && macdCurrent < macdPrev1 && macdPrev1 > macdPrev3 && macdCurrent < 0m)
		{
			TryOpenShort(candle);
			_sellPatternReady = false;
			_maxThresholdReached = false;
		}

		if (macdCurrent < 0m)
		{
			_minThresholdReached = true;
			_buyPatternReady = false;
		}

		if (macdCurrent < MaxThreshold && macdCurrent < macdPrev1 && macdPrev1 > macdPrev3 && _minThresholdReached && macdCurrent > 0m && !_buyPatternReady)
		{
			_buyPatternReady = true;
			_patternMaxValue = Math.Abs(macdPrev1 * 10000m);
		}

		if (_buyPatternReady && macdCurrent > macdPrev1 && macdPrev1 < macdPrev3 && macdCurrent > 0m && _patternMaxValue <= currentMagnitude)
		{
			_minThresholdReached = false;
		}

		if (_buyPatternReady && macdCurrent > macdPrev1 && macdPrev1 < macdPrev3 && macdCurrent > 0m)
		{
			TryOpenLong(candle);
			_buyPatternReady = false;
			_minThresholdReached = false;
		}

		ManagePosition(candle);
	}

	private void TryOpenShort(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			var closeVolume = NormalizeVolume(Math.Abs(Position));
			if (closeVolume > 0m)
			{
				SellMarket(closeVolume);
				ResetPositionState();
			}
		}

		if (Position < 0m)
			return;

		var volume = NormalizeVolume(TradeVolume);
		if (volume <= 0m)
			return;

		var entryPrice = candle.ClosePrice;
		SellMarket(volume);
		RegisterEntry(-1, entryPrice, volume);
	}

	private void TryOpenLong(ICandleMessage candle)
	{
		if (Position < 0m)
		{
			var closeVolume = NormalizeVolume(Math.Abs(Position));
			if (closeVolume > 0m)
			{
				BuyMarket(closeVolume);
				ResetPositionState();
			}
		}

		if (Position > 0m)
			return;

		var volume = NormalizeVolume(TradeVolume);
		if (volume <= 0m)
			return;

		var entryPrice = candle.ClosePrice;
		BuyMarket(volume);
		RegisterEntry(1, entryPrice, volume);
	}

	private void RegisterEntry(int direction, decimal entryPrice, decimal volume)
	{
		_entryDirection = direction;
		_entryPrice = entryPrice;
		_openVolume = volume;
		_stopLossPrice = direction > 0 ? CalculateLongStop() : CalculateShortStop();
		_takeProfitPrice = direction > 0 ? CalculateLongTarget() : CalculateShortTarget();
		_longPartialStage = 0;
		_shortPartialStage = 0;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (_entryDirection == 0 || _openVolume <= 0m)
			return;

		if (CheckRiskManagement(candle))
			return;

		var previousCandle = GetCandle(1);
		if (previousCandle is null || _ema2Prev is null || _ema3Prev is null || _smaPrev is null)
			return;

		var ema2Prev = _ema2Prev.Value;
		var ema3Prev = _ema3Prev.Value;
		var smaPrev = _smaPrev.Value;

		var profitPoints = CalculateOpenProfitPoints(candle.ClosePrice);

		if (_entryDirection > 0)
		{
			if (profitPoints > ProfitThresholdPoints && previousCandle.ClosePrice > ema2Prev && _longPartialStage == 0)
			{
				var volume = NormalizeVolume(_openVolume / 3m);
				if (volume > 0m)
				{
					SellMarket(volume);
					RegisterClose(volume, candle.ClosePrice);
					_longPartialStage = 1;
				}
			}
			else if (profitPoints > ProfitThresholdPoints && previousCandle.HighPrice > (smaPrev + ema3Prev) / 2m && _longPartialStage == 1)
			{
				var volume = NormalizeVolume(_openVolume / 2m);
				if (volume > 0m)
				{
					SellMarket(volume);
					RegisterClose(volume, candle.ClosePrice);
					_longPartialStage = 2;
				}
			}
		}
		else if (_entryDirection < 0)
		{
			if (profitPoints > ProfitThresholdPoints && previousCandle.ClosePrice < ema2Prev && _shortPartialStage == 0)
			{
				var volume = NormalizeVolume(_openVolume / 3m);
				if (volume > 0m)
				{
					BuyMarket(volume);
					RegisterClose(volume, candle.ClosePrice);
					_shortPartialStage = 1;
				}
			}
			else if (profitPoints > ProfitThresholdPoints && previousCandle.LowPrice < (smaPrev + ema3Prev) / 2m && _shortPartialStage == 1)
			{
				var volume = NormalizeVolume(_openVolume / 2m);
				if (volume > 0m)
				{
					BuyMarket(volume);
					RegisterClose(volume, candle.ClosePrice);
					_shortPartialStage = 2;
				}
			}
		}
	}

	private bool CheckRiskManagement(ICandleMessage candle)
	{
		if (_entryDirection == 0 || _openVolume <= 0m)
			return false;

		if (_entryDirection > 0)
		{
			if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
			{
				SellMarket(_openVolume);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(_openVolume);
				ResetPositionState();
				return true;
			}
		}
		else
		{
			if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
			{
				BuyMarket(_openVolume);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(_openVolume);
				ResetPositionState();
				return true;
			}
		}

		return false;
	}

	private decimal CalculateOpenProfitPoints(decimal currentPrice)
	{
		if (_pointSize <= 0m)
			return 0m;

		var difference = _entryDirection > 0 ? currentPrice - _entryPrice : _entryPrice - currentPrice;
		return Math.Abs(difference / _pointSize);
	}

	private void RegisterClose(decimal volume, decimal price)
	{
		_openVolume -= volume;
		if (_openVolume <= 0m || Math.Abs(Position) < 1e-6m)
			ResetPositionState();
	}

	private void ResetPositionState()
	{
		_entryDirection = 0;
		_entryPrice = 0m;
		_openVolume = 0m;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_longPartialStage = 0;
		_shortPartialStage = 0;
	}

	private decimal? CalculateShortStop()
	{
		var candles = GetCandlesRange(StopLossBars, 1);
		if (candles.Count == 0)
			return null;

		var highest = decimal.MinValue;
		foreach (var candle in candles)
			highest = Math.Max(highest, candle.HighPrice);

		return highest + OffsetPoints * _pointSize;
	}

	private decimal? CalculateLongStop()
	{
		var candles = GetCandlesRange(StopLossBars, 1);
		if (candles.Count == 0)
			return null;

		var lowest = decimal.MaxValue;
		foreach (var candle in candles)
			lowest = Math.Min(lowest, candle.LowPrice);

		return lowest - OffsetPoints * _pointSize;
	}

	private decimal? CalculateShortTarget()
	{
		return ScanSequentialExtremum(TakeProfitBars, true);
	}

	private decimal? CalculateLongTarget()
	{
		return ScanSequentialExtremum(TakeProfitBars, false);
	}

	private decimal? ScanSequentialExtremum(int window, bool isShort)
	{
		if (window <= 0)
			return null;

		decimal? best = null;
		var shift = 0;

		while (true)
		{
			var candles = GetCandlesRange(window, shift);
			if (candles.Count == 0)
				break;

			decimal candidate;
			if (isShort)
			{
				candidate = decimal.MaxValue;
				foreach (var candle in candles)
					candidate = Math.Min(candidate, candle.LowPrice);

				if (best is null || candidate < best)
				{
					best = candidate;
					shift += window;
					continue;
				}
			}
			else
			{
				candidate = decimal.MinValue;
				foreach (var candle in candles)
					candidate = Math.Max(candidate, candle.HighPrice);

				if (best is null || candidate > best)
				{
					best = candidate;
					shift += window;
					continue;
				}
			}

			break;
		}

		return best;
	}

	private List<ICandleMessage> GetCandlesRange(int length, int shift)
	{
		var result = new List<ICandleMessage>();
		if (length <= 0)
			return result;

		var startIndex = _history.Count - 1 - shift;
		for (var i = startIndex; i >= 0 && result.Count < length; i--)
			result.Add(_history[i]);

		return result;
	}

	private ICandleMessage GetCandle(int shift)
	{
		var index = _history.Count - 1 - shift;
		if (index < 0 || index >= _history.Count)
			return null;

		return _history[index];
	}

	private void AddCandle(ICandleMessage candle)
	{
		_history.Add(candle);
		if (_history.Count > MaxHistory)
			_history.RemoveAt(0);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security?.VolumeStep is { } step && step > 0m)
			volume = Math.Round(volume / step) * step;

		if (security?.MinVolume is { } minVolume && minVolume > 0m && volume < minVolume)
			return 0m;

		return volume.Max(0m);
	}
}

