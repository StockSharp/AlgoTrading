using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using four MESA Stochastic oscillators with different lengths.
/// Long position when all oscillators are above their triggers and short when below.
/// </summary>
public class MesaStochasticMultiLengthStrategy : Strategy
{
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<int> _length3;
	private readonly StrategyParam<int> _length4;
	private readonly StrategyParam<int> _triggerLength;
	private readonly StrategyParam<DataType> _candleType;

	private MesaState _state1;
	private MesaState _state2;
	private MesaState _state3;
	private MesaState _state4;

	/// <summary>
	/// Lookback for the first oscillator.
	/// </summary>
	public int Length1 { get => _length1.Value; set => _length1.Value = value; }

	/// <summary>
	/// Lookback for the second oscillator.
	/// </summary>
	public int Length2 { get => _length2.Value; set => _length2.Value = value; }

	/// <summary>
	/// Lookback for the third oscillator.
	/// </summary>
	public int Length3 { get => _length3.Value; set => _length3.Value = value; }

	/// <summary>
	/// Lookback for the fourth oscillator.
	/// </summary>
	public int Length4 { get => _length4.Value; set => _length4.Value = value; }

	/// <summary>
	/// Trigger smoothing period.
	/// </summary>
	public int TriggerLength { get => _triggerLength.Value; set => _triggerLength.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MesaStochasticMultiLengthStrategy()
	{
		_length1 = Param(nameof(Length1), 50)
		.SetGreaterThanZero()
		.SetDisplay("Length 1", "Lookback for first oscillator", "Indicators")
		.SetCanOptimize(true);
		_length2 = Param(nameof(Length2), 21)
		.SetGreaterThanZero()
		.SetDisplay("Length 2", "Lookback for second oscillator", "Indicators")
		.SetCanOptimize(true);
		_length3 = Param(nameof(Length3), 14)
		.SetGreaterThanZero()
		.SetDisplay("Length 3", "Lookback for third oscillator", "Indicators")
		.SetCanOptimize(true);
		_length4 = Param(nameof(Length4), 9)
		.SetGreaterThanZero()
		.SetDisplay("Length 4", "Lookback for fourth oscillator", "Indicators")
		.SetCanOptimize(true);
		_triggerLength = Param(nameof(TriggerLength), 2)
		.SetGreaterThanZero()
		.SetDisplay("Trigger Length", "Smoothing for triggers", "Indicators")
		.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_state1 = CreateState(Length1);
		_state2 = CreateState(Length2);
		_state3 = CreateState(Length3);
		_state4 = CreateState(Length4);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = (candle.HighPrice + candle.LowPrice) / 2m;

		var m1 = CalculateMesa(price, _state1);
		var m2 = CalculateMesa(price, _state2);
		var m3 = CalculateMesa(price, _state3);
		var m4 = CalculateMesa(price, _state4);

		if (m1 is null || m2 is null || m3 is null || m4 is null)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var up = m1 > _state1.TriggerValue && m2 > _state2.TriggerValue && m3 > _state3.TriggerValue && m4 > _state4.TriggerValue;
		var down = m1 < _state1.TriggerValue && m2 < _state2.TriggerValue && m3 < _state3.TriggerValue && m4 < _state4.TriggerValue;

		if (up && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (down && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}

	private decimal? CalculateMesa(decimal price, MesaState state)
	{
		const decimal alpha1 = (decimal)((Math.Cos(0.707 * 2 * Math.PI / 48) + Math.Sin(0.707 * 2 * Math.PI / 48) - 1) / Math.Cos(0.707 * 2 * Math.PI / 48));
		const decimal a1 = (decimal)Math.Exp(-1.414 * Math.PI / 10);
		const decimal b1 = 2m * a1 * (decimal)Math.Cos(1.414 * Math.PI / 10);
		const decimal c2 = b1;
		const decimal c3 = -a1 * a1;
		const decimal c1 = 1m - c2 - c3;

		var coef = 1m - alpha1 / 2m;
		var hp = coef * coef * (price - 2m * state.Price1 + state.Price2) + 2m * (1m - alpha1) * state.Hp1 - (1m - alpha1) * (1m - alpha1) * state.Hp2;
		var filt = c1 * (hp + state.Hp1) / 2m + c2 * state.Filt1 + c3 * state.Filt2;

		var hVal = state.Highest.Process(filt);
		var lVal = state.Lowest.Process(filt);

		state.Price2 = state.Price1;
		state.Price1 = price;
		state.Hp2 = state.Hp1;
		state.Hp1 = hp;
		state.Filt2 = state.Filt1;
		state.Filt1 = filt;

		if (!hVal.IsFinal || !lVal.IsFinal)
		return null;

		var highestC = hVal.GetValue<decimal>();
		var lowestC = lVal.GetValue<decimal>();
		var stoc = highestC == lowestC ? 0m : (filt - lowestC) / (highestC - lowestC);
		var mesa = c1 * (stoc + state.Stoc1) / 2m + c2 * state.Mesa1 + c3 * state.Mesa2;

		state.Stoc2 = state.Stoc1;
		state.Stoc1 = stoc;
		state.Mesa2 = state.Mesa1;
		state.Mesa1 = mesa;

		var trigVal = state.Trigger.Process(mesa);
		if (!trigVal.IsFinal)
		return null;

		state.TriggerValue = trigVal.GetValue<decimal>();
		return mesa;
	}

	private MesaState CreateState(int length)
	{
		return new MesaState
		{
			Highest = new Highest { Length = length },
			Lowest = new Lowest { Length = length },
			Trigger = new SimpleMovingAverage { Length = TriggerLength }
		};
	}

	private class MesaState
	{
		public decimal Price1;
		public decimal Price2;
		public decimal Hp1;
		public decimal Hp2;
		public decimal Filt1;
		public decimal Filt2;
		public decimal Stoc1;
		public decimal Stoc2;
		public decimal Mesa1;
		public decimal Mesa2;
		public decimal TriggerValue;
		public Highest Highest;
		public Lowest Lowest;
		public SimpleMovingAverage Trigger;
	}
}
