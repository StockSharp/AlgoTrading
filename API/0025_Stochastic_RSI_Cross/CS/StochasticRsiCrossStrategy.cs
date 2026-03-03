using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Stochastic Oscillator K/D crossover.
/// Buys when %K crosses above %D in oversold zone.
/// Sells when %K crosses below %D in overbought zone.
/// </summary>
public class StochasticRsiCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;
	private decimal _prevD;
	private bool _hasPrevValues;
	private int _cooldown;

	/// <summary>
	/// K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticRsiCrossStrategy"/>.
	/// </summary>
	public StochasticRsiCrossStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 14)
			.SetDisplay("K Period", "Period for %K line", "Indicators")
			.SetOptimize(10, 20, 2);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetDisplay("D Period", "Period for %D line", "Indicators")
			.SetOptimize(3, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevK = default;
		_prevD = default;
		_hasPrevValues = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stoch = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stoch, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stoch);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (IStochasticOscillatorValue)stochValue;

		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevK = k;
			_prevD = d;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevK = k;
			_prevD = d;
			return;
		}

		// %K crosses above %D in oversold zone (< 20) - buy
		if (_prevK <= _prevD && k > d && k < 20 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = 5;
		}
		// %K crosses below %D in overbought zone (> 80) - sell
		else if (_prevK >= _prevD && k < d && k > 80 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = 5;
		}

		_prevK = k;
		_prevD = d;
	}
}
