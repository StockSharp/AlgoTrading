using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD strategy with time session filter.
/// </summary>
public class TimeSessionFilterMacdExampleStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<bool> _closeAtSessionEnd;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _trendMaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _trendMa;
	private bool _prevIsMacdAboveSignal;

	/// <summary>
	/// Session start time.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Session end time.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Close positions when session ends.
	/// </summary>
	public bool CloseAtSessionEnd
	{
		get => _closeAtSessionEnd.Value;
		set => _closeAtSessionEnd.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Signal period for MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Trend EMA length.
	/// </summary>
	public int TrendMaLength
	{
		get => _trendMaLength.Value;
		set => _trendMaLength.Value = value;
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
	/// Initialize parameters.
	/// </summary>
	public TimeSessionFilterMacdExampleStrategy()
	{
		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromHours(11))
			.SetDisplay("Session Start", "Start time of trading session", "Session");
		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromHours(15))
			.SetDisplay("Session End", "End time of trading session", "Session");
		_closeAtSessionEnd = Param(nameof(CloseAtSessionEnd), false)
			.SetDisplay("Close At Session End", "Close positions when session ends", "Session");
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 11)
			.SetDisplay("Fast EMA Period", "Fast length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 15, 2);
		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
			.SetDisplay("Slow EMA Period", "Slow length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 32, 2);
		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("Signal Period", "MACD signal length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 13, 2);
		_trendMaLength = Param(nameof(TrendMaLength), 55)
			.SetDisplay("Trend EMA Length", "Length of trend EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(30, 80, 5);
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
		_prevIsMacdAboveSignal = false;
		_trendMa = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = FastEmaPeriod }, LongMa = { Length = SlowEmaPeriod } },
			SignalMa = { Length = SignalPeriod }
		};
		var trendMa = new ExponentialMovingAverage { Length = TrendMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(macd, ProcessCandle);
		subscription.Bind(trendMa, UpdateTrend);
		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, trendMa);
			DrawOwnTrades(area);
		}
	}

	private void UpdateTrend(ICandleMessage candle, decimal value)
	{
		_trendMa = value;
	}

	private bool IsSessionActive(DateTimeOffset time)
	{
		var tod = time.LocalDateTime.TimeOfDay;
		return tod >= SessionStart && tod <= SessionEnd;
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var inSession = IsSessionActive(candle.OpenTime);

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = typed.Macd;
		var signal = typed.Signal;
		var isAbove = macd > signal;
		var crossedUp = isAbove && !_prevIsMacdAboveSignal;
		var crossedDown = !isAbove && _prevIsMacdAboveSignal;

		if (!inSession)
		{
			if (CloseAtSessionEnd && Position != 0)
				ClosePosition();
			_prevIsMacdAboveSignal = isAbove;
			return;
		}

		if (crossedUp && candle.ClosePrice > _trendMa && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossedDown && candle.ClosePrice < _trendMa && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevIsMacdAboveSignal = isAbove;
	}
}