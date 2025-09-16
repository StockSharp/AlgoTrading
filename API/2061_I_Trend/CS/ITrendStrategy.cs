using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// i_Trend strategy built on Bollinger Bands and Moving Average.
/// Generates buy/sell signals when the iTrend value crosses the signal line.
/// </summary>
public class ITrendStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<AppliedPrice> _priceType;
	private readonly StrategyParam<BandMode> _bbMode;
	
	private decimal _prevInd;
	private decimal _prevSign;
	private bool _isInitialized;
	
	/// <summary>
	/// Period for the moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}
	
	/// <summary>
	/// Period for Bollinger Bands.
	/// </summary>
	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}
	
	/// <summary>
	/// Standard deviation for Bollinger Bands.
	/// </summary>
	public decimal BbDeviation
	{
		get => _bbDeviation.Value;
		set => _bbDeviation.Value = value;
	}
	
	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Price type for iTrend calculation.
	/// </summary>
	public AppliedPrice PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}
	
	/// <summary>
	/// Selected Bollinger Band line.
	/// </summary>
	public BandMode BbMode
	{
		get => _bbMode.Value;
		set => _bbMode.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public ITrendStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Moving average length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 5);
		
		_bbPeriod = Param(nameof(BbPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("BB Period", "Bollinger Bands period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);
		
		_bbDeviation = Param(nameof(BbDeviation), 2.0m)
		.SetGreaterThanZero()
		.SetDisplay("BB Deviation", "Standard deviation for Bollinger Bands", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(1.0m, 3.0m, 0.5m);
		
		_priceType = Param(nameof(PriceType), AppliedPrice.PriceClose)
		.SetDisplay("Price Type", "Applied price for iTrend", "General");
		
		_bbMode = Param(nameof(BbMode), BandMode.Upper)
		.SetDisplay("Band Mode", "Bollinger Band line used for comparison", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used", "General");
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
		_prevInd = 0m;
		_prevSign = 0m;
		_isInitialized = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var ma = new EMA { Length = MaPeriod };
		var bb = new BollingerBands { Length = BbPeriod, Width = BbDeviation };
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(ma, bb, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var price = GetPrice(candle, PriceType);
		
		var band = BbMode switch
		{
			BandMode.Upper => upperBand,
			BandMode.Lower => lowerBand,
			_ => middleBand,
		};
		
		var ind = price - band;
		var sign = 2m * maValue - (candle.LowPrice + candle.HighPrice);
		
		if (!_isInitialized)
		{
			_prevInd = ind;
			_prevSign = sign;
			_isInitialized = true;
			return;
		}
		
		var crossUp = _prevInd <= _prevSign && ind > sign;
		var crossDown = _prevInd >= _prevSign && ind < sign;
		
		if (crossUp)
		{
			if (Position < 0)
			BuyMarket(Math.Abs(Position));
			if (Position <= 0)
			BuyMarket(Volume);
		}
		else if (crossDown)
		{
			if (Position > 0)
			SellMarket(Math.Abs(Position));
			if (Position >= 0)
			SellMarket(Volume);
		}
		
		_prevInd = ind;
		_prevSign = sign;
	}
	
	private static decimal GetPrice(ICandleMessage candle, AppliedPrice type)
	{
		return type switch
		{
			AppliedPrice.PriceOpen => candle.OpenPrice,
			AppliedPrice.PriceHigh => candle.HighPrice,
			AppliedPrice.PriceLow => candle.LowPrice,
			AppliedPrice.PriceMedian => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.PriceTypical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.PriceWeighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.PriceSimpl => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.PriceQuarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.PriceTrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice :
			candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			AppliedPrice.PriceTrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m :
			candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			AppliedPrice.PriceDemark =>
			{
				var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;
				if (candle.ClosePrice < candle.OpenPrice)
				res = (res + candle.LowPrice) / 2m;
				else if (candle.ClosePrice > candle.OpenPrice)
				res = (res + candle.HighPrice) / 2m;
				else
				res = (res + candle.ClosePrice) / 2m;
				return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
			},
			_ => candle.ClosePrice,
		};
	}
}

/// <summary>
/// Types of price used for calculations.
/// </summary>
public enum AppliedPrice
{
	PriceClose,
	PriceOpen,
	PriceHigh,
	PriceLow,
	PriceMedian,
	PriceTypical,
	PriceWeighted,
	PriceSimpl,
	PriceQuarter,
	PriceTrendFollow0,
	PriceTrendFollow1,
	PriceDemark
}

/// <summary>
/// Bollinger Band line selection.
/// </summary>
public enum BandMode
{
	Upper,
	Lower,
	Middle
}
