using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long strategy based on Gartley 222 harmonic pattern.
/// </summary>
public class Gartley222Strategy : Strategy
{
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<decimal> _fibTolerance;
	private readonly StrategyParam<decimal> _tpFibExtension;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _x;
	private decimal? _a;
	private decimal? _b;
	private decimal? _c;
	private int? _patternBar;

	private decimal?[] _highs = [];
	private decimal?[] _lows = [];
	private int _bufferIndex;
	private int _bufferCount;
	private int _barIndex;

	/// <summary>
	/// Pivot length.
	/// </summary>
	public int PivotLength
	{
		get => _pivotLength.Value;
		set => _pivotLength.Value = value;
	}

	/// <summary>
	/// Fibonacci tolerance.
	/// </summary>
	public decimal FibTolerance
	{
		get => _fibTolerance.Value;
		set => _fibTolerance.Value = value;
	}

	/// <summary>
	/// Take profit Fibonacci extension.
	/// </summary>
	public decimal TpFibExtension
	{
		get => _tpFibExtension.Value;
		set => _tpFibExtension.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="Gartley222Strategy"/>.
	/// </summary>
	public Gartley222Strategy()
	{
		_pivotLength = Param(nameof(PivotLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Bars left/right for pivot", "General")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_fibTolerance = Param(nameof(FibTolerance), 0.05m)
			.SetRange(0m, 0.2m)
			.SetDisplay("Fib Tolerance", "Allowed Fibonacci ratio deviation", "General")
			.SetCanOptimize(true)
			.SetOptimize(0m, 0.1m, 0.01m);

		_tpFibExtension = Param(nameof(TpFibExtension), 1.27m)
			.SetGreaterThanZero()
			.SetDisplay("TP Fib Extension", "Take profit Fibonacci extension", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1.1m, 1.6m, 0.05m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.1m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_x = _a = _b = _c = null;
		_patternBar = null;
		_highs = [];
		_lows = [];
		_bufferIndex = 0;
		_bufferCount = 0;
		_barIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var size = PivotLength * 2 + 1;
		_highs = new decimal?[size];
		_lows = new decimal?[size];
		_bufferIndex = 0;
		_bufferCount = 0;
		_barIndex = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit((TpFibExtension - 1m) * 100m, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs[_bufferIndex] = candle.HighPrice;
		_lows[_bufferIndex] = candle.LowPrice;
		_bufferIndex = (_bufferIndex + 1) % _highs.Length;

		if (_bufferCount < _highs.Length)
		{
			_bufferCount++;
			_barIndex++;
			return;
		}

		var center = (_bufferIndex - PivotLength - 1 + _highs.Length) % _highs.Length;
		var centerHigh = _highs[center];
		var centerLow = _lows[center];

		var isPivotHigh = centerHigh.HasValue;
		var isPivotLow = centerLow.HasValue;
		for (var i = 0; i < _highs.Length; i++)
		{
			if (i == center)
				continue;
			if (centerHigh <= _highs[i])
				isPivotHigh = false;
			if (centerLow >= _lows[i])
				isPivotLow = false;
			if (!isPivotHigh && !isPivotLow)
				break;
		}

		decimal? ph = isPivotHigh ? centerHigh : null;
		decimal? pl = isPivotLow ? centerLow : null;

		if (pl is decimal low)
			_x = low;
		if (ph is decimal high && _x != null)
			_a = high;
		if (pl is decimal low2 && _a is decimal aVal && low2 < aVal)
			_b = low2;
		if (ph is decimal high2 && _b is decimal bVal && _a is decimal aVal2 && high2 < aVal2 && high2 > bVal)
			_c = high2;

		var validAb = false;
		if (_x is decimal x && _a is decimal a && _b is decimal b)
		{
			var abRatio = Math.Abs(b - a) / Math.Abs(a - x);
			validAb = abRatio > (0.618m - FibTolerance) && abRatio < (0.786m + FibTolerance);
		}

		var validCd = false;
		if (_c is decimal c && _b is decimal b2 && _a is decimal a2)
		{
			var cdRatio = Math.Abs(c - b2) / Math.Abs(a2 - b2);
			validCd = cdRatio > (0.786m - FibTolerance) && cdRatio < (0.886m + FibTolerance);
		}

		if (validAb && validCd && _c is decimal cPoint && candle.ClosePrice > cPoint)
			_patternBar = _barIndex;

		var delayedEntry = _patternBar is int bar && _barIndex == bar + PivotLength;

		if (delayedEntry && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			BuyMarket(Volume + Math.Abs(Position));
			_patternBar = null;
			_x = _a = _b = _c = null;
		}

		_barIndex++;
	}
}
