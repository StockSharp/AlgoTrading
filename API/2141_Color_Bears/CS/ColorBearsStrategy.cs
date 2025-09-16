namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on a double-smoothed Bears Power indicator.
/// Opens a long position when the indicator turns down after rising,
/// and opens a short position when it turns up after falling.
/// </summary>
public class ColorBearsStrategy : Strategy
{
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ma1 = null!;
	private ExponentialMovingAverage _ma2 = null!;
	private decimal? _prevValue;
	private int? _prevColor;

	/// <summary>
	/// Length of the first moving average.
	/// </summary>
	public int Ma1Period
	{
		get => _ma1Period.Value;
		set => _ma1Period.Value = value;
	}

	/// <summary>
	/// Length of the second moving average.
	/// </summary>
	public int Ma2Period
	{
		get => _ma2Period.Value;
		set => _ma2Period.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorBearsStrategy"/>.
	/// </summary>
	public ColorBearsStrategy()
	{
		_ma1Period = Param(nameof(Ma1Period), 12)
			.SetGreaterThanZero()
			.SetDisplay("MA1", "First MA length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_ma2Period = Param(nameof(Ma2Period), 5)
			.SetGreaterThanZero()
			.SetDisplay("MA2", "Second MA length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame())
			.SetDisplay("Candle", "Candle type", "Parameters");
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

		_ma1 = new ExponentialMovingAverage { Length = Ma1Period };
		_ma2 = new ExponentialMovingAverage { Length = Ma2Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ma1Value = _ma1.Process(candle.ClosePrice);
		if (!ma1Value.IsFinal)
			return;

		var bears = candle.LowPrice - ma1Value.ToDecimal();
		var ma2Value = _ma2.Process(bears);
		if (!ma2Value.IsFinal)
			return;

		var current = ma2Value.ToDecimal();
		var color = 1;
		if (_prevValue != null)
		{
			if (_prevValue < current)
				color = 0;
			else if (_prevValue > current)
				color = 2;

			if (_prevColor == 0 && color == 2)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				if (Position <= 0)
					BuyMarket();
			}
			else if (_prevColor == 2 && color == 0)
			{
				if (Position > 0)
					SellMarket(Position);
				if (Position >= 0)
					SellMarket();
			}
		}

		_prevColor = color;
		_prevValue = current;
	}
}
