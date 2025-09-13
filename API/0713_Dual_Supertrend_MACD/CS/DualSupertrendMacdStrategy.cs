using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Charting;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual Supertrend with MACD strategy.
/// </summary>
public class DualSupertrendMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<MovingAverageTypeEnum> _oscillatorMaType;
	private readonly StrategyParam<MovingAverageTypeEnum> _signalMaType;
	private readonly StrategyParam<int> _atrPeriod1;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<int> _atrPeriod2;
private readonly StrategyParam<decimal> _factor2;
private readonly StrategyParam<Sides?> _direction;

	/// <summary>
	/// Initializes a new instance of the <see cref="DualSupertrendMacdStrategy"/>.
	/// </summary>
	public DualSupertrendMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
						  .SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_macdFast =
			Param(nameof(MacdFast), 12).SetCanOptimize(true).SetDisplay("Fast Length", "MACD fast MA length", "MACD");

		_macdSlow =
			Param(nameof(MacdSlow), 26).SetCanOptimize(true).SetDisplay("Slow Length", "MACD slow MA length", "MACD");

		_macdSignal =
			Param(nameof(MacdSignal), 9).SetCanOptimize(true).SetDisplay("Signal Length", "MACD signal length", "MACD");

		_oscillatorMaType = Param(nameof(OscillatorMaType), MovingAverageTypeEnum.Exponential)
								.SetDisplay("Oscillator MA Type", "Type of MA for MACD fast/slow lines", "MACD");

		_signalMaType = Param(nameof(SignalMaType), MovingAverageTypeEnum.Exponential)
							.SetDisplay("Signal MA Type", "Type of MA for MACD signal line", "MACD");

		_atrPeriod1 = Param(nameof(AtrPeriod1), 10)
						  .SetCanOptimize(true)
						  .SetDisplay("ATR Period 1", "ATR period for first Supertrend", "Supertrend");

		_factor1 = Param(nameof(Factor1), 3.0m)
					   .SetCanOptimize(true)
					   .SetDisplay("Factor 1", "ATR multiplier for first Supertrend", "Supertrend");

		_atrPeriod2 = Param(nameof(AtrPeriod2), 20)
						  .SetCanOptimize(true)
						  .SetDisplay("ATR Period 2", "ATR period for second Supertrend", "Supertrend");

		_factor2 = Param(nameof(Factor2), 5.0m)
					   .SetCanOptimize(true)
					   .SetDisplay("Factor 2", "ATR multiplier for second Supertrend", "Supertrend");

_direction = Param(nameof(Direction), (Sides?)null)
.SetDisplay("Direction", "Trading direction: Long, Short or Both", "Strategy");
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// MACD fast MA length.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow MA length.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal length.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Type of MA for MACD fast/slow lines.
	/// </summary>
	public MovingAverageTypeEnum OscillatorMaType
	{
		get => _oscillatorMaType.Value;
		set => _oscillatorMaType.Value = value;
	}

	/// <summary>
	/// Type of MA for MACD signal line.
	/// </summary>
	public MovingAverageTypeEnum SignalMaType
	{
		get => _signalMaType.Value;
		set => _signalMaType.Value = value;
	}

	/// <summary>
	/// ATR period for first Supertrend.
	/// </summary>
	public int AtrPeriod1
	{
		get => _atrPeriod1.Value;
		set => _atrPeriod1.Value = value;
	}

	/// <summary>
	/// ATR multiplier for first Supertrend.
	/// </summary>
	public decimal Factor1
	{
		get => _factor1.Value;
		set => _factor1.Value = value;
	}

	/// <summary>
	/// ATR period for second Supertrend.
	/// </summary>
	public int AtrPeriod2
	{
		get => _atrPeriod2.Value;
		set => _atrPeriod2.Value = value;
	}

	/// <summary>
	/// ATR multiplier for second Supertrend.
	/// </summary>
	public decimal Factor2
	{
		get => _factor2.Value;
		set => _factor2.Value = value;
	}

	/// <summary>
	/// Trading direction.
	/// </summary>
public Sides? Direction
{
get => _direction.Value;
set => _direction.Value = value;
}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var st1 = new SuperTrend { Length = AtrPeriod1, Multiplier = Factor1 };

		var st2 = new SuperTrend { Length = AtrPeriod2, Multiplier = Factor2 };

		var macd =
			new MovingAverageConvergenceDivergenceSignal { Macd =
															   {
																   ShortMa = CreateMa(OscillatorMaType, MacdFast),
																   LongMa = CreateMa(OscillatorMaType, MacdSlow),
															   },
														   SignalMa = CreateMa(SignalMaType, MacdSignal) };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(st1, st2, macd, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, st1);
			DrawIndicator(area, st2);
			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue st1Value, IIndicatorValue st2Value,
							   IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var st1 = st1Value.ToDecimal();
		var st2 = st2Value.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var hist = macdTyped.Histogram;
		var close = candle.ClosePrice;

		var isBullish = close > st1 && close > st2 && hist > 0;
		var isBearish = close < st1 && close < st2 && hist < 0;
		var exitLong = close < st1 || close < st2 || hist < 0;
		var exitShort = close > st1 || close > st2 || hist > 0;

var dir = Direction;

if ((dir is null or Sides.Buy) && isBullish && Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
else if (Position > 0 && exitLong)
SellMarket(Position);

if ((dir is null or Sides.Sell) && isBearish && Position >= 0)
SellMarket(Volume + Math.Abs(Position));
else if (Position < 0 && exitShort)
BuyMarket(Math.Abs(Position));
	}

	private MovingAverage CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch { MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
							 _ => new ExponentialMovingAverage { Length = length } };
	}

	/// <summary>
	/// Moving average type enumeration.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		/// <summary>
		/// Simple Moving Average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential Moving Average.
		/// </summary>
		Exponential
	}
}
