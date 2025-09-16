namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

public class ColorSchaffMomentumTrendCycleStrategy : Strategy
{
	private Momentum _fastMomentum;
	private Momentum _slowMomentum;
	private readonly Queue<decimal> _macdQueue = new(); // recent MACD values
	private readonly Queue<decimal> _stQueue = new();	// intermediate ST values
	private int? _prevColor;	// previous color code
	private decimal _prevStc;	// previous STC value

	private StrategyParam<int> _fastMomentumLength;
	private StrategyParam<int> _slowMomentumLength;
	private StrategyParam<int> _cycle;
	private StrategyParam<int> _highLevel;
	private StrategyParam<int> _lowLevel;
	private StrategyParam<bool> _buyPosOpen;
	private StrategyParam<bool> _sellPosOpen;
	private StrategyParam<bool> _buyPosClose;
	private StrategyParam<bool> _sellPosClose;

	public int FastMomentum
	{
		get => _fastMomentumLength.Value;
		set => _fastMomentumLength.Value = value;
	}

	public int SlowMomentum
	{
		get => _slowMomentumLength.Value;
		set => _slowMomentumLength.Value = value;
	}

	public int Cycle
	{
		get => _cycle.Value;
		set => _cycle.Value = value;
	}

	public int HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	public int LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	public ColorSchaffMomentumTrendCycleStrategy()
	{
		_fastMomentumLength = Param(nameof(FastMomentum), 23).SetDisplay("Fast Momentum", "Fast momentum length", "Indicator");
		_slowMomentumLength = Param(nameof(SlowMomentum), 50).SetDisplay("Slow Momentum", "Slow momentum length", "Indicator");
		_cycle = Param(nameof(Cycle), 10).SetDisplay(nameof(Cycle), "Cycle length", "Indicator");
		_highLevel = Param(nameof(HighLevel), 60).SetDisplay("High Level", "Upper threshold", "Indicator");
		_lowLevel = Param(nameof(LowLevel), -60).SetDisplay("Low Level", "Lower threshold", "Indicator");
		_buyPosOpen = Param(nameof(BuyPosOpen), true).SetDisplay("Enable Long", "Allow long entries", "Trading");
		_sellPosOpen = Param(nameof(SellPosOpen), true).SetDisplay("Enable Short", "Allow short entries", "Trading");
		_buyPosClose = Param(nameof(BuyPosClose), true).SetDisplay("Close Long", "Allow closing long positions", "Trading");
		_sellPosClose = Param(nameof(SellPosClose), true).SetDisplay("Close Short", "Allow closing short positions", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_macdQueue.Clear();
		_stQueue.Clear();
		_prevColor = null;
		_prevStc = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// create momentum indicators using selected lengths
		_fastMomentum = new Momentum { Length = FastMomentum };
		_slowMomentum = new Momentum { Length = SlowMomentum };

		// subscribe to candle series and start processing
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		var fast = _fastMomentum.Process(price, candle.OpenTime, true).ToDecimal();
		var slow = _slowMomentum.Process(price, candle.OpenTime, true).ToDecimal();
		var macd = fast - slow;

		// store MACD and keep limited history
		_macdQueue.Enqueue(macd);
		if (_macdQueue.Count > Cycle)
			_macdQueue.Dequeue();

		// find min/max for MACD window
		var llv = decimal.MaxValue;
		var hhv = decimal.MinValue;
		foreach (var value in _macdQueue)
		{
			if (value < llv)
				llv = value;
			if (value > hhv)
				hhv = value;
		}

		// normalize MACD into ST range
		var st = hhv == llv ? (_stQueue.Count > 0 ? _stQueue.Peek() : 0m) : (macd - llv) / (hhv - llv) * 100m;

		// store ST values and keep history
		_stQueue.Enqueue(st);
		if (_stQueue.Count > Cycle)
			_stQueue.Dequeue();

		llv = decimal.MaxValue;
		hhv = decimal.MinValue;
		foreach (var value in _stQueue)
		{
			if (value < llv)
				llv = value;
			if (value > hhv)
				hhv = value;
		}

		// final STC calculation
		var stc = hhv == llv ? _prevStc : (st - llv) / (hhv - llv) * 200m - 100m;
		var dStc = stc - _prevStc;
		var color = 4;

		// determine color code
		if (stc > 0)
		{
			if (stc > HighLevel)
				color = dStc >= 0 ? 7 : 6;
			else
				color = dStc >= 0 ? 5 : 4;
		}
		else
		{
			if (stc < LowLevel)
				color = dStc < 0 ? 0 : 1;
			else
				color = dStc < 0 ? 2 : 3;
		}

		// generate trading signals based on color transition
		if (_prevColor.HasValue)
		{
			var prev = _prevColor.Value;

			if (prev > 5)
			{
				if (SellPosClose && Position < 0)
					ClosePosition();

				if (BuyPosOpen && color < 6 && Position <= 0)
					BuyMarket();
			}
			else if (prev < 2)
			{
				if (BuyPosClose && Position > 0)
					ClosePosition();

				if (SellPosOpen && color > 1 && Position >= 0)
					SellMarket();
			}
		}

		_prevColor = color;
		_prevStc = stc;
	}
}

