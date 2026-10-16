using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// LRMA trend reversal with selectable percentage-deviation threshold crossing
/// or LRMA-slope reversal. Zero protection is intentional and documented.
/// </summary>
public class VininITrendLrmaStrategy : Strategy
{
	public enum SignalModes
	{
		Breakdown,
		Twist,
	}

	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _dnLevel;
	private readonly StrategyParam<SignalModes> _mode;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousOscillator;
	private decimal? _previousLrma;
	private decimal? _previousSlope;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public decimal UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }
	public decimal DnLevel { get => _dnLevel.Value; set => _dnLevel.Value = value; }
	public SignalModes Mode { get => _mode.Value; set => _mode.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VininITrendLrmaStrategy()
	{
		_period = Param(nameof(Period), 13).SetGreaterThanZero()
			.SetDisplay("LRMA Period", "Linear regression lookback", "Indicators");
		_upLevel = Param(nameof(UpLevel), 10m)
			.SetDisplay("Upper Level", "Upper percent deviation trigger for Breakdown", "Signals");
		_dnLevel = Param(nameof(DnLevel), -10m)
			.SetDisplay("Lower Level", "Lower percent deviation trigger for Breakdown", "Signals");
		_mode = Param(nameof(Mode), SignalModes.Breakdown)
			.SetDisplay("Signal Mode", "Breakdown uses percent deviation; Twist uses LRMA slope reversal", "Signals");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousOscillator = null;
		_previousLrma = null;
		_previousSlope = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousOscillator = null;
		_previousLrma = null;
		_previousSlope = null;
		var lrma = new LinearReg { Length = Period };
		SubscribeCandles(CandleType).Bind(lrma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal lrma)
	{
		if (candle.State != CandleStates.Finished || lrma == 0m)
			return;

		var oscillator = (candle.ClosePrice - lrma) / lrma * 100m;
		var slope = _previousLrma.HasValue ? lrma - _previousLrma.Value : (decimal?)null;
		var buy = false;
		var sell = false;

		switch (Mode)
		{
			case SignalModes.Breakdown:
				if (_previousOscillator is decimal prev)
				{
					buy = prev <= UpLevel && oscillator > UpLevel;
					sell = prev >= DnLevel && oscillator < DnLevel;
				}
				break;
			case SignalModes.Twist:
				if (_previousSlope is decimal old && slope is decimal current)
				{
					buy = old < 0m && current > 0m;
					sell = old > 0m && current < 0m;
				}
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Unsupported LRMA signal mode.");
		}

		_previousOscillator = oscillator;
		_previousLrma = lrma;
		_previousSlope = slope;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var position = Position;
		if (buy && position <= 0m)
		{
			if (position < 0m)
				BuyMarket(-position);
			BuyMarket(Volume);
		}
		else if (sell && position >= 0m)
		{
			if (position > 0m)
				SellMarket(position);
			SellMarket(Volume);
		}
	}
}
