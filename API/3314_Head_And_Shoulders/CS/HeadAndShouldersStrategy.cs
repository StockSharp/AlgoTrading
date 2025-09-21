namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Head and shoulders breakout strategy converted from the MetaTrader expert.
/// </summary>
public class HeadAndShouldersStrategy : Strategy
{
	private const int FractalWindow = 5;
	private const int FractalWing = 2;
	private const int MaxStoredFractals = 20;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _shoulderTolerancePercent;
	private readonly StrategyParam<decimal> _headDominancePercent;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _trailingStopSteps;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private readonly decimal[] _highBuffer = new decimal[FractalWindow];
	private readonly decimal[] _lowBuffer = new decimal[FractalWindow];
	private int _bufferFillCount;
	private int _finishedCandles;

	private readonly List<FractalPoint> _highFractals = new();
	private readonly List<FractalPoint> _lowFractals = new();

	private bool _bearishPatternActive;
	private decimal _bearishNeckline;
	private int _bearishHeadIndex = -1;

	private bool _bullishPatternActive;
	private decimal _bullishNeckline;
	private int _bullishHeadIndex = -1;

	private decimal _activeShortNeckline;
	private decimal _activeLongNeckline;

	/// <summary>
	/// Initializes a new instance of the <see cref="HeadAndShouldersStrategy"/> class.
	/// </summary>
	public HeadAndShouldersStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for pattern detection.", "Data");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Base volume used for entries.", "Trading");

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetDisplay("Fast MA", "Fast moving average length.", "Trend");

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetDisplay("Slow MA", "Slow moving average length.", "Trend");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetDisplay("Momentum Period", "Number of candles used for momentum calculation.", "Momentum");

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetDisplay("Momentum Threshold", "Absolute momentum required for confirmations.", "Momentum")
		.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetDisplay("MACD Fast", "Fast period for the MACD filter.", "Momentum");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetDisplay("MACD Slow", "Slow period for the MACD filter.", "Momentum");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetDisplay("MACD Signal", "Signal period for the MACD filter.", "Momentum");

		_shoulderTolerancePercent = Param(nameof(ShoulderTolerancePercent), 5m)
		.SetDisplay("Shoulder Symmetry %", "Maximum percent deviation between shoulders.", "Pattern")
		.SetCanOptimize(true);

		_headDominancePercent = Param(nameof(HeadDominancePercent), 2m)
		.SetDisplay("Head Dominance %", "Minimum percent the head must exceed each shoulder.", "Pattern")
		.SetCanOptimize(true);

		_stopLossSteps = Param(nameof(StopLossSteps), 100m)
		.SetDisplay("Stop-Loss (steps)", "Protective stop size measured in price steps.", "Risk")
		.SetCanOptimize(true);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 200m)
		.SetDisplay("Take-Profit (steps)", "Profit target size measured in price steps.", "Risk")
		.SetCanOptimize(true);

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 0m)
		.SetDisplay("Trailing Stop (steps)", "Trailing stop size in price steps. Zero disables trailing.", "Risk")
		.SetCanOptimize(true);
	}

	/// <summary>Primary candle type.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Base order volume.</summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>Fast moving average length.</summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>Slow moving average length.</summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>Momentum lookback period.</summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>Minimum absolute momentum.</summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>MACD fast period.</summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>MACD slow period.</summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>MACD signal period.</summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>Maximum percent deviation allowed between the shoulder highs or lows.</summary>
	public decimal ShoulderTolerancePercent
	{
		get => _shoulderTolerancePercent.Value;
		set => _shoulderTolerancePercent.Value = value;
	}

	/// <summary>Minimum percent dominance of the head compared to each shoulder.</summary>
	public decimal HeadDominancePercent
	{
		get => _headDominancePercent.Value;
		set => _headDominancePercent.Value = value;
	}

	/// <summary>Stop-loss size expressed in price steps.</summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>Take-profit size expressed in price steps.</summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>Trailing stop size expressed in price steps.</summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_fastMa = new SimpleMovingAverage { Length = FastMaLength };
		_slowMa = new SimpleMovingAverage { Length = SlowMaLength };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = MacdFastLength,
			Slow = MacdSlowLength,
			Signal = MacdSignalLength
		};

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastMa, _slowMa, _momentum, _macd, ProcessCandle)
		.Start();

		var takeProfitUnit = TakeProfitSteps > 0m ? new Unit(TakeProfitSteps, UnitTypes.PriceStep) : null;
		var stopLossUnit = StopLossSteps > 0m ? new Unit(StopLossSteps, UnitTypes.PriceStep) : null;
		var trailingUnit = TrailingStopSteps > 0m ? new Unit(TrailingStopSteps, UnitTypes.PriceStep) : null;

		if (takeProfitUnit != null || stopLossUnit != null || trailingUnit != null)
		{
			StartProtection(
			takeProfit: takeProfitUnit,
			stopLoss: stopLossUnit,
			trailingStop: trailingUnit,
			isStopTrailing: trailingUnit != null,
			useMarketOrders: true);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal momentumValue, decimal macdValue)
	{
		UpdateFractalBuffers(candle);

		if (candle.State != CandleStates.Finished)
		return;

		_finishedCandles++;

		DetectFractals();
		UpdatePatterns();

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_momentum.IsFormed || !_macd.IsFormed)
		return;

		var trendUp = fastValue > slowValue;
		var trendDown = fastValue < slowValue;
		var momentumStrength = Math.Abs(momentumValue);
		var momentumUp = momentumValue > 0m && momentumStrength >= MomentumThreshold;
		var momentumDown = momentumValue < 0m && momentumStrength >= MomentumThreshold;
		var macdUp = macdValue > 0m;
		var macdDown = macdValue < 0m;

		if (Position <= 0 && _bullishPatternActive && candle.ClosePrice > _bullishNeckline && trendUp && momentumUp && macdUp)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_activeLongNeckline = _bullishNeckline;
			_bullishPatternActive = false;
			LogInfo($"Bullish breakout triggered at {candle.ClosePrice:F4} with neckline {_activeLongNeckline:F4}.");
		}
		else if (Position >= 0 && _bearishPatternActive && candle.ClosePrice < _bearishNeckline && trendDown && momentumDown && macdDown)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_activeShortNeckline = _bearishNeckline;
			_bearishPatternActive = false;
			LogInfo($"Bearish breakout triggered at {candle.ClosePrice:F4} with neckline {_activeShortNeckline:F4}.");
		}

		if (Position > 0)
		{
			var exitLong = (!trendUp && macdValue <= 0m) || (_activeLongNeckline > 0m && candle.ClosePrice < _activeLongNeckline);
			if (exitLong)
			{
				SellMarket(Position);
				_activeLongNeckline = 0m;
				LogInfo("Exit long due to trend or neckline violation.");
			}
		}
		else if (Position < 0)
		{
			var exitShort = (!trendDown && macdValue >= 0m) || (_activeShortNeckline > 0m && candle.ClosePrice > _activeShortNeckline);
			if (exitShort)
			{
				BuyMarket(-Position);
				_activeShortNeckline = 0m;
				LogInfo("Exit short due to trend or neckline violation.");
			}
		}
	}

	private void UpdateFractalBuffers(ICandleMessage candle)
	{
		for (var i = 0; i < FractalWindow - 1; i++)
		{
			_highBuffer[i] = _highBuffer[i + 1];
			_lowBuffer[i] = _lowBuffer[i + 1];
		}

		_highBuffer[FractalWindow - 1] = candle.HighPrice;
		_lowBuffer[FractalWindow - 1] = candle.LowPrice;

		if (_bufferFillCount < FractalWindow)
		_bufferFillCount++;
	}

	private void DetectFractals()
	{
		if (_bufferFillCount < FractalWindow)
		return;

		var middleHigh = _highBuffer[FractalWing];
		var middleLow = _lowBuffer[FractalWing];
		var middleIndex = _finishedCandles - FractalWing - 1;
		if (middleIndex < 0)
		return;

		var isUpFractal = true;
		var isDownFractal = true;

		for (var offset = 0; offset < FractalWindow; offset++)
		{
			if (offset == FractalWing)
			continue;

			if (_highBuffer[offset] >= middleHigh)
			isUpFractal = false;

			if (_lowBuffer[offset] <= middleLow)
			isDownFractal = false;
		}

		if (isUpFractal)
		AddFractal(_highFractals, new FractalPoint(middleIndex, middleHigh));

		if (isDownFractal)
		AddFractal(_lowFractals, new FractalPoint(middleIndex, middleLow));
	}

	private void UpdatePatterns()
	{
		if (TryGetBearishPattern(out var bearishNeckline, out var headIndex) && headIndex != _bearishHeadIndex)
		{
			_bearishHeadIndex = headIndex;
			_bearishNeckline = bearishNeckline;
			_bearishPatternActive = true;
			LogInfo($"Bearish head and shoulders detected with neckline {_bearishNeckline:F4}.");
		}

		if (TryGetBullishPattern(out var bullishNeckline, out var invertedHeadIndex) && invertedHeadIndex != _bullishHeadIndex)
		{
			_bullishHeadIndex = invertedHeadIndex;
			_bullishNeckline = bullishNeckline;
			_bullishPatternActive = true;
			LogInfo($"Inverted head and shoulders detected with neckline {_bullishNeckline:F4}.");
		}
	}

	private bool TryGetBearishPattern(out decimal neckline, out int headIndex)
	{
		neckline = 0m;
		headIndex = -1;

		if (_highFractals.Count < 3)
		return false;

		var left = _highFractals[^3];
		var head = _highFractals[^2];
		var right = _highFractals[^1];

		if (!(left.Index < head.Index && head.Index < right.Index))
		return false;

		if (!IsHeadDominant(head.Price, left.Price, right.Price))
		return false;

		if (!AreShouldersSymmetric(left.Price, right.Price))
		return false;

		var leftNeck = GetLastLowBetween(left.Index, head.Index);
		var rightNeck = GetLastLowBetween(head.Index, right.Index);

		if (leftNeck is null || rightNeck is null)
		return false;

		neckline = (leftNeck.Value + rightNeck.Value) / 2m;
		headIndex = head.Index;
		return true;
	}

	private bool TryGetBullishPattern(out decimal neckline, out int headIndex)
	{
		neckline = 0m;
		headIndex = -1;

		if (_lowFractals.Count < 3)
		return false;

		var left = _lowFractals[^3];
		var head = _lowFractals[^2];
		var right = _lowFractals[^1];

		if (!(left.Index < head.Index && head.Index < right.Index))
		return false;

		if (!IsInvertedHeadDominant(head.Price, left.Price, right.Price))
		return false;

		if (!AreShouldersSymmetric(left.Price, right.Price))
		return false;

		var leftNeck = GetLastHighBetween(left.Index, head.Index);
		var rightNeck = GetLastHighBetween(head.Index, right.Index);

		if (leftNeck is null || rightNeck is null)
		return false;

		neckline = (leftNeck.Value + rightNeck.Value) / 2m;
		headIndex = head.Index;
		return true;
	}

	private bool IsHeadDominant(decimal head, decimal leftShoulder, decimal rightShoulder)
	{
		if (leftShoulder == 0m || rightShoulder == 0m)
		return false;

		var minDominance = HeadDominancePercent / 100m;
		return head >= leftShoulder * (1m + minDominance) && head >= rightShoulder * (1m + minDominance);
	}

	private bool IsInvertedHeadDominant(decimal head, decimal leftShoulder, decimal rightShoulder)
	{
		var minDominance = HeadDominancePercent / 100m;
		return head <= leftShoulder * (1m - minDominance) && head <= rightShoulder * (1m - minDominance);
	}

	private bool AreShouldersSymmetric(decimal leftShoulder, decimal rightShoulder)
	{
		var maxShoulder = Math.Max(leftShoulder, rightShoulder);
		if (maxShoulder == 0m)
		return false;

		var difference = Math.Abs(leftShoulder - rightShoulder);
		var deviationPercent = (difference / maxShoulder) * 100m;
		return deviationPercent <= ShoulderTolerancePercent;
	}

	private decimal? GetLastLowBetween(int startIndex, int endIndex)
	{
		decimal? result = null;
		foreach (var fractal in _lowFractals)
		{
			if (fractal.Index > startIndex && fractal.Index < endIndex)
			result = fractal.Price;
		}

		return result;
	}

	private decimal? GetLastHighBetween(int startIndex, int endIndex)
	{
		decimal? result = null;
		foreach (var fractal in _highFractals)
		{
			if (fractal.Index > startIndex && fractal.Index < endIndex)
			result = fractal.Price;
		}

		return result;
	}

	private void AddFractal(List<FractalPoint> list, FractalPoint point)
	{
		list.Add(point);
		while (list.Count > MaxStoredFractals)
		list.RemoveAt(0);
	}

	private void ResetState()
	{
		_bufferFillCount = 0;
		_finishedCandles = 0;
		_highFractals.Clear();
		_lowFractals.Clear();
		_bearishPatternActive = false;
		_bearishNeckline = 0m;
		_bearishHeadIndex = -1;
		_bullishPatternActive = false;
		_bullishNeckline = 0m;
		_bullishHeadIndex = -1;
		_activeShortNeckline = 0m;
		_activeLongNeckline = 0m;
		Array.Clear(_highBuffer, 0, _highBuffer.Length);
		Array.Clear(_lowBuffer, 0, _lowBuffer.Length);
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
