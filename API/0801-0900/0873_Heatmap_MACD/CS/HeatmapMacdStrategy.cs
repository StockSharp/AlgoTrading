using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heatmap MACD strategy.
/// A MACD histogram (MACD minus signal) is calculated on each of five timeframes. When all five turn positive after not all
/// being positive the strategy goes long; when all five turn negative after not all being negative it goes short, reversing an
/// opposite position. With CloseOnOpposite a position is also closed as soon as any histogram flips against it.
/// </summary>
public class HeatmapMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _timeFrame1;
	private readonly StrategyParam<DataType> _timeFrame2;
	private readonly StrategyParam<DataType> _timeFrame3;
	private readonly StrategyParam<DataType> _timeFrame4;
	private readonly StrategyParam<DataType> _timeFrame5;
	private readonly StrategyParam<bool> _closeOnOpposite;

	private readonly decimal?[] _histograms = new decimal?[5];
	private bool _wasAllPositive;
	private bool _wasAllNegative;

	/// <summary>
	/// Fast EMA length of MACD.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length of MACD.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal line length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// First timeframe.
	/// </summary>
	public DataType TimeFrame1
	{
		get => _timeFrame1.Value;
		set => _timeFrame1.Value = value;
	}

	/// <summary>
	/// Second timeframe.
	/// </summary>
	public DataType TimeFrame2
	{
		get => _timeFrame2.Value;
		set => _timeFrame2.Value = value;
	}

	/// <summary>
	/// Third timeframe.
	/// </summary>
	public DataType TimeFrame3
	{
		get => _timeFrame3.Value;
		set => _timeFrame3.Value = value;
	}

	/// <summary>
	/// Fourth timeframe.
	/// </summary>
	public DataType TimeFrame4
	{
		get => _timeFrame4.Value;
		set => _timeFrame4.Value = value;
	}

	/// <summary>
	/// Fifth timeframe.
	/// </summary>
	public DataType TimeFrame5
	{
		get => _timeFrame5.Value;
		set => _timeFrame5.Value = value;
	}

	/// <summary>
	/// Close the position when any histogram flips against it.
	/// </summary>
	public bool CloseOnOpposite
	{
		get => _closeOnOpposite.Value;
		set => _closeOnOpposite.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HeatmapMacdStrategy()
	{
		_fastLength = Param(nameof(FastLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA length of MACD", "MACD");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA length of MACD", "MACD");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal line length", "MACD");

		_timeFrame1 = Param(nameof(TimeFrame1), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Timeframe 1", "First timeframe", "Timeframes");

		_timeFrame2 = Param(nameof(TimeFrame2), TimeSpan.FromMinutes(120).TimeFrame())
			.SetDisplay("Timeframe 2", "Second timeframe", "Timeframes");

		_timeFrame3 = Param(nameof(TimeFrame3), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("Timeframe 3", "Third timeframe", "Timeframes");

		_timeFrame4 = Param(nameof(TimeFrame4), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("Timeframe 4", "Fourth timeframe", "Timeframes");

		_timeFrame5 = Param(nameof(TimeFrame5), TimeSpan.FromMinutes(480).TimeFrame())
			.SetDisplay("Timeframe 5", "Fifth timeframe", "Timeframes");

		_closeOnOpposite = Param(nameof(CloseOnOpposite), false)
			.SetDisplay("Close On Opposite", "Close the position when any histogram flips against it", "Trading");
	}

	private DataType[] TimeFrames => [TimeFrame1, TimeFrame2, TimeFrame3, TimeFrame4, TimeFrame5];

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return TimeFrames.Distinct().Select(dt => (Security, dt));
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		Array.Clear(_histograms);
		_wasAllPositive = false;
		_wasAllNegative = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var timeFrames = TimeFrames;

		for (var i = 0; i < timeFrames.Length; i++)
		{
			var index = i;
			var macd = new MovingAverageConvergenceDivergenceSignal
			{
				Macd =
				{
					ShortMa = { Length = FastLength },
					LongMa = { Length = SlowLength },
				},
				SignalMa = { Length = SignalLength },
			};

			var subscription = SubscribeCandles(timeFrames[i]);
			subscription
				.BindEx(macd, (candle, value) => ProcessCandle(index, candle, value))
				.Start();

			if (i == 0)
			{
				var area = CreateChartArea();
				if (area != null)
				{
					DrawCandles(area, subscription);
					DrawOwnTrades(area);
				}
			}
		}
	}

	private void ProcessCandle(int index, ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFormed || value is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macd, Signal: decimal signal })
			return;

		_histograms[index] = macd - signal;

		if (_histograms.Any(h => h is null))
			return;

		var allPositive = _histograms.All(h => h > 0);
		var allNegative = _histograms.All(h => h < 0);
		var anyPositive = _histograms.Any(h => h > 0);
		var anyNegative = _histograms.Any(h => h < 0);

		var wasAllPositive = _wasAllPositive;
		var wasAllNegative = _wasAllNegative;
		_wasAllPositive = allPositive;
		_wasAllNegative = allNegative;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (allPositive && !wasAllPositive && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (allNegative && !wasAllNegative && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (CloseOnOpposite)
		{
			if (Position > 0 && anyNegative)
				SellMarket(Position);
			else if (Position < 0 && anyPositive)
				BuyMarket(-Position);
		}
	}
}
