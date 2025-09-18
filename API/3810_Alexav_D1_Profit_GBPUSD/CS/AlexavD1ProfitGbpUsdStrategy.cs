using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy for GBPUSD that mirrors the Alexav D1 Profit expert advisor.
/// Combines exponential moving averages of highs and lows with RSI, MACD, and ATR filters
/// to open batches of four scaled positions with ATR-based stops and staggered profit targets.
/// </summary>
public class AlexavD1ProfitGbpUsdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrStopMultiplier;
	private readonly StrategyParam<decimal> _atrTargetMultiplier;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiUpperLimit;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<decimal> _rsiLowerLimit;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _signalMaPeriod;
	private readonly StrategyParam<decimal> _macdDiffBuy;
	private readonly StrategyParam<decimal> _macdDiffSell;

	private EMA _emaHigh;
	private EMA _emaLow;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;
	private MovingAverageConvergenceDivergenceSignal _macd;

	private ICandleMessage _previousCandle;
	private DateTime? _lastProcessedDate;

	private decimal? _emaHighPrev1;
	private decimal? _emaHighPrev2;
	private decimal? _emaLowPrev1;
	private decimal? _emaLowPrev2;
	private decimal? _rsiPrev;
	private decimal? _atrPrev;
	private decimal? _macdPrev1;
	private decimal? _macdPrev2;

	private readonly decimal[] _longTargets = new decimal[4];
	private readonly decimal[] _longStops = new decimal[4];
	private readonly bool[] _longActive = new bool[4];
	private readonly decimal[] _shortTargets = new decimal[4];
	private readonly decimal[] _shortStops = new decimal[4];
	private readonly bool[] _shortActive = new bool[4];

	private bool _blockBuy;
	private bool _blockSell;
	private bool _lastBuyTag;
	private bool _lastSellTag;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for the exponential moving averages applied to highs and lows.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// RSI period for momentum filtering.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// ATR averaging period used for stops and targets.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for the initial protective stop.
	/// </summary>
	public decimal AtrStopMultiplier
	{
		get => _atrStopMultiplier.Value;
		set => _atrStopMultiplier.Value = value;
	}

	/// <summary>
	/// Base multiplier applied to ATR for staggered profit targets.
	/// </summary>
	public decimal AtrTargetMultiplier
	{
		get => _atrTargetMultiplier.Value;
		set => _atrTargetMultiplier.Value = value;
	}

	/// <summary>
	/// RSI upper threshold that confirms bullish momentum.
	/// </summary>
	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	/// <summary>
	/// RSI overbought level that blocks new long setups.
	/// </summary>
	public decimal RsiUpperLimit
	{
		get => _rsiUpperLimit.Value;
		set => _rsiUpperLimit.Value = value;
	}

	/// <summary>
	/// RSI lower threshold that confirms bearish momentum.
	/// </summary>
	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	/// <summary>
	/// RSI oversold level that blocks new short setups.
	/// </summary>
	public decimal RsiLowerLimit
	{
		get => _rsiLowerLimit.Value;
		set => _rsiLowerLimit.Value = value;
	}

	/// <summary>
	/// Fast EMA period for the MACD calculation.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the MACD calculation.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period for the MACD calculation.
	/// </summary>
	public int SignalMaPeriod
	{
		get => _signalMaPeriod.Value;
		set => _signalMaPeriod.Value = value;
	}

	/// <summary>
	/// Minimum positive MACD acceleration required for long entries.
	/// </summary>
	public decimal MacdDiffBuy
	{
		get => _macdDiffBuy.Value;
		set => _macdDiffBuy.Value = value;
	}

	/// <summary>
	/// Minimum positive MACD acceleration required for short entries.
	/// </summary>
	public decimal MacdDiffSell
	{
		get => _macdDiffSell.Value;
		set => _macdDiffSell.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AlexavD1ProfitGbpUsdStrategy"/>.
	/// </summary>
	public AlexavD1ProfitGbpUsdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "General");

		_maPeriod = Param(nameof(MaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "Length of the EMA applied to highs and lows", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(3, 15, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Length of the RSI filter", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 28)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Length of the ATR used for money management", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 2);

		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 1.6m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Stop Multiplier", "ATR multiplier for stop-loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.8m, 3m, 0.2m);

		_atrTargetMultiplier = Param(nameof(AtrTargetMultiplier), 1m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Target Multiplier", "Base ATR multiplier for profit targets", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.1m);

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 60m)
		.SetDisplay("RSI Upper Level", "Momentum confirmation level for longs", "Filters");

		_rsiUpperLimit = Param(nameof(RsiUpperLimit), 80m)
		.SetDisplay("RSI Upper Limit", "Overbought level blocking new longs", "Filters");

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 39m)
		.SetDisplay("RSI Lower Level", "Momentum confirmation level for shorts", "Filters");

		_rsiLowerLimit = Param(nameof(RsiLowerLimit), 25m)
		.SetDisplay("RSI Lower Limit", "Oversold level blocking new shorts", "Filters");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast EMA", "Fast EMA for the MACD", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 24)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow EMA", "Slow EMA for the MACD", "Indicators");

		_signalMaPeriod = Param(nameof(SignalMaPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal EMA", "Signal EMA for the MACD", "Indicators");

		_macdDiffBuy = Param(nameof(MacdDiffBuy), 0.5m)
		.SetDisplay("MACD Diff Buy", "Minimum MACD change for long entries", "Filters");

		_macdDiffSell = Param(nameof(MacdDiffSell), 0.15m)
		.SetDisplay("MACD Diff Sell", "Minimum MACD change for short entries", "Filters");
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

		_previousCandle = null;
		_lastProcessedDate = null;

		_emaHighPrev1 = null;
		_emaHighPrev2 = null;
		_emaLowPrev1 = null;
		_emaLowPrev2 = null;
		_rsiPrev = null;
		_atrPrev = null;
		_macdPrev1 = null;
		_macdPrev2 = null;

		Array.Clear(_longTargets, 0, _longTargets.Length);
		Array.Clear(_longStops, 0, _longStops.Length);
		Array.Clear(_longActive, 0, _longActive.Length);
		Array.Clear(_shortTargets, 0, _shortTargets.Length);
		Array.Clear(_shortStops, 0, _shortStops.Length);
		Array.Clear(_shortActive, 0, _shortActive.Length);

		_blockBuy = false;
		_blockSell = false;
		_lastBuyTag = false;
		_lastSellTag = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaHigh = new EMA { Length = MaPeriod };
		_emaLow = new EMA { Length = MaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastMaPeriod },
				LongMa = { Length = SlowMaPeriod },
			},
			SignalMa = { Length = SignalMaPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaHigh);
			DrawIndicator(area, _emaLow);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageActivePositions(candle);

		var emaHighValue = _emaHigh.Process(new CandleIndicatorValue(candle, candle.HighPrice));
		var emaLowValue = _emaLow.Process(new CandleIndicatorValue(candle, candle.LowPrice));
		var rsiValue = _rsi.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		var atrValue = _atr.Process(new CandleIndicatorValue(_atr, candle));
		var macdValue = _macd.Process(new CandleIndicatorValue(candle, candle.ClosePrice));

		if (!emaHighValue.IsFinal || !emaLowValue.IsFinal || !rsiValue.IsFinal || !atrValue.IsFinal || !macdValue.IsFinal)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
			return;

		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;

		var histogram = macdLine - signalLine;
		var emaHigh = emaHighValue.ToDecimal();
		var emaLow = emaLowValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var atr = atrValue.ToDecimal();

		TryGenerateSignals();

		_emaHighPrev2 = _emaHighPrev1;
		_emaHighPrev1 = emaHigh;
		_emaLowPrev2 = _emaLowPrev1;
		_emaLowPrev1 = emaLow;
		_rsiPrev = rsi;
		_atrPrev = atr;
		_macdPrev2 = _macdPrev1;
		_macdPrev1 = histogram;
		_previousCandle = candle;
	}

	private void TryGenerateSignals()
	{
		if (_previousCandle == null || !_emaHighPrev2.HasValue || !_emaLowPrev2.HasValue || !_rsiPrev.HasValue || !_atrPrev.HasValue || !_macdPrev1.HasValue || !_macdPrev2.HasValue)
			return;

		var previousDate = _previousCandle.OpenTime.Date;
		if (_lastProcessedDate == previousDate)
			return;

		var day = _previousCandle.OpenTime.DayOfWeek;
		if (day is not DayOfWeek.Tuesday and not DayOfWeek.Wednesday and not DayOfWeek.Thursday and not DayOfWeek.Friday)
			return;

		_lastProcessedDate = previousDate;

		if (_previousCandle.OpenPrice < _emaHighPrev2.Value)
		{
			_lastBuyTag = false;
		}

		if (_previousCandle.OpenPrice > _emaLowPrev2.Value)
		{
			_lastSellTag = false;
		}

		if (_rsiPrev.Value >= RsiUpperLimit)
		{
			_blockBuy = true;
		}

		if (_rsiPrev.Value <= RsiLowerLimit)
		{
			_blockSell = true;
		}

		var canBuy = !_blockBuy && !_lastBuyTag && Position <= 0;
		var canSell = !_blockSell && !_lastSellTag && Position >= 0;

		var bullishBreakout = _previousCandle.ClosePrice > _emaHighPrev2.Value && _previousCandle.OpenPrice < _emaHighPrev2.Value;
		var bullishContinuation = _previousCandle.ClosePrice > _emaHighPrev2.Value && _previousCandle.OpenPrice >= _emaHighPrev2.Value;
		var bullishMomentum = _rsiPrev.Value > RsiUpperLevel;
		var macdAcceleration = _macdPrev1.Value != 0m ? (_macdPrev1.Value - _macdPrev2.Value) / _macdPrev1.Value : 0m;
		var macdFilter = _macdPrev2.Value < 0m || macdAcceleration > MacdDiffBuy;

		if (canBuy && bullishMomentum && (bullishBreakout || bullishContinuation) && macdFilter)
		{
			EnterLongBatch(_previousCandle.ClosePrice, _atrPrev.Value);
			_blockSell = false;
			_lastBuyTag = true;
		}

		var bearishBreakout = _previousCandle.ClosePrice < _emaLowPrev2.Value && _previousCandle.OpenPrice > _emaLowPrev2.Value;
		var bearishContinuation = _previousCandle.ClosePrice < _emaLowPrev2.Value && _previousCandle.OpenPrice <= _emaLowPrev2.Value;
		var bearishMomentum = _rsiPrev.Value < RsiLowerLevel;
		var macdAccelerationShort = _macdPrev1.Value != 0m ? (_macdPrev1.Value - _macdPrev2.Value) / _macdPrev1.Value : 0m;
		var macdFilterShort = _macdPrev2.Value > 0m || macdAccelerationShort > MacdDiffSell;

		if (canSell && bearishMomentum && (bearishBreakout || bearishContinuation) && macdFilterShort)
		{
			EnterShortBatch(_previousCandle.ClosePrice, _atrPrev.Value);
			_blockBuy = false;
			_lastSellTag = true;
		}
	}

	private void EnterLongBatch(decimal entryPrice, decimal atr)
	{
		if (atr <= 0m)
			return;

		if (Position < 0)
		{
			BuyMarket(-Position);
		}

		for (var i = 0; i < _longActive.Length; i++)
		{
			BuyMarket(Volume);
			var targetMultiplier = (i / 2m) + 1m;
			_longTargets[i] = entryPrice + AtrTargetMultiplier * targetMultiplier * atr;
			_longStops[i] = entryPrice - AtrStopMultiplier * atr;
			_longActive[i] = true;
		}

		Array.Clear(_shortActive, 0, _shortActive.Length);
	}

	private void EnterShortBatch(decimal entryPrice, decimal atr)
	{
		if (atr <= 0m)
			return;

		if (Position > 0)
		{
			SellMarket(Position);
		}

		for (var i = 0; i < _shortActive.Length; i++)
		{
			SellMarket(Volume);
			var targetMultiplier = (i / 2m) + 1m;
			_shortTargets[i] = entryPrice - AtrTargetMultiplier * targetMultiplier * atr;
			_shortStops[i] = entryPrice + AtrStopMultiplier * atr;
			_shortActive[i] = true;
		}

		Array.Clear(_longActive, 0, _longActive.Length);
	}

	private void ManageActivePositions(ICandleMessage candle)
	{
		if (Position > 0)
		{
			for (var i = 0; i < _longActive.Length; i++)
			{
				if (!_longActive[i])
					continue;

				if (candle.LowPrice <= _longStops[i])
				{
					SellMarket(Math.Min(Volume, Position));
					_longActive[i] = false;
				}
				else if (candle.HighPrice >= _longTargets[i])
				{
					SellMarket(Math.Min(Volume, Position));
					_longActive[i] = false;
				}
			}
		}
		else if (Position < 0)
		{
			for (var i = 0; i < _shortActive.Length; i++)
			{
				if (!_shortActive[i])
					continue;

				if (candle.HighPrice >= _shortStops[i])
				{
					BuyMarket(Math.Min(Volume, -Position));
					_shortActive[i] = false;
				}
				else if (candle.LowPrice <= _shortTargets[i])
				{
					BuyMarket(Math.Min(Volume, -Position));
					_shortActive[i] = false;
				}
			}
		}
	}
}
