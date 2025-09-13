using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ride Alligator strategy based on Lips and Jaws crossover.
/// Opens a long position when Lips crosses above Jaws while Teeth is below Jaws.
/// Opens a short position when Lips crosses below Jaws while Teeth is above Jaws.
/// Positions are exited if price crosses the Jaws line.
/// </summary>
public class RideAlligatorStrategy : Strategy
{
	private readonly StrategyParam<int> _alligatorPeriod;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _jaw;
	private LengthIndicator<decimal> _teeth;
	private LengthIndicator<decimal> _lips;

	private decimal? _prevJaw;
	private decimal? _prevLips;

	/// <summary>
	/// Base period used to calculate Alligator lengths.
	/// </summary>
	public int AlligatorPeriod
	{
		get => _alligatorPeriod.Value;
		set => _alligatorPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type for Alligator lines.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="RideAlligatorStrategy"/>.
	/// </summary>
	public RideAlligatorStrategy()
	{
		_alligatorPeriod = Param(nameof(AlligatorPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Alligator Period", "Base period for Alligator", "Alligator");

		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Weighted)
		.SetDisplay("MA Type", "Moving average type", "Alligator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_prevJaw = null;
		_prevLips = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var phi = 1.61803398874989m;
		var a1 = (int)Math.Round(AlligatorPeriod * phi);
		var a2 = (int)Math.Round(a1 * phi);
		var a3 = (int)Math.Round(a2 * phi);

		_jaw = CreateMa(MaType, a3);
		_teeth = CreateMa(MaType, a2);
		_lips = CreateMa(MaType, a1);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var jawVal = _jaw.Process(median);
		var teethVal = _teeth.Process(median);
		var lipsVal = _lips.Process(median);

		if (!jawVal.IsFinal || !teethVal.IsFinal || !lipsVal.IsFinal)
		return;

		var jaw = jawVal.GetValue<decimal>();
		var teeth = teethVal.GetValue<decimal>();
		var lips = lipsVal.GetValue<decimal>();

		if (_prevJaw is decimal prevJaw && _prevLips is decimal prevLips)
		{
			var upCross = prevLips < prevJaw && lips > jaw && teeth < jaw;
			var downCross = prevLips > prevJaw && lips < jaw && teeth > jaw;

			if (upCross && Position <= 0)
			{
				BuyMarket();
			}
			else if (downCross && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevJaw = jaw;
		_prevLips = lips;

		if (Position > 0 && candle.LowPrice <= jaw)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && candle.HighPrice >= jaw)
		{
			BuyMarket(-Position);
		}
	}

	private static LengthIndicator<decimal> CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
	}

	/// <summary>
	/// Moving average types supported by the strategy.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
	}
}
