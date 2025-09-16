using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color Momentum AMA strategy.
/// Generates signals based on momentum smoothed by the Kaufman Adaptive Moving Average.
/// A long position is opened after two consecutive rises of the smoothed momentum, a short position is opened after two consecutive falls.
/// Opposite signals close existing positions.
/// </summary>
public class ColorMomentumAmaStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _amaPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<DataType> _candleType;

	private Momentum _momentum = null!;
	private KaufmanAdaptiveMovingAverage _ama = null!;
	private decimal?[] _buffer = null!;

	/// <summary>
	/// Momentum lookback period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// AMA smoothing length.
	/// </summary>
	public int AmaPeriod
	{
		get => _amaPeriod.Value;
		set => _amaPeriod.Value = value;
	}

	/// <summary>
	/// Fast period for AMA efficiency ratio.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow period for AMA efficiency ratio.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars back used for signal calculation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ColorMomentumAmaStrategy()
	{
		_momentumPeriod = Param(nameof(MomentumPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Momentum period", "Lookback period for momentum", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_amaPeriod = Param(nameof(AmaPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("AMA period", "Smoothing length for AMA", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);

		_fastPeriod = Param(nameof(FastPeriod), 2)
		.SetGreaterThanZero()
		.SetDisplay("Fast period", "Fast period of AMA", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(2, 10, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Slow period", "Slow period of AMA", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(20, 60, 5);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetRange(1, 5)
		.SetDisplay("Signal bar", "Bar index used for signals", "Strategy")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle type", "Type of candles", "General");
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

		_buffer = new decimal?[SignalBar + 3];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_momentum = new Momentum { Length = MomentumPeriod };
		_ama = new KaufmanAdaptiveMovingAverage
		{
			Length = AmaPeriod,
			FastSCPeriod = FastPeriod,
			SlowSCPeriod = SlowPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_momentum, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _momentum);
			DrawIndicator(area, _ama);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Update AMA with the momentum value
		var amaValue = _ama.Process(momentumValue, candle.OpenTime, true).ToDecimal();
		if (!_ama.IsFormed)
		return;

		// Maintain circular buffer of last values for signal evaluation
		for (var i = _buffer.Length - 1; i > 0; i--) _buffer[i] = _buffer[i - 1];
		_buffer[0] = amaValue;

		if (_buffer[SignalBar + 2] == null || _buffer[SignalBar + 1] == null) return;

		var v0 = _buffer[SignalBar]!.Value;
		var v1 = _buffer[SignalBar + 1]!.Value;
		var v2 = _buffer[SignalBar + 2]!.Value;

		// Evaluate trend direction using consecutive values
		var rising = v1 < v2;
		var falling = v1 > v2;

		if (rising)
		{
			// Close short positions on upward momentum
			if (Position < 0)
			BuyMarket(Math.Abs(Position));

			// Open long position if momentum continues rising
			if (v0 > v1 && Position == 0)
			BuyMarket(Volume);
		}
		else if (falling)
		{
			// Close long positions on downward momentum
			if (Position > 0)
			SellMarket(Position);

			// Open short position if momentum continues falling
			if (v0 < v1 && Position == 0)
			SellMarket(Volume);
		}
	}
}
