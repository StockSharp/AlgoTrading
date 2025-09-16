using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color Schaff JCCX Trend Cycle indicator.
/// Converted from the MQL5 expert Exp_ColorSchaffJCCXTrendCycle.
/// </summary>
public class ColorSchaffJccxTrendCycleStrategy : Strategy
{
	private SchaffTrendCycle _stc;
	private decimal? _prev;

	private StrategyParam<int> _fastLength;
	private StrategyParam<int> _slowLength;
	private StrategyParam<int> _smooth;
	private StrategyParam<int> _phase;
	private StrategyParam<int> _cycle;
	private StrategyParam<int> _highLevel;
	private StrategyParam<int> _lowLevel;
	private StrategyParam<bool> _buyPosOpen;
	private StrategyParam<bool> _sellPosOpen;
	private StrategyParam<bool> _buyPosClose;
	private StrategyParam<bool> _sellPosClose;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int Smooth { get => _smooth.Value; set => _smooth.Value = value; }
	public int Phase { get => _phase.Value; set => _phase.Value = value; }
	public int Cycle { get => _cycle.Value; set => _cycle.Value = value; }
	public int HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public int LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	public ColorSchaffJccxTrendCycleStrategy()
	{
		_fastLength = Param(nameof(FastLength), 23)
			.SetDisplay("Fast JCCX", "Fast JCCX length", "Indicator")
			.SetCanOptimize();
		_slowLength = Param(nameof(SlowLength), 50)
			.SetDisplay("Slow JCCX", "Slow JCCX length", "Indicator")
			.SetCanOptimize();
		_smooth = Param(nameof(Smooth), 8)
			.SetDisplay("Smoothing", "JJMA smoothing factor", "Indicator")
			.SetCanOptimize();
		_phase = Param(nameof(Phase), 100)
			.SetDisplay("Phase", "JJMA phase value", "Indicator")
			.SetCanOptimize();
		_cycle = Param(nameof(Cycle), 10)
			.SetDisplay("Cycle", "Cycle length", "Indicator")
			.SetCanOptimize();
		_highLevel = Param(nameof(HighLevel), 60)
			.SetDisplay("High Level", "Upper trigger level", "Signal")
			.SetCanOptimize();
		_lowLevel = Param(nameof(LowLevel), -60)
			.SetDisplay("Low Level", "Lower trigger level", "Signal")
			.SetCanOptimize();
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Open Long", "Allow opening long positions", "Trading");
		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Open Short", "Allow opening short positions", "Trading");
		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Close Long", "Allow closing long positions", "Trading");
		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Close Short", "Allow closing short positions", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stc = new SchaffTrendCycle
		{
			FastPeriod = FastLength,
			SlowPeriod = SlowLength,
			Smooth = Smooth,
			Phase = Phase,
			Cycle = Cycle
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_stc, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal stc)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prev is null)
		{
			_prev = stc;
			return;
		}

		// Open long when STC leaves the overbought zone
		if (_prev > HighLevel && stc <= HighLevel)
		{
			if (SellPosClose && Position < 0)
				ClosePosition();
			if (BuyPosOpen && Position <= 0)
				BuyMarket();
		}

		// Open short when STC leaves the oversold zone
		if (_prev < LowLevel && stc >= LowLevel)
		{
			if (BuyPosClose && Position > 0)
				ClosePosition();
			if (SellPosOpen && Position >= 0)
				SellMarket();
		}

		_prev = stc;
	}
}
