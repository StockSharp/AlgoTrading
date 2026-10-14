using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// How To Set Backtest Time Ranges strategy.
/// Goes long when the fast SMA crosses above the slow SMA and closes the long when it crosses below. Signals are taken only
/// between FromDate and ThruDate; entries additionally require the candle time of day to be inside EntryStart..EntryEnd and
/// exits inside ExitStart..ExitEnd. Equal window bounds mean the whole day, and a window may wrap past midnight.
/// </summary>
public class HowToSetBacktestTimeRangesStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DateTimeOffset> _fromDate;
	private readonly StrategyParam<DateTimeOffset> _thruDate;
	private readonly StrategyParam<TimeSpan> _entryStart;
	private readonly StrategyParam<TimeSpan> _entryEnd;
	private readonly StrategyParam<TimeSpan> _exitStart;
	private readonly StrategyParam<TimeSpan> _exitEnd;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// First date of the trading range.
	/// </summary>
	public DateTimeOffset FromDate
	{
		get => _fromDate.Value;
		set => _fromDate.Value = value;
	}

	/// <summary>
	/// Last date of the trading range.
	/// </summary>
	public DateTimeOffset ThruDate
	{
		get => _thruDate.Value;
		set => _thruDate.Value = value;
	}

	/// <summary>
	/// Start of the entry time window.
	/// </summary>
	public TimeSpan EntryStart
	{
		get => _entryStart.Value;
		set => _entryStart.Value = value;
	}

	/// <summary>
	/// End of the entry time window.
	/// </summary>
	public TimeSpan EntryEnd
	{
		get => _entryEnd.Value;
		set => _entryEnd.Value = value;
	}

	/// <summary>
	/// Start of the exit time window.
	/// </summary>
	public TimeSpan ExitStart
	{
		get => _exitStart.Value;
		set => _exitStart.Value = value;
	}

	/// <summary>
	/// End of the exit time window.
	/// </summary>
	public TimeSpan ExitEnd
	{
		get => _exitEnd.Value;
		set => _exitEnd.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HowToSetBacktestTimeRangesStrategy()
	{
		_fastLength = Param(nameof(FastLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA length", "Indicators");

		_slowLength = Param(nameof(SlowLength), 28)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA length", "Indicators");

		_fromDate = Param(nameof(FromDate), new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("From Date", "First date of the trading range", "Time");

		_thruDate = Param(nameof(ThruDate), new DateTimeOffset(2112, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Thru Date", "Last date of the trading range", "Time");

		_entryStart = Param(nameof(EntryStart), TimeSpan.Zero)
			.SetDisplay("Entry Start", "Start of the entry time window", "Time");

		_entryEnd = Param(nameof(EntryEnd), TimeSpan.Zero)
			.SetDisplay("Entry End", "End of the entry time window", "Time");

		_exitStart = Param(nameof(ExitStart), TimeSpan.Zero)
			.SetDisplay("Exit Start", "Start of the exit time window", "Time");

		_exitEnd = Param(nameof(ExitEnd), TimeSpan.Zero)
			.SetDisplay("Exit End", "End of the exit time window", "Time");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		var fastSma = new SimpleMovingAverage { Length = FastLength };
		var slowSma = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private static bool InWindow(TimeSpan time, TimeSpan start, TimeSpan end)
	{
		if (start == end)
			return true;

		return start < end
			? time >= start && time < end
			: time >= start || time < end;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (prevFast is not decimal pf || prevSlow is not decimal ps)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var openTime = candle.OpenTime;
		if (openTime < FromDate.UtcDateTime || openTime > ThruDate.UtcDateTime)
			return;

		var timeOfDay = openTime.TimeOfDay;

		if (pf <= ps && fast > slow && Position <= 0 && InWindow(timeOfDay, EntryStart, EntryEnd))
			BuyMarket(Volume + Math.Abs(Position));
		else if (pf >= ps && fast < slow && Position > 0 && InWindow(timeOfDay, ExitStart, ExitEnd))
			SellMarket(Position);
	}
}
