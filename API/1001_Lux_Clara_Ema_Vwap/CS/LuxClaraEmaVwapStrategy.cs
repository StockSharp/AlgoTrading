using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Lux Clara EMA + VWAP strategy.
/// </summary>
public class LuxClaraEmaVwapStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private VolumeWeightedMovingAverage _vwap;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }

	/// <summary>
	/// Session start time.
	/// </summary>
	public TimeSpan StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// Session end time.
	/// </summary>
	public TimeSpan EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="LuxClaraEmaVwapStrategy"/>.
	/// </summary>
	public LuxClaraEmaVwapStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 8)
			.SetDisplay("Fast EMA Length", "Length of fast EMA", "Indicators")
			.SetCanOptimize(true);

		_slowEmaLength = Param(nameof(SlowEmaLength), 50)
			.SetDisplay("Slow EMA Length", "Length of slow EMA", "Indicators")
			.SetCanOptimize(true);

		_startTime = Param(nameof(StartTime), new TimeSpan(7, 30, 0))
			.SetDisplay("Start Time", "Start of trading session", "Time Filter");

		_endTime = Param(nameof(EndTime), new TimeSpan(14, 30, 0))
			.SetDisplay("End Time", "End of trading session", "Time Filter");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of data for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastEma?.Reset();
		_slowEma?.Reset();
		_vwap?.Reset();

		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new() { Length = FastEmaLength };
		_slowEma = new() { Length = SlowEmaLength };
		_vwap = new();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, _vwap, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal vwap)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var inSession = InSession(candle.CloseTime);
		var fastCrossAbove = _prevFast <= _prevSlow && fast > slow;
		var fastCrossBelow = _prevFast >= _prevSlow && fast < slow;

		if (Position <= 0 && fastCrossAbove && slow > vwap && inSession)
			BuyMarket();
		else if (Position >= 0 && fastCrossBelow && slow < vwap && inSession)
			SellMarket();
		else if (Position > 0 && fastCrossBelow)
			ClosePosition();
		else if (Position < 0 && fastCrossAbove)
			ClosePosition();

		_prevFast = fast;
		_prevSlow = slow;
	}

	private bool InSession(DateTimeOffset time)
	{
		var t = time.TimeOfDay;
		return StartTime <= EndTime ? t >= StartTime && t <= EndTime : t >= StartTime || t <= EndTime;
	}
}
