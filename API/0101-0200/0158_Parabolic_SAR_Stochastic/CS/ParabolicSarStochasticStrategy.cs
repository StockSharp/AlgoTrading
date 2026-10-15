using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR Stochastic strategy.
/// A close above the Parabolic SAR with %K below StochOversold goes long and a close below the SAR with %K above StochOverbought goes short,
/// reversing an opposite position; %K is the stochastic over StochPeriod candles smoothed over StochK candles. The SAR is the trailing stop:
/// a long closes when the SAR flips above price and a short when it flips below.
/// </summary>
public class ParabolicSarStochasticStrategy : Strategy
{
	private readonly StrategyParam<decimal> _accelerationFactor;
	private readonly StrategyParam<decimal> _maxAccelerationFactor;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Initial acceleration factor of the SAR.
	/// </summary>
	public decimal AccelerationFactor
	{
		get => _accelerationFactor.Value;
		set => _accelerationFactor.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor of the SAR.
	/// </summary>
	public decimal MaxAccelerationFactor
	{
		get => _maxAccelerationFactor.Value;
		set => _maxAccelerationFactor.Value = value;
	}

	/// <summary>
	/// Smoothing period of %K.
	/// </summary>
	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}

	/// <summary>
	/// Lookback period of the raw stochastic.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// %K level for longs.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	/// <summary>
	/// %K level for shorts.
	/// </summary>
	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
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
	/// Constructor.
	/// </summary>
	public ParabolicSarStochasticStrategy()
	{
		_accelerationFactor = Param(nameof(AccelerationFactor), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Acceleration", "Initial acceleration factor of the SAR", "SAR");

		_maxAccelerationFactor = Param(nameof(MaxAccelerationFactor), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max Acceleration", "Maximum acceleration factor of the SAR", "SAR");

		_stochK = Param(nameof(StochK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Stochastic Oversold", "%K level for longs", "Stochastic");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Stochastic Overbought", "%K level for shorts", "Stochastic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sar = new ParabolicSar
		{
			Acceleration = AccelerationFactor,
			AccelerationMax = MaxAccelerationFactor,
		};
		// The D line of the core oscillator is the smoothed %K.
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochPeriod },
			D = { Length = StochK },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sar, stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, stochastic);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The first SAR value is formed but empty.
		if (!sarValue.IsFormed || sarValue.IsEmpty || !stochasticValue.IsFormed)
			return;

		if (stochasticValue is not IStochasticOscillatorValue { D: decimal k })
			return;

		var sar = sarValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (close > sar && k < StochOversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < sar && k > StochOverbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < sar)
			SellMarket(Position);
		else if (Position < 0 && close > sar)
			BuyMarket(-Position);
	}
}
