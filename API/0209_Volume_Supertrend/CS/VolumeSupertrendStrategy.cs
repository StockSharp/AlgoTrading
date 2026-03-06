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
/// Strategy based on Volume and Supertrend indicators
/// </summary>
public class VolumeSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private decimal _supertrendValue;
	private int _supertrendDirection;
	private int _cooldown;

	/// <summary>
	/// Volume average period
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
	}

	/// <summary>
	/// Supertrend ATR period
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// Supertrend multiplier
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for strategy
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Constructor
	/// </summary>
	public VolumeSupertrendStrategy()
	{
		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("Volume Avg Period", "Period for volume average calculation", "Volume")
			;

		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetRange(5, 30)
			.SetDisplay("Supertrend Period", "ATR period for Supertrend", "Supertrend")
			;

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetRange(1m, 5m)
			.SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend calculation", "Supertrend")
			;

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management")
			
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

		_supertrendValue = default;
		_supertrendDirection = default;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Initialize indicators
		var volumeMA = new SMA { Length = VolumeAvgPeriod };
		var supertrend = new SuperTrend
		{
			Length = SupertrendPeriod,
			Multiplier = SupertrendMultiplier
		};

		// Create subscription
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, volumeMA, ProcessSignals)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawIndicator(area, volumeMA);

			// Create a secondary area for volume
			var volumeArea = CreateChartArea();
			if (volumeArea != null)
			{
				// Use Volume indicator to visualize volume
				var volumeIndicator = new VolumeIndicator();
				DrawIndicator(volumeArea, volumeIndicator);
			}

			DrawOwnTrades(area);
		}
	}

	private void ProcessSignals(ICandleMessage candle, IIndicatorValue supertrendValue, IIndicatorValue volumeValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (supertrendValue is not SuperTrendIndicatorValue st)
			return;

		var volumeAvg = volumeValue.ToDecimal();
		if (volumeAvg <= 0)
			return;

		var currentVolume = candle.TotalVolume;
		var supertrendDirection = st.IsUpTrend ? 1 : -1;
		if (_cooldown > 0)
			_cooldown--;

		// Trading logic:
		// Long: Volume > Avg(Volume) && Price > Supertrend (volume surge with uptrend)
		// Short: Volume > Avg(Volume) && Price < Supertrend (volume surge with downtrend)

		var volumeSurge = currentVolume >= volumeAvg;

		if (_cooldown == 0 && volumeSurge && supertrendDirection == 1 && Position <= 0)
		{
			// Buy signal - Volume surge with Supertrend uptrend
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = 10;
		}
		else if (_cooldown == 0 && volumeSurge && supertrendDirection == -1 && Position >= 0)
		{
			// Sell signal - Volume surge with Supertrend downtrend
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = 10;
		}
		// Exit conditions based on Supertrend reversal
		else if (Position > 0 && supertrendDirection == -1)
		{
			// Exit long position when Supertrend turns down
			SellMarket(Position);
			_cooldown = 10;
		}
		else if (Position < 0 && supertrendDirection == 1)
		{
			// Exit short position when Supertrend turns up
			BuyMarket(Math.Abs(Position));
			_cooldown = 10;
		}
	}
}
