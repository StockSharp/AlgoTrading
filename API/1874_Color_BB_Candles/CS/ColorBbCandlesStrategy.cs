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
/// Bollinger Bands candle color strategy.
/// Opens a long position when price closes above the upper band,
/// opens a short position when price closes below the lower band,
/// and closes positions when price returns between the bands.
/// </summary>
public class ColorBbCandlesStrategy : Strategy
{
	private enum BandStates
	{
		Neutral,
		Above,
		Below
	}
	
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;
	
	private BandStates _previousState = BandStates.Neutral;
	private decimal _entryPrice;
	
	/// <summary>
	/// Bollinger Bands period length.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}
	
	/// <summary>
	/// Bollinger Bands width.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}
	
	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="ColorBbCandlesStrategy"/> class.
	/// </summary>
	public ColorBbCandlesStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 100)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Length of Bollinger Bands", "General")
		
		.SetOptimize(50, 200, 25);
		
		_bollingerDeviation = Param(nameof(BollingerDeviation), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Deviation", "Width of Bollinger Bands", "General")
		
		.SetOptimize(0.5m, 3m, 0.5m);
		
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
		_previousState = BandStates.Neutral;
		_entryPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		
		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.BindEx(bollinger, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upperBand || bb.LowBand is not decimal lowerBand)
			return;

		var state = BandStates.Neutral;

		if (candle.ClosePrice > upperBand)
			state = BandStates.Above;
		else if (candle.ClosePrice < lowerBand)
			state = BandStates.Below;

		if (state == BandStates.Above && _previousState != BandStates.Above)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (state == BandStates.Below && _previousState != BandStates.Below)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (state == BandStates.Neutral && _previousState != BandStates.Neutral)
		{
			if (Position > 0) SellMarket();
			else if (Position < 0) BuyMarket();
		}

		_previousState = state;
	}
}
