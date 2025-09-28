namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Symmetrical wedge breakout strategy converted from MetaTrader.
/// Combines fractal-derived wedge detection with LWMA, momentum and MACD filters.
/// </summary>
public class WedgePatternStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _fractalDepth;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _useBreakeven;
	private readonly StrategyParam<int> _breakevenTriggerPips;
	private readonly StrategyParam<int> _breakevenOffsetPips;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<int> _trailingActivationPips;
	private readonly StrategyParam<int> _trailingDistancePips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _breakoutBufferPips;
	private readonly StrategyParam<int> _maxStoredCandles;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly List<CandleSnapshot> _candles = new();
	private readonly List<FractalPoint> _highFractals = new();
	private readonly List<FractalPoint> _lowFractals = new();

	private int _currentIndex;
	private int _baseIndex;
	private decimal _pipSize;
	private readonly decimal[] _momentumHistory = new decimal[3];
	private int _momentumCount;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="WedgePatternStrategy"/> class.
	/// </summary>
	public WedgePatternStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for signals", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Length of the fast linear weighted MA", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(4, 20, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Length of the slow linear weighted MA", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(40, 150, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Lookback used for the momentum filter", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 1);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Threshold", "Minimum distance from the neutral momentum level", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1.5m, 0.1m);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Short EMA period inside MACD", "Momentum");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Long EMA period inside MACD", "Momentum");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA period inside MACD", "Momentum");

		_fractalDepth = Param(nameof(FractalDepth), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fractal Depth", "Bars on each side required to confirm a fractal", "Pattern");

		_stopLossPips = Param(nameof(StopLossPips), 20)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 80, 5);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20, 150, 5);

		_useBreakeven = Param(nameof(UseBreakeven), true)
		.SetDisplay("Use Breakeven", "Enable automatic break-even", "Risk");

		_breakevenTriggerPips = Param(nameof(BreakevenTriggerPips), 30)
		.SetGreaterThanZero()
		.SetDisplay("Breakeven Trigger (pips)", "Unrealised gain required to move stop", "Risk");

		_breakevenOffsetPips = Param(nameof(BreakevenOffsetPips), 30)
		.SetGreaterThanZero()
		.SetDisplay("Breakeven Offset (pips)", "Extra pips locked once break-even triggers", "Risk");

		_useTrailing = Param(nameof(UseTrailing), true)
		.SetDisplay("Use Trailing", "Enable adaptive trailing stop", "Risk");

		_trailingActivationPips = Param(nameof(TrailingActivationPips), 40)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Activation (pips)", "Unrealised gain required to start trailing", "Risk");

		_trailingDistancePips = Param(nameof(TrailingDistancePips), 40)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Distance (pips)", "Distance maintained behind price", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 10)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Step (pips)", "Minimum improvement before moving the stop", "Risk");

		_breakoutBufferPips = Param(nameof(BreakoutBufferPips), 5)
		.SetGreaterThanZero()
		.SetDisplay("Breakout Buffer (pips)", "Extra confirmation distance above/below wedge", "Pattern");

		_maxStoredCandles = Param(nameof(MaxStoredCandles), 500)
		.SetGreaterOrEqual(100)
		.SetDisplay("Stored Candles", "Maximum number of candles cached for wedge detection", "Pattern");
	}

	/// <summary>
	/// Trading timeframe used for the analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Momentum indicator length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum required momentum deviation from neutrality.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
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
	/// MACD signal EMA length.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Bars on each side required to confirm a fractal extreme.
	/// </summary>
	public int FractalDepth
	{
		get => _fractalDepth.Value;
		set => _fractalDepth.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable or disable automatic break-even protection.
	/// </summary>
	public bool UseBreakeven
	{
		get => _useBreakeven.Value;
		set => _useBreakeven.Value = value;
	}

	/// <summary>
	/// Required unrealised gain before break-even moves the stop.
	/// </summary>
	public int BreakevenTriggerPips
	{
		get => _breakevenTriggerPips.Value;
		set => _breakevenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional pips locked once break-even activates.
	/// </summary>
	public int BreakevenOffsetPips
	{
		get => _breakevenOffsetPips.Value;
		set => _breakevenOffsetPips.Value = value;
	}

	/// <summary>
	/// Enable or disable trailing stops.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Unrealised gain required to activate the trailing stop.
	/// </summary>
	public int TrailingActivationPips
	{
		get => _trailingActivationPips.Value;
		set => _trailingActivationPips.Value = value;
	}

	/// <summary>
	/// Distance maintained between price and trailing stop.
	/// </summary>
	public int TrailingDistancePips
	{
		get => _trailingDistancePips.Value;
		set => _trailingDistancePips.Value = value;
	}

	/// <summary>
	/// Minimum improvement required before adjusting the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Additional buffer to confirm a breakout beyond the wedge.
	/// </summary>
	public int BreakoutBufferPips
	{
		get => _breakoutBufferPips.Value;
		set => _breakoutBufferPips.Value = value;
	}

	public int MaxStoredCandles
	{
		get => _maxStoredCandles.Value;
		set => _maxStoredCandles.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_candles.Clear();
		_highFractals.Clear();
		_lowFractals.Clear();
		_currentIndex = 0;
		_baseIndex = 0;
		_pipSize = 0m;
		_momentumCount = 0;
		Array.Clear(_momentumHistory, 0, _momentumHistory.Length);
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage
		{
			Length = FastMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_slowMa = new WeightedMovingAverage
		{
			Length = SlowMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_momentum = new Momentum
		{
			Length = MomentumPeriod
		};

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		_pipSize = CalculatePipSize();
		_currentIndex = 0;
		_baseIndex = 0;
		_momentumCount = 0;
		Array.Clear(_momentumHistory, 0, _momentumHistory.Length);
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_fastMa, _slowMa, _momentum, _macd, ProcessCandle)
		.Start();

		Volume = Volume == 0m ? 1m : Volume;

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position > 0m)
		{
			_longEntryPrice = PositionPrice;
			_shortEntryPrice = null;
		}
		else if (Position < 0m)
		{
			_shortEntryPrice = PositionPrice;
			_longEntryPrice = null;
		}
		else
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_longStopPrice = null;
			_shortStopPrice = null;
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue momentumValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		AddCandle(candle);

		if (!fastValue.IsFinal || !slowValue.IsFinal || !momentumValue.IsFinal || !macdValue.IsFinal)
		return;

		var fastMa = fastValue.GetValue<decimal>();
		var slowMa = slowValue.GetValue<decimal>();
		var momentumRaw = momentumValue.GetValue<decimal>();
		var momentumDistance = Math.Abs(100m - momentumRaw);
		UpdateMomentumHistory(momentumDistance);

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
		return;

		var closePrice = candle.ClosePrice;
		var canTrade = IsFormedAndOnlineAndAllowTrading();

		ApplyBreakeven(closePrice);
		ApplyTrailing(closePrice);

		if (!canTrade)
		return;

		var momentumReady = HasMomentumImpulse();
		var wedgeReady = TryGetWedgeLines(out var upperLine, out var lowerLine);
		var buffer = ConvertPipsToPrice(BreakoutBufferPips);

		var longBreakout = wedgeReady && closePrice > upperLine + buffer;
		var shortBreakout = wedgeReady && closePrice < lowerLine - buffer;

		if (Position <= 0m && fastMa > slowMa && macdLine > signalLine && momentumReady && longBreakout)
		{
			EnterLong(closePrice);
		}
		else if (Position >= 0m && fastMa < slowMa && macdLine < signalLine && momentumReady && shortBreakout)
		{
			EnterShort(closePrice);
		}
	}

	private void EnterLong(decimal referencePrice)
	{
		var currentPosition = Position;
		var coverVolume = currentPosition < 0m ? Math.Abs(currentPosition) : 0m;
		var totalVolume = coverVolume + Volume;

		if (totalVolume <= 0m)
		return;

		var resultingPosition = currentPosition + totalVolume;

		BuyMarket(totalVolume);
		ApplyRiskControls(referencePrice, resultingPosition);
	}

	private void EnterShort(decimal referencePrice)
	{
		var currentPosition = Position;
		var coverVolume = currentPosition > 0m ? currentPosition : 0m;
		var totalVolume = coverVolume + Volume;

		if (totalVolume <= 0m)
		return;

		var resultingPosition = currentPosition - totalVolume;

		SellMarket(totalVolume);
		ApplyRiskControls(referencePrice, resultingPosition);
	}

	private void ApplyRiskControls(decimal referencePrice, decimal resultingPosition)
	{
		var stopDistance = ConvertPipsToPrice(StopLossPips);
		var takeDistance = ConvertPipsToPrice(TakeProfitPips);

		if (resultingPosition > 0m)
		{
			if (stopDistance > 0m)
			SetStopLoss(stopDistance, referencePrice, resultingPosition);

			if (takeDistance > 0m)
			SetTakeProfit(takeDistance, referencePrice, resultingPosition);
		}
		else if (resultingPosition < 0m)
		{
			if (stopDistance > 0m)
			SetStopLoss(stopDistance, referencePrice, resultingPosition);

			if (takeDistance > 0m)
			SetTakeProfit(takeDistance, referencePrice, resultingPosition);
		}
	}

	private void ApplyBreakeven(decimal currentPrice)
	{
		if (!UseBreakeven || _pipSize <= 0m || Position == 0m)
		return;

		if (PositionPrice is not decimal entryPrice)
		return;

		var trigger = ConvertPipsToPrice(BreakevenTriggerPips);
		var offset = ConvertPipsToPrice(BreakevenOffsetPips);

		if (trigger <= 0m)
		return;

		if (Position > 0m)
		{
			var gain = currentPrice - entryPrice;
			if (gain < trigger)
			return;

			var desiredStop = entryPrice + offset;
			if (_longStopPrice is decimal existing && desiredStop <= existing)
			return;

			var distance = currentPrice - desiredStop;
			if (distance <= 0m)
			return;

			SetStopLoss(distance, currentPrice, Position);
			_longStopPrice = desiredStop;
		}
		else
		{
			var gain = entryPrice - currentPrice;
			if (gain < trigger)
			return;

			var desiredStop = entryPrice - offset;
			if (_shortStopPrice is decimal existing && desiredStop >= existing)
			return;

			var distance = desiredStop - currentPrice;
			if (distance <= 0m)
			return;

			SetStopLoss(distance, currentPrice, Position);
			_shortStopPrice = desiredStop;
		}
	}

	private void ApplyTrailing(decimal currentPrice)
	{
		if (!UseTrailing || _pipSize <= 0m || Position == 0m)
		return;

		if (PositionPrice is not decimal entryPrice)
		return;

		var activation = ConvertPipsToPrice(TrailingActivationPips);
		var distanceBase = ConvertPipsToPrice(TrailingDistancePips);
		var step = ConvertPipsToPrice(TrailingStepPips);

		if (activation <= 0m || distanceBase <= 0m)
		return;

		if (Position > 0m)
		{
			var gain = currentPrice - entryPrice;
			if (gain < activation + distanceBase)
			return;

			var desiredStop = currentPrice - distanceBase;
			if (_longStopPrice is decimal existing && desiredStop - existing < step)
			return;

			var distance = currentPrice - desiredStop;
			if (distance <= 0m)
			return;

			SetStopLoss(distance, currentPrice, Position);
			_longStopPrice = desiredStop;
		}
		else
		{
			var gain = entryPrice - currentPrice;
			if (gain < activation + distanceBase)
			return;

			var desiredStop = currentPrice + distanceBase;
			if (_shortStopPrice is decimal existing && existing - desiredStop < step)
			return;

			var distance = desiredStop - currentPrice;
			if (distance <= 0m)
			return;

			SetStopLoss(distance, currentPrice, Position);
			_shortStopPrice = desiredStop;
		}
	}

	private void UpdateMomentumHistory(decimal value)
	{
		for (var i = _momentumHistory.Length - 1; i > 0; i--)
		_momentumHistory[i] = _momentumHistory[i - 1];

		_momentumHistory[0] = value;

		if (_momentumCount < _momentumHistory.Length)
		_momentumCount++;
	}

	private bool HasMomentumImpulse()
	{
		if (_momentumCount == 0)
		return false;

		for (var i = 0; i < _momentumCount; i++)
		{
			if (_momentumHistory[i] >= MomentumThreshold)
			return true;
		}

		return false;
	}

	private void AddCandle(ICandleMessage candle)
	{
		var snapshot = new CandleSnapshot(candle.HighPrice, candle.LowPrice);
		_candles.Add(snapshot);
		_currentIndex++;

		if (_candles.Count > MaxStoredCandles)
		{
			_candles.RemoveAt(0);
			_baseIndex++;
			TrimFractals();
		}

		UpdateFractals();
	}

	private void UpdateFractals()
	{
		var depth = FractalDepth;
		var window = depth * 2 + 1;

		if (depth <= 0 || _candles.Count < window)
		return;

		var centerAbsolute = _currentIndex - depth - 1;
		if (centerAbsolute < _baseIndex + depth)
		return;

		var centerLocal = centerAbsolute - _baseIndex;
		if (centerLocal < depth || centerLocal >= _candles.Count - depth)
		return;

		var candidate = _candles[centerLocal];

		var isHigh = true;
		for (var i = centerLocal - depth; i <= centerLocal + depth; i++)
		{
			if (i == centerLocal)
			continue;

			if (_candles[i].High >= candidate.High)
			{
				isHigh = false;
				break;
			}
		}

		if (isHigh)
		AddFractal(_highFractals, centerAbsolute, candidate.High, true);

		var isLow = true;
		for (var i = centerLocal - depth; i <= centerLocal + depth; i++)
		{
			if (i == centerLocal)
			continue;

			if (_candles[i].Low <= candidate.Low)
			{
				isLow = false;
				break;
			}
		}

		if (isLow)
		AddFractal(_lowFractals, centerAbsolute, candidate.Low, false);
	}

	private void AddFractal(List<FractalPoint> storage, int index, decimal price, bool isHigh)
	{
		if (storage.Count > 0 && storage[^1].Index == index)
		return;

		storage.Add(new FractalPoint(index, price));

		while (storage.Count > 20)
		storage.RemoveAt(0);

		if (isHigh)
		{
			_highFractals.Sort((a, b) => a.Index.CompareTo(b.Index));
		}
		else
		{
			_lowFractals.Sort((a, b) => a.Index.CompareTo(b.Index));
		}
	}

	private void TrimFractals()
	{
		var minIndex = _baseIndex;

		while (_highFractals.Count > 0 && _highFractals[0].Index < minIndex)
		_highFractals.RemoveAt(0);

		while (_lowFractals.Count > 0 && _lowFractals[0].Index < minIndex)
		_lowFractals.RemoveAt(0);
	}

	private bool TryGetWedgeLines(out decimal upperLine, out decimal lowerLine)
	{
		upperLine = 0m;
		lowerLine = 0m;

		if (_highFractals.Count < 2 || _lowFractals.Count < 2)
		return false;

		var lastHigh = _highFractals[^1];
		var prevHigh = _highFractals[^2];
		var lastLow = _lowFractals[^1];
		var prevLow = _lowFractals[^2];

		if (lastHigh.Index == prevHigh.Index || lastLow.Index == prevLow.Index)
		return false;

		if (lastHigh.Price >= prevHigh.Price || lastLow.Price <= prevLow.Price)
		return false;

		var current = _currentIndex - 1;
		upperLine = ProjectLine(prevHigh, lastHigh, current);
		lowerLine = ProjectLine(prevLow, lastLow, current);

		return upperLine > lowerLine;
	}

	private static decimal ProjectLine(FractalPoint first, FractalPoint second, int targetIndex)
	{
		var deltaIndex = second.Index - first.Index;
		if (deltaIndex == 0)
		return second.Price;

		var slope = (second.Price - first.Price) / deltaIndex;
		var intercept = second.Price - slope * second.Index;
		return slope * targetIndex + intercept;
	}

	private decimal ConvertPipsToPrice(int pips)
	{
		if (pips <= 0 || _pipSize <= 0m)
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

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal high, decimal low)
		{
			High = high;
			Low = low;
		}

		public decimal High { get; }
		public decimal Low { get; }
	}

	private readonly struct FractalPoint
	{
		public FractalPoint(int index, decimal price)
		{
			Index = index;
			Price = price;
		}

		public int Index { get; }
		public decimal Price { get; }
	}
}

