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
/// Strategy that trades based on Keltner Channel breakouts with seasonal bias filter.
/// Enters position when price breaks Keltner Channel with seasonal bias confirmation.
/// </summary>
public class KeltnerSeasonalStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _seasonalThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<int, decimal> _monthlyReturns = [];
	private decimal _currentSeasonalStrength;
	private decimal _stopPrice;

	/// <summary>
	/// Strategy parameter: EMA period for Keltner Channel.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Strategy parameter: ATR period for Keltner Channel.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Strategy parameter: ATR multiplier for Keltner Channel.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Seasonal strength threshold.
	/// </summary>
	public decimal SeasonalThreshold
	{
		get => _seasonalThreshold.Value;
		set => _seasonalThreshold.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public KeltnerSeasonalStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA in Keltner Channel", "Indicator Settings");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR in Keltner Channel", "Indicator Settings");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR to set channel width", "Indicator Settings");

		_seasonalThreshold = Param(nameof(SeasonalThreshold), 0.5m)
			.SetDisplay("Seasonal Threshold", "Minimum seasonal strength to consider for trading", "Seasonal Settings");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		// Initialize seasonal returns (this would typically be loaded from historical data)
		// These are example values - in a real implementation, these would be calculated from historical data
		InitializeSeasonalData();
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

		_currentSeasonalStrength = 0;
		_stopPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create indicators for Keltner Channel
		var ema = new EMA
		{
			Length = EmaPeriod
		};

		var atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		// Create subscription for candles
		var subscription = SubscribeCandles(CandleType);

		// Bind indicators to subscription and start
		subscription
			.Bind(ema, atr, ProcessKeltner)
			.Start();

		// Add chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Evaluates the published seasonal Keltner entry conditions on the original
	/// EMA/ATR profile. Returns 1 for buy, -1 for sell and 0 for no entry.
	/// </summary>
	public static int GetEntrySignal(
		decimal seasonalStrength,
		decimal close,
		decimal ema,
		decimal atr,
		decimal multiplier,
		decimal threshold,
		decimal position)
	{
		if (seasonalStrength > threshold && close > ema + atr * multiplier && position <= 0m)
			return 1;
		if (seasonalStrength < -threshold && close < ema - atr * multiplier && position >= 0m)
			return -1;
		return 0;
	}

	private void ProcessKeltner(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Seasonality belongs to the candle being evaluated, never to host/start time.
		UpdateSeasonalStrength(candle.OpenTime);

		// Calculate Keltner Channel bands
		decimal upperBand = emaValue + atrValue * AtrMultiplier;
		decimal lowerBand = emaValue - atrValue * AtrMultiplier;

		// Stop placed AtrMultiplier x ATR from the entry close, checked on the finished candle
		if (_stopPrice > 0m &&
		((Position > 0 && candle.ClosePrice <= _stopPrice) ||
		(Position < 0 && candle.ClosePrice >= _stopPrice)))
		{
			LogInfo($"Stop exit: close {candle.ClosePrice} reached ATR stop ({_stopPrice})");
			ClosePosition();
			_stopPrice = 0m;
			return;
		}

		// The same formula is exercised by independent positive and forbidden
		// season tests on the unmodified 20/14/2/0.5 profile.
		var signal = GetEntrySignal(
			_currentSeasonalStrength, candle.ClosePrice, emaValue, atrValue,
			AtrMultiplier, SeasonalThreshold, Position);

		if (signal > 0)
		{
			LogInfo($"Buy signal: breakout above Keltner upper band ({upperBand}) with seasonal bias ({_currentSeasonalStrength})");
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice - atrValue * AtrMultiplier;
			return;
		}
		if (signal < 0)
		{
			LogInfo($"Sell signal: breakout below Keltner lower band ({lowerBand}) with seasonal bias ({_currentSeasonalStrength})");
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice + atrValue * AtrMultiplier;
			return;
		}

		// Exit rules based on middle line reversion
		if ((Position > 0 && candle.ClosePrice < emaValue) ||
		(Position < 0 && candle.ClosePrice > emaValue))
		{
			LogInfo($"Exit signal: Price reverted to EMA ({emaValue})");
			ClosePosition();
			_stopPrice = 0m;
		}
	}

	private void InitializeSeasonalData()
	{
		// Example seasonal bias by month (positive values favor longs, negative values favor shorts)
		// This would typically be calculated from historical data
		_monthlyReturns[1] = 0.8m;   // January - Strong bullish
		_monthlyReturns[2] = 0.3m;   // February - Mildly bullish
		_monthlyReturns[3] = 0.6m;   // March - Moderately bullish
		_monthlyReturns[4] = 0.7m;   // April - Moderately bullish
		_monthlyReturns[5] = 0.2m;   // May - Mildly bullish
		_monthlyReturns[6] = -0.3m;  // June - Mildly bearish
		_monthlyReturns[7] = -0.1m;  // July - Neutral to mildly bearish
		_monthlyReturns[8] = -0.4m;  // August - Moderately bearish
		_monthlyReturns[9] = -0.8m;  // September - Strong bearish
		_monthlyReturns[10] = 0.1m;  // October - Neutral to mildly bullish
		_monthlyReturns[11] = 0.9m;  // November - Strong bullish
		_monthlyReturns[12] = 0.7m;  // December - Moderately bullish
	}

	private void UpdateSeasonalStrength(DateTimeOffset time)
	{
		var month = time.Month;
		if (_monthlyReturns.TryGetValue(month, out decimal seasonalStrength))
		{
			_currentSeasonalStrength = seasonalStrength;
			LogInfo($"Updated seasonal strength for month {month}: {_currentSeasonalStrength}");
		}
		else
		{
			_currentSeasonalStrength = 0;
			LogInfo($"No seasonal data found for month {month}, setting neutral bias");
		}
	}
}
