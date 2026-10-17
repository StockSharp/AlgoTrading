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

	private decimal? _prevSignal;
	private int _count;
	private Order _pending;

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
		_prevSignal = null;
		_count = 0;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSignal = null;
		_count = 0;
		_pending = null;

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

		// Both RSI and EMA paths must have enough finished bars before the
		// previous composite sign is meaningful.
		_count++;
		if (_count < Math.Max(Math.Max(EmaFastLength, EmaSlowLength),
			Math.Max(RsiFastLength, RsiSlowLength)) + 2)
			return;

		// The documented MQL5 trigger is intentionally counter-directional:
		// positive -> zero/negative opens LONG; negative -> zero/positive opens SHORT.
		var buy = _prevSignal is decimal oldPositive && oldPositive > 0m && signal <= 0m;
		var sell = _prevSignal is decimal oldNegative && oldNegative < 0m && signal >= 0m;
		_prevSignal = signal;

		if (!IsFormedAndOnlineAndAllowTrading()
			|| (_pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		if (buy)
		{
			if (Position < 0m)
				_pending = BuyMarket(-Position + Volume);
			else if (Position == 0m)
				_pending = BuyMarket(Volume);
		}
		else if (sell)
		{
			if (Position > 0m)
				_pending = SellMarket(Position + Volume);
			else if (Position == 0m)
				_pending = SellMarket(Volume);
		}
	}
}
