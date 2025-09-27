using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD crossover strategy within predefined zone.
/// Goes long when MACD crosses above signal line inside the zone and short on opposite condition.
/// Closes positions on opposite crossovers regardless of zone.
/// </summary>
public class MacdCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevIsMacdAboveSignal;

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Lower bound of important MACD zone.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
	}

	/// <summary>
	/// Upper bound of important MACD zone.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
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
	/// Initializes a new instance of <see cref="MacdCrossoverStrategy"/>.
	/// </summary>
	public MacdCrossoverStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period", "MACD Settings")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 2);

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "MACD Settings")
			.SetCanOptimize(true)
			.SetOptimize(20, 32, 2);

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal line period", "MACD Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 13, 2);

		_lowerThreshold = Param(nameof(LowerThreshold), -0.5m)
			.SetDisplay("Lower Threshold", "Lower bound for MACD zone", "MACD Zone")
			.SetCanOptimize(true)
			.SetOptimize(-1m, 0m, 0.1m);

		_upperThreshold = Param(nameof(UpperThreshold), 0.5m)
			.SetDisplay("Upper Threshold", "Upper bound for MACD zone", "MACD Zone")
			.SetCanOptimize(true)
			.SetOptimize(0m, 1m, 0.1m);

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
		_prevIsMacdAboveSignal = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd;
		var signal = macdTyped.Signal;

		var isMacdAboveSignal = macd > signal;
		var crossUp = isMacdAboveSignal && !_prevIsMacdAboveSignal;
		var crossDown = !isMacdAboveSignal && _prevIsMacdAboveSignal;
		var inZone = macd >= LowerThreshold && macd <= UpperThreshold;

		if (crossDown && Position > 0)
			SellMarket(Position);
		if (crossUp && Position < 0)
			BuyMarket(Math.Abs(Position));

		if (crossUp && inZone && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		if (crossDown && inZone && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevIsMacdAboveSignal = isMacdAboveSignal;
	}
}
