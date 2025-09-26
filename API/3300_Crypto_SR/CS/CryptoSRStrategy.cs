namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Crypto support and resistance strategy converted from the "Crypto S&R" MetaTrader expert advisor.
/// Combines LWMA trend detection, higher timeframe momentum, and a long-term MACD filter with fractal-based support/resistance levels.
/// </summary>
public class CryptoSrStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<int> _fractalWindowLength;
	private readonly StrategyParam<decimal> _fractalBufferPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _longTermCandleType;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _longTermMacd = null!;

	private readonly Queue<decimal> _momentumValues = new();
	private readonly Queue<ICandleMessage> _fractalBuffer = new();

	private decimal? _fastMaValue;
	private decimal? _slowMaValue;
	private decimal? _longMacdValue;
	private decimal? _longSignalValue;

	private decimal? _lastSupportLevel;
	private decimal? _lastResistanceLevel;
	private DateTimeOffset? _lastSupportTime;
	private DateTimeOffset? _lastResistanceTime;

	private decimal _pipSize;
	private decimal? _entryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _longHighestPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private decimal? _shortLowestPrice;
	private int _positionDirection;

	private ICandleMessage _previousCandle;

	/// <summary>
	/// Initializes a new instance of the <see cref="CryptoSrStrategy"/> class.
	/// </summary>
	public CryptoSrStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Length of the fast LWMA calculated from typical price", "Trend")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Length of the slow LWMA calculated from typical price", "Trend")
			.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Period of the higher timeframe momentum filter", "Filters")
			.SetCanOptimize(true);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Buy Threshold", "Minimum |Momentum-100| to allow long entries", "Filters")
			.SetCanOptimize(true);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Sell Threshold", "Minimum |Momentum-100| to allow short entries", "Filters")
			.SetCanOptimize(true);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for the long-term MACD filter", "Filters");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for the long-term MACD filter", "Filters");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA length for the long-term MACD filter", "Filters");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetDisplay("Stop Loss (pips)", "Distance of the protective stop in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Distance of the profit target in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
			.SetDisplay("Trailing Stop (pips)", "Distance of the price-based trailing stop", "Risk")
			.SetCanOptimize(true);

		_useBreakEven = Param(nameof(UseBreakEven), true)
			.SetDisplay("Use Break Even", "Move the stop to break even after sufficient profit", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
			.SetDisplay("Break Even Trigger", "Profit in pips required before moving to break even", "Risk")
			.SetCanOptimize(true);

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
			.SetDisplay("Break Even Offset", "Additional offset applied when moving the stop", "Risk")
			.SetCanOptimize(true);

		_fractalWindowLength = Param(nameof(FractalWindowLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fractal Window", "Number of bars kept to confirm fractal highs and lows", "Fractals");

		_fractalBufferPips = Param(nameof(FractalBufferPips), 10m)
			.SetDisplay("Fractal Buffer (pips)", "Additional buffer applied around support/resistance lines", "Fractals");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume for entries", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary Candles", "Timeframe used for LWMA and fractal calculations", "Data");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Momentum Candles", "Higher timeframe used for the momentum filter", "Data");

		_longTermCandleType = Param(nameof(LongTermCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Long-Term Candles", "Timeframe used for the MACD trend filter", "Data");
	}

	/// <summary>
	/// Fast LWMA length.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow LWMA length.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum calculation period on the higher timeframe.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum deviation of Momentum from 100 to allow long trades.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum deviation of Momentum from 100 to allow short trades.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the MACD filter.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the MACD filter.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA length for the MACD filter.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
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
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enable or disable break-even logic.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit distance required before moving to break-even.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional offset applied when moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Number of candles kept to detect fractals.
	/// </summary>
	public int FractalWindowLength
	{
		get => _fractalWindowLength.Value;
		set => _fractalWindowLength.Value = value;
	}

	/// <summary>
	/// Buffer around fractal levels expressed in pips.
	/// </summary>
	public decimal FractalBufferPips
	{
		get => _fractalBufferPips.Value;
		set => _fractalBufferPips.Value = value;
	}

	/// <summary>
	/// Trading volume used for each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Primary candle series.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for the momentum filter.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Long-term candle series used for the MACD filter.
	/// </summary>
	public DataType LongTermCandleType
	{
		get => _longTermCandleType.Value;
		set => _longTermCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (!HigherCandleType.Equals(CandleType))
			yield return (Security, HigherCandleType);

		if (!LongTermCandleType.Equals(CandleType) && !LongTermCandleType.Equals(HigherCandleType))
			yield return (Security, LongTermCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_momentumValues.Clear();
		_fractalBuffer.Clear();
		_fastMaValue = null;
		_slowMaValue = null;
		_longMacdValue = null;
		_longSignalValue = null;
		_lastSupportLevel = null;
		_lastResistanceLevel = null;
		_lastSupportTime = null;
		_lastResistanceTime = null;
		_pipSize = 0m;
		_entryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_longHighestPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_shortLowestPrice = null;
		_positionDirection = 0;
		_previousCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		StartProtection();

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_longTermMacd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		_pipSize = CalculatePipSize();

		var primarySubscription = SubscribeCandles(CandleType);
		primarySubscription.Bind(ProcessPrimaryCandle).Start();

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription.Bind(ProcessHigherCandle).Start();

		var longTermSubscription = SubscribeCandles(LongTermCandleType);
		longTermSubscription.BindEx(_longTermMacd, ProcessLongTermCandle).Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, primarySubscription);
			DrawIndicator(priceArea, _fastMa, "Fast LWMA");
			DrawIndicator(priceArea, _slowMa, "Slow LWMA");
			DrawOwnTrades(priceArea);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var direction = Math.Sign(Position);
		if (direction == 0)
		{
			_entryPrice = null;
			_positionDirection = 0;
			ResetLongRisk();
			ResetShortRisk();
			return;
		}

		var tradePrice = trade.Trade?.Price;
		if (tradePrice is null)
		return;

		if (direction > 0 && _positionDirection <= 0)
		{
			_entryPrice = tradePrice;
			_positionDirection = 1;
			InitializeLongRisk();
		}
		else if (direction < 0 && _positionDirection >= 0)
		{
			_entryPrice = tradePrice;
			_positionDirection = -1;
			InitializeShortRisk();
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var typicalPrice = GetTypicalPrice(candle);
		var fastResult = _fastMa.Process(new DecimalIndicatorValue(_fastMa, typicalPrice, candle.OpenTime));
		var slowResult = _slowMa.Process(new DecimalIndicatorValue(_slowMa, typicalPrice, candle.OpenTime));

		if (!fastResult.IsFinal || fastResult is not DecimalIndicatorValue fastValue)
		{
			_previousCandle = candle;
			return;
		}

		if (!slowResult.IsFinal || slowResult is not DecimalIndicatorValue slowValue)
		{
			_previousCandle = candle;
			return;
		}

		_fastMaValue = fastValue.Value;
		_slowMaValue = slowValue.Value;

		UpdateFractals(candle);
		UpdateRiskManagement(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCandle = candle;
			return;
		}

		if (_previousCandle is null)
		{
			_previousCandle = candle;
			return;
		}

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		{
			_previousCandle = candle;
			return;
		}

		if (_momentumValues.Count == 0 || _longMacdValue is null || _longSignalValue is null)
		{
			_previousCandle = candle;
			return;
		}

		if (Orders.Any(o => o.State == OrderStates.Active))
		{
			_previousCandle = candle;
			return;
		}

		CheckLongMacdExit();
		CheckShortMacdExit();

		var fast = _fastMaValue.Value;
		var slow = _slowMaValue.Value;
		var momentumDeviation = _momentumValues.Max(v => Math.Abs(100m - v));
		var bullishMomentum = momentumDeviation >= MomentumBuyThreshold;
		var bearishMomentum = momentumDeviation >= MomentumSellThreshold;
		var macdBullish = _longMacdValue > _longSignalValue;
		var macdBearish = _longMacdValue < _longSignalValue;

		if (Position <= 0m && fast > slow && bullishMomentum && macdBullish && IsSupportTriggered(candle))
		{
			BuyMarket(TradeVolume);
		}
		else if (Position >= 0m && fast < slow && bearishMomentum && macdBearish && IsResistanceTriggered(candle))
		{
			SellMarket(TradeVolume);
		}

		_previousCandle = candle;
	}

	private void ProcessHigherCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var result = _momentum.Process(new DecimalIndicatorValue(_momentum, candle.ClosePrice, candle.OpenTime));
		if (!result.IsFinal || result is not DecimalIndicatorValue momentumValue)
		return;

		_momentumValues.Enqueue(momentumValue.Value);
		while (_momentumValues.Count > 3)
		_momentumValues.Dequeue();
	}

	private void ProcessLongTermCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished || !macdValue.IsFinal)
		return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macd)
		return;

		_longMacdValue = macd.Macd as decimal?;
		_longSignalValue = macd.Signal as decimal?;
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		var window = Math.Max(FractalWindowLength, 5);
		_fractalBuffer.Enqueue(candle);
		while (_fractalBuffer.Count > window)
		_fractalBuffer.Dequeue();

		if (_fractalBuffer.Count < window)
		return;

		var items = _fractalBuffer.ToArray();
		var centerIndex = items.Length - 3;
		if (centerIndex < 2 || centerIndex + 2 >= items.Length)
		return;

		var center = items[centerIndex];
		var isUpper = center.HighPrice > items[centerIndex - 1].HighPrice &&
		center.HighPrice > items[centerIndex - 2].HighPrice &&
		center.HighPrice > items[centerIndex + 1].HighPrice &&
		center.HighPrice > items[centerIndex + 2].HighPrice;

		var isLower = center.LowPrice < items[centerIndex - 1].LowPrice &&
		center.LowPrice < items[centerIndex - 2].LowPrice &&
		center.LowPrice < items[centerIndex + 1].LowPrice &&
		center.LowPrice < items[centerIndex + 2].LowPrice;

		if (isUpper && _lastResistanceTime != center.CloseTime)
		{
			_lastResistanceLevel = center.HighPrice;
			_lastResistanceTime = center.CloseTime;
		}

		if (isLower && _lastSupportTime != center.CloseTime)
		{
			_lastSupportLevel = center.LowPrice;
			_lastSupportTime = center.CloseTime;
		}
	}

	private bool IsSupportTriggered(ICandleMessage candle)
	{
		if (_lastSupportLevel is null || _previousCandle is null)
		return false;

		var buffer = FractalBufferPips > 0m ? FractalBufferPips * _pipSize : 0m;
		var support = _lastSupportLevel.Value - buffer;
		var touchedNow = candle.LowPrice <= support;
		var touchedBefore = _previousCandle.LowPrice <= support || _previousCandle.HighPrice >= support;
		var bullishClose = candle.ClosePrice > _previousCandle.ClosePrice;

		return touchedNow && touchedBefore && bullishClose;
	}

	private bool IsResistanceTriggered(ICandleMessage candle)
	{
		if (_lastResistanceLevel is null || _previousCandle is null)
		return false;

		var buffer = FractalBufferPips > 0m ? FractalBufferPips * _pipSize : 0m;
		var resistance = _lastResistanceLevel.Value + buffer;
		var touchedNow = candle.HighPrice >= resistance;
		var touchedBefore = _previousCandle.HighPrice >= resistance || _previousCandle.LowPrice <= resistance;
		var bearishClose = candle.ClosePrice < _previousCandle.ClosePrice;

		return touchedNow && touchedBefore && bearishClose;
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		if (_pipSize <= 0m)
		return;

		if (Position > 0m && _entryPrice is decimal entryLong)
		{
			_longHighestPrice = _longHighestPrice.HasValue ? Math.Max(_longHighestPrice.Value, candle.HighPrice) : candle.HighPrice;

			if (UseBreakEven && _longHighestPrice.HasValue)
			{
				var trigger = entryLong + BreakEvenTriggerPips * _pipSize;
				if (_longHighestPrice.Value >= trigger)
				{
					var breakEven = entryLong + BreakEvenOffsetPips * _pipSize;
					_longStopPrice = _longStopPrice.HasValue ? Math.Max(_longStopPrice.Value, breakEven) : breakEven;
				}
			}

			if (TrailingStopPips > 0m)
			{
				var trigger = entryLong + TrailingStopPips * _pipSize;
				if (candle.HighPrice >= trigger)
				{
					var trailingCandidate = candle.HighPrice - TrailingStopPips * _pipSize;
					_longStopPrice = _longStopPrice.HasValue ? Math.Max(_longStopPrice.Value, trailingCandidate) : trailingCandidate;
				}
			}

			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetLongRisk();
				return;
			}

			if (_longTakePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetLongRisk();
				return;
			}
		}
		else if (Position < 0m && _entryPrice is decimal entryShort)
		{
			_shortLowestPrice = _shortLowestPrice.HasValue ? Math.Min(_shortLowestPrice.Value, candle.LowPrice) : candle.LowPrice;

			if (UseBreakEven && _shortLowestPrice.HasValue)
			{
				var trigger = entryShort - BreakEvenTriggerPips * _pipSize;
				if (_shortLowestPrice.Value <= trigger)
				{
					var breakEven = entryShort - BreakEvenOffsetPips * _pipSize;
					_shortStopPrice = _shortStopPrice.HasValue ? Math.Min(_shortStopPrice.Value, breakEven) : breakEven;
				}
			}

			if (TrailingStopPips > 0m)
			{
				var trigger = entryShort - TrailingStopPips * _pipSize;
				if (candle.LowPrice <= trigger)
				{
					var trailingCandidate = candle.LowPrice + TrailingStopPips * _pipSize;
					_shortStopPrice = _shortStopPrice.HasValue ? Math.Min(_shortStopPrice.Value, trailingCandidate) : trailingCandidate;
				}
			}

			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortRisk();
				return;
			}

			if (_shortTakePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortRisk();
				return;
			}
		}
		else
		{
			ResetLongRisk();
			ResetShortRisk();
		}
	}

	private void InitializeLongRisk()
	{
		if (_entryPrice is not decimal entry)
		return;

		_longStopPrice = StopLossPips > 0m ? entry - StopLossPips * _pipSize : null;
		_longTakePrice = TakeProfitPips > 0m ? entry + TakeProfitPips * _pipSize : null;
		_longHighestPrice = entry;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_shortLowestPrice = null;
	}

	private void InitializeShortRisk()
	{
		if (_entryPrice is not decimal entry)
		return;

		_shortStopPrice = StopLossPips > 0m ? entry + StopLossPips * _pipSize : null;
		_shortTakePrice = TakeProfitPips > 0m ? entry - TakeProfitPips * _pipSize : null;
		_shortLowestPrice = entry;
		_longStopPrice = null;
		_longTakePrice = null;
		_longHighestPrice = null;
	}

	private void ResetLongRisk()
	{
		_longStopPrice = null;
		_longTakePrice = null;
		_longHighestPrice = null;
	}

	private void ResetShortRisk()
	{
		_shortStopPrice = null;
		_shortTakePrice = null;
		_shortLowestPrice = null;
	}

	private void CheckLongMacdExit()
	{
		if (Position <= 0m || _longMacdValue is null || _longSignalValue is null)
		return;

		if (_longMacdValue <= _longSignalValue)
		{
			SellMarket(Position);
			ResetLongRisk();
		}
	}

	private void CheckShortMacdExit()
	{
		if (Position >= 0m || _longMacdValue is null || _longSignalValue is null)
		return;

		if (_longMacdValue >= _longSignalValue)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortRisk();
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 0.0001m;

		if (step == 0.00001m || step == 0.001m)
		return step * 10m;

	return step;
	}

	private static decimal GetTypicalPrice(ICandleMessage candle)
	=> (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
}
