using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Majors Volume Sum Strategy - trades based on the signed volume sum relative to its historical maximum.
/// </summary>
public class MajorsVolumeSumStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _threshold;

	private SimpleMovingAverage _sma10;
	private SimpleMovingAverage _sma100;
	private SimpleMovingAverage _sma200;

	private decimal _max;
	private decimal? _prevClose;

	/// <summary>
	/// Type of candles for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fraction of maximum absolute sum required to trigger trades.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MajorsVolumeSumStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");

		_threshold = Param(nameof(Threshold), 0.75m)
			.SetDisplay("Threshold", "Fraction of max volume sum", "Signals")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma10 = new SimpleMovingAverage { Length = 10 };
		_sma100 = new SimpleMovingAverage { Length = 100 };
		_sma200 = new SimpleMovingAverage { Length = 200 };

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose is null)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var signedVol = candle.ClosePrice > _prevClose ? candle.TotalVolume : -candle.TotalVolume;

		var sum10 = _sma10.Process(signedVol).ToDecimal() * 10m;
		_sma100.Process(signedVol);
		_sma200.Process(signedVol);

		var abs = Math.Abs(sum10);
		if (abs > _max)
			_max = abs;

		var trigger = _max * Threshold;

		if (sum10 > trigger && Position <= 0)
			BuyMarket();
		else if (sum10 < -trigger && Position >= 0)
			SellMarket();

		_prevClose = candle.ClosePrice;
	}
}

