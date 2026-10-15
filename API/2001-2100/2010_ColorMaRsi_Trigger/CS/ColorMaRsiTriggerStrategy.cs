using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ColorMaRsi Trigger strategy.
/// Combines fast and slow EMA crossover with RSI crossover to generate trading signals.
/// </summary>
public class ColorMaRsiTriggerStrategy : Strategy
{
	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<int> _emaSlowLength;
	private readonly StrategyParam<int> _rsiFastLength;
	private readonly StrategyParam<int> _rsiSlowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSignal;

	public int EmaFastLength { get => _emaFastLength.Value; set => _emaFastLength.Value = value; }
	public int EmaSlowLength { get => _emaSlowLength.Value; set => _emaSlowLength.Value = value; }
	public int RsiFastLength { get => _rsiFastLength.Value; set => _rsiFastLength.Value = value; }
	public int RsiSlowLength { get => _rsiSlowLength.Value; set => _rsiSlowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorMaRsiTriggerStrategy()
	{
		_emaFastLength = Param(nameof(EmaFastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "General");

		_emaSlowLength = Param(nameof(EmaSlowLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "General");

		_rsiFastLength = Param(nameof(RsiFastLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast RSI", "Fast RSI period", "General");

		_rsiSlowLength = Param(nameof(RsiSlowLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow RSI", "Slow RSI period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaFast = new ExponentialMovingAverage { Length = EmaFastLength };
		var emaSlow = new ExponentialMovingAverage { Length = EmaSlowLength };
		var rsiFast = new RelativeStrengthIndex { Length = RsiFastLength };
		var rsiSlow = new RelativeStrengthIndex { Length = RsiSlowLength };

		SubscribeCandles(CandleType)
			.Bind(emaFast, emaSlow, rsiFast, rsiSlow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFastValue, decimal emaSlowValue, decimal rsiFastValue, decimal rsiSlowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Compute composite signal from EMA and RSI crossovers
		var signal = 0m;
		if (emaFastValue > emaSlowValue)
			signal += 1;
		if (emaFastValue < emaSlowValue)
			signal -= 1;
		if (rsiFastValue > rsiSlowValue)
			signal += 1;
		if (rsiFastValue < rsiSlowValue)
			signal -= 1;

		// Clamp signal to [-1, 1]
		signal = Math.Clamp(signal, -1, 1);

		// Detect signal change for entry/exit
		if (_prevSignal <= 0 && signal > 0 && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (_prevSignal >= 0 && signal < 0 && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevSignal = signal;
	}
}
