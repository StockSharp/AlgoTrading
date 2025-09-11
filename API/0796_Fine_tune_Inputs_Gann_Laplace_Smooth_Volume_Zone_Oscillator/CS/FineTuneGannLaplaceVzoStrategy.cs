namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Volume oscillator strategy with EMA smoothing.
/// Buys when oscillator rises above threshold and sells when it falls below.
/// </summary>
public class FineTuneGannLaplaceVzoStrategy : Strategy
{
	private readonly StrategyParam<int> _fastVolumeLength;
	private readonly StrategyParam<int> _slowVolumeLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<bool> _closeAll;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastVolumeEma;
	private ExponentialMovingAverage _slowVolumeEma;
	private ExponentialMovingAverage _smoothEma;
	private decimal _previousSmooth;

	/// <summary>
	/// Fast volume EMA length.
	/// </summary>
	public int FastVolumeLength
	{
		get => _fastVolumeLength.Value;
		set => _fastVolumeLength.Value = value;
	}

	/// <summary>
	/// Slow volume EMA length.
	/// </summary>
	public int SlowVolumeLength
	{
		get => _slowVolumeLength.Value;
		set => _slowVolumeLength.Value = value;
	}

	/// <summary>
	/// Smoothing EMA length for oscillator.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Threshold level for signals.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Enable closing positions when no signals.
	/// </summary>
	public bool CloseAll
	{
		get => _closeAll.Value;
		set => _closeAll.Value = value;
	}

	/// <summary>
	/// Type of candles for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FineTuneGannLaplaceVzoStrategy"/>.
	/// </summary>
	public FineTuneGannLaplaceVzoStrategy()
	{
		_fastVolumeLength = Param(nameof(FastVolumeLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Volume EMA", "Fast volume EMA length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_slowVolumeLength = Param(nameof(SlowVolumeLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow Volume EMA", "Slow volume EMA length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_smoothLength = Param(nameof(SmoothLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "EMA smoothing length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_threshold = Param(nameof(Threshold), 0m)
			.SetDisplay("Threshold", "Signal threshold", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(-10m, 10m, 1m);

		_closeAll = Param(nameof(CloseAll), true)
			.SetDisplay("Close All", "Close position when no signal", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for calculations", "Parameters");
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

		_fastVolumeEma = null;
		_slowVolumeEma = null;
		_smoothEma = null;
		_previousSmooth = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastVolumeEma = new ExponentialMovingAverage { Length = FastVolumeLength };
		_slowVolumeEma = new ExponentialMovingAverage { Length = SlowVolumeLength };
		_smoothEma = new ExponentialMovingAverage { Length = SmoothLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = _fastVolumeEma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		var slow = _slowVolumeEma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		if (!_fastVolumeEma.IsFormed || !_slowVolumeEma.IsFormed)
			return;

		var vzo = slow == 0m ? 0m : (fast - slow) / slow * 100m;
		var smooth = _smoothEma.Process(vzo, candle.ServerTime, true).ToDecimal();

		if (!_smoothEma.IsFormed)
		{
			_previousSmooth = smooth;
			return;
		}

		var rising = smooth > _previousSmooth;
		var falling = smooth < _previousSmooth;

		if (rising && smooth > Threshold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (falling && smooth < -Threshold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (!rising && !falling && CloseAll && Position != 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else
				BuyMarket(Math.Abs(Position));
		}

		_previousSmooth = smooth;
	}
}
