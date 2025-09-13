namespace StockSharp.Samples.Strategies;

using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// ZMFX Stolid 5a strategy converted from MQL.
/// Uses multi-timeframe indicators to trade pullbacks within the main trend.
/// </summary>
public class ZmfxStolid5aEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;
	
	private RelativeStrengthIndex _rsi;
	private RelativeStrengthIndex _rsi15;
	private SmoothedMovingAverage _emaFast;
	private SmoothedMovingAverage _emaSlow;
	private StochasticOscillator _stoch4h;
	
	private decimal? _rsiPrev;
	private decimal? _rsi15Value;
	private decimal? _emaFastValue;
	private decimal? _emaSlowValue;
	private decimal? _stoch4hValue;
	
	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrevCandle;
	private DateTimeOffset _lastSignalTime;
	
	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}
	
	/// <summary>
	/// Base candle type for main calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes <see cref="ZmfxStolid5aEaStrategy"/>.
	/// </summary>
	public ZmfxStolid5aEaStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Base candle timeframe", "Common");
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_rsi = new RelativeStrengthIndex { Length = 11 };
		_rsi15 = new RelativeStrengthIndex { Length = 11 };
		_emaFast = new SmoothedMovingAverage { Length = 50 };
		_emaSlow = new SmoothedMovingAverage { Length = 100 };
		_stoch4h = new StochasticOscillator
		{
			Length = 30,
			K = { Length = 3 },
			D = { Length = 3 },
		};
		
		var baseSub = SubscribeCandles(CandleType);
		baseSub.Bind(_rsi, ProcessBase).Start();
		
		var rsi15Sub = SubscribeCandles(TimeSpan.FromMinutes(15).TimeFrame());
		rsi15Sub.Bind(_rsi15, ProcessRsi15).Start();
		
		var emaSub = SubscribeCandles(TimeSpan.FromHours(1).TimeFrame());
		emaSub.Bind(_emaFast, _emaSlow, ProcessEma).Start();
		
		var stochSub = SubscribeCandles(TimeSpan.FromHours(4).TimeFrame());
		stochSub.BindEx(_stoch4h, ProcessStoch).Start();
	}
	
	private void ProcessRsi15(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_rsi15Value = rsi;
	}
	
	private void ProcessEma(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_emaFastValue = fast;
		_emaSlowValue = slow;
	}
	
	private void ProcessStoch(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var stoch = (StochasticOscillatorValue)value;
		if (stoch.K is decimal k)
		_stoch4hValue = k;
	}
	
	private void ProcessBase(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var prevRsi = _rsiPrev;
		_rsiPrev = rsi;
		
		var barUp = _hasPrevCandle && _prevClose > _prevOpen;
		var barDown = _hasPrevCandle && _prevClose < _prevOpen;
		
		var upTrend = false;
		var downTrend = false;
		
		if (_lastSignalTime != candle.OpenTime &&
		_stoch4hValue is decimal s &&
		_emaFastValue is decimal ef &&
		_emaSlowValue is decimal es)
		{
			if (s < 50m && ef < es)
			downTrend = true;
			else if (s > 50m && ef > es)
			upTrend = true;
		}
		
		if (upTrend && barDown && prevRsi is decimal rsiPrev && rsiPrev < 30m && Position <= 0)
		{
			var vol = Volume;
			if (_rsi15Value is decimal rsi15 && rsi15 < 30m)
			BuyMarket(vol * 2);
			else
			BuyMarket(vol);
			
			_lastSignalTime = candle.OpenTime;
		}
		else if (downTrend && barUp && prevRsi is decimal rsiPrev2 && rsiPrev2 > 70m && Position >= 0)
		{
			var vol = Volume;
			if (_rsi15Value is decimal rsi15 && rsi15 > 70m)
			SellMarket(vol * 2);
			else
			SellMarket(vol);
			
			_lastSignalTime = candle.OpenTime;
		}
		
		if (Position > 0 &&
		(rsi > 70m ||
		_emaFastValue < _emaSlowValue ||
		(_stoch4hValue < 50m && rsi > 50m)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 &&
		(rsi < 30m ||
		_emaFastValue > _emaSlowValue ||
		(_stoch4hValue > 50m && rsi < 50m)))
		{
			BuyMarket(-Position);
		}
		
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_hasPrevCandle = true;
	}
}
