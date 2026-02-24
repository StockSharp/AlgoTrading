using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fast and slow Relative Vigor Index crossover strategy.
/// Opens a long position when the RVI average line crosses above the signal line,
/// and opens a short position on the opposite crossover.
/// </summary>
public class FastSlowRviCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _rviPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousAverage;
	private decimal? _previousSignal;

	public int RviPeriod
	{
		get => _rviPeriod.Value;
		set => _rviPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public FastSlowRviCrossoverStrategy()
	{
		_rviPeriod = Param(nameof(RviPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("RVI Period", "Period for the Relative Vigor Index", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for analysis", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rvi = new RelativeVigorIndex
		{
			Average = { Length = RviPeriod },
			Signal = { Length = 4 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rvi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rvi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rviValue = (IRelativeVigorIndexValue)indicatorValue;
		if (rviValue.Average is not decimal avgValue || rviValue.Signal is not decimal sigValue)
			return;

		if (_previousAverage.HasValue && _previousSignal.HasValue)
		{
			var longSignal = _previousAverage.Value <= _previousSignal.Value && avgValue > sigValue;
			var shortSignal = _previousAverage.Value >= _previousSignal.Value && avgValue < sigValue;

			if (longSignal && Position <= 0)
			{
				BuyMarket();
			}
			else if (shortSignal && Position >= 0)
			{
				SellMarket();
			}
		}

		_previousAverage = avgValue;
		_previousSignal = sigValue;
	}
}
