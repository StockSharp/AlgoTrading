using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TMA Strategy using smoothed moving averages and candlestick patterns.
/// </summary>
public class TmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _midLength;
	private readonly StrategyParam<int> _mid2Length;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _useSession;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;

	private SimpleMovingAverage _smaFast;
	private SimpleMovingAverage _smaMid;
	private SimpleMovingAverage _smaMid2;
	private SimpleMovingAverage _smaSlow;
	private ExponentialMovingAverage _ema2;

	private ICandleMessage _candle1;
	private ICandleMessage _candle2;
	private ICandleMessage _candle3;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int MidLength { get => _midLength.Value; set => _midLength.Value = value; }
	public int Mid2Length { get => _mid2Length.Value; set => _mid2Length.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public bool UseSession { get => _useSession.Value; set => _useSession.Value = value; }
	public TimeSpan SessionStart { get => _sessionStart.Value; set => _sessionStart.Value = value; }
	public TimeSpan SessionEnd { get => _sessionEnd.Value; set => _sessionEnd.Value = value; }

	public TmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle type", "General");

		_fastLength = Param(nameof(FastLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Length for first SMA", "Parameters")
			.SetCanOptimize(true);

		_midLength = Param(nameof(MidLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Mid SMA", "Length for second SMA", "Parameters")
			.SetCanOptimize(true);

		_mid2Length = Param(nameof(Mid2Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("Third SMA", "Length for third SMA", "Parameters")
			.SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Length for trend filter", "Parameters")
			.SetCanOptimize(true);

		_useSession = Param(nameof(UseSession), false)
			.SetDisplay("Use Session", "Enable session filter", "Session");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(8, 30, 0))
			.SetDisplay("Session Start", "Session start (UTC)", "Session");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(12, 0, 0))
			.SetDisplay("Session End", "Session end (UTC)", "Session");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_smaFast = default;
		_smaMid = default;
		_smaMid2 = default;
		_smaSlow = default;
		_ema2 = default;
		_candle1 = default;
		_candle2 = default;
		_candle3 = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_smaFast = new SimpleMovingAverage { Length = FastLength };
		_smaMid = new SimpleMovingAverage { Length = MidLength };
		_smaMid2 = new SimpleMovingAverage { Length = Mid2Length };
		_smaSlow = new SimpleMovingAverage { Length = SlowLength };
		_ema2 = new ExponentialMovingAverage { Length = 2 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_smaFast, _smaMid, _smaMid2, _smaSlow, _ema2, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smaFast);
			DrawIndicator(area, _smaMid);
			DrawIndicator(area, _smaMid2);
			DrawIndicator(area, _smaSlow);
			DrawIndicator(area, _ema2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaFast, decimal smaMid, decimal smaMid2, decimal smaSlow, decimal ema2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var inSession = !UseSession || IsInSession(candle.OpenTime);

		var bullSig = _candle3 != null && _candle2 != null && _candle1 != null
			&& _candle3.ClosePrice < _candle3.OpenPrice
			&& _candle2.ClosePrice < _candle2.OpenPrice
			&& _candle1.ClosePrice < _candle1.OpenPrice
			&& candle.ClosePrice > _candle1.OpenPrice;

		var bearSig = _candle3 != null && _candle2 != null && _candle1 != null
			&& _candle3.ClosePrice > _candle3.OpenPrice
			&& _candle2.ClosePrice > _candle2.OpenPrice
			&& _candle1.ClosePrice > _candle1.OpenPrice
			&& candle.ClosePrice < _candle1.OpenPrice;

		var bullishEngulfing = _candle1 != null
			&& candle.OpenPrice <= _candle1.ClosePrice
			&& candle.OpenPrice < _candle1.OpenPrice
			&& candle.ClosePrice > _candle1.OpenPrice;

		var bearishEngulfing = _candle1 != null
			&& candle.OpenPrice >= _candle1.ClosePrice
			&& candle.OpenPrice > _candle1.OpenPrice
			&& candle.ClosePrice < _candle1.OpenPrice;

		var longCondition = inSession && (bullishEngulfing || bullSig) && ema2 > smaSlow;
		var shortCondition = inSession && (bearishEngulfing || bearSig) && ema2 < smaSlow;

		if (longCondition && Position <= 0)
			BuyMarket();

		if (shortCondition && Position >= 0)
			SellMarket();

		var exitLong = ema2 < smaSlow;
		var exitShort = ema2 > smaSlow;

		if (exitLong && Position > 0)
			SellMarket(Position);

		if (exitShort && Position < 0)
			BuyMarket(-Position);

		_candle3 = _candle2;
		_candle2 = _candle1;
		_candle1 = candle;
	}

	private bool IsInSession(DateTimeOffset time)
	{
		var date = time.Date;
		var start = date + SessionStart;
		var end = date + SessionEnd;
		if (SessionEnd <= SessionStart)
			end = end.AddDays(1);
		return time >= start && time <= end;
	}
}

