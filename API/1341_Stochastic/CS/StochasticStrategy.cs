using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple strategy based on the Stochastic oscillator crossing a threshold.
/// </summary>
public class StochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _smoothK;
	private readonly StrategyParam<int> _smoothD;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;

	/// <summary>
	/// Stochastic length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// %K smoothing period.
	/// </summary>
	public int SmoothK
	{
		get => _smoothK.Value;
		set => _smoothK.Value = value;
	}

	/// <summary>
	/// %D smoothing period.
	/// </summary>
	public int SmoothD
	{
		get => _smoothD.Value;
		set => _smoothD.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="StochasticStrategy"/>.
	/// </summary>
	public StochasticStrategy()
	{
		_length = Param(nameof(Length), 7)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Length", "Base period for Stochastic", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_smoothK = Param(nameof(SmoothK), 2)
			.SetGreaterThanZero()
			.SetDisplay("Smooth %K", "%K smoothing period", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_smoothD = Param(nameof(SmoothD), 2)
			.SetGreaterThanZero()
			.SetDisplay("Smooth %D", "%D smoothing period", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_overbought = Param(nameof(Overbought), 50m)
			.SetDisplay("Overbought Level", "Upper threshold", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(50m, 80m, 5m);

		_oversold = Param(nameof(Oversold), 50m)
			.SetDisplay("Oversold Level", "Lower threshold", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(20m, 50m, 5m);

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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stochastic = new StochasticOscillator
		{
			Length = Length,
			K = { Length = SmoothK },
			D = { Length = SmoothD },
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

		if (_prevK <= Oversold && k > Oversold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevK >= Overbought && k < Overbought && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevK = k;
	}
}
