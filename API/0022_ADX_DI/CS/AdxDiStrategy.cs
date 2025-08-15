using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on ADX and Directional Movement indicators.
/// </summary>
public class AdxDiStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold for trend confirmation.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop-loss.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AdxDiStrategy"/>.
	/// </summary>
	public AdxDiStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(7, 30)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
			.SetCanOptimize(true);

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetRange(15m, 35m)
			.SetDisplay("ADX Threshold", "ADX level to confirm trend", "Indicators")
			.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetRange(1m, 5m)
			.SetDisplay("ATR Multiplier", "Multiplier for ATR stop loss", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		// Create ADX Indicator
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var atr = new AverageTrueRange { Length = 14 };

		// Subscribe to candles
		var subscription = SubscribeCandles(CandleType);
		
		// Bind indicators and process candles
		subscription
			.BindEx(adx, atr, ProcessCandle)
			.Start();

		// Enable position protection
		StartProtection(
			takeProfit: null, 
			stopLoss: new Unit(AtrMultiplier, UnitTypes.Absolute),
			isStopTrailing: true,
			useMarketOrders: true
		);

		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue atrValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Get ADX and +DI/-DI values
		var adx = (AverageDirectionalIndexValue)adxValue;
		var adxMain = adx.MovingAverage;
		var plusDi = adx.Dx.Plus;
		var minusDi = adx.Dx.Minus;

		// Trading logic
		if (adxMain >= AdxThreshold)
		{
			// Strong trend detected
			
			// Long signal: +DI > -DI
			if (plusDi > minusDi && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Buy signal: ADX = {adxMain:F2}, +DI = {plusDi:F2}, -DI = {minusDi:F2}");
			}
			// Short signal: -DI > +DI
			else if (minusDi > plusDi && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Sell signal: ADX = {adxMain:F2}, +DI = {plusDi:F2}, -DI = {minusDi:F2}");
			}
		}

		// Exit logic when trend weakens
		if (adxMain < 20)
		{
			if (Position > 0)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Exiting long position: ADX weakened to {adxMain:F2}");
			}
			else if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exiting short position: ADX weakened to {adxMain:F2}");
			}
		}
	}
}