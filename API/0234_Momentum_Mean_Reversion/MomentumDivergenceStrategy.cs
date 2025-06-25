using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on momentum divergence between price and momentum indicator.
	/// </summary>
	public class MomentumDivergenceStrategy : Strategy
	{
		private readonly StrategyParam<int> _momentumPeriodParam;
		private readonly StrategyParam<DataType> _candleTypeParam;
		private readonly StrategyParam<int> _maPeriodParam;
		private readonly StrategyParam<decimal> _volumeParam;
		private readonly StrategyParam<decimal> _stopLossPercentParam;

		private readonly SimpleMovingAverage _priceMA;
		private readonly Momentum _momentum;
		
		private decimal _previousPrice;
		private decimal _previousMomentum;
		private bool _hasPreviousValues;

		/// <summary>
		/// Momentum indicator period.
		/// </summary>
		public int MomentumPeriod
		{
			get => _momentumPeriodParam.Value;
			set => _momentumPeriodParam.Value = value;
		}

		/// <summary>
		/// Candle type for data.
		/// </summary>
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		/// <summary>
		/// Moving average period for exit signals.
		/// </summary>
		public int MAPeriod
		{
			get => _maPeriodParam.Value;
			set => _maPeriodParam.Value = value;
		}

		/// <summary>
		/// Trading volume.
		/// </summary>
		public decimal Volume
		{
			get => _volumeParam.Value;
			set => _volumeParam.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercentParam.Value;
			set => _stopLossPercentParam.Value = value;
		}

		/// <summary>
		/// Strategy constructor.
		/// </summary>
		public MomentumDivergenceStrategy()
		{
			_momentumPeriodParam = Param(nameof(MomentumPeriod), 14)
				.SetDisplay("Momentum Period", "Period for momentum indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5)
				.SetGreaterThanZero();

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_maPeriodParam = Param(nameof(MAPeriod), 20)
				.SetDisplay("MA Period", "Period for moving average (exit signal)", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5)
				.SetGreaterThanZero();

			_volumeParam = Param(nameof(Volume), 1m)
				.SetDisplay("Volume", "Trading volume", "General")
				.SetNotNegative();

			_stopLossPercentParam = Param(nameof(StopLossPercent), 2m)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
				.SetNotNegative();

			// Initialize indicators
			_momentum = new Momentum { Length = MomentumPeriod };
			_priceMA = new SimpleMovingAverage { Length = MAPeriod };
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (Security != null && CandleType != null)
				yield return (Security, CandleType);
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			_hasPreviousValues = false;

			// Subscribe to candles
			if (Security != null && CandleType != null)
			{
				var subscription = SubscribeCandles(CandleType);
				
				subscription
					.Bind(ProcessCandle)
					.Start();

				// Create chart areas if available
				var area = CreateChartArea();
				if (area != null)
				{
					DrawCandles(area, subscription);
					DrawIndicator(area, _momentum);
					DrawIndicator(area, _priceMA);
					DrawOwnTrades(area);
				}
			}
			else
			{
				this.AddWarningLog("Security or candle type not specified. Strategy won't work properly.");
			}

			// Start position protection with stop-loss
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			// Process indicators
			var momentumValue = _momentum.Process(new CandleIndicatorValue(candle));
			var maValue = _priceMA.Process(new CandleIndicatorValue(candle));

			// Check if indicators are formed
			if (!_momentum.IsFormed || !_priceMA.IsFormed)
				return;

			decimal currentPrice = candle.ClosePrice;
			decimal currentMomentum = momentumValue.GetValue<decimal>();
			decimal currentMA = maValue.GetValue<decimal>();

			// Check if we have previous values to compare
			if (!_hasPreviousValues)
			{
				_previousPrice = currentPrice;
				_previousMomentum = currentMomentum;
				_hasPreviousValues = true;
				return;
			}

			// Check trading conditions
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check for bullish divergence (price lower, momentum higher)
			bool bullishDivergence = currentPrice < _previousPrice && currentMomentum > _previousMomentum;
			
			// Check for bearish divergence (price higher, momentum lower)
			bool bearishDivergence = currentPrice > _previousPrice && currentMomentum < _previousMomentum;

			// Store current values for next comparison
			_previousPrice = currentPrice;
			_previousMomentum = currentMomentum;

			// Trading logic based on divergences
			if (bullishDivergence && Position <= 0)
			{
				// Bullish divergence - buy signal
				LogInfo($"Bullish divergence detected. Price: {currentPrice}, Momentum: {currentMomentum}");
				BuyMarket(Volume);
			}
			else if (bearishDivergence && Position >= 0)
			{
				// Bearish divergence - sell signal
				LogInfo($"Bearish divergence detected. Price: {currentPrice}, Momentum: {currentMomentum}");
				SellMarket(Volume + Position);
			}
			
			// Exit logic based on price crossing MA
			if (Position > 0 && currentPrice < currentMA)
			{
				// Long position and price below MA - exit
				LogInfo($"Exit long position. Price below MA: {currentPrice} < {currentMA}");
				SellMarket(Position);
			}
			else if (Position < 0 && currentPrice > currentMA)
			{
				// Short position and price above MA - exit
				LogInfo($"Exit short position. Price above MA: {currentPrice} > {currentMA}");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
