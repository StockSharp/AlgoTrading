using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades breakouts based on historical volatility.
	/// It calculates price levels for breakouts using the historical volatility of the instrument
	/// and enters positions when price breaks above or below those levels.
	/// </summary>
	public class HvBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _hvPeriod;
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _referencePrice;
		private decimal _historicalVolatility;
		private bool _isReferenceSet;

		/// <summary>
		/// Period for Historical Volatility calculation (default: 20)
		/// </summary>
		public int HvPeriod
		{
			get => _hvPeriod.Value;
			set => _hvPeriod.Value = value;
		}

		/// <summary>
		/// Period for Moving Average calculation for exit (default: 20)
		/// </summary>
		public int MAPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// Stop-loss as percentage from entry price (default: 2%)
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
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
		/// Initialize the Historical Volatility Breakout strategy
		/// </summary>
		public HvBreakoutStrategy()
		{
			_hvPeriod = Param(nameof(HvPeriod), 20)
				.SetDisplay("HV Period", "Period for Historical Volatility calculation", "Volatility Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_maPeriod = Param(nameof(MAPeriod), 20)
				.SetDisplay("MA Period", "Period for Moving Average calculation for exit", "Exit Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "Data");
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
			_referencePrice = 0;
			_historicalVolatility = 0;
			_isReferenceSet = false;

			// Create indicators
			var standardDeviation = new StandardDeviation { Length = HvPeriod };
			var sma = new SimpleMovingAverage { Length = MAPeriod };
			
			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(standardDeviation, sma, ProcessCandle)
				.Start();

			// Configure chart
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, sma);
				DrawIndicator(area, standardDeviation);
				DrawOwnTrades(area);
			}

			// Setup protection with stop-loss
			StartProtection(
				new Unit(0), // No take profit
				new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss as percentage of entry price
			);
		}

		/// <summary>
		/// Process candle and check for HV breakout signals
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, decimal? stdDevValue, decimal? smaValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate historical volatility based on standard deviation
			// HV is annualized by multiplying by sqrt(252) for daily data
			// Note: We're using a simplified approach for demonstration
			_historicalVolatility = stdDevValue / candle.ClosePrice;

			// On first formed candle, set reference price
			if (!_isReferenceSet)
			{
				_referencePrice = candle.ClosePrice;
				_isReferenceSet = true;
				return;
			}

			// Calculate breakout levels
			decimal upperBreakoutLevel = _referencePrice * (1 + _historicalVolatility);
			decimal lowerBreakoutLevel = _referencePrice * (1 - _historicalVolatility);

			if (Position == 0)
			{
				// No position - check for entry signals
				if (candle.ClosePrice > upperBreakoutLevel)
				{
					// Price broke above upper level - buy (long)
					BuyMarket(Volume);
					
					// Update reference price after breakout
					_referencePrice = candle.ClosePrice;
				}
				else if (candle.ClosePrice < lowerBreakoutLevel)
				{
					// Price broke below lower level - sell (short)
					SellMarket(Volume);
					
					// Update reference price after breakout
					_referencePrice = candle.ClosePrice;
				}
			}
			else if (Position > 0)
			{
				// Long position - check for exit signal
				if (candle.ClosePrice < smaValue)
				{
					// Price below MA - exit long
					SellMarket(Position);
				}
			}
			else if (Position < 0)
			{
				// Short position - check for exit signal
				if (candle.ClosePrice > smaValue)
				{
					// Price above MA - exit short
					BuyMarket(Math.Abs(Position));
				}
			}
		}
	}
}
