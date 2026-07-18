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
/// Strategy based on %K and %D crossover from the Stochastic oscillator.
/// </summary>
public class AutoKdStrategy : Strategy
{
	private readonly StrategyParam<int> _kdPeriod;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevK;
	private decimal? _prevD;

	/// <summary>
	/// Lookback period for RSV calculation.
	/// </summary>
	public int KdPeriod
	{
		get => _kdPeriod.Value;
		set => _kdPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for %K.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for %D.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AutoKdStrategy"/>.
	/// </summary>
	public AutoKdStrategy()
	{
		_kdPeriod = Param(nameof(KdPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("KD Period", "Base period for RSV", "Parameters")
		
		.SetOptimize(10, 60, 5);

		_kPeriod = Param(nameof(KPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("K Period", "%K smoothing", "Parameters")
		
		.SetOptimize(1, 10, 1);

		_dPeriod = Param(nameof(DPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("D Period", "%D smoothing", "Parameters")
		
		.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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
		_prevK = null;
		_prevD = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = KdPeriod;
		stochastic.D.Length = DPeriod;

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

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		if (_prevK is decimal prevK && _prevD is decimal prevD)
		{
			if (prevK < prevD && k > d && Math.Min(prevK, k) < 30m && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (prevK > prevD && k < d && Math.Max(prevK, k) > 70m && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevK = k;
		_prevD = d;
	}
}
