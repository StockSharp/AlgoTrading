using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double ZigZag alignment strategy converted from MQL5 DoubleZigZag expert.
/// The strategy looks for swing points confirmed by a fast and a slow swing detector.
/// </summary>
public class DoubleZigZagStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _strengthMultiplier;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _fastHighest;
	private Lowest _fastLowest;
	private Highest _slowHighest;
	private Lowest _slowLowest;

	private int _fastDirection;
	private int _slowDirection;
	private int _fastPivotCountSinceLastAlign;

	private readonly decimal[] _alignedPrices = new decimal[3];
	private readonly int[] _alignedDirections = new int[3];
	private readonly int[] _alignedPivotCounts = new int[3];

	/// <summary>
	/// Lookback used for the fast swing detector.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Lookback used for the slow confirmation swing detector.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Ratio between fast and slow swing counts required to trigger a trade.
	/// </summary>
	public decimal StrengthMultiplier
	{
		get => _strengthMultiplier.Value;
		set => _strengthMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier controlling how much the newest swing must exceed the previous one.
	/// </summary>
	public decimal BreakoutMultiplier
	{
		get => _breakoutMultiplier.Value;
		set => _breakoutMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for the analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DoubleZigZagStrategy"/>.
	/// </summary>
	public DoubleZigZagStrategy()
	{
		_fastLength = Param(nameof(FastLength), 13)
			.SetDisplay("Fast Length", "Lookback for the fast swing detector", "Indicators")
			.SetCanOptimize(true);
		_slowLength = Param(nameof(SlowLength), 104)
			.SetDisplay("Slow Length", "Lookback for the slow confirmation swing", "Indicators")
			.SetCanOptimize(true);
		_strengthMultiplier = Param(nameof(StrengthMultiplier), 2.1m)
			.SetDisplay("Strength Multiplier", "Required ratio between fast and opposite swing counts", "Signals")
			.SetCanOptimize(true);
		_breakoutMultiplier = Param(nameof(BreakoutMultiplier), 2.1m)
			.SetDisplay("Breakout Multiplier", "Required expansion of the newest swing over the previous swing", "Signals")
			.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyze", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Array.Clear(_alignedPrices, 0, _alignedPrices.Length);
		Array.Clear(_alignedDirections, 0, _alignedDirections.Length);
		Array.Clear(_alignedPivotCounts, 0, _alignedPivotCounts.Length);

		_fastDirection = 0;
		_slowDirection = 0;
		_fastPivotCountSinceLastAlign = 0;

		_fastHighest = null;
		_fastLowest = null;
		_slowHighest = null;
		_slowLowest = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create swing detectors that approximate the original ZigZag behaviour.
		_fastHighest = new Highest { Length = FastLength };
		_fastLowest = new Lowest { Length = FastLength };
		_slowHighest = new Highest { Length = SlowLength };
		_slowLowest = new Lowest { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastHighest, _fastLowest, _slowHighest, _slowLowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastHighest);
			DrawIndicator(area, _fastLowest);
			DrawIndicator(area, _slowHighest);
			DrawIndicator(area, _slowLowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastHighest, decimal fastLowest, decimal slowHighest, decimal slowLowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_fastHighest == null || _fastLowest == null || _slowHighest == null || _slowLowest == null)
			return;

		if (!_fastHighest.IsFormed || !_fastLowest.IsFormed || !_slowHighest.IsFormed || !_slowLowest.IsFormed)
			return;

		var fastPivotDirection = 0;
		var slowPivotDirection = 0;

		// Detect the fast swing pivot direction.
		if (_fastDirection <= 0 && candle.HighPrice >= fastHighest)
		{
			fastPivotDirection = 1;
			_fastDirection = 1;
		}
		else if (_fastDirection >= 0 && candle.LowPrice <= fastLowest)
		{
			fastPivotDirection = -1;
			_fastDirection = -1;
		}

		// Detect the slow swing pivot direction.
		if (_slowDirection <= 0 && candle.HighPrice >= slowHighest)
		{
			slowPivotDirection = 1;
			_slowDirection = 1;
		}
		else if (_slowDirection >= 0 && candle.LowPrice <= slowLowest)
		{
			slowPivotDirection = -1;
			_slowDirection = -1;
		}

		if (fastPivotDirection != 0)
		{
			// Count how many fast pivots occurred since the previous alignment.
			_fastPivotCountSinceLastAlign++;
		}

		if (fastPivotDirection != 0 && slowPivotDirection == fastPivotDirection)
		{
			var pivotPrice = fastPivotDirection == 1 ? candle.HighPrice : candle.LowPrice;

			RegisterAlignEvent(pivotPrice, fastPivotDirection, _fastPivotCountSinceLastAlign);

			_fastPivotCountSinceLastAlign = 0;

			EvaluateSignals();
		}
	}

	private void RegisterAlignEvent(decimal price, int direction, int pivotCount)
	{
		for (var i = _alignedPrices.Length - 1; i > 0; i--)
		{
			_alignedPrices[i] = _alignedPrices[i - 1];
			_alignedDirections[i] = _alignedDirections[i - 1];
			_alignedPivotCounts[i] = _alignedPivotCounts[i - 1];
		}

		_alignedPrices[0] = price;
		_alignedDirections[0] = direction;
		_alignedPivotCounts[0] = pivotCount;
	}

	private void EvaluateSignals()
	{
		if (_alignedDirections[2] == 0)
			return;

		var recentDirection = _alignedDirections[0];
		var middleDirection = _alignedDirections[1];
		var olderDirection = _alignedDirections[2];

		var recentCount = _alignedPivotCounts[0];
		var previousCount = _alignedPivotCounts[1];

		if (recentDirection == 1 && middleDirection == -1 && olderDirection == 1)
		{
			var newestHigh = _alignedPrices[0];
			var swingLow = _alignedPrices[1];
			var previousHigh = _alignedPrices[2];

			if (swingLow > 0m &&
				recentCount > previousCount * StrengthMultiplier &&
				newestHigh > swingLow &&
				previousHigh > swingLow &&
				(previousHigh - swingLow) * BreakoutMultiplier < newestHigh - swingLow)
			{
				EnterLong();
			}
		}
		else if (recentDirection == -1 && middleDirection == 1 && olderDirection == -1)
		{
			var newestLow = _alignedPrices[0];
			var swingHigh = _alignedPrices[1];
			var previousLow = _alignedPrices[2];

			if (swingHigh > 0m &&
				recentCount * StrengthMultiplier < previousCount &&
				newestLow < swingHigh &&
				previousLow < swingHigh &&
				(swingHigh - previousLow) * BreakoutMultiplier < swingHigh - newestLow)
			{
				EnterShort();
			}
		}
	}

	private void EnterLong()
	{
		if (Position > 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Volume + (Position < 0 ? -Position : 0m);
		if (volume <= 0m)
			return;

		// Buy enough volume to close any short position and establish a new long.
		BuyMarket(volume);
	}

	private void EnterShort()
	{
		if (Position < 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Volume + (Position > 0 ? Position : 0m);
		if (volume <= 0m)
			return;

		// Sell enough volume to close any long position and establish a new short.
		SellMarket(volume);
	}
}
