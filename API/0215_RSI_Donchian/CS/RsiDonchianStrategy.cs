using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;
	
/// <summary>
/// Strategy based on RSI and Donchian Channel indicators.
/// Enters long when RSI is below 30 (oversold) and price breaks above Donchian high.
/// Enters short when RSI is above 70 (overbought) and price breaks below Donchian low.
/// Uses middle line of Donchian Channel for exit signals.
/// </summary>
public class RsiDonchianStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private RelativeStrengthIndex _rsi;
	private Highest _highestHigh;
	private Lowest _lowestLow;
	
	private decimal _previousRsi;
	private decimal _donchianHigh;
	private decimal _donchianLow;
	private decimal _donchianMiddle;
	private decimal _currentRsi;

	/// <summary>
	/// RSI period parameter.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}
	
	/// <summary>
	/// Donchian Channel period parameter.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}
	
	/// <summary>
	/// Stop-loss percentage parameter.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}
	
	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public RsiDonchianStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 2);
			
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Period for Donchian Channel calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);
			
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);
			
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

		_rsi = null;
		_highestHigh = null;
		_lowestLow = null;
		_previousRsi = 0;
		_donchianHigh = 0;
		_donchianLow = 0;
		_donchianMiddle = 0;
		_currentRsi = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

// Initialize indicators
		_rsi = new RelativeStrengthIndex
	{
Length = RsiPeriod
	};
		
		_highestHigh = new Highest
		{
			Length = DonchianPeriod
		};
		
		_lowestLow = new Lowest
		{
			Length = DonchianPeriod
		};
		
		// Create candles subscription
		var subscription = SubscribeCandles(CandleType);
		
		// Bind indicators
		subscription
			.Bind(_rsi, _highestHigh, _lowestLow, ProcessIndicators)
			.Start();

		// Enable position protection with stop-loss
		StartProtection(
			takeProfit: new Unit(0, UnitTypes.Absolute), // No take-profit
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent) // Stop-loss as percentage
		);
		
		// Setup chart if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessIndicators(ICandleMessage candle, decimal rsiValue, decimal highestValue, decimal lowestValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;
		
		// Save previous RSI value
		_previousRsi = _currentRsi;
		
		// Get current RSI value
		_currentRsi = rsiValue;

		// Update Donchian high value
		_donchianHigh = highestValue;

		// Update Donchian low value
		_donchianLow = lowestValue;

		// Calculate Donchian middle line
		_donchianMiddle = (_donchianHigh + _donchianLow) / 2;

		// Process trading logic after all indicators are updated
		ProcessTradingLogic(candle);
	}

	private void ProcessTradingLogic(ICandleMessage candle)
	{
		// Skip if strategy is not ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		// Skip if not all indicators are initialized
		if (_donchianHigh == 0 || _donchianLow == 0 || _currentRsi == 0)
			return;
		
		// Trading signals
		bool isRsiOversold = _currentRsi < 30;
		bool isRsiOverbought = _currentRsi > 70;
		bool isPriceBreakingHigher = candle.ClosePrice > _donchianHigh;
		bool isPriceBreakingLower = candle.ClosePrice < _donchianLow;
		
		// Long signal: RSI < 30 (oversold) and price breaks above Donchian high
		if (isRsiOversold && isPriceBreakingHigher)
		{
			if (Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long Entry: RSI({_currentRsi:F2}) < 30 && Price({candle.ClosePrice}) > Donchian High({_donchianHigh})");
			}
		}
		// Short signal: RSI > 70 (overbought) and price breaks below Donchian low
		else if (isRsiOverbought && isPriceBreakingLower)
		{
			if (Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short Entry: RSI({_currentRsi:F2}) > 70 && Price({candle.ClosePrice}) < Donchian Low({_donchianLow})");
			}
		}
		// Exit signals based on Donchian middle line
		else if ((Position > 0 && candle.ClosePrice < _donchianMiddle) || 
				(Position < 0 && candle.ClosePrice > _donchianMiddle))
		{
			if (Position > 0)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit Long: Price({candle.ClosePrice}) < Donchian Middle({_donchianMiddle})");
			}
			else if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit Short: Price({candle.ClosePrice}) > Donchian Middle({_donchianMiddle})");
			}
		}
	}
}
	
