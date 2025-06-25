using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades on Bollinger Bands Width expansion.
	/// It identifies periods of increasing volatility (widening Bollinger Bands)
	/// and trades in the direction of the trend as identified by price position relative to the middle band.
	/// </summary>
	public class BollingerBandWidthStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerPeriod;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _prevWidth;

		/// <summary>
		/// Period for Bollinger Bands calculation (default: 20)
		/// </summary>
		public int BollingerPeriod
		{
			get => _bollingerPeriod.Value;
			set => _bollingerPeriod.Value = value;
		}

		/// <summary>
		/// Deviation for Bollinger Bands calculation (default: 2.0)
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop-loss calculation (default: 2.0)
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Type of candles used for strategy calculation
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize the Bollinger Band Width strategy
		/// </summary>
		public BollingerBandWidthStrategy()
		{
			_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
				.SetDisplayName("Bollinger Period")
				.SetDescription("Period for Bollinger Bands calculation")
				.SetGroup("Bollinger Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetDisplayName("Bollinger Deviation")
				.SetDescription("Deviation for Bollinger Bands calculation")
				.SetGroup("Bollinger Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 2.5m, 0.25m);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
				.SetDisplayName("ATR Multiplier")
				.SetDescription("ATR multiplier for stop-loss calculation")
				.SetGroup("Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplayName("Candle Type")
				.SetDescription("Type of candles to use")
				.SetGroup("Data");
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

			// Reset state variables
			_prevWidth = 0;

			// Create indicators
			var bollinger = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};

			var atr = new AverageTrueRange { Length = BollingerPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(bollinger, atr, ProcessCandle)
				.Start();

			// Configure chart
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, bollinger);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process candle and calculate Bollinger Band Width
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate Bollinger Band Width
			decimal bbWidth = upperBand - lowerBand;
			
			// Initialize _prevWidth on first formed candle
			if (_prevWidth == 0)
			{
				_prevWidth = bbWidth;
				return;
			}

			// Check if Bollinger Band Width is expanding (increasing)
			bool isBBWidthExpanding = bbWidth > _prevWidth;
			
			// Determine price position relative to middle band for trend direction
			bool isPriceAboveMiddleBand = candle.ClosePrice > middleBand;
			
			// Calculate stop-loss amount based on ATR
			decimal stopLossAmount = atrValue * AtrMultiplier;

			if (Position == 0)
			{
				// No position - check for entry signals
				if (isBBWidthExpanding)
				{
					if (isPriceAboveMiddleBand)
					{
						// BB Width expanding and price above middle band - buy (long)
						BuyMarket(Volume);
					}
					else
					{
						// BB Width expanding and price below middle band - sell (short)
						SellMarket(Volume);
					}
				}
			}
			else if (Position > 0)
			{
				// Long position - check for exit signal
				if (!isBBWidthExpanding)
				{
					// BB Width contracting - exit long
					SellMarket(Position);
				}
			}
			else if (Position < 0)
			{
				// Short position - check for exit signal
				if (!isBBWidthExpanding)
				{
					// BB Width contracting - exit short
					BuyMarket(Math.Abs(Position));
				}
			}

			// Update previous BB Width
			_prevWidth = bbWidth;
		}
	}
}
