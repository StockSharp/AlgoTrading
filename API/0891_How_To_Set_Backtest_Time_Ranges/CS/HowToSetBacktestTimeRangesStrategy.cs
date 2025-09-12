using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy demonstrating restricted trading by date and time ranges.
/// Goes long when fast SMA crosses above slow SMA and exits on opposite cross.
/// </summary>
public class HowToSetBacktestTimeRangesStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DateTime> _fromDate;
	private readonly StrategyParam<DateTime> _thruDate;
	private readonly StrategyParam<TimeSpan> _entryStart;
	private readonly StrategyParam<TimeSpan> _entryEnd;
	private readonly StrategyParam<TimeSpan> _exitStart;
	private readonly StrategyParam<TimeSpan> _exitEnd;
	private readonly StrategyParam<DataType> _candleType;

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public DateTime FromDate
	{
		get => _fromDate.Value;
		set => _fromDate.Value = value;
	}

	public DateTime ThruDate
	{
		get => _thruDate.Value;
		set => _thruDate.Value = value;
	}

	public TimeSpan EntryStart
	{
		get => _entryStart.Value;
		set => _entryStart.Value = value;
	}

	public TimeSpan EntryEnd
	{
		get => _entryEnd.Value;
		set => _entryEnd.Value = value;
	}

	public TimeSpan ExitStart
	{
		get => _exitStart.Value;
		set => _exitStart.Value = value;
	}

	public TimeSpan ExitEnd
	{
		get => _exitEnd.Value;
		set => _exitEnd.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public HowToSetBacktestTimeRangesStrategy()
	{
		_fastLength = Param(nameof(FastLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("FastMA Length", "Period of the fast moving average", "MA Settings")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);

		_slowLength = Param(nameof(SlowLength), 28)
		.SetGreaterThanZero()
		.SetDisplay("SlowMA Length", "Period of the slow moving average", "MA Settings")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_fromDate = Param(nameof(FromDate), new DateTime(2021, 1, 1))
		.SetDisplay("From Date", "Start date for trading", "Date Range");

		_thruDate = Param(nameof(ThruDate), new DateTime(2112, 1, 1))
		.SetDisplay("Thru Date", "End date for trading", "Date Range");

		_entryStart = Param(nameof(EntryStart), TimeSpan.Zero)
		.SetDisplay("Entry Start", "Start of allowed entry time", "Time Range");

		_entryEnd = Param(nameof(EntryEnd), TimeSpan.Zero)
		.SetDisplay("Entry End", "End of allowed entry time", "Time Range");

		_exitStart = Param(nameof(ExitStart), TimeSpan.Zero)
		.SetDisplay("Exit Start", "Start of allowed exit time", "Time Range");

		_exitEnd = Param(nameof(ExitEnd), TimeSpan.Zero)
		.SetDisplay("Exit End", "End of allowed exit time", "Time Range");

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

		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);

		var wasFastBelowSlow = false;
		var initialized = false;

		subscription
		.Bind(fastMa, slowMa, (candle, fastValue, slowValue) =>
		{
			if (candle.State != CandleStates.Finished)
			return;

			var candleTime = candle.OpenTime;

			if (!IsInDateRange(candleTime))
			return;

			if (!initialized && fastMa.IsFormed && slowMa.IsFormed)
			{
				wasFastBelowSlow = fastValue < slowValue;
				initialized = true;
				return;
			}

			if (!initialized)
			return;

			var isFastBelowSlow = fastValue < slowValue;
			var crossOver = wasFastBelowSlow && !isFastBelowSlow;
			var crossUnder = !wasFastBelowSlow && isFastBelowSlow;

			if (crossOver && Position <= 0 && IsInTimeRange(candleTime, EntryStart, EntryEnd))
			BuyMarket(Volume + Math.Abs(Position));

			if (crossUnder && Position > 0 && IsInTimeRange(candleTime, ExitStart, ExitEnd))
			SellMarket(Math.Abs(Position));

			wasFastBelowSlow = isFastBelowSlow;
		})
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private bool IsInDateRange(DateTimeOffset time)
	{
		var date = time.Date;
		return date >= FromDate.Date && date <= ThruDate.Date;
	}

	private static bool IsInTimeRange(DateTimeOffset time, TimeSpan start, TimeSpan end)
	{
		if (start == TimeSpan.Zero && end == TimeSpan.Zero)
		return true;

		var t = time.TimeOfDay;
		if (start <= end)
		return t >= start && t <= end;

		return t >= start || t <= end;
	}
}
