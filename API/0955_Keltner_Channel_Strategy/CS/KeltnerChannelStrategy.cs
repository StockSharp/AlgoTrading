using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner Channel breakout and EMA trend strategy.
/// </summary>
public class KeltnerChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _trendEmaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevClose;
	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _stopPrice;
	private decimal _takePrice;
	
	/// <summary>
	/// EMA period for Keltner Channels.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for channel width.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for stops.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}
	
	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}
	
	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}
	
	/// <summary>
	/// Trend filter EMA period.
	/// </summary>
	public int TrendEmaPeriod
	{
		get => _trendEmaPeriod.Value;
		set => _trendEmaPeriod.Value = value;
	}
	
	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="KeltnerChannelStrategy"/>.
	/// </summary>
	public KeltnerChannelStrategy()
	{
		_length = Param(nameof(Length), 20)
		.SetRange(5, 100)
		.SetDisplay("Length", "EMA period for Keltner channels", "Keltner");
		
		_multiplier = Param(nameof(Multiplier), 1.5m)
		.SetRange(0.5m, 5m)
		.SetDisplay("Multiplier", "ATR multiplier for channel", "Keltner");
		
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
		.SetRange(0.5m, 5m)
		.SetDisplay("ATR Multiplier", "ATR multiplier for stops", "Risk Management");
		
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 9)
		.SetRange(2, 50)
		.SetDisplay("Fast EMA", "Fast EMA period", "Trend");
		
		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 21)
		.SetRange(5, 100)
		.SetDisplay("Slow EMA", "Slow EMA period", "Trend");
		
		_trendEmaPeriod = Param(nameof(TrendEmaPeriod), 50)
		.SetRange(10, 200)
		.SetDisplay("Trend EMA", "Trend filter EMA period", "Trend");
		
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
		
		_prevClose = 0m;
		_prevFast = 0m;
		_prevSlow = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var kc = new KeltnerChannels
		{
			Length = Length,
			Multiplier = Multiplier
		};
		
		var emaFast = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var emaSlow = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		var emaTrend = new ExponentialMovingAverage { Length = TrendEmaPeriod };
		var atr = new AverageTrueRange { Length = Length };
		
		var sub = SubscribeCandles(CandleType);
		sub.Bind(kc, emaFast, emaSlow, emaTrend, atr, ProcessCandle).Start();
		
		StartProtection();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, kc);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal emaFast, decimal emaSlow, decimal emaTrend, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var price = candle.ClosePrice;
		
		var crossUnderLower = _prevClose >= lower && price < lower;
		var crossOverUpper = _prevClose <= upper && price > upper;
		
		var crossOverEma = _prevFast <= _prevSlow && emaFast > emaSlow;
		var crossUnderEma = _prevFast >= _prevSlow && emaFast < emaSlow;
		
		var longEntryKC = crossUnderLower;
		var shortEntryKC = crossOverUpper;
		
		var longEntryTrend = crossOverEma && price > emaTrend;
		var shortEntryTrend = crossUnderEma && price < emaTrend;
		
		var exitLongKC = _prevClose <= middle && price > middle;
		var exitShortKC = _prevClose >= middle && price < middle;
		var exitLongTrend = crossUnderEma;
		var exitShortTrend = crossOverEma;
		
		var atrDistance = atr * AtrMultiplier;
		
		if (Position <= 0 && (longEntryKC || longEntryTrend))
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_stopPrice = price - atrDistance;
			_takePrice = price + 2m * atrDistance;
		}
		else if (Position >= 0 && (shortEntryKC || shortEntryTrend))
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_stopPrice = price + atrDistance;
			_takePrice = price - 2m * atrDistance;
		}
		
		if (Position > 0)
		{
			if (exitLongKC || exitLongTrend || price <= _stopPrice || price >= _takePrice)
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (exitShortKC || exitShortTrend || price >= _stopPrice || price <= _takePrice)
			BuyMarket(Math.Abs(Position));
		}
		
		_prevClose = price;
		_prevFast = emaFast;
		_prevSlow = emaSlow;
	}
}
