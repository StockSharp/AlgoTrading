using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes existing positions when the close price crosses a simple moving average.
/// </summary>
public class CloseCrossMaStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevDiff;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public CloseCrossMaStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of the moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Time frame for incoming candles", "Parameters")
			.SetCanOptimize(false);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SimpleMovingAverage
		{
			Length = MaPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var diff = candle.ClosePrice - ma;

		if (_prevDiff is decimal prevDiff)
		{
			if (Position > 0 && prevDiff > 0 && diff <= 0)
				SellMarket();

			if (Position < 0 && prevDiff < 0 && diff >= 0)
				BuyMarket();
		}

		_prevDiff = diff;
	}
}
