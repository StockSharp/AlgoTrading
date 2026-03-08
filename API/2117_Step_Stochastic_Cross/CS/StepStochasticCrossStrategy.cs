using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Step Stochastic cross strategy using standard Stochastic Oscillator.
/// Opens long when slow %D is above 50 and %K crosses below %D.
/// Opens short when slow %D is below 50 and %K crosses above %D.
/// </summary>
public class StepStochasticCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;

	private decimal _prevK;
	private decimal _prevD;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

	public StepStochasticCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame", "General");

		_kPeriod = Param(nameof(KPeriod), 14)
			.SetDisplay("K Period", "Stochastic K period", "Parameters");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetDisplay("D Period", "Stochastic D period", "Parameters");
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
		_prevK = 0;
		_prevD = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevK = 0;
		_prevD = 0;
		_hasPrev = false;

		var stoch = new StochasticOscillator();
		stoch.K.Length = KPeriod;
		stoch.D.Length = DPeriod;

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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stochVal = (IStochasticOscillatorValue)value;
		if (stochVal.K is not decimal kValue || stochVal.D is not decimal dValue)
			return;

		if (!_hasPrev)
		{
			_prevK = kValue;
			_prevD = dValue;
			_hasPrev = true;
			return;
		}

		// When D is above 50, look for K crossing below D (buy signal - oversold bounce)
		if (dValue > 50m)
		{
			if (Position < 0)
				BuyMarket();

			if (_prevK > _prevD && kValue <= dValue && Position <= 0)
				BuyMarket();
		}
		// When D is below 50, look for K crossing above D (sell signal - overbought drop)
		else if (dValue < 50m)
		{
			if (Position > 0)
				SellMarket();

			if (_prevK < _prevD && kValue >= dValue && Position >= 0)
				SellMarket();
		}

		_prevK = kValue;
		_prevD = dValue;
	}
}
