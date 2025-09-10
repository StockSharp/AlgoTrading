using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bitcoin Momentum Strategy - captures upside momentum while avoiding caution conditions.
/// </summary>
public class BitcoinMomentumStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _trailStopLookback;
	private readonly StrategyParam<decimal> _trailStopMultiplier;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;
	
	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;
	private ExponentialMovingAverage _higherEma;
	private Highest _swingHigh;
	private Highest _trailSource;
	private decimal? _trailStop;
	private bool _prevCaution;
	private decimal _higherEmaValue;
	
	public BitcoinMomentumStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromDays(7).TimeFrame())
		.SetDisplay("Higher Candle Type", "Higher timeframe for EMA", "General");
		
		_emaLength = Param(nameof(EmaLength), 20)
		.SetDisplay("EMA Length", "EMA period", "Indicators");
		
		_atrLength = Param(nameof(AtrLength), 5)
		.SetDisplay("ATR Length", "ATR period", "Indicators");
		
		_trailStopLookback = Param(nameof(TrailStopLookback), 7)
		.SetDisplay("Trail Stop Lookback", "Lookback for trailing stop", "Exit");
		
		_trailStopMultiplier = Param(nameof(TrailStopMultiplier), 0.2m)
		.SetDisplay("Trail Stop Multiplier", "ATR multiplier in caution", "Exit");
		
		_startTime = Param(nameof(StartTime), new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero))
		.SetDisplay("Start Time", "Begin trading after this time", "Time Filter");
		
		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
		.SetDisplay("End Time", "Stop trading after this time", "Time Filter");
	}
	
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DataType HigherCandleType { get => _higherCandleType.Value; set => _higherCandleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public int TrailStopLookback { get => _trailStopLookback.Value; set => _trailStopLookback.Value = value; }
	public decimal TrailStopMultiplier { get => _trailStopMultiplier.Value; set => _trailStopMultiplier.Value = value; }
	public DateTimeOffset StartTime { get => _startTime.Value; set => _startTime.Value = value; }
	public DateTimeOffset EndTime { get => _endTime.Value; set => _endTime.Value = value; }
	
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType), (Security, HigherCandleType)];
	
	protected override void OnReseted()
	{
		base.OnReseted();
		_trailStop = null;
		_prevCaution = false;
		_higherEmaValue = 0m;
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_higherEma = new ExponentialMovingAverage { Length = EmaLength };
		_swingHigh = new Highest { Length = 7 };
		_trailSource = new Highest { Length = TrailStopLookback };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, _ema, ProcessCandle)
		.Start();
		
		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription
		.Bind(_higherEma, ProcessHigher)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _higherEma);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessHigher(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_higherEmaValue = emaValue;
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var highVal = _swingHigh.Process(candle.HighPrice, candle.OpenTime, true);
		var trailVal = _trailSource.Process(candle.LowPrice, candle.OpenTime, true);
		
		if (!_swingHigh.IsFormed || !_trailSource.IsFormed || !_higherEma.IsFormed)
		return;
		
		var swingHigh = highVal.ToDecimal();
		var highestLow = trailVal.ToDecimal();
		var htfEma = _higherEmaValue;
		
		var isBullish = candle.ClosePrice > htfEma;
		var isCaution = isBullish && ((swingHigh - candle.LowPrice) > (atrValue * 1.5m) || candle.ClosePrice < emaValue);
		
		if (isBullish && Position <= 0 && candle.OpenTime >= StartTime && candle.OpenTime <= EndTime && !isCaution)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_trailStop = null;
		}
		
		var distance = _prevCaution ? atrValue * TrailStopMultiplier : atrValue;
		var tempTrail = highestLow - distance;
		
		if (Position > 0 && (_trailStop == null || tempTrail > _trailStop))
		_trailStop = tempTrail;
		
		if (Position > 0 && (candle.ClosePrice < _trailStop || candle.ClosePrice < htfEma))
		{
			SellMarket(Position);
		}
		
		_prevCaution = isCaution;
	}
}
