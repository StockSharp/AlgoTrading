using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Double Bottom reversal strategy: looks for two similar bottoms with confirmation.
	/// This pattern often indicates a trend reversal from bearish to bullish.
	/// </summary>
	public class DoubleBottomStrategy : Strategy
	{
		private readonly StrategyParam<int> _distanceParam;
		private readonly StrategyParam<decimal> _similarityPercent;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private decimal? _firstBottomLow;
		private decimal? _secondBottomLow;
		private int _barsSinceFirstBottom;
		private bool _patternConfirmed;
		
		private Lowest _lowestIndicator;

		/// <summary>
		/// Distance between bottoms in bars.
		/// </summary>
		public int Distance
		{
			get => _distanceParam.Value;
			set => _distanceParam.Value = value;
		}

		/// <summary>
		/// Maximum percent difference between two bottoms to consider them similar.
		/// </summary>
		public decimal SimilarityPercent
		{
			get => _similarityPercent.Value;
			set => _similarityPercent.Value = value;
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
		/// Stop-loss percentage below the lower of the two bottoms.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DoubleBottomStrategy"/>.
		/// </summary>
		public DoubleBottomStrategy()
		{
			_distanceParam = Param(nameof(Distance), 5)
				.SetRange(3, 15)
				.SetDisplay("Distance between bottoms", "Number of bars between two bottoms", "Pattern Parameters")
				.SetCanOptimize(true);

			_similarityPercent = Param(nameof(SimilarityPercent), 2.0m)
				.SetRange(0.5m, 5.0m)
				.SetDisplay("Similarity %", "Maximum percentage difference between two bottoms", "Pattern Parameters")
				.SetCanOptimize(true);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
				.SetRange(0.5m, 3.0m)
				.SetDisplay("Stop Loss %", "Percentage below bottom for stop-loss", "Risk Management")
				.SetCanOptimize(true);
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

			_firstBottomLow = null;
			_secondBottomLow = null;
			_barsSinceFirstBottom = 0;
			_patternConfirmed = false;

			// Create indicator to find lowest values
			_lowestIndicator = new Lowest { Length = Distance * 2 };

			// Subscribe to candles
			var subscription = SubscribeCandles(CandleType);

			// Bind candle processing 
			subscription
				.Bind(ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				new Unit(0, UnitTypes.Absolute), // No take profit (manual exit)
				new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss at defined percentage
				false // No trailing
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Process the candle with the Lowest indicator
			var lowestValue = _lowestIndicator.Process(candle).GetValue<decimal>();

			// If strategy is not ready yet, return
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Already in position, no need to search for new patterns
			if (Position > 0)
				return;

			// If we have a confirmed pattern and price rises above resistance
			if (_patternConfirmed && candle.ClosePrice > candle.OpenPrice)
			{
				// Buy signal - Double Bottom with confirmation candle
				BuyMarket(Volume);
				LogInfo($"Double Bottom signal: Buy at {candle.ClosePrice}, Stop Loss at {Math.Min(_firstBottomLow.Value, _secondBottomLow.Value) * (1 - StopLossPercent / 100)}");
				
				// Reset pattern detection
				_patternConfirmed = false;
				_firstBottomLow = null;
				_secondBottomLow = null;
				_barsSinceFirstBottom = 0;
				return;
			}

			// Pattern detection logic
			if (_firstBottomLow == null)
			{
				// Looking for first bottom
				if (candle.LowPrice == lowestValue)
				{
					_firstBottomLow = candle.LowPrice;
					_barsSinceFirstBottom = 0;
					LogInfo($"Potential first bottom detected at price {_firstBottomLow}");
				}
			}
			else
			{
				_barsSinceFirstBottom++;

				// If we're at the appropriate distance, check for second bottom
				if (_barsSinceFirstBottom >= Distance && _secondBottomLow == null)
				{
					// Check if current low is close to first bottom
					var priceDifference = Math.Abs((candle.LowPrice - _firstBottomLow.Value) / _firstBottomLow.Value * 100);
					
					if (priceDifference <= SimilarityPercent)
					{
						_secondBottomLow = candle.LowPrice;
						_patternConfirmed = true;
						LogInfo($"Double Bottom pattern confirmed. First: {_firstBottomLow}, Second: {_secondBottomLow}");
					}
				}

				// If too much time has passed, reset pattern search
				if (_barsSinceFirstBottom > Distance * 3 || (_secondBottomLow != null && _barsSinceFirstBottom > Distance * 4))
				{
					_firstBottomLow = null;
					_secondBottomLow = null;
					_barsSinceFirstBottom = 0;
					_patternConfirmed = false;
				}
			}
		}
	}
}