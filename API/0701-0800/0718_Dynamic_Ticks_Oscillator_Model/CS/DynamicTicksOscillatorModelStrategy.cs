using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dynamic Ticks Oscillator Model strategy.
/// Built for the NYSE Down Ticks index as the strategy security. The RocLength rate of change is compared with the standard
/// deviation of that rate over VolatilityLookback candles: a long opens when the ROC drops below -StdDev * EntryStdDevMultiplier
/// and closes when it rises above StdDev * ExitStdDevMultiplier.
/// </summary>
public class DynamicTicksOscillatorModelStrategy : Strategy
{
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<int> _volatilityLookback;
	private readonly StrategyParam<decimal> _entryStdDevMultiplier;
	private readonly StrategyParam<decimal> _exitStdDevMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation _rocStdDev;

	/// <summary>
	/// Rate of change length.
	/// </summary>
	public int RocLength
	{
		get => _rocLength.Value;
		set => _rocLength.Value = value;
	}

	/// <summary>
	/// Candles over which the standard deviation of the ROC is measured.
	/// </summary>
	public int VolatilityLookback
	{
		get => _volatilityLookback.Value;
		set => _volatilityLookback.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier of the entry threshold.
	/// </summary>
	public decimal EntryStdDevMultiplier
	{
		get => _entryStdDevMultiplier.Value;
		set => _entryStdDevMultiplier.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier of the exit threshold.
	/// </summary>
	public decimal ExitStdDevMultiplier
	{
		get => _exitStdDevMultiplier.Value;
		set => _exitStdDevMultiplier.Value = value;
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
	public DynamicTicksOscillatorModelStrategy()
	{
		_rocLength = Param(nameof(RocLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("ROC Length", "Rate of change length", "Indicators");

		_volatilityLookback = Param(nameof(VolatilityLookback), 24)
			.SetGreaterThanZero()
			.SetDisplay("Volatility Lookback", "Standard deviation length of the ROC", "Indicators");

		_entryStdDevMultiplier = Param(nameof(EntryStdDevMultiplier), 1.6m)
			.SetGreaterThanZero()
			.SetDisplay("Entry StdDev Mult", "Standard deviation multiplier of the entry threshold", "Signals");

		_exitStdDevMultiplier = Param(nameof(ExitStdDevMultiplier), 1.4m)
			.SetGreaterThanZero()
			.SetDisplay("Exit StdDev Mult", "Standard deviation multiplier of the exit threshold", "Signals");

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
		_rocStdDev = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var roc = new RateOfChange { Length = RocLength };
		_rocStdDev = new StandardDeviation { Length = VolatilityLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, roc);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The deviation is measured on the ROC series itself.
		var stdValue = _rocStdDev.Process(new DecimalIndicatorValue(_rocStdDev, rocValue, candle.OpenTime) { IsFinal = true });

		if (!stdValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var std = stdValue.GetValue<decimal>();

		if (Position == 0 && rocValue < -std * EntryStdDevMultiplier)
			BuyMarket(Volume);
		else if (Position > 0 && rocValue > std * ExitStdDevMultiplier)
			SellMarket(Position);
	}
}
