using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// QQE Cloud strategy with time-based entries and exits.
/// </summary>
public class ExpQqeCloudStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiSmoothing;
	private readonly StrategyParam<decimal> _qqeFactor;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<int> _stopMinute;
	
	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _rsiMa;
	private ExponentialMovingAverage _atrRsi;
	private ExponentialMovingAverage _maAtrRsi;
	private ExponentialMovingAverage _dar;
	
	private decimal _longband;
	private decimal _shortband;
	private int _trend;
	
	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}
	
	/// <summary>
	/// RSI smoothing period.
	/// </summary>
	public int RsiSmoothing
	{
		get => _rsiSmoothing.Value;
		set => _rsiSmoothing.Value = value;
	}
	
	/// <summary>
	/// QQE volatility factor.
	/// </summary>
	public decimal QqeFactor
	{
		get => _qqeFactor.Value;
		set => _qqeFactor.Value = value;
	}
	
	/// <summary>
	/// Trading session start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}
	
	/// <summary>
	/// Trading session start minute.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}
	
	/// <summary>
	/// Trading session stop hour.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}
	
	/// <summary>
	/// Trading session stop minute.
	/// </summary>
	public int StopMinute
	{
		get => _stopMinute.Value;
		set => _stopMinute.Value = value;
	}
	
	
	public ExpQqeCloudStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");
		
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Length", "RSI period", "QQE");
		
		_rsiSmoothing = Param(nameof(RsiSmoothing), 5)
		.SetDisplay("RSI Smoothing", "RSI smoothing period", "QQE");
		
		_qqeFactor = Param(nameof(QqeFactor), 4.236m)
		.SetDisplay("QQE Factor", "QQE factor", "QQE");
		
		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start hour", "Hour to allow entries", "Session");
		
		_startMinute = Param(nameof(StartMinute), 0)
		.SetDisplay("Start minute", "Minute to allow entries", "Session");
		
		_stopHour = Param(nameof(StopHour), 23)
		.SetDisplay("Stop hour", "Hour to exit trades", "Session");
		
		_stopMinute = Param(nameof(StopMinute), 59)
		.SetDisplay("Stop minute", "Minute to exit trades", "Session");
	}
	
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiMa = new ExponentialMovingAverage { Length = RsiSmoothing };
		_atrRsi = new ExponentialMovingAverage { Length = RsiSmoothing };
		_maAtrRsi = new ExponentialMovingAverage { Length = RsiSmoothing };
		_dar = new ExponentialMovingAverage { Length = RsiSmoothing };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var rsiValue = _rsi.Process(candle);
		if (!_rsi.IsFormed)
		return;
		
		var rsiMaValue = _rsiMa.Process(rsiValue);
		if (!_rsiMa.IsFormed)
		return;
		
		var rsIndex = rsiMaValue.GetValue<decimal>();
		var prevRsiMa = _rsiMa.GetValue(1);
		var atrRsiValue = Math.Abs(prevRsiMa - rsIndex);
		
		var maAtrRsiValue = _maAtrRsi.Process(atrRsiValue, candle.ServerTime, candle.State == CandleStates.Finished);
		if (!_maAtrRsi.IsFormed)
		return;
		
		var darValue = _dar.Process(maAtrRsiValue);
		if (!_dar.IsFormed)
		return;
		
		var deltaFastAtrRsi = darValue.GetValue<decimal>() * QqeFactor;
		
		var newShortband = rsIndex + deltaFastAtrRsi;
		var newLongband = rsIndex - deltaFastAtrRsi;
		
		var prevLongband = _longband;
		var prevShortband = _shortband;
		var prevRsIndex = _rsiMa.GetValue(1);
		
		if (prevRsIndex > prevLongband && rsIndex > prevLongband)
		_longband = Math.Max(prevLongband, newLongband);
		else
		_longband = newLongband;
		
		if (prevRsIndex < prevShortband && rsIndex < prevShortband)
		_shortband = Math.Min(prevShortband, newShortband);
		else
		_shortband = newShortband;
		
		var prevTrend = _trend;
		
		if (rsIndex > _shortband && prevRsIndex <= prevShortband)
		_trend = 1;
		else if (rsIndex < _longband && prevRsIndex >= prevLongband)
		_trend = -1;
		
		var time = candle.OpenTime.LocalDateTime;
		
		var afterStop = time.Hour > StopHour || time.Hour < StartHour || (time.Hour == StopHour && time.Minute >= StopMinute);
		if (afterStop && Position != 0)
		ClosePosition();
		
		if (_trend == 1 && Position < 0)
		ClosePosition();
		else if (_trend == -1 && Position > 0)
		ClosePosition();
		
		if (time.Hour == StartHour && time.Minute == StartMinute)
		{
			if (_trend == 1 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
			else if (_trend == -1 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
