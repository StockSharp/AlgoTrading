namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that trades on Donchian Channel width breakouts.
/// When Donchian Channel width increases significantly above its average,
/// it enters position in the direction determined by price movement.
/// </summary>
public class DonchianWidthBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<decimal> _widthThreshold;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Donchian Channel period.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Width threshold multiplier for breakout detection.
	/// </summary>
	public decimal WidthThreshold
	{
		get => _widthThreshold.Value;
		set => _widthThreshold.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="DonchianWidthBreakoutStrategy"/>.
	/// </summary>
	public DonchianWidthBreakoutStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetDisplay("Donchian Period", "Period for the Donchian Channel", "Indicators");

		_widthThreshold = Param(nameof(WidthThreshold), 1.2m)
			.SetDisplay("Width Threshold", "Threshold multiplier for width breakout", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = DonchianPeriod };
		var lowest = new Lowest { Length = DonchianPeriod };
		var widthAverage = new SimpleMovingAverage { Length = Math.Max(5, DonchianPeriod / 2) };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(highest, lowest, (candle, highestValue, lowestValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var width = highestValue - lowestValue;

				if (width <= 0)
					return;

				var avgWidthValue = widthAverage.Process(new DecimalIndicatorValue(widthAverage, width, candle.ServerTime) { IsFinal = true });

				if (!widthAverage.IsFormed)
					return;

				var avgWidth = avgWidthValue.ToDecimal();
				if (avgWidth <= 0)
					return;

				var middleChannel = (highestValue + lowestValue) / 2m;

				// Width breakout detection
				if (width > avgWidth * WidthThreshold && Position == 0)
				{
					if (candle.ClosePrice > middleChannel)
						BuyMarket();
					else if (candle.ClosePrice < middleChannel)
						SellMarket();
				}
			})
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}
