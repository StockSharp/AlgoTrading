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
/// Ichimoku retracement strategy converted from the MetaTrader "ICHMOKU RETRACEMENT" expert advisor.
/// </summary>
public class IchimokuRetracementStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _macroCandleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _spanBPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Ichimoku _ichimoku = null!;
	private MovingAverageConvergenceDivergenceSignal _monthlyMacd = null!;

	private decimal? _fastValue;
	private decimal? _slowValue;
	private decimal? _tenkanValue;
	private decimal? _kijunValue;
	private decimal? _senkouAValue;
	private decimal? _senkouBValue;
	private decimal? _macdMain;
	private decimal? _macdSignal;
	private bool _macdReady;
	private bool _maReady;
	private bool _ichimokuReady;
	private bool _evaluationDone;
	private DateTimeOffset? _currentCandleTime;
	private decimal? _prevOpen;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevClose;
	private decimal? _momentumDeviation;
	private readonly Queue<decimal> _momentumWindow = new();
	private decimal _pipSize;
	private decimal _entryPrice;
	private Sides? _currentSide;

	/// <summary>
	/// Initializes a new instance of <see cref="IchimokuRetracementStrategy"/>.
	/// </summary>
	public IchimokuRetracementStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Signal Candle Type", "Primary timeframe used for signal generation", "General");

		_macroCandleType = Param(nameof(MacroCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("Macro Candle Type", "Higher timeframe used for the MACD trend filter", "Filters");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Trend")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Trend")
			.SetCanOptimize(true);

		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen period used by the Ichimoku cloud", "Ichimoku")
			.SetCanOptimize(true);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen period used by the Ichimoku cloud", "Ichimoku")
			.SetCanOptimize(true);

		_spanBPeriod = Param(nameof(SpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Span B Period", "Senkou Span B period used by the Ichimoku cloud", "Ichimoku")
			.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Lookback used for the momentum retracement test", "Momentum")
			.SetCanOptimize(true);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Threshold", "Minimum distance from 100 for the momentum ratio", "Momentum")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Primary candle type used for trading decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used to evaluate the MACD trend filter.
	/// </summary>
	public DataType MacroCandleType
	{
		get => _macroCandleType.Value;
		set => _macroCandleType.Value = value;
	}

	/// <summary>
	/// Length of the fast linear weighted moving average.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow linear weighted moving average.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Tenkan-sen period for the Ichimoku cloud.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period for the Ichimoku cloud.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period for the Ichimoku cloud.
	/// </summary>
	public int SpanBPeriod
	{
		get => _spanBPeriod.Value;
		set => _spanBPeriod.Value = value;
	}

	/// <summary>
	/// Lookback used to compute the momentum ratio.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum deviation from 100 required by the momentum ratio.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
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
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastValue = null;
		_slowValue = null;
		_tenkanValue = null;
		_kijunValue = null;
		_senkouAValue = null;
		_senkouBValue = null;
		_macdMain = null;
		_macdSignal = null;
		_macdReady = false;
		_maReady = false;
		_ichimokuReady = false;
		_evaluationDone = false;
		_currentCandleTime = null;
		_prevOpen = null;
		_prevHigh = null;
		_prevLow = null;
		_prevClose = null;
		_momentumDeviation = null;
		_momentumWindow.Clear();
		_pipSize = 0m;
		_entryPrice = 0m;
		_currentSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Derive the pip size from the security definition or fall back to a default.
		_pipSize = Security?.PriceStep ?? 0.0001m;

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod };

		_ichimoku = new Ichimoku
		{
			TenkanLength = TenkanPeriod,
			KijunLength = KijunPeriod,
			SenkouSpanBLength = SpanBPeriod,
			Displacement = KijunPeriod
		};

		_monthlyMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		var subscription = SubscribeCandles(CandleType, allowBuildFromSmallerTimeFrame: true);
		subscription
			.Bind(_fastMa, _slowMa, ProcessTrendCandle)
			.BindEx(_ichimoku, ProcessIchimokuValues)
			.Start();

		var macroSubscription = SubscribeCandles(MacroCandleType, allowBuildFromSmallerTimeFrame: true);
		macroSubscription
			.BindEx(_monthlyMacd, ProcessMacroCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _ichimoku);
			DrawOwnTrades(area);
		}

		var macdArea = CreateChartArea();
		if (macdArea != null)
		{
			DrawIndicator(macdArea, _monthlyMacd);
		}
	}

	private void ProcessTrendCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ResetCandleState(candle);

		_fastValue = fastValue;
		_slowValue = slowValue;

		UpdateMomentum(candle.ClosePrice);

		if (_momentumDeviation is null)
			return;

		_maReady = true;

		TryEvaluate(candle);
	}

	private void ProcessIchimokuValues(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ResetCandleState(candle);

		if (!indicatorValue.IsFinal)
			return;

		var ichimokuValue = (IchimokuValue)indicatorValue;
		if (ichimokuValue.Tenkan is not decimal tenkan ||
			ichimokuValue.Kijun is not decimal kijun ||
			ichimokuValue.SenkouA is not decimal spanA ||
			ichimokuValue.SenkouB is not decimal spanB)
		{
			return;
		}

		_tenkanValue = tenkan;
		_kijunValue = kijun;
		_senkouAValue = spanA;
		_senkouBValue = spanB;
		_ichimokuReady = true;

		TryEvaluate(candle);
	}

	private void ProcessMacroCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal)
			return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdLine || macd.Signal is not decimal signalLine)
			return;

		_macdMain = macdLine;
		_macdSignal = signalLine;
		_macdReady = true;
	}

	private void ResetCandleState(ICandleMessage candle)
	{
		if (_currentCandleTime == candle.OpenTime)
			return;

		_currentCandleTime = candle.OpenTime;
		_maReady = false;
		_ichimokuReady = false;
		_evaluationDone = false;
	}

	private void TryEvaluate(ICandleMessage candle)
	{
		if (_evaluationDone)
			return;

		if (!_maReady || !_ichimokuReady || !_macdReady)
			return;

		if (_fastValue is not decimal fast ||
			_slowValue is not decimal slow ||
			_tenkanValue is not decimal tenkan ||
			_kijunValue is not decimal kijun ||
			_senkouAValue is not decimal spanA ||
			_senkouBValue is not decimal spanB ||
			_macdMain is not decimal macdMain ||
			_macdSignal is not decimal macdSignal ||
			_momentumDeviation is not decimal momentumDeviation)
		{
			return;
		}

		if (_prevOpen is null || _prevHigh is null || _prevLow is null || _prevClose is null)
		{
			UpdatePreviousCandle(candle);
			_evaluationDone = true;
			return;
		}

		// Manage open risk before considering new signals.
		ManageOpenPosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePreviousCandle(candle);
			_evaluationDone = true;
			return;
		}

		var bullishRetracement = CheckBullishRetracement(candle.OpenPrice, tenkan, kijun, spanA, spanB);
		var bearishRetracement = CheckBearishRetracement(candle.OpenPrice, tenkan, kijun, spanA, spanB);

		var macdBullish = macdMain > macdSignal;
		var macdBearish = macdMain < macdSignal;

		if (Position <= 0 && fast > slow && macdBullish && momentumDeviation >= MomentumThreshold && bullishRetracement)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_currentSide = Sides.Buy;
			LogInfo($"Bullish retracement entry at {candle.ClosePrice}. Momentum deviation: {momentumDeviation}, MACD: {macdMain} > {macdSignal}.");
		}
		else if (Position >= 0 && fast < slow && macdBearish && momentumDeviation >= MomentumThreshold && bearishRetracement)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			_currentSide = Sides.Sell;
			LogInfo($"Bearish retracement entry at {candle.ClosePrice}. Momentum deviation: {momentumDeviation}, MACD: {macdMain} < {macdSignal}.");
		}

		UpdatePreviousCandle(candle);
		_evaluationDone = true;
	}

	private bool CheckBullishRetracement(decimal currentOpen, decimal tenkan, decimal kijun, decimal spanA, decimal spanB)
	{
		if (_prevLow is not decimal prevLow)
			return false;

		var touchedTenkan = prevLow <= tenkan && currentOpen > tenkan;
		var touchedKijun = prevLow <= kijun && currentOpen > kijun;
		var touchedSpanA = prevLow <= spanA && currentOpen > spanA;
		var touchedSpanB = prevLow <= spanB && currentOpen > spanB;

		return touchedTenkan || touchedKijun || touchedSpanA || touchedSpanB;
	}

	private bool CheckBearishRetracement(decimal currentOpen, decimal tenkan, decimal kijun, decimal spanA, decimal spanB)
	{
		if (_prevHigh is not decimal prevHigh)
			return false;

		var touchedTenkan = prevHigh >= tenkan && currentOpen < tenkan;
		var touchedKijun = prevHigh >= kijun && currentOpen < kijun;
		var touchedSpanA = prevHigh >= spanA && currentOpen < spanA;
		var touchedSpanB = prevHigh >= spanB && currentOpen < spanB;

		return touchedTenkan || touchedKijun || touchedSpanA || touchedSpanB;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (_entryPrice == 0m || _currentSide is null)
			return;

		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		if (_currentSide == Sides.Buy && Position > 0)
		{
			var stopPrice = _entryPrice - stopDistance;
			var takePrice = _entryPrice + takeDistance;

			if (candle.LowPrice <= stopPrice)
			{
				SellMarket(Position);
				LogInfo($"Stop-loss hit for long position at {stopPrice}.");
				_entryPrice = 0m;
				_currentSide = null;
			}
			else if (candle.HighPrice >= takePrice)
			{
				SellMarket(Position);
				LogInfo($"Take-profit hit for long position at {takePrice}.");
				_entryPrice = 0m;
				_currentSide = null;
			}
		}
		else if (_currentSide == Sides.Sell && Position < 0)
		{
			var stopPrice = _entryPrice + stopDistance;
			var takePrice = _entryPrice - takeDistance;

			if (candle.HighPrice >= stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Stop-loss hit for short position at {stopPrice}.");
				_entryPrice = 0m;
				_currentSide = null;
			}
			else if (candle.LowPrice <= takePrice)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Take-profit hit for short position at {takePrice}.");
				_entryPrice = 0m;
				_currentSide = null;
			}
		}
	}

	private void UpdatePreviousCandle(ICandleMessage candle)
	{
		_prevOpen = candle.OpenPrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;
	}

	private void UpdateMomentum(decimal closePrice)
	{
		var period = MomentumPeriod;
		if (period <= 0)
		{
			_momentumWindow.Clear();
			_momentumDeviation = null;
			return;
		}

		if (_momentumWindow.Count == period)
		{
			var pricePeriodAgo = _momentumWindow.Peek();
			if (pricePeriodAgo != 0m)
			{
				var ratio = closePrice / pricePeriodAgo * 100m;
				_momentumDeviation = Math.Abs(100m - ratio);
			}
			else
			{
				_momentumDeviation = null;
			}

			_momentumWindow.Dequeue();
		}
		else
		{
			_momentumDeviation = null;
		}

		_momentumWindow.Enqueue(closePrice);
	}
}

