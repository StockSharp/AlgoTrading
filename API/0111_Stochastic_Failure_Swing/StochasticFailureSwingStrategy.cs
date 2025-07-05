using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on Stochastic Oscillator Failure Swing pattern.
	/// A failure swing occurs when Stochastic reverses direction without crossing through centerline.
	/// </summary>
	public class StochasticFailureSwingStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _kPeriod;
		private readonly StrategyParam<int> _dPeriod;
		private readonly StrategyParam<int> _slowing;
		private readonly StrategyParam<decimal> _oversoldLevel;
		private readonly StrategyParam<decimal> _overboughtLevel;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private StochasticOscillator _stochastic;
		
		private decimal _prevKValue;
		private decimal _prevPrevKValue;
		private bool _inPosition;
		private Sides _positionSide;

		/// <summary>
		/// Candle type and timeframe for the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// K period for Stochastic calculation.
		/// </summary>
		public int KPeriod
		{
			get => _kPeriod.Value;
			set => _kPeriod.Value = value;
		}

		/// <summary>
		/// D period for Stochastic calculation.
		/// </summary>
		public int DPeriod
		{
			get => _dPeriod.Value;
			set => _dPeriod.Value = value;
		}

		/// <summary>
		/// Slowing period for Stochastic calculation.
		/// </summary>
		public int Slowing
		{
			get => _slowing.Value;
			set => _slowing.Value = value;
		}

		/// <summary>
		/// Oversold level for Stochastic.
		/// </summary>
		public decimal OversoldLevel
		{
			get => _oversoldLevel.Value;
			set => _oversoldLevel.Value = value;
		}

		/// <summary>
		/// Overbought level for Stochastic.
		/// </summary>
		public decimal OverboughtLevel
		{
			get => _overboughtLevel.Value;
			set => _overboughtLevel.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage from entry price.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StochasticFailureSwingStrategy"/>.
		/// </summary>
		public StochasticFailureSwingStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						 .SetDisplay("Candle Type", "Type of candles to use for analysis", "General");
			
			_kPeriod = Param(nameof(KPeriod), 14)
					  .SetDisplay("K Period", "Period for %K line calculation", "Stochastic Settings")
					  .SetRange(5, 30);
			
			_dPeriod = Param(nameof(DPeriod), 3)
					  .SetDisplay("D Period", "Period for %D line calculation", "Stochastic Settings")
					  .SetRange(2, 10);
			
			_slowing = Param(nameof(Slowing), 3)
					 .SetDisplay("Slowing", "Slowing period for Stochastic calculation", "Stochastic Settings")
					 .SetRange(1, 5);
			
			_oversoldLevel = Param(nameof(OversoldLevel), 20m)
						   .SetDisplay("Oversold Level", "Stochastic level considered oversold", "Stochastic Settings")
						   .SetRange(10m, 30m);
			
			_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
							 .SetDisplay("Overbought Level", "Stochastic level considered overbought", "Stochastic Settings")
							 .SetRange(70m, 90m);
			
			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
							  .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection")
							  .SetRange(0.5m, 5m);
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
			
			// Initialize indicators
			_stochastic = new StochasticOscillator
			{
				K = { Length = KPeriod },
				D = { Length = DPeriod },
			};
			
			_prevKValue = 0;
			_prevPrevKValue = 0;
			_inPosition = false;
			_positionSide = default;
			
			// Create and setup subscription for candles
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicator and processor
			subscription
				.BindEx(_stochastic, ProcessCandle)
				.Start();
			
			// Enable stop-loss protection
			StartProtection(new Unit(0), new Unit(StopLossPercent, UnitTypes.Percent));
			
			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _stochastic);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Get current K value (we use %K for the strategy, not %D)
			var stochTyped = (StochasticOscillatorValue)stochasticValue;
			
			if (stochTyped.K is not decimal kValue)
				return;

			// Need at least 3 Stochastic values to detect failure swing
			if (_prevKValue == 0 || _prevPrevKValue == 0)
			{
				_prevPrevKValue = _prevKValue;
				_prevKValue = kValue;
				return;
			}
			
			// Detect Bullish Failure Swing:
			// 1. Stochastic falls below oversold level
			// 2. Stochastic rises without crossing centerline
			// 3. Stochastic pulls back but stays above previous low
			// 4. Stochastic breaks above the high point of first rise
			bool isBullishFailureSwing = _prevPrevKValue < OversoldLevel &&
										_prevKValue > _prevPrevKValue &&
										kValue < _prevKValue &&
										kValue > _prevPrevKValue;
			
			// Detect Bearish Failure Swing:
			// 1. Stochastic rises above overbought level
			// 2. Stochastic falls without crossing centerline
			// 3. Stochastic bounces up but stays below previous high
			// 4. Stochastic breaks below the low point of first decline
			bool isBearishFailureSwing = _prevPrevKValue > OverboughtLevel &&
										 _prevKValue < _prevPrevKValue &&
										 kValue > _prevKValue &&
										 kValue < _prevPrevKValue;
			
			// Trading logic
			if (isBullishFailureSwing && !_inPosition)
			{
				// Enter long position
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				
				_inPosition = true;
				_positionSide = Sides.Buy;
				
				LogInfo($"Bullish Stochastic Failure Swing detected. %K values: {_prevPrevKValue:F2} -> {_prevKValue:F2} -> {kValue:F2}. Long entry at {candle.ClosePrice}");
			}
			else if (isBearishFailureSwing && !_inPosition)
			{
				// Enter short position
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				
				_inPosition = true;
				_positionSide = Sides.Sell;
				
				LogInfo($"Bearish Stochastic Failure Swing detected. %K values: {_prevPrevKValue:F2} -> {_prevKValue:F2} -> {kValue:F2}. Short entry at {candle.ClosePrice}");
			}
			
			// Exit conditions
			if (_inPosition)
			{
				// For long positions: exit when Stochastic crosses above 50
				if (_positionSide == Sides.Buy && kValue > 50)
				{
					SellMarket(Math.Abs(Position));
					_inPosition = false;
					_positionSide = default;
					
					LogInfo($"Exit signal for long position: Stochastic %K ({kValue:F2}) crossed above 50. Closing at {candle.ClosePrice}");
				}
				// For short positions: exit when Stochastic crosses below 50
				else if (_positionSide == Sides.Sell && kValue < 50)
				{
					BuyMarket(Math.Abs(Position));
					_inPosition = false;
					_positionSide = default;
					
					LogInfo($"Exit signal for short position: Stochastic %K ({kValue:F2}) crossed below 50. Closing at {candle.ClosePrice}");
				}
			}
			
			// Update Stochastic values for next iteration
			_prevPrevKValue = _prevKValue;
			_prevKValue = kValue;
		}
	}
}