using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on breakouts of Bollinger Bands with adaptively adjusted parameters
	/// based on market volatility.
	/// </summary>
	public class AdaptiveBollingerBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _minBollingerPeriod;
		private readonly StrategyParam<int> _maxBollingerPeriod;
		private readonly StrategyParam<decimal> _minBollingerDeviation;
		private readonly StrategyParam<decimal> _maxBollingerDeviation;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<DataType> _candleType;

		private int _currentBollingerPeriod;
		private decimal _currentBollingerDeviation;
		private BollingerBands _bollinger;
		private AverageTrueRange _atr;

		private decimal _atrSum;
		private int _atrCount;

		/// <summary>
		/// Strategy parameter: Minimum Bollinger period.
		/// </summary>
		public int MinBollingerPeriod
		{
			get => _minBollingerPeriod.Value;
			set => _minBollingerPeriod.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Maximum Bollinger period.
		/// </summary>
		public int MaxBollingerPeriod
		{
			get => _maxBollingerPeriod.Value;
			set => _maxBollingerPeriod.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Minimum Bollinger deviation.
		/// </summary>
		public decimal MinBollingerDeviation
		{
			get => _minBollingerDeviation.Value;
			set => _minBollingerDeviation.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Maximum Bollinger deviation.
		/// </summary>
		public decimal MaxBollingerDeviation
		{
			get => _maxBollingerDeviation.Value;
			set => _maxBollingerDeviation.Value = value;
		}

		/// <summary>
		/// Strategy parameter: ATR period for volatility calculation.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
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
		public AdaptiveBollingerBreakoutStrategy()
		{
			_minBollingerPeriod = Param(nameof(MinBollingerPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Min Bollinger Period", "Minimum period for adaptive Bollinger Bands", "Indicator Settings");

			_maxBollingerPeriod = Param(nameof(MaxBollingerPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Max Bollinger Period", "Maximum period for adaptive Bollinger Bands", "Indicator Settings");

			_minBollingerDeviation = Param(nameof(MinBollingerDeviation), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Min Bollinger Deviation", "Minimum standard deviation multiplier", "Indicator Settings");

			_maxBollingerDeviation = Param(nameof(MaxBollingerDeviation), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Max Bollinger Deviation", "Maximum standard deviation multiplier", "Indicator Settings");

			_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR volatility calculation", "Indicator Settings");

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

			_currentBollingerPeriod = MaxBollingerPeriod; // Start with maximum period
			_currentBollingerDeviation = MinBollingerDeviation; // Start with minimum deviation
			_atr = default;
			_bollinger = default;
			_atrSum = default;
			_atrCount = default;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create ATR indicator for volatility measurement
			_atr = new AverageTrueRange
			{
				Length = AtrPeriod
			};

			// Create Bollinger Bands indicator with initial parameters
			_bollinger = new BollingerBands
			{
				Length = _currentBollingerPeriod,
				Width = _currentBollingerDeviation
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);

			// Bind indicators to subscription and start
			subscription
			.BindEx(_atr, _bollinger, ProcessIndicators)
			.Start();

			// Add chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _bollinger);
				DrawOwnTrades(area);
			}

			// Start position protection with ATR-based stop-loss
			StartProtection(
			takeProfit: new Unit(0), // No fixed take profit
			stopLoss: new Unit(2, UnitTypes.Absolute) // 2 ATR stop-loss
			);
		}

		private void ProcessIndicators(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue bollingerValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
			return;

			// --- ATR logic (was ProcessAtr) ---
			if (atrValue.IsFinal)
			{
				decimal atr = atrValue.ToDecimal();
				// Maintain running average for ATR
				_atrSum += atr;
				_atrCount++;
				decimal avgAtr = _atrCount > 0 ? _atrSum / _atrCount : atr;
				decimal volatilityRatio = Math.Min(Math.Max(atr / (candle.ClosePrice * 0.01m), 0), 1);

				// Higher volatility = shorter period and wider bands
				int newPeriod = MaxBollingerPeriod - (int)Math.Round(volatilityRatio * (MaxBollingerPeriod - MinBollingerPeriod));
				decimal newDeviation = MinBollingerDeviation + volatilityRatio * (MaxBollingerDeviation - MinBollingerDeviation);

				// Ensure parameters stay within bounds
				newPeriod = Math.Max(MinBollingerPeriod, Math.Min(MaxBollingerPeriod, newPeriod));
				newDeviation = Math.Max(MinBollingerDeviation, Math.Min(MaxBollingerDeviation, newDeviation));

				// Update Bollinger parameters if changed
				if (newPeriod != _currentBollingerPeriod || newDeviation != _currentBollingerDeviation)
				{
					_currentBollingerPeriod = newPeriod;
					_currentBollingerDeviation = newDeviation;

					_bollinger.Length = _currentBollingerPeriod;
					_bollinger.Width = _currentBollingerDeviation;

					LogInfo($"Adjusted Bollinger parameters: Period={_currentBollingerPeriod}, Deviation={_currentBollingerDeviation:F2} based on ATR={atr:F6}");
				}
			}

			// --- Bollinger logic (was ProcessBollinger) ---
			if (!IsFormedAndOnlineAndAllowTrading())
			return;

			if (bollingerValue.IsFinal && _atr.IsFormed)
			{
				decimal atrVal = atrValue.ToDecimal(); // use current ATR value
				// use running average
				bool isHighVolatility = atrVal > (_atrCount > 0 ? _atrSum / _atrCount : atrVal);

				var bollingerTyped = (BollingerBandsValue)bollingerValue;

				if (bollingerTyped.UpBand is not decimal upperBand ||
				bollingerTyped.LowBand is not decimal lowerBand ||
				bollingerTyped.MovingAverage is not decimal middleBand)
				{
					return; // Not enough data to calculate bands
				}

				if (isHighVolatility)
				{
					// Breakout above upper band - Sell signal
					if (candle.ClosePrice > upperBand && Position >= 0)
					{
						LogInfo($"Sell signal: Price ({candle.ClosePrice}) broke above upper Bollinger Band ({upperBand}) in high volatility");
						SellMarket(Volume + Math.Abs(Position));
					}
					// Breakout below lower band - Buy signal
					else if (candle.ClosePrice < lowerBand && Position <= 0)
					{
						LogInfo($"Buy signal: Price ({candle.ClosePrice}) broke below lower Bollinger Band ({lowerBand}) in high volatility");
						BuyMarket(Volume + Math.Abs(Position));
					}
				}

				// Exit logic based on middle band reversion
				if ((Position > 0 && candle.ClosePrice > middleBand) || 
				(Position < 0 && candle.ClosePrice < middleBand))
				{
					LogInfo($"Exit signal: Price ({candle.ClosePrice}) reverted to middle band ({middleBand})");
					ClosePosition();
				}
			}
		}
	}
}
