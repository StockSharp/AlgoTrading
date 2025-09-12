using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend strategy using EMA, MACD and RSI confirmation with ATR trailing stop.
/// </summary>
public class GoldProStrategy : Strategy
{
	private readonly StrategyParam<int> _emaFastLen;
	private readonly StrategyParam<int> _emaSlowLen;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _trailAtrMult;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _longStop;
	private decimal _shortStop;
	
	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int EmaFastLen { get => _emaFastLen.Value; set => _emaFastLen.Value = value; }
	
	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int EmaSlowLen { get => _emaSlowLen.Value; set => _emaSlowLen.Value = value; }
	
	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	
	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	
	/// <summary>
	/// ATR trailing multiplier.
	/// </summary>
	public decimal TrailAtrMultiplier { get => _trailAtrMult.Value; set => _trailAtrMult.Value = value; }
	
	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="GoldProStrategy"/> class.
	/// </summary>
	public GoldProStrategy()
	{
		_emaFastLen = Param(nameof(EmaFastLen), 50)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA Length", "Fast EMA length", "Parameters");
		
		_emaSlowLen = Param(nameof(EmaSlowLen), 200)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA Length", "Slow EMA length", "Parameters");
		
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI period", "Parameters");
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR period", "Risk");
		
		_trailAtrMult = Param(nameof(TrailAtrMultiplier), 3m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Trail Multiplier", "ATR trail multiplier", "Risk");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevMacd = 0m;
		_prevSignal = 0m;
		_longStop = 0m;
		_shortStop = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var emaFast = new ExponentialMovingAverage { Length = EmaFastLen };
		var emaSlow = new ExponentialMovingAverage { Length = EmaSlowLen };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(emaFast, emaSlow, macd, rsi, atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
		
		StartProtection();
	}
	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue emaFastValue,
	IIndicatorValue emaSlowValue,
	IIndicatorValue macdValue,
	IIndicatorValue rsiValue,
	IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var fast = emaFastValue.ToDecimal();
		var slow = emaSlowValue.ToDecimal();
		
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdLine = macdTyped.Macd;
		var signalLine = macdTyped.Signal;
		
		var rsi = rsiValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		
		var bullishTrend = fast > slow && candle.ClosePrice > slow;
		var bearishTrend = fast < slow && candle.ClosePrice < slow;
		
		var macdBullish = _prevMacd <= _prevSignal && macdLine > signalLine;
		var macdBearish = _prevMacd >= _prevSignal && macdLine < signalLine;
		
		var rsiBullish = rsi > 50m && rsi < 70m;
		var rsiBearish = rsi < 50m && rsi > 30m;
		
		var trailOffset = atr * TrailAtrMultiplier;
		
		if (bullishTrend && macdBullish && rsiBullish && Position <= 0)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
			_longStop = candle.ClosePrice - trailOffset;
		}
		else if (bearishTrend && macdBearish && rsiBearish && Position >= 0)
		{
			CancelActiveOrders();
			SellMarket(Volume + Math.Abs(Position));
			_shortStop = candle.ClosePrice + trailOffset;
		}
		
		if (Position > 0)
		{
			var stop = candle.ClosePrice - trailOffset;
			if (stop > _longStop)
			_longStop = stop;
			
			if (candle.ClosePrice <= _longStop)
			{
				SellMarket(Math.Abs(Position));
				_longStop = 0m;
			}
		}
		else if (Position < 0)
		{
			var stop = candle.ClosePrice + trailOffset;
			if (_shortStop == 0m || stop < _shortStop)
			_shortStop = stop;
			
			if (candle.ClosePrice >= _shortStop)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = 0m;
			}
		}
		
		_prevMacd = macdLine;
		_prevSignal = signalLine;
	}
}
