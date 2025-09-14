using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the AFL WinnerSign indicator. It uses a double-smoothed
/// stochastic oscillator calculated on volume-weighted price. Long positions
/// are opened when the fast stochastic line crosses above the slow line, while
/// short positions are opened on the opposite cross. Positions are reversed on
/// opposite signals.
/// </summary>
public class AflWinnerSignStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;
	private decimal _prevD;

	/// <summary>
	/// Base period for the stochastic oscillator.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %K line.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %D line.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AflWinnerSignStrategy"/>.
	/// </summary>
	public AflWinnerSignStrategy()
	{
		_period = Param(nameof(Period), 10)
		.SetGreaterThanZero()
		.SetDisplay("Stoch Period", "Base period for stochastic calculation", "AFL WinnerSign")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_kPeriod = Param(nameof(KPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("%K Period", "Smoothing period for %K line", "AFL WinnerSign")
		.SetCanOptimize(true)
		.SetOptimize(3, 10, 1);

		_dPeriod = Param(nameof(DPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("%D Period", "Smoothing period for %D line", "AFL WinnerSign")
		.SetCanOptimize(true)
		.SetOptimize(3, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevK = 0m;
		_prevD = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stochastic = new StochasticOscillator
		{
			Length = Period,
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(stochastic, ProcessCandle)
		.Start();

		StartProtection(
		new Unit(2, UnitTypes.Percent),
		new Unit(2, UnitTypes.Percent)
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
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var stoch = (StochasticOscillatorValue)stochValue;
		var k = stoch.K;
		var d = stoch.D;

		if (_prevK <= _prevD && k > d && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (_prevK >= _prevD && k < d && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevK = k;
		_prevD = d;
	}
}
