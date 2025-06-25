using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Volatility Breakout strategy. Enters trades when price breaks out from average price with volatility threshold.
	/// </summary>
	public class VolatilityBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _periodParam;
		private readonly StrategyParam<decimal> _multiplierParam;
		private readonly StrategyParam<DataType> _candleTypeParam;

		private SimpleMovingAverage _sma;
		private AverageTrueRange _atr;
		
		private decimal _prevSma;
		private decimal _prevAtr;

		/// <summary>
		/// Period for SMA and ATR calculations.
		/// </summary>
		public int Period
		{
			get => _periodParam.Value;
			set => _periodParam.Value = value;
		}

		/// <summary>
		/// Volatility multiplier for breakout threshold.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplierParam.Value;
			set => _multiplierParam.Value = value;
		}

		/// <summary>
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public VolatilityBreakoutStrategy()
		{
			_periodParam = Param(nameof(Period), 20)
				.SetGreaterThanZero()
				.SetDisplay("Period", "Period for SMA and ATR", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_multiplierParam = Param(nameof(Multiplier), 2.0m)
				.SetGreaterThan(0.1m)
				.SetDisplay("Multiplier", "Volatility multiplier for breakout threshold", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "Common");
		}

		/// <summary>
		/// Returns working securities.
		/// </summary>
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_sma = new SimpleMovingAverage { Length = Period };
			_atr = new AverageTrueRange { Length = Period };
			
			// Reset state
			_prevSma = 0;
			_prevAtr = 0;

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_sma, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _sma);
				DrawOwnTrades(area);
			}
			
			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(Multiplier, UnitTypes.Absolute) // Stop loss at 2*ATR
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Save values for the next candle
			var currentSma = smaValue;
			var currentAtr = atrValue;
			
			// Skip first candle after indicators become formed
			if (_prevSma == 0 || _prevAtr == 0)
			{
				_prevSma = currentSma;
				_prevAtr = currentAtr;
				return;
			}
			
			// Calculate volatility threshold
			var threshold = Multiplier * currentAtr;
			
			// Check for long setup - price breaks above SMA + threshold
			if (candle.ClosePrice > currentSma + threshold && Position <= 0)
			{
				// Close any short position and open long
				BuyMarket(Volume + Math.Abs(Position));
			}
			// Check for short setup - price breaks below SMA - threshold
			else if (candle.ClosePrice < currentSma - threshold && Position >= 0)
			{
				// Close any long position and open short
				SellMarket(Volume + Math.Abs(Position));
			}
			
			// Update previous values for next candle
			_prevSma = currentSma;
			_prevAtr = currentAtr;
		}
	}
}