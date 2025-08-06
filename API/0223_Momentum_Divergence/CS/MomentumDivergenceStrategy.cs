using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Momentum Divergence strategy.
	/// Trades based on divergence between price and momentum.
	/// </summary>
	public class MomentumDivergenceStrategy : Strategy
	{
		private readonly StrategyParam<int> _momentumPeriodParam;
		private readonly StrategyParam<int> _maPeriodParam;
		private readonly StrategyParam<DataType> _candleTypeParam;
		
		private Momentum _momentum;
		private SimpleMovingAverage _sma;
		
		private decimal _prevPrice;
		private decimal _prevMomentum;
		private decimal _currentPrice;
		private decimal _currentMomentum;

		/// <summary>
		/// Momentum indicator period.
		/// </summary>
		public int MomentumPeriod
		{
			get => _momentumPeriodParam.Value;
			set => _momentumPeriodParam.Value = value;
		}
		
		/// <summary>
		/// Moving average period.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriodParam.Value;
			set => _maPeriodParam.Value = value;
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
		public MomentumDivergenceStrategy()
		{
			_momentumPeriodParam = Param(nameof(MomentumPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Momentum Period", "Period for Momentum indicator", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
			_maPeriodParam = Param(nameof(MaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("MA Period", "Period for Moving Average", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10);

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "Common");
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

			_prevPrice = 0;
			_prevMomentum = 0;
			_currentPrice = 0;
			_currentMomentum = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_momentum = new Momentum { Length = MomentumPeriod };
			_sma = new SimpleMovingAverage { Length = MaPeriod };
			
			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_momentum, _sma, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _momentum);
				DrawIndicator(area, _sma);
				DrawOwnTrades(area);
			}
			
			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(2, UnitTypes.Percent) // 2% stop loss
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal momentumValue, decimal smaValue)
		{
			if (candle.State != CandleStates.Finished)
				return;
				
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Store previous values before updating current ones
			_prevPrice = _currentPrice;
			_prevMomentum = _currentMomentum;
			
			// Update current values
			_currentPrice = candle.ClosePrice;
			_currentMomentum = momentumValue;
			
			// Skip first candle after indicators become formed
			if (_prevPrice == 0 || _prevMomentum == 0)
				return;
				
			// Detect bullish divergence (price makes lower low but momentum makes higher low)
			bool bullishDivergence = _currentPrice < _prevPrice && _currentMomentum > _prevMomentum;
			
			// Detect bearish divergence (price makes higher high but momentum makes lower high)
			bool bearishDivergence = _currentPrice > _prevPrice && _currentMomentum < _prevMomentum;
			
			// Trading signals
			if (bullishDivergence && Position <= 0)
			{
				// Bullish divergence - buy signal
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (bearishDivergence && Position >= 0)
			{
				// Bearish divergence - sell signal
				SellMarket(Volume + Math.Abs(Position));
			}
			// Exit when price crosses MA in the opposite direction
			else if (Position > 0 && candle.ClosePrice < smaValue)
			{
				// Exit long position
				SellMarket(Position);
			}
			else if (Position < 0 && candle.ClosePrice > smaValue)
			{
				// Exit short position
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}