namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Arpeet MACD strategy - trades MACD crossovers with zero-line filter.
/// </summary>
public class ArpeetMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;

	private decimal _prevDiff;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast MA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow MA period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal line period.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ArpeetMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(3).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastLength = Param(nameof(FastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA", "Fast MA period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(6, 18, 2);

		_slowLength = Param(nameof(SlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA", "Slow MA period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 2);

		_signalLength = Param(nameof(SignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Signal Length", "Signal line period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 2);
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)value;

		if (macdValue.Macd is not decimal macd || macdValue.Signal is not decimal signal)
		return;

		var diff = macd - signal;
		var crossedUp = diff > 0 && _prevDiff <= 0;
		var crossedDown = diff < 0 && _prevDiff >= 0;

		if (crossedUp && macd < 0 && Position <= 0)
		{
		RegisterBuy();
		}
		else if (crossedDown && macd > 0 && Position >= 0)
		{
		RegisterSell();
		}

		_prevDiff = diff;
	}
}
