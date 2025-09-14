namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the stochastic oscillator with multiple signal modes.
/// </summary>
public class ColorStochNrStrategy : Strategy
{
	public enum AlgMode
	{
		Breakdown,
		OscTwist,
		SignalTwist,
		OscDisposition,
		SignalBreakdown
	}

	private readonly StrategyParam<AlgMode> _mode;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;
	private decimal _prevD;
	private decimal _prevKDelta;
	private decimal _prevDDelta;

	public AlgMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ColorStochNrStrategy()
	{
		_mode = Param(nameof(Mode), AlgMode.OscDisposition)
			.SetDisplay("Algorithm Mode", "Trading algorithm to use", "Parameters");

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Length for %K line", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Length for %D line", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevK = 0m;
		_prevD = 0m;
		_prevKDelta = 0m;
		_prevDDelta = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure we are allowed to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		var k = stoch.K;
		var d = stoch.D;

		var deltaK = k - _prevK;
		var deltaD = d - _prevD;

		var buy = false;
		var sell = false;

		switch (Mode)
		{
			case AlgMode.Breakdown:
				if (_prevK <= 50m && k > 50m)
					buy = true;
				else if (_prevK >= 50m && k < 50m)
					sell = true;
				break;
			case AlgMode.OscTwist:
				if (_prevKDelta <= 0m && deltaK > 0m)
					buy = true;
				else if (_prevKDelta >= 0m && deltaK < 0m)
					sell = true;
				break;
			case AlgMode.SignalTwist:
				if (_prevDDelta <= 0m && deltaD > 0m)
					buy = true;
				else if (_prevDDelta >= 0m && deltaD < 0m)
					sell = true;
				break;
			case AlgMode.OscDisposition:
				if (_prevK <= _prevD && k > d)
					buy = true;
				else if (_prevK >= _prevD && k < d)
					sell = true;
				break;
			case AlgMode.SignalBreakdown:
				if (_prevD <= 50m && d > 50m)
					buy = true;
				else if (_prevD >= 50m && d < 50m)
					sell = true;
				break;
		}

		if (buy && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (sell && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevKDelta = deltaK;
		_prevDDelta = deltaD;
		_prevK = k;
		_prevD = d;
	}
}
