using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA/EMA crossover strategy filtered by Ichimoku Cloud and 200-period SMA.
/// </summary>
public class RefinedSmaEmaCrossoverWithIchimokuAnd200SmaFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _sma200Length;
	private readonly StrategyParam<DataType> _candleType;
	
	private Ichimoku _ichimoku;
	private SimpleMovingAverage _sma;
	private ExponentialMovingAverage _ema;
	private SimpleMovingAverage _sma200;
	
	private decimal _prevSma;
	private decimal _prevEma;
	private bool _isInitialized;
	
	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}
	
	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}
	
	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}
	
	/// <summary>
	/// Short SMA period.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}
	
	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}
	
	/// <summary>
	/// Long SMA period.
	/// </summary>
	public int Sma200Length
	{
		get => _sma200Length.Value;
		set => _sma200Length.Value = value;
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
	/// Initializes the strategy.
	/// </summary>
	public RefinedSmaEmaCrossoverWithIchimokuAnd200SmaFilterStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku");
		
		_kijunPeriod = Param(nameof(KijunPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku");
		
		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
		.SetGreaterThanZero()
		.SetDisplay("Senkou Span B Period", "Senkou Span B period", "Ichimoku");
		
		_smaLength = Param(nameof(SmaLength), 28)
		.SetGreaterThanZero()
		.SetDisplay("SMA Length", "Short SMA period", "Indicators");
		
		_emaLength = Param(nameof(EmaLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "EMA period", "Indicators");
		
		_sma200Length = Param(nameof(Sma200Length), 200)
		.SetGreaterThanZero()
		.SetDisplay("Long SMA Length", "Long-term SMA period", "Indicators");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles for calculations", "General");
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
		
		_prevSma = 0m;
		_prevEma = 0m;
		_isInitialized = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};
		
		_sma = new SimpleMovingAverage { Length = SmaLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_sma200 = new SimpleMovingAverage { Length = Sma200Length };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_ichimoku, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _sma200);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var ich = (IchimokuValue)ichimokuValue;
		
		if (ich.SenkouA is not decimal spanA || ich.SenkouB is not decimal spanB)
		return;
		
		var close = candle.ClosePrice;
		var sma = _sma.Process(close, candle.ServerTime, true).ToDecimal();
		var ema = _ema.Process(close, candle.ServerTime, true).ToDecimal();
		var sma200 = _sma200.Process(close, candle.ServerTime, true).ToDecimal();
		
		if (!_isInitialized)
		{
			_prevSma = sma;
			_prevEma = ema;
			_isInitialized = true;
			return;
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevSma = sma;
			_prevEma = ema;
			return;
		}
		
		var crossUp = _prevSma <= _prevEma && sma > ema;
		var crossDown = _prevSma >= _prevEma && sma < ema;
		var priceAboveCloud = close > spanA || close > spanB;
		var priceBelowCloud = close < spanA || close < spanB;
		var priceAboveSma200 = close > sma200;
		var priceBelowSma200 = close < sma200;
		
		if (crossUp && priceAboveCloud && priceAboveSma200 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && priceBelowCloud && priceBelowSma200 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		
		_prevSma = sma;
		_prevEma = ema;
	}
}
