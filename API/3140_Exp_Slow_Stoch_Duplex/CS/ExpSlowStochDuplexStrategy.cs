namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Dual slow stochastic crossover strategy.
/// Monitors stochastic %K/%D crossovers for long and short signals.
/// </summary>
public class ExpSlowStochDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;

	private decimal _prevK;
	private decimal _prevD;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }

	public ExpSlowStochDuplexStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetDisplay("Stochastic %K", "%K period", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetDisplay("Stochastic %D", "%D period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = KPeriod;
		stochastic.D.Length = DPeriod;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochValue.IsFinal || !stochValue.IsFormed)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		var k = stoch.K ?? 50m;
		var d = stoch.D ?? 50m;

		if (!_hasPrev)
		{
			_prevK = k;
			_prevD = d;
			_hasPrev = true;
			return;
		}

		// Long signal: %K crosses above %D
		var longSignal = _prevK <= _prevD && k > d;
		// Short signal: %K crosses below %D
		var shortSignal = _prevK >= _prevD && k < d;

		if (Position > 0 && shortSignal)
		{
			SellMarket();
		}
		else if (Position < 0 && longSignal)
		{
			BuyMarket();
		}
		else if (Position == 0)
		{
			if (longSignal)
				BuyMarket();
			else if (shortSignal)
				SellMarket();
		}

		_prevK = k;
		_prevD = d;
	}
}
