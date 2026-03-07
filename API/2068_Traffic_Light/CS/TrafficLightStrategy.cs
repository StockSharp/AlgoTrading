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
		_redMaPeriod = Param(nameof(RedMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Red MA", "EMA period representing the slow trend", "Parameters");

		_yellowMaPeriod = Param(nameof(YellowMaPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Yellow MA", "EMA period representing the medium trend", "Parameters");

		_greenMaPeriod = Param(nameof(GreenMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Green MA", "EMA period representing the fast trend", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var redMa = new ExponentialMovingAverage { Length = RedMaPeriod };
		var yellowMa = new ExponentialMovingAverage { Length = YellowMaPeriod };
		var greenMa = new ExponentialMovingAverage { Length = GreenMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(redMa, yellowMa, greenMa, ProcessCandle)
			.Start();
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
