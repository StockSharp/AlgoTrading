using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Schaff Trend Cycle calculated from WPR.
/// </summary>
public class ColorSchaffWprTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastWpr;
	private readonly StrategyParam<int> _slowWpr;
	private readonly StrategyParam<int> _cycle;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevStc;

	public int FastWpr { get => _fastWpr.Value; set => _fastWpr.Value = value; }
	public int SlowWpr { get => _slowWpr.Value; set => _slowWpr.Value = value; }
	public int Cycle { get => _cycle.Value; set => _cycle.Value = value; }
	public int HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public int LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorSchaffWprTrendCycleStrategy()
	{
		_fastWpr = Param(nameof(FastWpr), 23)
			.SetDisplay("Fast WPR", "Fast Williams %R period", "Indicator");

		_slowWpr = Param(nameof(SlowWpr), 50)
			.SetDisplay("Slow WPR", "Slow Williams %R period", "Indicator");

		_cycle = Param(nameof(Cycle), 10)
			.SetDisplay("Cycle", "Cycle length", "Indicator");

		_highLevel = Param(nameof(HighLevel), 60)
			.SetDisplay("High Level", "Upper trigger level", "Indicator");

		_lowLevel = Param(nameof(LowLevel), -60)
			.SetDisplay("Low Level", "Lower trigger level", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Schaff trend cycle indicator using WPR values
		var stc = new SchaffTrendCycle
		{
			Fast = FastWpr,
			Slow = SlowWpr,
			Cycle = Cycle
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(stc, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal stcValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prev = _prevStc;
		_prevStc = stcValue;

		// Generate buy signal when STC crosses above HighLevel
		var crossUp = prev <= HighLevel && stcValue > HighLevel;
		// Generate sell signal when STC crosses below LowLevel
		var crossDown = prev >= LowLevel && stcValue < LowLevel;

		if (crossUp)
		{
			if (Position <= 0)
				BuyMarket();

			if (Position < 0)
				BuyMarket();
		}
		else if (crossDown)
		{
			if (Position >= 0)
				SellMarket();

			if (Position > 0)
				SellMarket();
		}
	}
}
