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
/// Quantum Stochastic strategy based on Stochastic oscillator thresholds.
/// Buys when %K leaves oversold zone and sells when %K leaves overbought zone.
/// </summary>
public class QuantumStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousK;

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

	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public QuantumStochasticStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Period of %K line", "Stochastic");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Period of %D line", "Stochastic");

		_highLevel = Param(nameof(HighLevel), 80m)
			.SetDisplay("High Level", "Bottom of overbought zone", "Levels");

		_lowLevel = Param(nameof(LowLevel), 20m)
			.SetDisplay("Low Level", "Top of oversold zone", "Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_previousK = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (IStochasticOscillatorValue)stochValue;

		if (stoch.K is not decimal kValue)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousK = kValue;
			return;
		}

		if (_previousK is not decimal prevK)
		{
			_previousK = kValue;
			return;
		}

		// Buy when %K crosses above oversold level
		if (prevK < LowLevel && kValue >= LowLevel && Position <= 0)
			BuyMarket();

		// Sell when %K crosses below overbought level
		if (prevK > HighLevel && kValue <= HighLevel && Position >= 0)
			SellMarket();

		_previousK = kValue;
	}
}
