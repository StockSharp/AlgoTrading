using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend detection using Fibonacci periods.
/// </summary>
public class FibonacciAutoTrendScouterStrategy : Strategy
{
	private readonly StrategyParam<int> _smallPeriod;
	private readonly StrategyParam<int> _mediumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private bool _upTrend;
	private bool _downTrend;

	public int SmallPeriod { get => _smallPeriod.Value; set => _smallPeriod.Value = value; }
	public int MediumPeriod { get => _mediumPeriod.Value; set => _mediumPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FibonacciAutoTrendScouterStrategy()
	{
		_smallPeriod = Param(nameof(SmallPeriod), 8).SetDisplay("Small Period").SetCanOptimize(true);
		_mediumPeriod = Param(nameof(MediumPeriod), 21).SetDisplay("Medium Period").SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var highSmall = new Highest { Length = SmallPeriod };
		var lowSmall = new Lowest { Length = SmallPeriod };
		var highMedium = new Highest { Length = MediumPeriod };
		var lowMedium = new Lowest { Length = MediumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highSmall, lowSmall, highMedium, lowMedium, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highSmall);
			DrawIndicator(area, lowSmall);
			DrawIndicator(area, highMedium);
			DrawIndicator(area, lowMedium);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highSmall, decimal lowSmall, decimal highMedium, decimal lowMedium)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var up = highSmall > highMedium;
		var down = lowSmall < lowMedium;

		if (up && !_upTrend && Position <= 0)
			BuyMarket();
		else if (down && !_downTrend && Position >= 0)
			SellMarket();

		_upTrend = up;
		_downTrend = down;
	}
}

