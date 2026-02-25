using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy using multiple moving averages similar to a traffic light.
/// Enter long when fast average is above slower ones, short when below.
/// </summary>
public class TrafficLightStrategy : Strategy
{
	private readonly StrategyParam<int> _redMaPeriod;
	private readonly StrategyParam<int> _yellowMaPeriod;
	private readonly StrategyParam<int> _greenMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	public int RedMaPeriod { get => _redMaPeriod.Value; set => _redMaPeriod.Value = value; }
	public int YellowMaPeriod { get => _yellowMaPeriod.Value; set => _yellowMaPeriod.Value = value; }
	public int GreenMaPeriod { get => _greenMaPeriod.Value; set => _greenMaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrafficLightStrategy()
	{
		_redMaPeriod = Param(nameof(RedMaPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("Red MA", "SMA period representing the slow trend", "Parameters");

		_yellowMaPeriod = Param(nameof(YellowMaPeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("Yellow MA", "SMA period representing the medium trend", "Parameters");

		_greenMaPeriod = Param(nameof(GreenMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Green MA", "EMA period representing the fast trend", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var redMa = new SimpleMovingAverage { Length = RedMaPeriod };
		var yellowMa = new SimpleMovingAverage { Length = YellowMaPeriod };
		var greenMa = new ExponentialMovingAverage { Length = GreenMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(redMa, yellowMa, greenMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, redMa);
			DrawIndicator(area, yellowMa);
			DrawIndicator(area, greenMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal red, decimal yellow, decimal green)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Long: green > yellow > red and price above green
		if (green > yellow && yellow > red && price > green && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Short: green < yellow < red and price below green
		else if (green < yellow && yellow < red && price < green && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		// Close on cross
		else if (Position > 0 && green < yellow)
		{
			SellMarket();
		}
		else if (Position < 0 && green > yellow)
		{
			BuyMarket();
		}
	}
}
