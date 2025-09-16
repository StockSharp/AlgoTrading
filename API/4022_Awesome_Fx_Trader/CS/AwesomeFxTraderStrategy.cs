using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Awesome Fx Trader strategy.
/// Recreates the MetaTrader logic that paints the Awesome Oscillator histogram and trend moving averages.
/// Goes long when the oscillator turns bullish while the linear weighted average stays above its smoother.
/// Opens shorts on the opposite momentum and trend alignment.
/// </summary>
public class AwesomeFxTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _trendLwmaPeriod;
	private readonly StrategyParam<int> _trendSmoothingPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private EMA? _fastEma;
	private EMA? _slowEma;
	private LinearWeightedMovingAverage? _trendLwma;
	private SimpleMovingAverage? _trendSmoother;

	private decimal _previousAo;
	private bool _hasPreviousAo;
	private bool _isAoIncreasing;

	/// <summary>
	/// Period for the fast EMA used in the oscillator.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the slow EMA used in the oscillator.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the trend linear weighted moving average.
	/// </summary>
	public int TrendLwmaPeriod
	{
		get => _trendLwmaPeriod.Value;
		set => _trendLwmaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing simple moving average applied to the trend LWMA.
	/// </summary>
	public int TrendSmoothingPeriod
	{
		get => _trendSmoothingPeriod.Value;
		set => _trendSmoothingPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles to use for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="AwesomeFxTraderStrategy"/>.
	/// </summary>
	public AwesomeFxTraderStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Period of the fast EMA driving the oscillator", "Awesome Oscillator")
			.SetCanOptimize(true)
			.SetOptimize(4, 20, 1);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Period of the slow EMA driving the oscillator", "Awesome Oscillator")
			.SetCanOptimize(true)
			.SetOptimize(8, 40, 1);

		_trendLwmaPeriod = Param(nameof(TrendLwmaPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Trend LWMA", "Length of the linear weighted trend average", "Trend Filter")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 2);

		_trendSmoothingPeriod = Param(nameof(TrendSmoothingPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Trend Smoother", "Length of the SMA applied to the trend LWMA", "Trend Filter")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame used for calculations", "General");
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

		_fastEma = null;
		_slowEma = null;
		_trendLwma = null;
		_trendSmoother = null;
		_previousAo = 0m;
		_hasPreviousAo = false;
		_isAoIncreasing = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new EMA { Length = FastEmaPeriod };
		_slowEma = new EMA { Length = SlowEmaPeriod };
		_trendLwma = new LinearWeightedMovingAverage { Length = TrendLwmaPeriod };
		_trendSmoother = new SimpleMovingAverage { Length = TrendSmoothingPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _trendLwma);
			DrawIndicator(priceArea, _trendSmoother);
			DrawOwnTrades(priceArea);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastValue = _fastEma!.Process(candle.OpenPrice);
		var slowValue = _slowEma!.Process(candle.OpenPrice);
		var lwmaValue = _trendLwma!.Process(candle.OpenPrice);

		if (!fastValue.IsFinal || !slowValue.IsFinal || !lwmaValue.IsFinal)
			return;

		if (!fastValue.TryGetValue(out var fastEma) ||
			!slowValue.TryGetValue(out var slowEma) ||
			!lwmaValue.TryGetValue(out var lwma))
			return;

		var smoothingValue = _trendSmoother!.Process(lwma);
		if (!smoothingValue.IsFinal || !smoothingValue.TryGetValue(out var smoothedLwma))
			return;

		var ao = fastEma - slowEma;

		if (!_hasPreviousAo)
		{
			_previousAo = ao;
			_hasPreviousAo = true;
			_isAoIncreasing = ao >= 0m;
			return;
		}

		if (ao > _previousAo)
			_isAoIncreasing = true;
		else if (ao < _previousAo)
			_isAoIncreasing = false;

		var isTrendBullish = lwma > smoothedLwma;
		var isTrendBearish = lwma < smoothedLwma;
		var bullishSignal = _isAoIncreasing && ao > 0m && isTrendBullish;
		var bearishSignal = !_isAoIncreasing && ao < 0m && isTrendBearish;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousAo = ao;
			return;
		}

		if (bullishSignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume <= 0)
				volume = 1;

			BuyMarket(volume);
			LogInfo($"Bullish signal: AO={ao:F5} LWMA={lwma:F5} smoother={smoothedLwma:F5}");
		}
		else if (bearishSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume <= 0)
				volume = 1;

			SellMarket(volume);
			LogInfo($"Bearish signal: AO={ao:F5} LWMA={lwma:F5} smoother={smoothedLwma:F5}");
		}

		_previousAo = ao;
	}
}
