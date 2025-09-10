using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Berlin candles with Donchian baseline.
/// </summary>
public class BerlinCandlesStrategy : Strategy
{
	private readonly StrategyParam<int> _smoothing;
	private readonly StrategyParam<int> _baselinePeriod;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevEma;
	private bool _isInitialized;
	
	public int Smoothing
	{
		get => _smoothing.Value;
		set => _smoothing.Value = value;
	}
	
	public int BaselinePeriod
	{
		get => _baselinePeriod.Value;
		set => _baselinePeriod.Value = value;
	}
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public BerlinCandlesStrategy()
	{
		_smoothing = Param(nameof(Smoothing), 1)
		.SetDisplay("Smoothing", "EMA smoothing for Berlin open", "Berlin")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);
		
		_baselinePeriod = Param(nameof(BaselinePeriod), 26)
		.SetDisplay("Baseline Period", "Donchian baseline period", "Berlin")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = default;
		_isInitialized = false;
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var ema = new ExponentialMovingAverage { Length = Smoothing + 1 };
		var donchian = new DonchianChannels { Length = BaselinePeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ema, donchian, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!_isInitialized)
		{
			_prevEma = emaValue;
			_isInitialized = true;
			return;
		}
		
		var openExpr = _prevEma;
		var closeExpr = candle.ClosePrice;
		
		decimal openValue;
		decimal closeValue;
		
		if (openExpr > closeExpr)
		{
			openValue = Math.Min(openExpr, candle.HighPrice);
			closeValue = Math.Max(closeExpr, candle.LowPrice);
		}
		else
		{
			openValue = Math.Max(openExpr, candle.LowPrice);
			closeValue = Math.Min(closeExpr, candle.HighPrice);
		}
		
		var baseline = middleBand;
		
		if (closeValue > openValue && closeValue > baseline && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (closeValue < openValue && closeValue < baseline && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		
		_prevEma = emaValue;
	}
}
