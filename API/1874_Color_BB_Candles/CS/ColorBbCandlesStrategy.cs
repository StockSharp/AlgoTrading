using System;

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
	private enum BandState
	{
		Neutral,
		Above,
		Below
	}
	
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;
	
	private BandState _previousState = BandState.Neutral;
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
		.SetCanOptimize(true)
		.SetOptimize(50, 200, 25);
		
		_bollingerDeviation = Param(nameof(BollingerDeviation), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Deviation", "Width of Bollinger Bands", "General")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.5m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_previousState = BandState.Neutral;
		_entryPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(bollinger, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var state = BandState.Neutral;
		
		if (candle.ClosePrice > upperBand)
		state = BandState.Above;
		else if (candle.ClosePrice < lowerBand)
		state = BandState.Below;
		
		if (state == BandState.Above && _previousState != BandState.Above)
		{
			var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0);
			if (volume > 0)
			{
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (state == BandState.Below && _previousState != BandState.Below)
		{
			var volume = Volume + (Position > 0 ? Math.Abs(Position) : 0);
			if (volume > 0)
			{
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (state == BandState.Neutral && _previousState != BandState.Neutral)
		{
			if (Position > 0)
			{
				SellMarket(Position);
			}
			else if (Position < 0)
			{
				BuyMarket(-Position);
			}
		}
		
		_previousState = state;
	}
}
