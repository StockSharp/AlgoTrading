using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crunchster's Turtle and Trend System.
/// Combines EMA trend signals with Donchian breakout logic and ATR based stop.
/// </summary>
public class CrunchstersTurtleAndTrendSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _breakoutPeriod;
	private readonly StrategyParam<int> _trailPeriod;
	private readonly StrategyParam<decimal> _stopAtrMultiple;
	private readonly StrategyParam<decimal> _orderPercent;
	private readonly StrategyParam<bool> _trendEnabled;
	private readonly StrategyParam<bool> _breakoutEnabled;
	private readonly StrategyParam<bool> _longEnabled;
	private readonly StrategyParam<bool> _shortEnabled;
	private readonly StrategyParam<DataType> _candleType;
	
	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private DonchianChannels _breakDonchian;
	private DonchianChannels _trailDonchian;
	private AverageTrueRange _atr;
	private StandardDeviation _stdev;
	
	private decimal _prevNemadiff;
	private decimal _prevSignal;
	private decimal _prevClose;
	private decimal _stopPrice;
	
	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaPeriod { get => _fastEmaPeriod.Value; set => _fastEmaPeriod.Value = value; }
	
	/// <summary>
	/// Breakout Donchian period.
	/// </summary>
	public int BreakoutPeriod { get => _breakoutPeriod.Value; set => _breakoutPeriod.Value = value; }
	
	/// <summary>
	/// Trailing stop Donchian period.
	/// </summary>
	public int TrailPeriod { get => _trailPeriod.Value; set => _trailPeriod.Value = value; }
	
	/// <summary>
	/// ATR stop multiplier.
	/// </summary>
	public decimal StopAtrMultiple { get => _stopAtrMultiple.Value; set => _stopAtrMultiple.Value = value; }
	
	/// <summary>
	/// Order size as percent of equity.
	/// </summary>
	public decimal OrderPercent { get => _orderPercent.Value; set => _orderPercent.Value = value; }
	
	/// <summary>
	/// Enable trend mode.
	/// </summary>
	public bool TrendEnabled { get => _trendEnabled.Value; set => _trendEnabled.Value = value; }
	
	/// <summary>
	/// Enable breakout mode.
	/// </summary>
	public bool BreakoutEnabled { get => _breakoutEnabled.Value; set => _breakoutEnabled.Value = value; }
	
	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool LongEnabled { get => _longEnabled.Value; set => _longEnabled.Value = value; }
	
	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool ShortEnabled { get => _shortEnabled.Value; set => _shortEnabled.Value = value; }
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public CrunchstersTurtleAndTrendSystemStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Lookback for fast EMA", "General");
		
		_breakoutPeriod = Param(nameof(BreakoutPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Breakout Period", "Donchian period for breakout", "General");
		
		_trailPeriod = Param(nameof(TrailPeriod), 1000)
		.SetGreaterThanZero()
		.SetDisplay("Trail Period", "Donchian period for trailing stop", "Risk");
		
		_stopAtrMultiple = Param(nameof(StopAtrMultiple), 20m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Mult", "ATR multiple for hard stop", "Risk");
		
		_orderPercent = Param(nameof(OrderPercent), 10m)
		.SetRange(1m, 100m)
		.SetDisplay("Order %", "Percent of equity per trade", "Risk");
		
		_trendEnabled = Param(nameof(TrendEnabled), true)
		.SetDisplay("Use Trend", "Enable EMA trend logic", "Toggle");
		
		_breakoutEnabled = Param(nameof(BreakoutEnabled), false)
		.SetDisplay("Use Breakout", "Enable breakout logic", "Toggle");
		
		_longEnabled = Param(nameof(LongEnabled), true)
		.SetDisplay("Long", "Allow long trades", "Toggle");
		
		_shortEnabled = Param(nameof(ShortEnabled), true)
		.SetDisplay("Short", "Allow short trades", "Toggle");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		
		_prevNemadiff = 0m;
		_prevSignal = 0m;
		_prevClose = 0m;
		_stopPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = FastEmaPeriod * 5 };
		_breakDonchian = new DonchianChannels { Length = BreakoutPeriod };
		_trailDonchian = new DonchianChannels { Length = TrailPeriod };
		_atr = new AverageTrueRange { Length = 14 };
		_stdev = new StandardDeviation { Length = 252 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var fastVal = _fastEma.Process(candle);
		var slowVal = _slowEma.Process(candle);
		var breakVal = (DonchianChannelsValue)_breakDonchian.Process(candle);
		var trailVal = (DonchianChannelsValue)_trailDonchian.Process(candle);
		var atrVal = _atr.Process(candle);
		
		if (!fastVal.IsFinal || !slowVal.IsFinal || !atrVal.IsFinal)
		return;
		
		if (breakVal.UpperBand is not decimal breakUpper || breakVal.LowerBand is not decimal breakLower ||
		trailVal.UpperBand is not decimal trailUpper || trailVal.LowerBand is not decimal trailLower)
		return;
		
		var fast = fastVal.ToDecimal();
		var slow = slowVal.ToDecimal();
		var atr = atrVal.ToDecimal();
		
		var ret = _prevClose == 0m ? 0m : candle.ClosePrice - _prevClose;
		var stdevVal = _stdev.Process(ret);
		if (!stdevVal.IsFinal)
		{
			_prevClose = candle.ClosePrice;
			return;
		}
		
		var stdev = stdevVal.ToDecimal();
		_prevClose = candle.ClosePrice;
		
		if (stdev == 0m || breakUpper == breakLower || trailUpper == trailLower)
		return;
		
		var nemadiff = 5m * (fast - slow) / stdev;
		var basis = (breakUpper + breakLower) / 2m;
		var signal = 20m * (candle.ClosePrice - basis) / (breakUpper - breakLower);
		var tbasis = (trailUpper + trailLower) / 2m;
		var tsignal = 20m * (candle.ClosePrice - tbasis) / (trailUpper - trailLower);
		
		var volume = CalculateVolume(candle.ClosePrice);
		
		if (TrendEnabled)
		{
			if (LongEnabled && nemadiff > 2.5m && _prevNemadiff <= 2.5m && Position <= 0)
			{
				BuyMarket(volume + Math.Abs(Position));
				_stopPrice = candle.ClosePrice - atr * StopAtrMultiple;
			}
			else if (ShortEnabled && nemadiff < -2.5m && _prevNemadiff >= -2.5m && Position >= 0)
			{
				SellMarket(volume + Math.Abs(Position));
				_stopPrice = candle.ClosePrice + atr * StopAtrMultiple;
			}
			
			if (Position > 0 && (tsignal <= -10m || (nemadiff < 2.5m && _prevNemadiff >= 2.5m)))
			SellMarket(Position);
			else if (Position < 0 && (tsignal >= 10m || (nemadiff > -2.5m && _prevNemadiff <= -2.5m)))
			BuyMarket(Math.Abs(Position));
		}
		
		if (BreakoutEnabled)
		{
			if (LongEnabled && signal >= 10m && Position <= 0)
			{
				BuyMarket(volume + Math.Abs(Position));
				_stopPrice = candle.ClosePrice - atr * StopAtrMultiple;
			}
			else if (ShortEnabled && signal <= -10m && Position >= 0)
			{
				SellMarket(volume + Math.Abs(Position));
				_stopPrice = candle.ClosePrice + atr * StopAtrMultiple;
			}
			
			if (Position > 0 && tsignal <= -10m)
			SellMarket(Position);
			else if (Position < 0 && tsignal >= 10m)
			BuyMarket(Math.Abs(Position));
		}
		
		if (Position > 0 && candle.ClosePrice <= _stopPrice)
		SellMarket(Position);
		else if (Position < 0 && candle.ClosePrice >= _stopPrice)
		BuyMarket(Math.Abs(Position));
		
		_prevNemadiff = nemadiff;
		_prevSignal = signal;
	}
	
	private decimal CalculateVolume(decimal price)
	{
		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		var size = portfolioValue * (OrderPercent / 100m) / price;
		return size > 0 ? size : Volume;
	}
}
