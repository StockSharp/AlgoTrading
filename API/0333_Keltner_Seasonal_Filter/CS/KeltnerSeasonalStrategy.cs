using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
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
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Initialize seasonal strength for current month
			UpdateSeasonalStrength(time);

			// Create indicators for Keltner Channel
			var ema = new ExponentialMovingAverage
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

			// Start position protection with ATR-based stop-loss
			StartProtection(
				takeProfit: new Unit(0), // No fixed take profit
				stopLoss: new Unit(AtrMultiplier, UnitTypes.Absolute)
			);
		}

		private void ProcessKeltner(ICandleMessage candle, decimal emaValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check if we need to update seasonal strength (month changed)
			var candleMonth = candle.OpenTime.Month;
			var currentMonth = CurrentTime.Month;
			if (candleMonth != currentMonth)
			{
				UpdateSeasonalStrength(CurrentTime);
			}

			// Calculate Keltner Channel bands
			decimal upperBand = emaValue + atrValue * AtrMultiplier;
			decimal lowerBand = emaValue - atrValue * AtrMultiplier;

			// Check for breakout signals with seasonal filter
			if (_currentSeasonalStrength > SeasonalThreshold)
			{
				// Strong positive seasonal bias
				if (candle.ClosePrice > upperBand && Position <= 0)
				{
					// Breakout above upper band - Buy signal
					LogInfo($"Buy signal: Breakout above Keltner upper band ({upperBand}) with positive seasonal bias ({_currentSeasonalStrength})");
					BuyMarket(Volume + Math.Abs(Position));
				}
			}
			else if (_currentSeasonalStrength < -SeasonalThreshold)
			{
				// Strong negative seasonal bias
				if (candle.ClosePrice < lowerBand && Position >= 0)
				{
					// Breakout below lower band - Sell signal
					LogInfo($"Sell signal: Breakout below Keltner lower band ({lowerBand}) with negative seasonal bias ({_currentSeasonalStrength})");
					SellMarket(Volume + Math.Abs(Position));
				}
			}

			// Exit rules based on middle line reversion
			if ((Position > 0 && candle.ClosePrice < emaValue) ||
			(Position < 0 && candle.ClosePrice > emaValue))
			{
				LogInfo($"Exit signal: Price reverted to EMA ({emaValue})");
				ClosePosition();
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
}
