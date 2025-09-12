using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on smoothed Hurst exponent.
/// Buys when the smoothed Hurst value is above the threshold and sells when it drops below.
/// </summary>
public class HurstExponentStrategy : Strategy
{
	private readonly StrategyParam<int> _hurstPeriod;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private HurstExponent _hurst;
	private ExponentialMovingAverage _smoother;

	/// <summary>
	/// Hurst exponent calculation period.
	/// </summary>
	public int HurstPeriod
	{
		get => _hurstPeriod.Value;
		set => _hurstPeriod.Value = value;
	}

	/// <summary>
	/// EMA length for smoothing the Hurst exponent.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Threshold separating trending and mean-reverting markets.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HurstExponentStrategy"/>.
	/// </summary>
	public HurstExponentStrategy()
	{
		_hurstPeriod = Param(nameof(HurstPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Hurst Period", "Lookback period for Hurst exponent", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 150, 25);

		_smoothLength = Param(nameof(SmoothLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "EMA period for smoothing Hurst", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_threshold = Param(nameof(Threshold), 0.5m)
			.SetRange(0.1m, 0.9m)
			.SetDisplay("Threshold", "Hurst threshold", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.45m, 0.65m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_hurst = null;
		_smoother = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_hurst = new HurstExponent { Length = HurstPeriod };
		_smoother = new ExponentialMovingAverage { Length = SmoothLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_hurst, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _hurst);
			DrawOwnTrades(area);
		}
		StartProtection(
			takeProfit: new Unit(0, UnitTypes.Absolute),
			stopLoss: new Unit(2, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal hurstValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var smoothed = _smoother.Process(hurstValue, candle.ServerTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (smoothed > Threshold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (smoothed < Threshold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
