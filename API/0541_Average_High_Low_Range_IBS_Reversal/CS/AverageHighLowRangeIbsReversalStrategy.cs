namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Average high-low range with IBS reversal strategy.
/// </summary>
public class AverageHighLowRangeIbsReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _barsBelowThreshold;
	private readonly StrategyParam<decimal> _ibsBuyThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private SMA _hlAverage;
	private Highest _highest;
	private Lowest _lowest;

	private int _barsSinceAbove;
	private decimal _previousHigh;

	/// <summary>
	/// Lookback length.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Bars required below threshold.
	/// </summary>
	public int BarsBelowThreshold { get => _barsBelowThreshold.Value; set => _barsBelowThreshold.Value = value; }

	/// <summary>
	/// IBS buy threshold.
	/// </summary>
	public decimal IbsBuyThreshold { get => _ibsBuyThreshold.Value; set => _ibsBuyThreshold.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Start time for signals.
	/// </summary>
	public DateTimeOffset StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// End time for signals.
	/// </summary>
	public DateTimeOffset EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AverageHighLowRangeIbsReversalStrategy"/>.
	/// </summary>
	public AverageHighLowRangeIbsReversalStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetRange(1, 100)
			.SetDisplay("Length", "Lookback length for calculations", "Parameters")
			.SetCanOptimize(true);

		_barsBelowThreshold = Param(nameof(BarsBelowThreshold), 2)
			.SetRange(1, 10)
			.SetDisplay("Bars Below Threshold", "Number of consecutive bars below buy threshold", "Parameters")
			.SetCanOptimize(true);

		_ibsBuyThreshold = Param(nameof(IbsBuyThreshold), 0.2m)
			.SetRange(0m, 1m)
			.SetDisplay("IBS Buy Threshold", "IBS value required for entry", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Start time for strategy", "Time");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End time for strategy", "Time");
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
		_hlAverage = null;
		_highest = null;
		_lowest = null;
		_barsSinceAbove = -1;
		_previousHigh = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_hlAverage = new SMA { Length = Length };
		_highest = new Highest { Length = Length };
		_lowest = new Lowest { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _hlAverage);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.OpenTime;

		var hlValue = _hlAverage.Process(candle.HighPrice - candle.LowPrice, time, true);
		var highValue = _highest.Process(candle.HighPrice, time, true);
		var lowValue = _lowest.Process(candle.LowPrice, time, true);

		if (!_hlAverage.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
		{
			_previousHigh = candle.HighPrice;
			return;
		}

		var hlAvg = hlValue.ToDecimal();
		var upper = highValue.ToDecimal();
		var lower = lowValue.ToDecimal();

		var buyThreshold = upper - (2.5m * hlAvg);

		if (candle.ClosePrice > buyThreshold)
		{
			_barsSinceAbove = 0;
		}
		else if (_barsSinceAbove >= 0)
		{
			_barsSinceAbove++;
		}

		var numberOfBarsBelow = _barsSinceAbove >= BarsBelowThreshold && _barsSinceAbove >= 0;

		var range = candle.HighPrice - candle.LowPrice;
		if (range == 0)
		{
			_previousHigh = candle.HighPrice;
			return;
		}

		var ibs = (candle.ClosePrice - candle.LowPrice) / range;

		var inWindow = time >= StartTime && time <= EndTime;

		var longCondition = numberOfBarsBelow && inWindow && ibs < IbsBuyThreshold;

		if (longCondition && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Long entry at {candle.ClosePrice}, IBS={ibs:F2}, threshold={buyThreshold:F2}");
		}

		if (Position > 0 && candle.ClosePrice > _previousHigh)
		{
			SellMarket(Math.Abs(Position));
			LogInfo($"Exit long at {candle.ClosePrice}, prev high={_previousHigh}");
		}

		_previousHigh = candle.HighPrice;
	}
}
