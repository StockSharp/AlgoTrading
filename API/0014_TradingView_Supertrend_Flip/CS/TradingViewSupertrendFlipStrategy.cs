using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Supertrend indicator flips with volume confirmation.
/// It detects when Supertrend flips from above price to below (bullish) or from below price to above (bearish)
/// and confirms the signal with above-average volume.
/// </summary>
public class TradingViewSupertrendFlipStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<DataType> _candleType;

	// State tracking
	private decimal _prevSupertrendValue;
	private bool _prevIsPriceAboveSupertrend;
	private decimal _avgVolume;
	private decimal _supertrendValue;
	private readonly Queue<decimal> _volumeQueue = [];

	/// <summary>
	/// Period for Supertrend calculation.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for Supertrend calculation.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>
	/// Period for volume average calculation.
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
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
	/// Initialize the TradingView Supertrend Flip strategy.
	/// </summary>
	public TradingViewSupertrendFlipStrategy()
	{
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetDisplay("Supertrend Period", "Period for Supertrend calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 14, 1);

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
			.SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2.0m, 4.0m, 0.5m);

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetDisplay("Volume Avg Period", "Period for volume average calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevSupertrendValue = 0;
		_prevIsPriceAboveSupertrend = false;
		_avgVolume = 0;
		_supertrendValue = 0;
		_volumeQueue.Clear();

	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create custom indicators
		// Since StockSharp doesn't have a built-in Supertrend indicator,
		// we'll use ATR and customize the calculation in the handler
		var atr = new AverageTrueRange { Length = SupertrendPeriod };
		var sma = new SimpleMovingAverage { Length = VolumeAvgPeriod }; // For volume average

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, sma, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal volumeAvgValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Calculate Supertrend components
		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2;
		var basicUpperBand = medianPrice + SupertrendMultiplier * atrValue;
		var basicLowerBand = medianPrice - SupertrendMultiplier * atrValue;

		// If this is the first processed candle, initialize values
		if (_prevSupertrendValue == 0)
		{
			_supertrendValue = candle.ClosePrice > medianPrice ? basicLowerBand : basicUpperBand;
			_prevSupertrendValue = _supertrendValue;
			_prevIsPriceAboveSupertrend = candle.ClosePrice > _supertrendValue;
			
			// Initialize volume tracking
			_volumeQueue.Enqueue(candle.TotalVolume);
			_avgVolume = candle.TotalVolume;
			return;
		}

		// Determine current Supertrend value based on previous value and current price
		if (_prevSupertrendValue <= candle.HighPrice)
		{
			// Previous Supertrend was resistance
			_supertrendValue = Math.Max(basicLowerBand, _prevSupertrendValue);
		}
		else if (_prevSupertrendValue >= candle.LowPrice)
		{
			// Previous Supertrend was support
			_supertrendValue = Math.Min(basicUpperBand, _prevSupertrendValue);
		}
		else
		{
			// Price crossed the Supertrend
			_supertrendValue = candle.ClosePrice > _prevSupertrendValue ? basicLowerBand : basicUpperBand;
		}

		// Update volume tracking
		_volumeQueue.Enqueue(candle.TotalVolume);
		if (_volumeQueue.Count > VolumeAvgPeriod)
			_volumeQueue.Dequeue();
			
		// Calculate average volume
		decimal totalVolume = 0;
		foreach (var vol in _volumeQueue)
			totalVolume += vol;
		_avgVolume = totalVolume / _volumeQueue.Count;

		// Check if price is above or below Supertrend
		var isPriceAboveSupertrend = candle.ClosePrice > _supertrendValue;
		
		// Check for Supertrend flip
		var isFlippedBullish = !_prevIsPriceAboveSupertrend && isPriceAboveSupertrend;
		var isFlippedBearish = _prevIsPriceAboveSupertrend && !isPriceAboveSupertrend;
		
		// Check volume confirmation
		var isHighVolume = candle.TotalVolume > _avgVolume;

		// Trading logic with volume confirmation
		if (isFlippedBullish && isHighVolume && Position <= 0)
		{
			// Supertrend flipped bullish with high volume - Buy signal
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal: Supertrend flipped bullish with volume {candle.TotalVolume} (avg: {_avgVolume})");
		}
		else if (isFlippedBearish && isHighVolume && Position >= 0)
		{
			// Supertrend flipped bearish with high volume - Sell signal
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal: Supertrend flipped bearish with volume {candle.TotalVolume} (avg: {_avgVolume})");
		}
		// Exit logic
		else if (isFlippedBearish && Position > 0)
		{
			// Supertrend flipped bearish - Exit long position
			SellMarket(Position);
			LogInfo($"Exit long: Supertrend flipped bearish");
		}
		else if (isFlippedBullish && Position < 0)
		{
			// Supertrend flipped bullish - Exit short position
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short: Supertrend flipped bullish");
		}

		// Update previous values for next candle
		_prevSupertrendValue = _supertrendValue;
		_prevIsPriceAboveSupertrend = isPriceAboveSupertrend;
	}
}