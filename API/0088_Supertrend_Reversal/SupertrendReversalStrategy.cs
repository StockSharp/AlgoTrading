using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Supertrend Reversal Strategy.
	/// Enters long when Supertrend switches from above to below price.
	/// Enters short when Supertrend switches from below to above price.
	/// </summary>
	public class SupertrendReversalStrategy : Strategy
	{
		private readonly StrategyParam<int> _period;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;
		
		private bool? _prevIsSupertrendAbovePrice;
		
		private AverageTrueRange _atr;
		private decimal _prevHighest;
		private decimal _prevLowest;
		private decimal _prevSupertrend;
		private decimal _prevClose;
		private bool _isFirstUpdate = true;

		/// <summary>
		/// Period for Supertrend calculation.
		/// </summary>
		public int Period
		{
			get => _period.Value;
			set => _period.Value = value;
		}

		/// <summary>
		/// Multiplier for Supertrend calculation.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplier.Value;
			set => _multiplier.Value = value;
		}

		/// <summary>
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SupertrendReversalStrategy"/>.
		/// </summary>
		public SupertrendReversalStrategy()
		{
			_period = Param(nameof(Period), 10)
				.SetDisplay("Period", "Period for Supertrend calculation", "Supertrend Settings")
				.SetRange(7, 20)
				.SetCanOptimize(true);
				
			_multiplier = Param(nameof(Multiplier), 3.0m)
				.SetDisplay("Multiplier", "Multiplier for Supertrend calculation", "Supertrend Settings")
				.SetRange(2.0m, 4.0m)
				.SetCanOptimize(true);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

			// Initialize previous state
			_prevIsSupertrendAbovePrice = null;
			_isFirstUpdate = true;
			
			// Create ATR indicator for Supertrend calculation
			_atr = new AverageTrueRange { Length = Period };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind ATR indicator and process candles
			subscription
				.Bind(_atr, ProcessCandle)
				.Start();
				
			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process candle with ATR value.
		/// </summary>
		/// <param name="candle">Candle.</param>
		/// <param name="atrValue">ATR value.</param>
		private void ProcessCandle(ICandleMessage candle, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate Supertrend value
			decimal medianPrice = (candle.HighPrice + candle.LowPrice) / 2;
			decimal upperBand = medianPrice + (Multiplier * atrValue);
			decimal lowerBand = medianPrice - (Multiplier * atrValue);
			
			decimal supertrend;
			
			if (_isFirstUpdate)
			{
				_prevHighest = upperBand;
				_prevLowest = lowerBand;
				_prevSupertrend = (candle.ClosePrice <= upperBand) ? upperBand : lowerBand;
				_prevClose = candle.ClosePrice;
				_isFirstUpdate = false;
				return;
			}
			
			// Calculate current upper and lower limits
			decimal currentUpperBand = upperBand;
			decimal currentLowerBand = lowerBand;
			
			// Adjust upper band
			if (currentUpperBand < _prevHighest || _prevClose > _prevHighest)
				currentUpperBand = upperBand;
			else
				currentUpperBand = _prevHighest;
				
			// Adjust lower band
			if (currentLowerBand > _prevLowest || _prevClose < _prevLowest)
				currentLowerBand = lowerBand;
			else
				currentLowerBand = _prevLowest;
				
			// Calculate Supertrend
			if (_prevSupertrend == _prevHighest)
			{
				if (candle.ClosePrice <= currentUpperBand)
					supertrend = currentUpperBand;
				else
					supertrend = currentLowerBand;
			}
			else
			{
				if (candle.ClosePrice >= currentLowerBand)
					supertrend = currentLowerBand;
				else
					supertrend = currentUpperBand;
			}
			
			// Determine if Supertrend is above or below price
			bool isSupertrendAbovePrice = supertrend > candle.ClosePrice;
			
			// If this is the first valid calculation, just store the state
			if (_prevIsSupertrendAbovePrice == null)
			{
				_prevIsSupertrendAbovePrice = isSupertrendAbovePrice;
				
				// Update previous values for next calculation
				_prevHighest = currentUpperBand;
				_prevLowest = currentLowerBand;
				_prevSupertrend = supertrend;
				_prevClose = candle.ClosePrice;
				return;
			}
			
			// Check for Supertrend reversal
			bool supertrendSwitchedBelow = _prevIsSupertrendAbovePrice.Value && !isSupertrendAbovePrice;
			bool supertrendSwitchedAbove = !_prevIsSupertrendAbovePrice.Value && isSupertrendAbovePrice;
			
			// Long entry: Supertrend switched from above to below price
			if (supertrendSwitchedBelow && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry: Supertrend switched below price");
			}
			// Short entry: Supertrend switched from below to above price
			else if (supertrendSwitchedAbove && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry: Supertrend switched above price");
			}
			
			// Update the previous state and values
			_prevIsSupertrendAbovePrice = isSupertrendAbovePrice;
			_prevHighest = currentUpperBand;
			_prevLowest = currentLowerBand;
			_prevSupertrend = supertrend;
			_prevClose = candle.ClosePrice;
		}
	}
}