using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from _HPCS_Inter7_MT4_EA_V01_We.mq4.
/// Sells when price crosses below the lower Bollinger band and buys when price crosses above the upper band.
/// Protective stop loss and take profit are placed at a fixed distance from the entry price.
/// </summary>
public class HpcsInter7Strategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _protectionDistancePoints;
	
	private decimal? _prevClose;
	private decimal? _prevLower;
	private decimal? _prevUpper;
	private DateTimeOffset? _lastTradeTime;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="HpcsInter7Strategy"/> class.
	/// </summary>
	public HpcsInter7Strategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Length", "Number of candles included in the Bollinger Bands calculation", "Indicators")
		.SetCanOptimize(true);
		
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for the Bollinger Bands", "Indicators")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for Bollinger Bands", "General");
		
		_protectionDistancePoints = Param(nameof(ProtectionDistancePoints), 10m)
		.SetNotNegative()
		.SetDisplay("Protection Distance (pts)", "Distance for stop loss and take profit expressed in price steps", "Risk")
		.SetCanOptimize(true);
	}
	
	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}
	
	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
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
	/// Stop loss and take profit distance in price steps.
	/// </summary>
	public decimal ProtectionDistancePoints
	{
		get => _protectionDistancePoints.Value;
		set => _protectionDistancePoints.Value = value;
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
		
		_prevClose = null;
		_prevLower = null;
		_prevUpper = null;
		_lastTradeTime = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
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
	
	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (_prevClose.HasValue && _prevLower.HasValue && _prevUpper.HasValue && _lastTradeTime != candle.OpenTime)
		{
			// Check for a downward cross through the lower band to open a short position.
			if (_prevClose.Value > _prevLower.Value && candle.ClosePrice < lower && Position <= 0m)
			{
				var volume = Volume;
				if (volume > 0m)
				{
					var resultingPosition = Position - volume;
					SellMarket(volume);
					ApplyProtection(candle.ClosePrice, resultingPosition);
					_lastTradeTime = candle.OpenTime;
				}
			}
			// Check for an upward cross through the upper band to open a long position.
			else if (_prevClose.Value < _prevUpper.Value && candle.ClosePrice > upper && Position >= 0m)
			{
				var volume = Volume;
				if (volume > 0m)
				{
					var resultingPosition = Position + volume;
					BuyMarket(volume);
					ApplyProtection(candle.ClosePrice, resultingPosition);
					_lastTradeTime = candle.OpenTime;
				}
			}
		}
		
		_prevClose = candle.ClosePrice;
		_prevLower = lower;
		_prevUpper = upper;
	}
	
	private void ApplyProtection(decimal referencePrice, decimal resultingPosition)
	{
		var distance = GetProtectionDistance();
		if (distance <= 0m || resultingPosition == 0m)
		return;
		
		// Apply both stop loss and take profit at the same distance from the entry price.
		SetStopLoss(distance, referencePrice, resultingPosition);
		SetTakeProfit(distance, referencePrice, resultingPosition);
	}
	
	private decimal GetProtectionDistance()
	{
		var step = Security?.PriceStep ?? 0m;
		if (ProtectionDistancePoints <= 0m)
		return 0m;
		
		// Convert point-based distance to absolute price distance using the security price step if available.
		return step > 0m ? step * ProtectionDistancePoints : ProtectionDistancePoints;
	}
}
