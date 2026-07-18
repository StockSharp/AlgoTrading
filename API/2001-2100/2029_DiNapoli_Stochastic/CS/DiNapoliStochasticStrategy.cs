using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DiNapoli Stochastic cross strategy.
/// Opens a long position when the %K line crosses above %D and
/// a short position when %K crosses below %D.
/// </summary>
public class DiNapoliStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _fastK;
	private readonly StrategyParam<int> _slowD;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;
	private decimal _prevD;
	private bool _prevReady;

	public int FastK { get => _fastK.Value; set => _fastK.Value = value; }
	public int SlowD { get => _slowD.Value; set => _slowD.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DiNapoliStochasticStrategy()
	{
		_fastK = Param(nameof(FastK), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast %K", "Base period for %K", "DiNapoli");

		_slowD = Param(nameof(SlowD), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slow %D", "%D smoothing period", "DiNapoli");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculation", "General");
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
		_prevK = 0m;
		_prevD = 0m;
		_prevReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(null, null);

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = FastK;
		stochastic.D.Length = SlowD;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

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
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (IStochasticOscillatorValue)stochValue;

		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		if (!_prevReady)
		{
			_prevK = k;
			_prevD = d;
			_prevReady = true;
			return;
		}

		// %K crosses above %D - buy signal
		if (_prevK <= _prevD && k > d && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// %K crosses below %D - sell signal
		else if (_prevK >= _prevD && k < d && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevK = k;
		_prevD = d;
	}
}
