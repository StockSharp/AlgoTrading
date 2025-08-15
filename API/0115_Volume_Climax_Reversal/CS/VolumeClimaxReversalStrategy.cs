namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

/// <summary>
/// Volume Climax Reversal strategy.
/// </summary>
public class VolumeClimaxReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	
	private SimpleMovingAverage _ma;
	private SimpleMovingAverage _volumeAverage;
	private AverageTrueRange _atr;

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for volume average calculation.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// Volume multiplier for signal detection.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Moving average period for trend determination.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal ATRMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VolumeClimaxReversalStrategy"/>.
	/// </summary>
	public VolumeClimaxReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Candles");

		_volumePeriod = Param(nameof(VolumePeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("Volume Period", "Period for volume average calculation", "Volume")
			.SetCanOptimize(true);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 3m)
			.SetRange(1.5m, 5m)
			.SetDisplay("Volume Multiplier", "Volume threshold as multiplier of average volume", "Volume")
			.SetCanOptimize(true);

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("MA Period", "Period for moving average calculation", "Moving Average")
			.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(ATRMultiplier), 2m)
			.SetRange(1m, 5m)
			.SetDisplay("ATR Multiplier", "Multiplier for ATR to calculate stop loss", "Risk")
			.SetCanOptimize(true);
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
		_ma = new SimpleMovingAverage { Length = MAPeriod };
		_volumeAverage = new SimpleMovingAverage { Length = VolumePeriod };
		_atr = new AverageTrueRange { Length = VolumePeriod };

		// Create and subscribe to candles
		var subscription = SubscribeCandles(CandleType);
		
		// Use BindEx to process both price and volume
		subscription
			.Bind(_ma, _atr, ProcessCandle)
			.Start();

		// Set up chart if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal atrValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Process indicators
		var volumeAverageValue = _volumeAverage.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Get current candle information
		decimal currentVolume = candle.TotalVolume;
		bool isBullishCandle = candle.ClosePrice > candle.OpenPrice;
		bool isBearishCandle = candle.ClosePrice < candle.OpenPrice;

		// Check for volume climax (volume spike)
		bool isVolumeClimaxDetected = currentVolume > volumeAverageValue * VolumeMultiplier;

		if (isVolumeClimaxDetected)
		{
			LogInfo($"Volume climax detected: {currentVolume} > {volumeAverageValue} * {VolumeMultiplier}");

			// Bullish reversal: High volume + bearish candle + price below MA
			if (isBearishCandle && candle.ClosePrice < maValue && Position <= 0)
			{
				LogInfo("Bullish reversal signal detected");
				BuyMarket(Volume + Math.Abs(Position));
			}
			// Bearish reversal: High volume + bullish candle + price above MA
			else if (isBullishCandle && candle.ClosePrice > maValue && Position >= 0)
			{
				LogInfo("Bearish reversal signal detected");
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		// Exit logic - Price crosses MA
		if (Position > 0 && candle.ClosePrice < maValue)
		{
			LogInfo("Exit long: Price moved below MA");
			ClosePosition();
		}
		else if (Position < 0 && candle.ClosePrice > maValue)
		{
			LogInfo("Exit short: Price moved above MA");
			ClosePosition();
		}
	}
}