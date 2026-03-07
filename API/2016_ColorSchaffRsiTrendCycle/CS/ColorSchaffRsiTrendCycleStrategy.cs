namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on a Schaff-style cycle built from fast and slow RSI values.
/// </summary>
public class ColorSchaffRsiTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastRsi;
	private readonly StrategyParam<int> _slowRsi;
	private readonly StrategyParam<int> _cycle;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalCooldownBars;

	private readonly List<decimal> _macdHistory = [];
	private readonly List<decimal> _stHistory = [];
	private RelativeStrengthIndex _fastIndicator = null!;
	private RelativeStrengthIndex _slowIndicator = null!;
	private decimal _prevStc;
	private int? _prevColor;
	private int _cooldownRemaining;

	/// <summary>
	/// Fast RSI period.
	/// </summary>
	public int FastRsi
	{
		get => _fastRsi.Value;
		set => _fastRsi.Value = value;
	}

	/// <summary>
	/// Slow RSI period.
	/// </summary>
	public int SlowRsi
	{
		get => _slowRsi.Value;
		set => _slowRsi.Value = value;
	}

	/// <summary>
	/// Cycle length used in the Schaff calculation.
	/// </summary>
	public int Cycle
	{
		get => _cycle.Value;
		set => _cycle.Value = value;
	}

	/// <summary>
	/// Upper level for color classification.
	/// </summary>
	public int HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower level for color classification.
	/// </summary>
	public int LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bars to wait after each trading action.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorSchaffRsiTrendCycleStrategy"/>.
	/// </summary>
	public ColorSchaffRsiTrendCycleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");

		_fastRsi = Param(nameof(FastRsi), 23)
			.SetGreaterThanZero()
			.SetDisplay("Fast RSI", "Fast RSI period", "Parameters")
			.SetOptimize(10, 30, 5);

		_slowRsi = Param(nameof(SlowRsi), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow RSI", "Slow RSI period", "Parameters")
			.SetOptimize(30, 70, 5);

		_cycle = Param(nameof(Cycle), 10)
			.SetGreaterThanZero()
			.SetDisplay("Cycle", "Cycle length", "Parameters")
			.SetOptimize(5, 20, 1);

		_highLevel = Param(nameof(HighLevel), 60)
			.SetDisplay("High Level", "Upper level for the cycle", "Parameters")
			.SetOptimize(40, 80, 5);

		_lowLevel = Param(nameof(LowLevel), -60)
			.SetDisplay("Low Level", "Lower level for the cycle", "Parameters")
			.SetOptimize(-80, -40, 5);

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trading actions", "Trading");
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

		_fastIndicator = null!;
		_slowIndicator = null!;
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

		_fastIndicator = new RelativeStrengthIndex { Length = FastRsi };
		_slowIndicator = new RelativeStrengthIndex { Length = SlowRsi };
		_macdHistory.Clear();
		_stHistory.Clear();
		_prevStc = 0m;
		_prevColor = null;
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var fastValue = _fastIndicator.Process(candle.ClosePrice, candle.OpenTime, true);
		var slowValue = _slowIndicator.Process(candle.ClosePrice, candle.OpenTime, true);
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
			if (_prevColor.Value == 6 && color == 7 && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (_prevColor.Value == 1 && color == 0 && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (Position > 0 && color <= 1)
			{
				SellMarket(Position);
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (Position < 0 && color >= 6)
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
