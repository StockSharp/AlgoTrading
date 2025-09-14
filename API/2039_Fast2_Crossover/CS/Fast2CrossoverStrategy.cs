// Fast2CrossoverStrategy.cs
// -----------------------------------------------------------------------------
// Implements crossover trading based on the Fast2 indicator.
// -----------------------------------------------------------------------------
// Date: 28 Dec 2023
// -----------------------------------------------------------------------------

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading strategy based on Fast2 histogram moving average crossover.
/// </summary>
public class Fast2CrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;

	private WeightedMovingAverage _fast = null!;
	private WeightedMovingAverage _slow = null!;

	private bool _hasPrevDiff1;
	private bool _hasPrevDiff2;
	private decimal _prevDiff1;
	private decimal _prevDiff2;

	private bool _hasPrevAverage;
	private decimal _prevFast;
	private decimal _prevSlow;

	/// <summary>Candle type.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Fast WMA length.</summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>Slow WMA length.</summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public Fast2CrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame());
		_fastLength = Param(nameof(FastLength), 3).SetDisplay("Fast length").SetCanOptimize(true);
		_slowLength = Param(nameof(SlowLength), 9).SetDisplay("Slow length").SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fast = new WeightedMovingAverage { Length = FastLength };
		_slow = new WeightedMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fast);
			DrawIndicator(area, _slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate histogram from candle bodies with square-root weights.
		var diff = candle.ClosePrice - candle.OpenPrice;
		var hist = diff;
		if (_hasPrevDiff1)
			hist += _prevDiff1 / (decimal)Math.Sqrt(2);
		if (_hasPrevDiff2)
			hist += _prevDiff2 / (decimal)Math.Sqrt(3);

		var fastValue = _fast.Process(hist);
		var slowValue = _slow.Process(hist);

		if (!fastValue.IsFinal || !slowValue.IsFinal)
		{
			_prevDiff2 = _prevDiff1;
			_prevDiff1 = diff;
			_hasPrevDiff2 = _hasPrevDiff1;
			_hasPrevDiff1 = true;
			return;
		}

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();

		if (_hasPrevAverage)
		{
			// Cross when fast goes below slow -> enter long.
			if (_prevFast > _prevSlow && fast < slow && Position <= 0)
				BuyMarket();

			// Cross when fast goes above slow -> enter short.
			if (_prevFast < _prevSlow && fast > slow && Position >= 0)
				SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
		_hasPrevAverage = true;

		_prevDiff2 = _prevDiff1;
		_prevDiff1 = diff;
		_hasPrevDiff2 = _hasPrevDiff1;
		_hasPrevDiff1 = true;
	}
}
