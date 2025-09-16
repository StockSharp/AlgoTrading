using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy implementing Elder Impulse System logic.
/// </summary>
public class ElderImpulseSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _previousEma;
	private decimal? _previousMacdHist;
	private int? _previousColor;
	private int? _previousPreviousColor;
	
	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}
	
	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}
	
	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}
	
	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}
	
	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="ElderImpulseSystemStrategy"/>.
	/// </summary>
	public ElderImpulseSystemStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 13)
		.SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(8, 21, 1);
		
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(8, 20, 1);
		
		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 1);
		
		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetDisplay("MACD Signal Period", "Signal EMA period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 1);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		
		_previousEma = null;
		_previousMacdHist = null;
		_previousColor = null;
		_previousPreviousColor = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
			LongMa = { Length = MacdSlowPeriod }
			},
		SignalMa = { Length = MacdSignalPeriod }
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(ema, macd, ProcessCandle)
		.Start();
		
		StartProtection(new Unit(2, UnitTypes.Percent), new Unit(2, UnitTypes.Percent));
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue macdValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var ema = emaValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
		return;
		
		var macdHist = macd - signal;
		
		if (_previousEma is null || _previousMacdHist is null)
		{
			_previousEma = ema;
			_previousMacdHist = macdHist;
			return;
		}
		
		var emaDelta = ema - _previousEma.Value;
		var macdDelta = macdHist - _previousMacdHist.Value;
		var color = 0;
		
		if (emaDelta > 0 && macdHist > 0 && macdDelta > 0)
		color = 2;
		else if (emaDelta < 0 && macdHist < 0 && macdDelta < 0)
		color = 1;
		
		if (_previousPreviousColor.HasValue)
		{
			if (_previousPreviousColor.Value == 2)
			{
				// Close short positions on bullish impulse
				if (Position < 0)
				BuyMarket(Math.Abs(Position));
				
				// Open long position when impulse weakens
				if ((_previousColor == 1 || _previousColor == 0) && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (_previousPreviousColor.Value == 1)
			{
				// Close long positions on bearish impulse
				if (Position > 0)
				SellMarket(Math.Abs(Position));
				
				// Open short position when impulse weakens
				if ((_previousColor == 0 || _previousColor == 2) && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			}
		}
		
		_previousPreviousColor = _previousColor;
		_previousColor = color;
		_previousEma = ema;
		_previousMacdHist = macdHist;
	}
}
