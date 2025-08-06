using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Beta Neutral Arbitrage strategy that trades pairs of assets based on their beta-adjusted prices.
	/// </summary>
	public class BetaNeutralArbitrageStrategy : Strategy
	{
		private readonly StrategyParam<Security> _asset1Param;
		private readonly StrategyParam<Security> _asset2Param;
		private readonly StrategyParam<Security> _marketIndexParam;
		private readonly StrategyParam<DataType> _candleTypeParam;
		private readonly StrategyParam<int> _lookbackPeriodParam;
		private readonly StrategyParam<decimal> _stopLossPercentParam;

		private decimal _asset1Beta;
		private decimal _asset2Beta;
		private decimal _lastSpread;
		private decimal _avgSpread;
		private readonly SimpleMovingAverage _spreadSma;
		private readonly StandardDeviation _spreadStdDev;
		private int _barCount;
		private decimal _asset1LastPrice;
		private decimal _asset2LastPrice;

		/// <summary>
		/// First asset for beta-neutral arbitrage.
		/// </summary>
		public Security Asset1
		{
			get => _asset1Param.Value;
			set => _asset1Param.Value = value;
		}

		/// <summary>
		/// Second asset for beta-neutral arbitrage.
		/// </summary>
		public Security Asset2
		{
			get => _asset2Param.Value;
			set => _asset2Param.Value = value;
		}

		/// <summary>
		/// Market index for beta calculation.
		/// </summary>
		public Security MarketIndex
		{
			get => _marketIndexParam.Value;
			set => _marketIndexParam.Value = value;
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
		/// Lookback period for calculations.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriodParam.Value;
			set => _lookbackPeriodParam.Value = value;
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
		public BetaNeutralArbitrageStrategy()
		{
			_asset1Param = Param<Security>(nameof(Asset1))
				.SetDisplay("Asset 1", "First asset for beta-neutral arbitrage", "Instruments");

			_asset2Param = Param<Security>(nameof(Asset2))
				.SetDisplay("Asset 2", "Second asset for beta-neutral arbitrage", "Instruments");

			_marketIndexParam = Param<Security>(nameof(MarketIndex))
				.SetDisplay("Market Index", "Market index for beta calculation", "Instruments");

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_lookbackPeriodParam = Param(nameof(LookbackPeriod), 20)
				.SetDisplay("Lookback Period", "Period for spread calculation", "Strategy")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5)
				.SetGreaterThanZero();

			_stopLossPercentParam = Param(nameof(StopLossPercent), 2m)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
				.SetNotNegative();

			// Initialize indicators
			_spreadSma = new SimpleMovingAverage { Length = LookbackPeriod };
			_spreadStdDev = new StandardDeviation { Length = LookbackPeriod };
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (Asset1 != null && CandleType != null)
				yield return (Asset1, CandleType);

			if (Asset2 != null && CandleType != null)
				yield return (Asset2, CandleType);

			if (MarketIndex != null && CandleType != null)
				yield return (MarketIndex, CandleType);
		}

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();
			_spreadSma.Reset();
			_spreadStdDev.Reset();
			_barCount = 0;
			_asset1Beta = 1;
			_asset2Beta = 1;
			_avgSpread = 0;
			_lastSpread = 0;
			_asset1LastPrice = 0;
			_asset2LastPrice = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);


			// Subscribe to candles for assets and market index
			if (Asset1 != null && Asset2 != null && MarketIndex != null && CandleType != null)
			{
				// We'll use historical data to calculate initial betas
				LogInfo("Calculating initial betas using historical data...");

				// In a real implementation, this would be done using historical data
				// For this example, we'll just set default values
				_asset1Beta = 1.2m;  // Example beta for Asset1
				_asset2Beta = 0.8m;  // Example beta for Asset2

				LogInfo($"Initial betas: Asset1={_asset1Beta}, Asset2={_asset2Beta}");

				// Subscribe to real-time candles for trading
				var asset1Subscription = SubscribeCandles(CandleType, security: Asset1);
				var asset2Subscription = SubscribeCandles(CandleType, security: Asset2);
				var marketSubscription = SubscribeCandles(CandleType, security: MarketIndex);

				// Bind processing to candle subscriptions
				asset1Subscription
					.Bind(ProcessAsset1Candle)
					.Start();

				asset2Subscription
					.Bind(ProcessAsset2Candle)
					.Start();

				marketSubscription
					.Bind(ProcessMarketCandle)
					.Start();

				// Create chart areas if available
				var area = CreateChartArea();
				if (area != null)
				{
					DrawCandles(area, asset1Subscription);
					DrawCandles(area, asset2Subscription);
					DrawCandles(area, marketSubscription);
					DrawOwnTrades(area);
				}
			}
			else
			{
				LogWarning("Assets or market index not specified. Strategy won't work properly.");
			}

			// Start position protection with stop-loss
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessAsset1Candle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			_asset1LastPrice = candle.ClosePrice;
			UpdateSpread(candle);
		}

		private void ProcessAsset2Candle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			_asset2LastPrice = candle.ClosePrice;
			UpdateSpread(candle);
		}

		private void ProcessMarketCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			// In a real implementation, this would update betas based on new market data
			// For this example, we'll just use the fixed betas
		}

		private void UpdateSpread(ICandleMessage candle)
		{
			if (_asset1LastPrice == 0 || _asset2LastPrice == 0)
				return;

			// Calculate beta-adjusted spread
			decimal betaAdjustedAsset1 = _asset1LastPrice / _asset1Beta;
			decimal betaAdjustedAsset2 = _asset2LastPrice / _asset2Beta;
			_lastSpread = betaAdjustedAsset1 - betaAdjustedAsset2;

			// Process through indicators
			var smaValue = _spreadSma.Process(_lastSpread, candle.ServerTime, candle.State == CandleStates.Finished);
			var stdDevValue = _spreadStdDev.Process(_lastSpread, candle.ServerTime, candle.State == CandleStates.Finished);

			// Update counter
			_barCount++;

			// Check if indicators are formed
			if (!_spreadSma.IsFormed)
				return;

			// Update average for trading decisions
			_avgSpread = smaValue.ToDecimal();

			// Check trading conditions
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var spreadStdDev = stdDevValue.ToDecimal();
			decimal threshold = 2m; // Standard deviations from mean to trigger

			// Trading logic for beta-neutral arbitrage
			if (_lastSpread < _avgSpread - threshold * spreadStdDev && GetPositionValue(Asset1) <= 0 && GetPositionValue(Asset2) >= 0)
			{
				// Spread is below threshold - buy Asset1, sell Asset2
				LogInfo($"Spread below threshold: {_lastSpread} < {_avgSpread - threshold * spreadStdDev}");

				// Calculate beta-neutral position sizes
				decimal asset1Volume = Volume;
				decimal asset2Volume = Volume * (_asset1Beta / _asset2Beta);

				BuyMarket(asset1Volume, Asset1);
				SellMarket(asset2Volume, Asset2);
			}
			else if (_lastSpread > _avgSpread + threshold * spreadStdDev && GetPositionValue(Asset1) >= 0 && GetPositionValue(Asset2) <= 0)
			{
				// Spread is above threshold - sell Asset1, buy Asset2
				LogInfo($"Spread above threshold: {_lastSpread} > {_avgSpread + threshold * spreadStdDev}");

				// Calculate beta-neutral position sizes
				decimal asset1Volume = Volume;
				decimal asset2Volume = Volume * (_asset1Beta / _asset2Beta);

				SellMarket(asset1Volume, Asset1);
				BuyMarket(asset2Volume, Asset2);
			}
			else if (Math.Abs(_lastSpread - _avgSpread) < 0.2m * spreadStdDev)
			{
				// Close position when spread returns to average
				LogInfo($"Spread returned to average: {_lastSpread} ≈ {_avgSpread}");

				if (GetPositionValue(Asset1) > 0)
					SellMarket(Math.Abs(GetPositionValue(Asset1)), Asset1);

				if (GetPositionValue(Asset1) < 0)
					BuyMarket(Math.Abs(GetPositionValue(Asset1)), Asset1);

				if (GetPositionValue(Asset2) > 0)
					SellMarket(Math.Abs(GetPositionValue(Asset2)), Asset2);

				if (GetPositionValue(Asset2) < 0)
					BuyMarket(Math.Abs(GetPositionValue(Asset2)), Asset2);
			}
		}

		private decimal GetPositionValue(Security security)
		{
			return security is null ? 0 : GetPositionValue(security, Portfolio) ?? 0;
		}
	}
}