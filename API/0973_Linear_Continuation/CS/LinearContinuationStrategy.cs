using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that calculates three moving averages and logs their values.
/// Implements a simple linear continuation projection similar to TradingView script.
/// </summary>
public class LinearContinuationStrategy : Strategy
{
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<int> _ma3Period;
	private readonly StrategyParam<bool> _aggressiveMode;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Moving average type (SMA or EMA).
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// First moving average period.
	/// </summary>
	public int Ma1Period
	{
		get => _ma1Period.Value;
		set => _ma1Period.Value = value;
	}

	/// <summary>
	/// Second moving average period.
	/// </summary>
	public int Ma2Period
	{
		get => _ma2Period.Value;
		set => _ma2Period.Value = value;
	}

	/// <summary>
	/// Third moving average period.
	/// </summary>
	public int Ma3Period
	{
		get => _ma3Period.Value;
		set => _ma3Period.Value = value;
	}

	/// <summary>
	/// Use aggressive mode for continuation calculation.
	/// </summary>
	public bool AggressiveMode
	{
		get => _aggressiveMode.Value;
		set => _aggressiveMode.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public LinearContinuationStrategy()
	{
		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("MA Type", "Moving average type", "General");

		_ma1Period = Param(nameof(Ma1Period), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Period for MA1", "General")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 10);

		_ma2Period = Param(nameof(Ma2Period), 100)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Period for MA2", "General")
			.SetCanOptimize(true)
			.SetOptimize(25, 200, 5);

		_ma3Period = Param(nameof(Ma3Period), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA3 Period", "Period for MA3", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_aggressiveMode = Param(nameof(AggressiveMode), true)
			.SetDisplay("Aggressive Mode", "Use aggressive continuation", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ma1 = CreateMa(MaType, Ma1Period);
		var ma2 = CreateMa(MaType, Ma2Period);
		var ma3 = CreateMa(MaType, Ma3Period);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ma1, ma2, ma3, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma1);
			DrawIndicator(area, ma2);
			DrawIndicator(area, ma3);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma1Value, decimal ma2Value, decimal ma3Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var br1 = GetBr(Ma3Period);
		var br2 = GetBr(Ma2Period);
		var br3 = GetBr(Ma1Period);

		LogInfo($"MA1: {ma1Value}, MA2: {ma2Value}, MA3: {ma3Value}, BR1: {br1}, BR2: {br2}, BR3: {br3}");
	}

	private int GetBr(int period)
	{
		return AggressiveMode ? 1 : (int)Math.Round(period / 4.669m) + 1;
	}

	private static IIndicator CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
	}
}
