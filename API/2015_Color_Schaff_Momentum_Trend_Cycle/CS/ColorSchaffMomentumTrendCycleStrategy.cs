namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Color Schaff momentum trend cycle strategy based on fast and slow momentum.
/// </summary>
public class ColorSchaffMomentumTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMomentumLength;
	private readonly StrategyParam<int> _slowMomentumLength;
	private readonly StrategyParam<int> _cycle;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _macdHistory = new();
	private readonly List<decimal> _stHistory = new();
	private Momentum _fastMomentum;
	private Momentum _slowMomentum;
	private decimal _prevStc;
	private int? _prevColor;
	private int _cooldownRemaining;

	public int FastMomentum { get => _fastMomentumLength.Value; set => _fastMomentumLength.Value = value; }
	public int SlowMomentum { get => _slowMomentumLength.Value; set => _slowMomentumLength.Value = value; }
	public int Cycle { get => _cycle.Value; set => _cycle.Value = value; }
	public int HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public int LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorSchaffMomentumTrendCycleStrategy()
	{
		_fastMomentumLength = Param(nameof(FastMomentum), 23)
			.SetDisplay("Fast Momentum", "Fast momentum length", "Indicator");

		_slowMomentumLength = Param(nameof(SlowMomentum), 50)
			.SetDisplay("Slow Momentum", "Slow momentum length", "Indicator");

		_cycle = Param(nameof(Cycle), 10)
			.SetDisplay(nameof(Cycle), "Cycle length", "Indicator");

		_highLevel = Param(nameof(HighLevel), 60)
			.SetDisplay("High Level", "Upper threshold", "Indicator");

		_lowLevel = Param(nameof(LowLevel), -60)
			.SetDisplay("Low Level", "Lower threshold", "Indicator");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");

		_buyPosOpen = Param(nameof(BuyPosOpen), true).SetDisplay("Enable Long", "Allow long entries", "Trading");
		_sellPosOpen = Param(nameof(SellPosOpen), true).SetDisplay("Enable Short", "Allow short entries", "Trading");
		_buyPosClose = Param(nameof(BuyPosClose), true).SetDisplay("Close Long", "Allow closing long positions", "Trading");
		_sellPosClose = Param(nameof(SellPosClose), true).SetDisplay("Close Short", "Allow closing short positions", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Candles timeframe", "General");
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
		_fastMomentum = null;
		_slowMomentum = null;
		_macdHistory.Clear();
		_stHistory.Clear();
		_prevStc = 0m;
		_prevColor = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMomentum = new Momentum { Length = FastMomentum };
		_slowMomentum = new Momentum { Length = SlowMomentum };
		_macdHistory.Clear();
		_stHistory.Clear();
		_prevStc = 0m;
		_prevColor = null;
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var fastValue = _fastMomentum.Process(candle.ClosePrice, candle.OpenTime, true);
		var slowValue = _slowMomentum.Process(candle.ClosePrice, candle.OpenTime, true);
		if (!fastValue.IsFormed || !slowValue.IsFormed)
			return;

		var diff = fastValue.ToDecimal() - slowValue.ToDecimal();
		AddValue(_macdHistory, diff, Cycle);

		if (_macdHistory.Count < Cycle)
			return;

		GetMinMax(_macdHistory, out var macdMin, out var macdMax);
		var previousSt = _stHistory.Count > 0 ? _stHistory[^1] : 0m;
		var st = macdMax == macdMin ? previousSt : (diff - macdMin) / (macdMax - macdMin) * 100m;
		AddValue(_stHistory, st, Cycle);

		GetMinMax(_stHistory, out var stMin, out var stMax);
		var stc = stMax == stMin ? _prevStc : (st - stMin) / (stMax - stMin) * 200m - 100m;
		var delta = stc - _prevStc;
		var color = GetColor(stc, delta);

		if (_prevColor.HasValue && _cooldownRemaining == 0)
		{
			if (_prevColor.Value == 6 && color == 7 && BuyPosOpen && Position <= 0)
			{
				if (Position < 0 && SellPosClose)
					BuyMarket(Math.Abs(Position));
				BuyMarket();
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (_prevColor.Value == 1 && color == 0 && SellPosOpen && Position >= 0)
			{
				if (Position > 0 && BuyPosClose)
					SellMarket(Position);
				SellMarket();
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (Position > 0 && BuyPosClose && color <= 1)
			{
				SellMarket(Position);
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (Position < 0 && SellPosClose && color >= 6)
			{
				BuyMarket(Math.Abs(Position));
				_cooldownRemaining = SignalCooldownBars;
			}
		}

		_prevColor = color;
		_prevStc = stc;
	}

	private static void AddValue(List<decimal> values, decimal value, int limit)
	{
		values.Add(value);
		if (values.Count > limit)
			values.RemoveAt(0);
	}

	private static void GetMinMax(List<decimal> values, out decimal min, out decimal max)
	{
		min = values[0];
		max = values[0];

		for (var i = 1; i < values.Count; i++)
		{
			var value = values[i];
			if (value < min)
				min = value;
			if (value > max)
				max = value;
		}
	}

	private int GetColor(decimal stc, decimal delta)
	{
		if (stc > 0m)
		{
			if (stc > HighLevel)
				return delta >= 0m ? 7 : 6;

			return delta >= 0m ? 5 : 4;
		}

		if (stc < LowLevel)
			return delta < 0m ? 0 : 1;

		return delta < 0m ? 2 : 3;
	}
}
