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
/// Three timeframe confirmation strategy converted from the MetaTrader expert "Three timeframes.mq5".
/// Combines a fast MACD on the trading timeframe, an Alligator trend filter on a higher timeframe,
/// and an RSI confirmation on an intermediate timeframe. Supports pip-based risk management and optional
/// trading session filtering.
/// </summary>
public class ThreeTimeframesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _alligatorCandleType;
	private readonly StrategyParam<DataType> _rsiCandleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private SmoothedMovingAverage _jaw = null!;
	private SmoothedMovingAverage _teeth = null!;
	private SmoothedMovingAverage _lips = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal?[] _jawHistory = Array.Empty<decimal?>();
	private decimal?[] _teethHistory = Array.Empty<decimal?>();
	private decimal?[] _lipsHistory = Array.Empty<decimal?>();
	private decimal?[] _macdMainHistory = Array.Empty<decimal?>();
	private decimal?[] _macdSignalHistory = Array.Empty<decimal?>();

	private decimal? _latestRsi;
	private decimal _pipSize;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	/// <summary>
	/// Trade volume expressed in lots or contracts.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips. Zero disables the protective stop.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Zero disables the target.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Zero disables trailing.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional pip movement required before advancing the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal smoothing period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Alligator jaw (blue) period.
	/// </summary>
	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the jaw line.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Alligator teeth (red) period.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the teeth line.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Alligator lips (green) period.
	/// </summary>
	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the lips line.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// RSI averaging length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Enables the intraday trading session filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Trading session start hour (0-23) when the filter is enabled.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading session end hour (0-23) when the filter is enabled.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Primary trading timeframe used for MACD signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used to evaluate the Alligator structure.
	/// </summary>
	public DataType AlligatorCandleType
	{
		get => _alligatorCandleType.Value;
		set => _alligatorCandleType.Value = value;
	}

	/// <summary>
	/// Intermediate timeframe used for RSI confirmation.
	/// </summary>
	public DataType RsiCandleType
	{
		get => _rsiCandleType.Value;
		set => _rsiCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ThreeTimeframesStrategy"/> class.
	/// </summary>
	public ThreeTimeframesStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Base volume for market orders", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Initial stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 140m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Initial target distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing distance from price", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Extra move before trailing updates", "Risk");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "MACD")
		.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "MACD")
		.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal smoothing length", "MACD")
		.SetCanOptimize(true);

		_jawPeriod = Param(nameof(JawPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Jaw Period", "Alligator jaw period", "Alligator");

		_jawShift = Param(nameof(JawShift), 8)
		.SetDisplay("Jaw Shift", "Forward shift for the jaw", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Teeth Period", "Alligator teeth period", "Alligator");

		_teethShift = Param(nameof(TeethShift), 5)
		.SetDisplay("Teeth Shift", "Forward shift for the teeth", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Lips Period", "Alligator lips period", "Alligator");

		_lipsShift = Param(nameof(LipsShift), 3)
		.SetDisplay("Lips Shift", "Forward shift for the lips", "Alligator");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "RSI averaging length", "RSI");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
		.SetDisplay("Use Time Filter", "Limit trading to specific hours", "Session");

		_startHour = Param(nameof(StartHour), 10)
		.SetDisplay("Start Hour", "Session start hour (0-23)", "Session");

		_endHour = Param(nameof(EndHour), 15)
		.SetDisplay("End Hour", "Session end hour (0-23)", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("MACD Timeframe", "Primary timeframe for MACD", "Timeframes");

		_alligatorCandleType = Param(nameof(AlligatorCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Alligator Timeframe", "Higher timeframe for Alligator", "Timeframes");

		_rsiCandleType = Param(nameof(RsiCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("RSI Timeframe", "Intermediate timeframe for RSI", "Timeframes");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[]
		{
			(Security, CandleType),
			(Security, AlligatorCandleType),
			(Security, RsiCandleType)
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_macd = null!;
		_jaw = null!;
		_teeth = null!;
		_lips = null!;
		_rsi = null!;

		_jawHistory = Array.Empty<decimal?>();
		_teethHistory = Array.Empty<decimal?>();
		_lipsHistory = Array.Empty<decimal?>();
		_macdMainHistory = Array.Empty<decimal?>();
		_macdSignalHistory = Array.Empty<decimal?>();

		_latestRsi = null;
		_pipSize = 0m;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		_pipSize = CalculatePipSize();

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		_jaw = new SmoothedMovingAverage { Length = JawPeriod };
		_teeth = new SmoothedMovingAverage { Length = TeethPeriod };
		_lips = new SmoothedMovingAverage { Length = LipsPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		_jawHistory = CreateHistoryBuffer(JawShift);
		_teethHistory = CreateHistoryBuffer(TeethShift);
		_lipsHistory = CreateHistoryBuffer(LipsShift);
		_macdMainHistory = new decimal?[3];
		_macdSignalHistory = new decimal?[3];

		var macdSubscription = SubscribeCandles(CandleType);
		macdSubscription
		.BindEx(_macd, ProcessMacdCandle)
		.Start();

		var alligatorSubscription = SubscribeCandles(AlligatorCandleType);
		alligatorSubscription
		.Bind(ProcessAlligatorCandle)
		.Start();

		var rsiSubscription = SubscribeCandles(RsiCandleType);
		rsiSubscription
		.Bind(_rsi, ProcessRsiCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, macdSubscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_longStopPrice = null;
			_shortStopPrice = null;
			return;
		}

		var entryPrice = PositionPrice;
		var stopDistance = ConvertPipsToPrice(StopLossPips);

		if (Position > 0m && delta > 0m)
		{
			_longEntryPrice = entryPrice;
			_longStopPrice = stopDistance > 0m ? entryPrice - stopDistance : null;
		}
		else if (Position < 0m && delta < 0m)
		{
			_shortEntryPrice = entryPrice;
			_shortStopPrice = stopDistance > 0m ? entryPrice + stopDistance : null;
		}
	}

	private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateTrailing(candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsWithinTradingHours(candle.CloseTime))
		return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macd.Macd is not decimal macdMain || macd.Signal is not decimal macdSignal)
		return;

		UpdateCircularHistory(_macdMainHistory, macdMain);
		UpdateCircularHistory(_macdSignalHistory, macdSignal);

		if (!HasHistory(_macdMainHistory, 3) || !HasHistory(_macdSignalHistory, 3))
		return;

		if (_latestRsi is null)
		return;

		if (!TryGetShiftedValue(_jawHistory, JawShift, 1, out var jawPrev) ||
		!TryGetShiftedValue(_teethHistory, TeethShift, 1, out var teethPrev) ||
		!TryGetShiftedValue(_lipsHistory, LipsShift, 1, out var lipsPrev))
		{
			return;
		}

		var macdMainPrev1 = _macdMainHistory[1]!.Value;
		var macdMainPrev2 = _macdMainHistory[2]!.Value;
		var macdSignalPrev1 = _macdSignalHistory[1]!.Value;
		var macdSignalPrev2 = _macdSignalHistory[2]!.Value;

		var rsiValue = _latestRsi.Value;

		var buySignal = macdMainPrev1 < macdSignalPrev1 &&
		macdMainPrev2 > macdSignalPrev2 &&
		rsiValue > 50m &&
		jawPrev > teethPrev &&
		teethPrev > lipsPrev;

		var sellSignal = macdMainPrev1 > macdSignalPrev1 &&
		macdMainPrev2 < macdSignalPrev2 &&
		rsiValue < 50m &&
		lipsPrev > teethPrev &&
		teethPrev > jawPrev;

		if (buySignal)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (sellSignal)
		{
			EnterShort(candle.ClosePrice);
		}
	}

	private void ProcessAlligatorCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawValue = _jaw.Process(median, candle.OpenTime, true);
		var teethValue = _teeth.Process(median, candle.OpenTime, true);
		var lipsValue = _lips.Process(median, candle.OpenTime, true);

		if (!jawValue.IsFinal || !teethValue.IsFinal || !lipsValue.IsFinal)
		return;

		UpdateHistory(_jawHistory, jawValue.ToDecimal());
		UpdateHistory(_teethHistory, teethValue.ToDecimal());
		UpdateHistory(_lipsHistory, lipsValue.ToDecimal());
	}

	private void ProcessRsiCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_latestRsi = rsiValue;
	}

	private void EnterLong(decimal price)
	{
		decimal volumeToBuy;
		decimal resultingPosition;

		if (Position < 0m)
		{
			volumeToBuy = Volume + Math.Abs(Position);
			resultingPosition = Position + volumeToBuy;
			BuyMarket(volumeToBuy);
		}
		else if (Position == 0m)
		{
			volumeToBuy = Volume;
			resultingPosition = Position + volumeToBuy;
			BuyMarket(volumeToBuy);
		}
		else
		{
			return;
		}

		ApplyRiskControls(price, resultingPosition);
	}

	private void EnterShort(decimal price)
	{
		decimal volumeToSell;
		decimal resultingPosition;

		if (Position > 0m)
		{
			volumeToSell = Volume + Math.Max(Position, 0m);
			resultingPosition = Position - volumeToSell;
			SellMarket(volumeToSell);
		}
		else if (Position == 0m)
		{
			volumeToSell = Volume;
			resultingPosition = Position - volumeToSell;
			SellMarket(volumeToSell);
		}
		else
		{
			return;
		}

		ApplyRiskControls(price, resultingPosition);
	}

	private void ApplyRiskControls(decimal referencePrice, decimal resultingPosition)
	{
		var stopDistance = ConvertPipsToPrice(StopLossPips);
		var takeDistance = ConvertPipsToPrice(TakeProfitPips);

		if (stopDistance > 0m)
		{
			SetStopLoss(stopDistance, referencePrice, resultingPosition);
		}

		if (takeDistance > 0m)
		{
			SetTakeProfit(takeDistance, referencePrice, resultingPosition);
		}
	}

	private void UpdateTrailing(decimal currentPrice)
	{
		var trailingDistance = ConvertPipsToPrice(TrailingStopPips);
		var trailingStep = ConvertPipsToPrice(TrailingStepPips);

		if (trailingDistance <= 0m)
		return;

		if (Position > 0m && _longEntryPrice is decimal entry)
		{
			var gain = currentPrice - entry;
			if (gain >= trailingDistance + trailingStep)
			{
				var desiredStop = currentPrice - trailingDistance;
				if (_longStopPrice is null || desiredStop - _longStopPrice.Value >= trailingStep)
				{
					SetStopLoss(trailingDistance, currentPrice, Position);
					_longStopPrice = desiredStop;
				}
			}
		}
		else if (Position < 0m && _shortEntryPrice is decimal entryShort)
		{
			var gain = entryShort - currentPrice;
			if (gain >= trailingDistance + trailingStep)
			{
				var desiredStop = currentPrice + trailingDistance;
				if (_shortStopPrice is null || _shortStopPrice.Value - desiredStop >= trailingStep)
				{
					SetStopLoss(trailingDistance, currentPrice, Position);
					_shortStopPrice = desiredStop;
				}
			}
		}
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		if (!UseTimeFilter)
		return true;

		var hour = time.Hour;

		if (StartHour == EndHour)
		return false;

		if (StartHour < EndHour)
		return hour >= StartHour && hour < EndHour;

		return hour >= StartHour || hour < EndHour;
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
		var factor = decimals == 3 || decimals == 5 ? 10m : 1m;
		return priceStep * factor;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);
		if (value == 0m)
		return 0;

		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}

	private static decimal?[] CreateHistoryBuffer(int shift)
	{
		var length = Math.Max(shift + 3, 3);
		return new decimal?[length];
	}

	private static void UpdateHistory(decimal?[] history, decimal value)
	{
		for (var i = history.Length - 1; i > 0; i--)
		{
			history[i] = history[i - 1];
		}

		history[0] = value;
	}

	private static void UpdateCircularHistory(decimal?[] history, decimal value)
	{
		for (var i = history.Length - 1; i > 0; i--)
		{
			history[i] = history[i - 1];
		}

		history[0] = value;
	}

	private static bool HasHistory(decimal?[] history, int required)
	{
		for (var i = 0; i < required; i++)
		{
			if (history[i] is null)
			return false;
		}

		return true;
	}

	private static bool TryGetShiftedValue(decimal?[] history, int shift, int offset, out decimal value)
	{
		var index = shift + offset;
		if (index >= history.Length)
		{
			value = 0m;
			return false;
		}

		var stored = history[index];
		if (stored is null)
		{
			value = 0m;
			return false;
		}

		value = stored.Value;
		return true;
	}
}

