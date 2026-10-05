using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Backtesting module strategy.
/// Inside the StartTime..EndTime interval a fast SMA crossing above the slow SMA goes long and a cross below goes short,
/// reversing an opposite position. Outside the interval no entries are taken and an open position is closed.
/// </summary>
public class BacktestingModuleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DateTime> _startTime;
	private readonly StrategyParam<DateTime> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Start of the trading interval.
	/// </summary>
	public DateTime StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End of the trading interval.
	/// </summary>
	public DateTime EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
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
	public BacktestingModuleStrategy()
	{
		_fastLength = Param(nameof(FastLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA period", "Indicators");

		_startTime = Param(nameof(StartTime), new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc))
			.SetDisplay("Start Time", "Start of the trading interval", "Time");

		_endTime = Param(nameof(EndTime), new DateTime(2050, 12, 31, 0, 0, 0, DateTimeKind.Utc))
			.SetDisplay("End Time", "End of the trading interval", "Time");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		var fast = new SimpleMovingAverage { Length = FastLength };
		var slow = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
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

		var inWindow = candle.OpenTime >= StartTime && candle.OpenTime <= EndTime;

		if (!inWindow)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			return;
		}

		if (pf <= ps && fast > slow && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (pf >= ps && fast < slow && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
