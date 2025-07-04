using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy combining VWAP and Stochastic indicators.
	/// Buys when price is below VWAP and Stochastic is oversold.
	/// Sells when price is above VWAP and Stochastic is overbought.
	/// </summary>
	public class VwapStochasticStrategy : Strategy
	{
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _stochKPeriod;
		private readonly StrategyParam<int> _stochDPeriod;
		private readonly StrategyParam<decimal> _overboughtLevel;
		private readonly StrategyParam<decimal> _oversoldLevel;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Stochastic %K period.
		/// </summary>
		public int StochPeriod
		{
			get => _stochPeriod.Value;
			set => _stochPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %K smoothing period.
		/// </summary>
		public int StochKPeriod
		{
			get => _stochKPeriod.Value;
			set => _stochKPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %D period.
		/// </summary>
		public int StochDPeriod
		{
			get => _stochDPeriod.Value;
			set => _stochDPeriod.Value = value;
		}

		/// <summary>
		/// Overbought level for stochastic (0-100).
		/// </summary>
		public decimal OverboughtLevel
		{
			get => _overboughtLevel.Value;
			set => _overboughtLevel.Value = value;
		}

		/// <summary>
		/// Oversold level for stochastic (0-100).
		/// </summary>
		public decimal OversoldLevel
		{
			get => _oversoldLevel.Value;
			set => _oversoldLevel.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
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
		/// Initialize strategy.
		/// </summary>
		public VwapStochasticStrategy()
		{
			_stochPeriod = Param(nameof(StochPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Stoch Period", "Period for Stochastic calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_stochKPeriod = Param(nameof(StochKPeriod), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stoch %K", "Smoothing period for %K line", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

			_stochDPeriod = Param(nameof(StochDPeriod), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stoch %D", "Smoothing period for %D line", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

			_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
				.SetRange(50, 95)
				.SetDisplay("Overbought Level", "Level considered overbought", "Trading Levels")
				.SetCanOptimize(true)
				.SetOptimize(70, 90, 5);

			_oversoldLevel = Param(nameof(OversoldLevel), 20m)
				.SetRange(5, 50)
				.SetDisplay("Oversold Level", "Level considered oversold", "Trading Levels")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 5m, 0.5m);

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
			var vwap = new VolumeWeightedMovingAverage();
			var stochastic = new StochasticOscillator
			{
				K = { Length = StochKPeriod },
				D = { Length = StochDPeriod },
			};

			// Create subscription
			var subscription = SubscribeCandles(CandleType);

			// Bind indicators to candles
			subscription
				.BindEx(vwap, stochastic, ProcessCandle)
				.Start();

			// Enable stop-loss and take-profit protection
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
				isStopTrailing: false,
				useMarketOrders: true
			);

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, vwap);
				
				// Create second area for stochastic
				var stochArea = CreateChartArea();
				DrawIndicator(stochArea, stochastic);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue vwapValue, IIndicatorValue stochValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var stochTyped = (StochasticOscillatorValue)stochValue;
			var kValue = stochTyped.K;

			var vwapDec = vwapValue.ToDecimal();

			// Trading logic
			if (candle.ClosePrice < vwapDec && kValue < OversoldLevel && Position <= 0)
			{
				// Price below VWAP and stochastic shows oversold - Buy
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (candle.ClosePrice > vwapDec && kValue > OverboughtLevel && Position >= 0)
			{
				// Price above VWAP and stochastic shows overbought - Sell
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			else if (Position > 0 && candle.ClosePrice > vwapDec)
			{
				// Exit long position when price crosses above VWAP
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && candle.ClosePrice < vwapDec)
			{
				// Exit short position when price crosses below VWAP
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
