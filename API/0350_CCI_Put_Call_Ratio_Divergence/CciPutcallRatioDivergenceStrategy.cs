using Ecng.Common;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// CCI strategy with Put/Call Ratio Divergence.
	/// </summary>
	public class CciPutCallRatioDivergenceStrategy : Strategy
	{
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		
		private CommodityChannelIndex _cci;
		private AverageTrueRange _atr;
		
		private decimal _prevPcr;
		private decimal _currentPcr;
		private decimal _prevPrice;

		/// <summary>
		/// CCI Period.
		/// </summary>
		public int CciPeriod
		{
			get => _cciPeriod.Value;
			set => _cciPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop loss.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
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
		/// Initialize <see cref="CciPutCallRatioDivergenceStrategy"/>.
		/// </summary>
		public CciPutCallRatioDivergenceStrategy()
		{
			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetRange(10, 50)
				.SetCanOptimize(true)
				.SetDisplay("CCI Period", "Period for CCI calculation", "Indicators");

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetRange(1m, 5m)
				.SetCanOptimize(true)
				.SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop loss", "Risk Management");

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

			// Create indicators
			_cci = new CommodityChannelIndex
			{
				Length = CciPeriod
			};

			_atr = new AverageTrueRange
			{
				Length = 14 // Standard ATR period
			};

			// Initialize state variables
			_prevPcr = 0;
			_currentPcr = 0;
			_prevPrice = 0;

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators
			subscription
				.Bind(_cci, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _cci);
				DrawIndicator(area, _atr);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal cci, decimal atr)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Get current price
			var price = candle.ClosePrice;

			// Simulate Put/Call Ratio (in real implementation, this would come from options data)
			UpdatePutCallRatio(candle);

			// For first candle just initialize values
			if (_prevPrice == 0)
			{
				_prevPrice = price;
				_prevPcr = _currentPcr;
				return;
			}

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check for divergences
			bool bullishDivergence = price < _prevPrice && _currentPcr > _prevPcr;
			bool bearishDivergence = price > _prevPrice && _currentPcr < _prevPcr;

			// Entry logic - using CCI with PCR divergence
			if (cci < -100 && bullishDivergence && Position <= 0)
			{
				// CCI oversold with bullish PCR divergence - Long entry
				BuyMarket(Volume);
				LogInfo($"Buy Signal: CCI={cci}, PCR={_currentPcr}, Price={price}, Bullish Divergence");
			}
			else if (cci > 100 && bearishDivergence && Position >= 0)
			{
				// CCI overbought with bearish PCR divergence - Short entry
				SellMarket(Volume);
				LogInfo($"Sell Signal: CCI={cci}, PCR={_currentPcr}, Price={price}, Bearish Divergence");
			}

			// Exit logic
			if (Position > 0 && cci > 0)
			{
				// Exit long position when CCI crosses above zero
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit Long: CCI={cci}");
			}
			else if (Position < 0 && cci < 0)
			{
				// Exit short position when CCI crosses below zero
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit Short: CCI={cci}");
			}

			// Dynamic stop loss using ATR
			if (Position != 0)
			{
				decimal stopDistance = atr * AtrMultiplier;
				
				if (Position > 0)
				{
					// For long positions, set stop below entry price - ATR*multiplier
					decimal stopPrice = price - stopDistance;
					UpdateStopLoss(stopPrice);
				}
				else if (Position < 0)
				{
					// For short positions, set stop above entry price + ATR*multiplier
					decimal stopPrice = price + stopDistance;
					UpdateStopLoss(stopPrice);
				}
			}

			// Update previous values
			_prevPrice = price;
			_prevPcr = _currentPcr;
		}

		private void UpdatePutCallRatio(ICandleMessage candle)
		{
			// This is a placeholder for real Put/Call Ratio data
			// In a real implementation, this would connect to an options data provider
			
			// Base PCR on price movement (inverse relation usually exists)
			bool priceUp = candle.OpenPrice < candle.ClosePrice;
			decimal priceChange = Math.Abs((candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice);
			
			if (priceUp)
			{
				// When price rises, PCR often falls (less put buying)
				_currentPcr = 0.7m - priceChange + (decimal)(RandomGen.GetDouble() * 0.2);
			}
			else
			{
				// When price falls, PCR often rises (more put buying for protection)
				_currentPcr = 1.0m + priceChange + (decimal)(RandomGen.GetDouble() * 0.3);
			}
			
			// Add some randomness for market events
			if (RandomGen.GetDouble() > 0.9)
			{
				// Occasional PCR spikes
				_currentPcr *= 1.3m;
			}
			
			// Keep PCR in realistic bounds
			_currentPcr = Math.Max(0.5m, Math.Min(2.0m, _currentPcr));
		}

		private void UpdateStopLoss(decimal stopPrice)
		{
			// In a real implementation, this would update the stop loss level
			// This could be done via order modification or canceling existing stops and placing new ones
			
			// For this example, we'll just log the new stop level
			LogInfo($"Updated Stop Loss: {stopPrice}");
		}
	}
}
