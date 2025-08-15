using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining VWAP and Volume indicators.
/// Buys/sells on VWAP breakouts confirmed by above-average volume.
/// </summary>
public class VwapVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeMA;

	/// <summary>
	/// Period for volume moving average.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// Volume threshold as percentage of average volume.
	/// </summary>
	public decimal VolumeThreshold
	{
		get => _volumeThreshold.Value;
		set => _volumeThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy.
	/// </summary>
			public VwapVolumeStrategy()
			{
					_volumePeriod = Param(nameof(VolumePeriod), 20)
							.SetGreaterThanZero()
							.SetDisplay("Volume MA Period", "Period for volume moving average", "Indicators")
							.SetCanOptimize(true)
							.SetOptimize(10, 50, 10);

					_volumeThreshold = Param(nameof(VolumeThreshold), 1.5m)
							.SetGreaterThanZero()
							.SetDisplay("Volume Threshold", "Multiplier for average volume to confirm signal", "Trading Levels")
							.SetCanOptimize(true)
							.SetOptimize(1.2m, 2.0m, 0.2m);

					_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
							.SetGreaterThanZero()
							.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
							.SetCanOptimize(true)
							.SetOptimize(1.0m, 3.0m, 0.5m);

					_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
							.SetDisplay("Candle Type", "Type of candles to use", "General");

					_volumeMA = new SimpleMovingAverage { Length = VolumePeriod };
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

					_volumeMA = new SimpleMovingAverage { Length = VolumePeriod };
			}

			/// <inheritdoc />
			protected override void OnStarted(DateTimeOffset time)
			{
					base.OnStarted(time);

					// Create indicators
					var vwap = new VolumeWeightedMovingAverage();

					// Create subscription
					var subscription = SubscribeCandles(CandleType);

		// Create custom bind for processing VWAP and volume data
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Enable stop-loss
		StartProtection(
			takeProfit: null,
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			isStopTrailing: false,
			useMarketOrders: true
		);

		// Setup chart if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			
			// Create second area for volume
			var volumeArea = CreateChartArea();
			DrawIndicator(volumeArea, _volumeMA);
			
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process volume with indicator
		var volumeMA = _volumeMA.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();

		// Calculate VWAP manually for the current candle
		decimal vwap = 0;
		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3;
		
		if (candle.TotalVolume > 0)
		{
			// Simple VWAP calculation for a single candle
			vwap = typicalPrice;
		}

		// Skip if volume MA is not formed yet
		if (!_volumeMA.IsFormed)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Check if volume is above threshold
		bool isHighVolume = candle.TotalVolume > volumeMA * VolumeThreshold;

		// Trading logic
		if (candle.ClosePrice > vwap && isHighVolume && Position <= 0)
		{
			// Price breaks above VWAP with high volume - Buy
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (candle.ClosePrice < vwap && isHighVolume && Position >= 0)
		{
			// Price breaks below VWAP with high volume - Sell
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (Position > 0 && candle.ClosePrice < vwap)
		{
			// Exit long position when price crosses below VWAP
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && candle.ClosePrice > vwap)
		{
			// Exit short position when price crosses above VWAP
			BuyMarket(Math.Abs(Position));
		}
	}
}
