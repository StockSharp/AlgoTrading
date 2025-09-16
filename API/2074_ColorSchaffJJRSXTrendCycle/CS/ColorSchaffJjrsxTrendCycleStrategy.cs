using System;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ColorSchaffJjrsxTrendCycleStrategy : Strategy
{
	private decimal? _prevStc;

	private StrategyParam<int> _fast;
	private StrategyParam<int> _slow;
	private StrategyParam<int> _cycle;
	private StrategyParam<decimal> _highLevel;
	private StrategyParam<decimal> _lowLevel;

	public int Fast { get => _fast.Value; set => _fast.Value = value; }
	public int Slow { get => _slow.Value; set => _slow.Value = value; }
	public int Cycle { get => _cycle.Value; set => _cycle.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }

	public ColorSchaffJjrsxTrendCycleStrategy()
	{
		_fast = Param(nameof(Fast), 23)
			.SetDisplay("Fast JJRSX", "Fast period for the Schaff Trend Cycle", "Indicator")
			.SetCanOptimize(true);

		_slow = Param(nameof(Slow), 50)
			.SetDisplay("Slow JJRSX", "Slow period for the Schaff Trend Cycle", "Indicator")
			.SetCanOptimize(true);

		_cycle = Param(nameof(Cycle), 10)
			.SetDisplay("Cycle", "Cycle length of the Schaff Trend Cycle", "Indicator")
			.SetCanOptimize(true);

		_highLevel = Param(nameof(HighLevel), 60m)
			.SetDisplay("High Level", "Upper threshold for signals", "Levels")
			.SetCanOptimize(true);

		_lowLevel = Param(nameof(LowLevel), -60m)
			.SetDisplay("Low Level", "Lower threshold for signals", "Levels")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var stc = new SchaffTrendCycle
		{
			FastPeriod = Fast,
			SlowPeriod = Slow,
			Cycle = Cycle
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(stc, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal stc)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevStc is null)
		{
			_prevStc = stc;
			return;
		}

		if (_prevStc <= HighLevel && stc > HighLevel)
		{
			if (Position < 0)
				ClosePosition();

			if (Position <= 0)
				BuyMarket();
		}
		else if (_prevStc >= LowLevel && stc < LowLevel)
		{
			if (Position > 0)
				ClosePosition();

			if (Position >= 0)
				SellMarket();
		}

		_prevStc = stc;
	}
}
