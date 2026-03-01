using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exponential moving average crossover with MACD filter and session start entry.
/// </summary>
public class EmaCrossMacdSessionStartStrategy : Strategy
{
private readonly StrategyParam<int> _fastEmaLength;
private readonly StrategyParam<int> _slowEmaLength;
private readonly StrategyParam<int> _macdFastLength;
private readonly StrategyParam<int> _macdSlowLength;
private readonly StrategyParam<int> _macdSignalLength;
private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<string> _session;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _prevInSession;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFastLength { get => _macdFastLength.Value; set => _macdFastLength.Value = value; }

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlowLength { get => _macdSlowLength.Value; set => _macdSlowLength.Value = value; }

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignalLength { get => _macdSignalLength.Value; set => _macdSignalLength.Value = value; }

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <summary>
	/// Trading start date.
	/// </summary>
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <summary>
	/// Trading end date.
	/// </summary>
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

	/// <summary>
	/// Session hours in HHmm-HHmm format.
	/// </summary>
	public string Session { get => _session.Value; set => _session.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public EmaCrossMacdSessionStartStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
			
			.SetOptimize(5, 20, 1);

		_slowEmaLength = Param(nameof(SlowEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
			
			.SetOptimize(10, 40, 1);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast length", "Indicators")
			
			.SetOptimize(5, 20, 1);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow length", "Indicators")
			
			.SetOptimize(20, 40, 1);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal length", "Indicators")
			
			.SetOptimize(5, 15, 1);

_direction = Param(nameof(Direction), (Sides?)null)
.SetDisplay("Trade Direction", "Allowed trade direction", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(1970, 1, 1)))
			.SetDisplay("Start Date", "Trading start date", "General");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2170, 12, 31, 23, 59, 0)))
			.SetDisplay("End Date", "Trading end date", "General");

		_session = Param(nameof(Session), "0930-1600")
			.SetDisplay("Session", "Trading session", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevFast = 0m;
		_prevSlow = 0m;
		_prevInSession = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = MacdFastLength }, LongMa = { Length = MacdSlowLength } },
			SignalMa = { Length = MacdSignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { macd, fastEma, slowEma }, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)values[0];
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;
		var histogram = macdLine - signalLine;
		var fastEma = values[1].ToDecimal();
		var slowEma = values[2].ToDecimal();

		var time = candle.OpenTime;
		var inDateRange = time >= StartDate.DateTime && time < EndDate.DateTime;

		ParseSession(Session, out var start, out var end);
		var t = time.TimeOfDay;
		var inSession = t >= start && t <= end;
		var justSessionStart = inSession && !_prevInSession;

		var longSignal = _prevFast <= _prevSlow && fastEma > slowEma;
		var shortSignal = _prevFast >= _prevSlow && fastEma < slowEma;
		var macdOkLong = histogram > 0m;
		var macdOkShort = histogram < 0m;

var longAllowed = Direction is null or Sides.Buy;
var shortAllowed = Direction is null or Sides.Sell;

		if (inDateRange && inSession && longAllowed && macdOkLong && (longSignal || (justSessionStart && fastEma > slowEma)) && Position <= 0)
		{
			BuyMarket();
		}
		else if (inDateRange && inSession && shortAllowed && macdOkShort && (shortSignal || (justSessionStart && fastEma < slowEma)) && Position >= 0)
		{
			SellMarket();
		}

		if (Position > 0 && (shortSignal || !inSession))
			SellMarket();
		else if (Position < 0 && (longSignal || !inSession))
			BuyMarket();

		_prevFast = fastEma;
		_prevSlow = slowEma;
		_prevInSession = inSession;
	}

	private static void ParseSession(string input, out TimeSpan start, out TimeSpan end)
	{
		start = TimeSpan.Zero;
		end = TimeSpan.FromHours(24);
		if (input.IsEmptyOrWhiteSpace())
			return;
		var parts = input.Split('-');
		if (parts.Length != 2)
			return;
		TimeSpan.TryParseExact(parts[0], "hhmm", null, out start);
		TimeSpan.TryParseExact(parts[1], "hhmm", null, out end);
	}
}
