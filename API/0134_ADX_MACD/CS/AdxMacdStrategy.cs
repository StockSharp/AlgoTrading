namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that combines ADX and MACD indicators to identify strong trends
/// and potential entry points.
/// </summary>
public class AdxMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _adxThreshold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _atrMultiplier;
	
	private AverageDirectionalIndex _adx;
	private MovingAverageConvergenceDivergenceSignal _macd;
	private AverageTrueRange _atr;
	
	/// <summary>
	/// Data type for candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Period for ADX calculation.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}
	
	/// <summary>
	/// ADX threshold for trend strength.
	/// </summary>
	public int AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}
	
	/// <summary>
	/// Fast period for MACD calculation.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}
	
	/// <summary>
	/// Slow period for MACD calculation.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}
	
	/// <summary>
	/// Signal period for MACD calculation.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for stop-loss calculation.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="AdxMacdStrategy"/>.
	/// </summary>
	public AdxMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
					  .SetDisplay("Candle Type", "Type of candles to use", "General");
					  
		_adxPeriod = Param(nameof(AdxPeriod), 14)
					 .SetRange(5, 30)
					 .SetDisplay("ADX Period", "Period for ADX calculation", "ADX Settings")
					 .SetCanOptimize(true);
					 
		_adxThreshold = Param(nameof(AdxThreshold), 25)
						.SetRange(15, 40)
						.SetDisplay("ADX Threshold", "ADX threshold for trend strength", "ADX Settings")
						.SetCanOptimize(true);
						
		_macdFast = Param(nameof(MacdFast), 12)
					.SetRange(5, 30)
					.SetDisplay("MACD Fast", "Fast period for MACD calculation", "MACD Settings")
					.SetCanOptimize(true);
					
		_macdSlow = Param(nameof(MacdSlow), 26)
					.SetRange(10, 50)
					.SetDisplay("MACD Slow", "Slow period for MACD calculation", "MACD Settings")
					.SetCanOptimize(true);
					
		_macdSignal = Param(nameof(MacdSignal), 9)
					  .SetRange(3, 20)
					  .SetDisplay("MACD Signal", "Signal period for MACD calculation", "MACD Settings")
					  .SetCanOptimize(true);
					  
		_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
						 .SetRange(1.0m, 5.0m)
						 .SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss calculation", "Risk Management");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		// Initialize indicators
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		
		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		
		_atr = new AverageTrueRange { Length = 14 };
		
		// Create candle subscription
		var subscription = SubscribeCandles(CandleType);
		
		// Bind the indicators and candle processor
		subscription
			.BindEx(_adx, _macd, _atr, ProcessCandle)
			.Start();
			
		// Set up chart if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			
			// Draw ADX in a separate area
			var adxArea = CreateChartArea();
			DrawIndicator(adxArea, _adx);
			
			// Draw MACD in a separate area
			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, _macd);
			
			DrawOwnTrades(area);
		}
	}
	
	/// <summary>
	/// Process incoming candle with indicator values.
	/// </summary>
	/// <param name="candle">Candle to process.</param>
	/// <param name="adxValue">ADX value.</param>
	/// <param name="macdValue">MACD value.</param>
	/// <param name="atrValue">ATR value.</param>
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue macdValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
			
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var typedAdx = (AverageDirectionalIndexValue)adxValue;

		if (typedAdx.MovingAverage is not decimal adxIndicatorValue)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macdLine ||
			macdTyped.Signal is not decimal signalLine)
			return;
		
		var atrIndicatorValue = atrValue.ToDecimal();
		
		// ADX trend strength check
		var strongTrend = adxIndicatorValue > AdxThreshold;
		
		// Calculate stop loss distance based on ATR
		var stopLossDistance = atrIndicatorValue * AtrMultiplier;
		
		// Trading logic
		if (strongTrend)
		{
			if (macdLine > signalLine) // Bullish signal
			{
				// Strong uptrend with bullish MACD - Long signal
				if (Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					LogInfo($"Buy signal: Strong trend (ADX={adxIndicatorValue:F2}) with bullish MACD ({macdLine:F4} > {signalLine:F4})");
					
					// Set stop loss
					var stopPrice = candle.ClosePrice - stopLossDistance;
					RegisterOrder(CreateOrder(Sides.Sell, stopPrice, Math.Abs(Position + Volume).Max(Volume)));
				}
			}
			else if (macdLine < signalLine) // Bearish signal
			{
				// Strong downtrend with bearish MACD - Short signal
				if (Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Sell signal: Strong trend (ADX={adxIndicatorValue:F2}) with bearish MACD ({macdLine:F4} < {signalLine:F4})");
					
					// Set stop loss
					var stopPrice = candle.ClosePrice + stopLossDistance;
					RegisterOrder(CreateOrder(Sides.Buy, stopPrice, Math.Abs(Position + Volume).Max(Volume)));
				}
			}
		}
		
		// Exit conditions
		if (adxIndicatorValue < AdxThreshold * 0.8m) // Exit when trend weakens (ADX drops below 80% of threshold)
		{
			if (Position != 0)
			{
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit long: Trend weakening (ADX={adxIndicatorValue:F2})");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit short: Trend weakening (ADX={adxIndicatorValue:F2})");
				}
				
				// Cancel any pending stop orders
				CancelActiveOrders();
			}
		}
		// Additional exit logic for MACD crossover against the position
		else if ((Position > 0 && macdLine < signalLine) || (Position < 0 && macdLine > signalLine))
		{
			if (Position != 0)
			{
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit long: MACD crossed below signal ({macdLine:F4} < {signalLine:F4})");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit short: MACD crossed above signal ({macdLine:F4} > {signalLine:F4})");
				}
				
				// Cancel any pending stop orders
				CancelActiveOrders();
			}
		}
	}
}