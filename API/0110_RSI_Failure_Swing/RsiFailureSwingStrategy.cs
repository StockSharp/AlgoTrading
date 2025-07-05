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
	/// Strategy that trades based on RSI Failure Swing pattern.
	/// A failure swing occurs when RSI reverses direction without crossing through centerline.
	/// </summary>
	public class RsiFailureSwingStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<decimal> _oversoldLevel;
		private readonly StrategyParam<decimal> _overboughtLevel;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private RelativeStrengthIndex _rsi;
		
		private decimal _prevRsiValue;
		private decimal _prevPrevRsiValue;
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
		/// Period for RSI calculation.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
		}

		/// <summary>
		/// Oversold level for RSI.
		/// </summary>
		public decimal OversoldLevel
		{
			get => _oversoldLevel.Value;
			set => _oversoldLevel.Value = value;
		}

		/// <summary>
		/// Overbought level for RSI.
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
		/// Initializes a new instance of the <see cref="RsiFailureSwingStrategy"/>.
		/// </summary>
		public RsiFailureSwingStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						 .SetDisplay("Candle Type", "Type of candles to use for analysis", "General");
			
			_rsiPeriod = Param(nameof(RsiPeriod), 14)
						.SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings")
						.SetRange(2, 50);
			
			_oversoldLevel = Param(nameof(OversoldLevel), 30m)
						   .SetDisplay("Oversold Level", "RSI level considered oversold", "RSI Settings")
						   .SetRange(10m, 40m);
			
			_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
							 .SetDisplay("Overbought Level", "RSI level considered overbought", "RSI Settings")
							 .SetRange(60m, 90m);
			
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
			_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
			
			_prevRsiValue = 0;
			_prevPrevRsiValue = 0;
			_inPosition = false;
			_positionSide = default;
			
			// Create and setup subscription for candles
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicator and processor
			subscription
				.Bind(_rsi, ProcessCandle)
				.Start();
			
			// Enable stop-loss protection
			StartProtection(new Unit(0), new Unit(StopLossPercent, UnitTypes.Percent));
			
			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _rsi);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal? rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Need at least 3 RSI values to detect failure swing
			if (_prevRsiValue == 0 || _prevPrevRsiValue == 0)
			{
				_prevPrevRsiValue = _prevRsiValue;
				_prevRsiValue = rsiValue;
				return;
			}
			
			// Detect Bullish Failure Swing:
			// 1. RSI falls below oversold level
			// 2. RSI rises without crossing centerline
			// 3. RSI pulls back but stays above previous low
			// 4. RSI breaks above the high point of first rise
			bool isBullishFailureSwing = _prevPrevRsiValue < OversoldLevel &&
										_prevRsiValue > _prevPrevRsiValue &&
										rsiValue < _prevRsiValue &&
										rsiValue > _prevPrevRsiValue;
			
			// Detect Bearish Failure Swing:
			// 1. RSI rises above overbought level
			// 2. RSI falls without crossing centerline
			// 3. RSI bounces up but stays below previous high
			// 4. RSI breaks below the low point of first decline
			bool isBearishFailureSwing = _prevPrevRsiValue > OverboughtLevel &&
										 _prevRsiValue < _prevPrevRsiValue &&
										 rsiValue > _prevRsiValue &&
										 rsiValue < _prevPrevRsiValue;
			
			// Trading logic
			if (isBullishFailureSwing && !_inPosition)
			{
				// Enter long position
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				
				_inPosition = true;
				_positionSide = Sides.Buy;
				
				LogInfo($"Bullish RSI Failure Swing detected. RSI values: {_prevPrevRsiValue:F2} -> {_prevRsiValue:F2} -> {rsiValue:F2}. Long entry at {candle.ClosePrice}");
			}
			else if (isBearishFailureSwing && !_inPosition)
			{
				// Enter short position
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				
				_inPosition = true;
				_positionSide = Sides.Sell;
				
				LogInfo($"Bearish RSI Failure Swing detected. RSI values: {_prevPrevRsiValue:F2} -> {_prevRsiValue:F2} -> {rsiValue:F2}. Short entry at {candle.ClosePrice}");
			}
			
			// Exit conditions
			if (_inPosition)
			{
				// For long positions: exit when RSI crosses above 50
				if (_positionSide == Sides.Buy && rsiValue > 50)
				{
					SellMarket(Math.Abs(Position));
					_inPosition = false;
					_positionSide = default;
					
					LogInfo($"Exit signal for long position: RSI ({rsiValue:F2}) crossed above 50. Closing at {candle.ClosePrice}");
				}
				// For short positions: exit when RSI crosses below 50
				else if (_positionSide == Sides.Sell && rsiValue < 50)
				{
					BuyMarket(Math.Abs(Position));
					_inPosition = false;
					_positionSide = default;
					
					LogInfo($"Exit signal for short position: RSI ({rsiValue:F2}) crossed below 50. Closing at {candle.ClosePrice}");
				}
			}
			
			// Update RSI values for next iteration
			_prevPrevRsiValue = _prevRsiValue;
			_prevRsiValue = rsiValue;
		}
	}
}