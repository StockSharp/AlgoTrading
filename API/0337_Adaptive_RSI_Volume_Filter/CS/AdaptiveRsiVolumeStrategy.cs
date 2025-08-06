using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on Adaptive RSI with volume confirmation.
	/// The RSI period adapts based on market volatility (ATR).
	/// </summary>
	public class AdaptiveRsiVolumeStrategy : Strategy
	{
		private readonly StrategyParam<int> _minRsiPeriod;
		private readonly StrategyParam<int> _maxRsiPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<int> _volumeLookback;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _adaptiveRsiValue;
		private decimal _avgVolume;
		private int _currentRsiPeriod;
		
		// Indicators
		private RelativeStrengthIndex _rsi;
		private AverageTrueRange _atr;
		private SimpleMovingAverage _volumeSma;

		/// <summary>
		/// Strategy parameter: Minimum RSI period.
		/// </summary>
		public int MinRsiPeriod
		{
			get => _minRsiPeriod.Value;
			set => _minRsiPeriod.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Maximum RSI period.
		/// </summary>
		public int MaxRsiPeriod
		{
			get => _maxRsiPeriod.Value;
			set => _maxRsiPeriod.Value = value;
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
		/// Strategy parameter: Volume lookback period.
		/// </summary>
		public int VolumeLookback
		{
			get => _volumeLookback.Value;
			set => _volumeLookback.Value = value;
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
		public AdaptiveRsiVolumeStrategy()
		{
			_minRsiPeriod = Param(nameof(MinRsiPeriod), 10)
				.SetGreaterThanZero()
				.SetDisplay("Min RSI Period", "Minimum period for adaptive RSI", "Indicator Settings");

			_maxRsiPeriod = Param(nameof(MaxRsiPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Max RSI Period", "Maximum period for adaptive RSI", "Indicator Settings");

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR volatility calculation", "Indicator Settings");

			_volumeLookback = Param(nameof(VolumeLookback), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume Lookback", "Number of periods to calculate volume average", "Volume Settings");

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

			_adaptiveRsiValue = 50;
			_avgVolume = 0;
			_currentRsiPeriod = MaxRsiPeriod;
			_atr = default;
			_rsi = default;
			_volumeSma = default;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			_currentRsiPeriod = MaxRsiPeriod;

			// Create indicators
			_atr = new AverageTrueRange
			{
				Length = AtrPeriod
			};

			_rsi = new RelativeStrengthIndex
			{
				Length = _currentRsiPeriod
			};

			_volumeSma = new SimpleMovingAverage
			{
				Length = VolumeLookback
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);

			// Bind indicators to subscription and start
			subscription
				.BindEx(_atr, _rsi, ProcessCandle)
				.Start();

			// Add chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _rsi);
				DrawOwnTrades(area);
			}

			// Start position protection with percentage-based stop-loss
			StartProtection(
				takeProfit: new Unit(0), // No fixed take profit
				stopLoss: new Unit(2, UnitTypes.Percent) // 2% stop-loss
			);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Process volume to calculate average
			ProcessVolume(candle);

			// Calculate adaptive RSI period based on ATR
			if (atrValue.IsFinal)
			{
				decimal atr = atrValue.ToDecimal();
				
				// Normalize ATR to a value between 0 and 1 using historical range
				// This is a simplified approach - in a real implementation you would
				// track ATR range over a longer period
				decimal normalizedAtr = Math.Min(Math.Max(atr / (candle.ClosePrice * 0.1m), 0), 1);
				
				// Adjust RSI period - higher volatility (ATR) = shorter period
				int newPeriod = MaxRsiPeriod - (int)Math.Round(normalizedAtr * (MaxRsiPeriod - MinRsiPeriod));
				
				// Ensure period stays within bounds
				newPeriod = Math.Max(MinRsiPeriod, Math.Min(MaxRsiPeriod, newPeriod));
				
				// Update RSI period if changed
				if (newPeriod != _currentRsiPeriod)
				{
					_currentRsiPeriod = newPeriod;
					_rsi.Length = _currentRsiPeriod;
					
					LogInfo($"Adjusted RSI period to {_currentRsiPeriod} based on ATR ({atr})");
				}
			}

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Store RSI value
			if (rsiValue.IsFinal)
			{
				_adaptiveRsiValue = rsiValue.ToDecimal();

				// Trading logic based on RSI with volume confirmation
				if (_avgVolume > 0) // Make sure we have volume data
				{
					bool isHighVolume = candle.TotalVolume > _avgVolume;

					// Oversold condition with volume confirmation
					if (_adaptiveRsiValue < 30 && isHighVolume && Position <= 0)
					{
						LogInfo($"Buy signal: RSI oversold ({_adaptiveRsiValue}) with high volume ({candle.TotalVolume} > {_avgVolume})");
						BuyMarket(Volume + Math.Abs(Position));
					}
					// Overbought condition with volume confirmation
					else if (_adaptiveRsiValue > 70 && isHighVolume && Position >= 0)
					{
						LogInfo($"Sell signal: RSI overbought ({_adaptiveRsiValue}) with high volume ({candle.TotalVolume} > {_avgVolume})");
						SellMarket(Volume + Math.Abs(Position));
					}
				}

				// Exit logic based on RSI returning to neutral zone
				if ((Position > 0 && _adaptiveRsiValue > 50) ||
					(Position < 0 && _adaptiveRsiValue < 50))
				{
					LogInfo($"Exit signal: RSI returned to neutral zone ({_adaptiveRsiValue})");
					ClosePosition();
				}
			}
		}

		private void ProcessVolume(ICandleMessage candle)
		{
			// Process volume with SMA
			var volumeValue = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished);
			
			if (volumeValue.IsFinal)
			{
				_avgVolume = volumeValue.ToDecimal();
			}
		}
	}
}
