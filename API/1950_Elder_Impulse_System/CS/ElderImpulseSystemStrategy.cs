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
/// Strategy implementing Elder Impulse System logic.
/// </summary>
public class ElderImpulseSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _previousEma;
	private decimal? _previousMacdHist;
	private int? _previousColor;
	private int _barsSinceTrade;
	
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
	/// Bars to wait after a completed trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
		
		.SetOptimize(8, 21, 1);
		
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")
		
		.SetOptimize(8, 20, 1);
		
		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")
		
		.SetOptimize(20, 40, 1);
		
		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetDisplay("MACD Signal Period", "Signal EMA period for MACD", "Indicators")
		
		.SetOptimize(5, 15, 1);
		
		_cooldownBars = Param(nameof(CooldownBars), 1)
		.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk");

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
		_barsSinceTrade = CooldownBars;
	}
	
	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		if (_barsSinceTrade < CooldownBars)
			_barsSinceTrade++;
		
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
		
		if (_previousColor.HasValue && _barsSinceTrade >= CooldownBars)
		{
			if (_previousColor.Value == 2 && color != 2)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				
				if (Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					_barsSinceTrade = 0;
				}
			}
			else if (_previousColor.Value == 1 && color != 1)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				
				if (Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					_barsSinceTrade = 0;
				}
			}
		}
		
		_previousColor = color;
		_previousEma = ema;
		_previousMacdHist = macdHist;
	}
}
