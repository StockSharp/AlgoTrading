using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that creates delta neutral arbitrage positions between two correlated assets.
	/// Goes long one asset and short another when spread deviates from the mean.
	/// </summary>
	public class DeltaNeutralArbitrageStrategy : Strategy
	{
		private readonly StrategyParam<Security> _asset2Security;
		private readonly StrategyParam<Portfolio> _asset2Portfolio;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _entryThreshold;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private SimpleMovingAverage _spreadSma;
		private StandardDeviation _spreadStdDev;
		private decimal _currentSpread;
		private decimal _lastAsset1Price;
		private decimal _lastAsset2Price;
		private decimal _asset1Volume;
		private decimal _asset2Volume;

		/// <summary>
		/// Secondary security for pair trading.
		/// </summary>
		public Security Asset2Security
		{
			get => _asset2Security.Value;
			set => _asset2Security.Value = value;
		}

		/// <summary>
		/// Portfolio for trading second asset.
		/// </summary>
		public Portfolio Asset2Portfolio
		{
			get => _asset2Portfolio.Value;
			set => _asset2Portfolio.Value = value;
		}

		/// <summary>
		/// Period for spread statistics calculation.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}

		/// <summary>
		/// Threshold for entries, in standard deviations.
		/// </summary>
		public decimal EntryThreshold
		{
			get => _entryThreshold.Value;
			set => _entryThreshold.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
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
		/// Constructor.
		/// </summary>
		public DeltaNeutralArbitrageStrategy()
		{
			_asset2Security = Param<Security>(nameof(Asset2Security))
				.SetDisplay("Asset 2", "Secondary asset for arbitrage", "Securities");

			_asset2Portfolio = Param<Portfolio>(nameof(Asset2Portfolio))
				.SetDisplay("Portfolio 2", "Portfolio for trading Asset 2", "Portfolios");

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetDisplay("Lookback period", "Period for spread statistics calculation", "Strategy parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_entryThreshold = Param(nameof(EntryThreshold), 2m)
				.SetDisplay("Entry threshold", "Entry threshold in standard deviations", "Strategy parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3m, 0.5m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetDisplay("Stop-loss %", "Stop-loss as percentage from entry spread", "Risk management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return
			[
				(Security, CandleType),
				(Asset2Security, CandleType)
			];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			_currentSpread = default;
			_lastAsset1Price = default;
			_lastAsset2Price = default;
			_asset1Volume = default;
			_asset2Volume = default;

			if (Asset2Security == null)
				throw new InvalidOperationException("Asset2Security is not specified.");

			if (Asset2Portfolio == null)
				throw new InvalidOperationException("Asset2Portfolio is not specified.");

			// Initialize indicators for spread statistics
			_spreadSma = new SimpleMovingAverage { Length = LookbackPeriod };
			_spreadStdDev = new StandardDeviation { Length = LookbackPeriod };

			// Create subscriptions to both securities
			var asset1Subscription = SubscribeCandles(CandleType, security: Security);
			var asset2Subscription = SubscribeCandles(CandleType, security: Asset2Security);

			// Subscribe to candle processing for Asset 1
			asset1Subscription
				.Bind(ProcessAsset1Candle)
				.Start();

			// Subscribe to candle processing for Asset 2
			asset2Subscription
				.Bind(ProcessAsset2Candle)
				.Start();

			// Calculate volumes to maintain beta neutrality (simplified approach)
			// In a real implementation, beta would be calculated dynamically
			_asset1Volume = Volume;
			_asset2Volume = Volume; // Simplified, in reality would be Volume * Beta ratio

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, asset1Subscription);
				DrawOwnTrades(area);
			}
		}

		private void ProcessAsset1Candle(ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Update asset1 price
			_lastAsset1Price = candle.ClosePrice;

			// Process spread if we have both prices
			ProcessSpreadIfReady(candle);
		}

		private void ProcessAsset2Candle(ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Update asset2 price
			_lastAsset2Price = candle.ClosePrice;

			// Process spread if we have both prices
			ProcessSpreadIfReady(candle);
		}

		private void ProcessSpreadIfReady(ICandleMessage candle)
		{
			// Ensure we have both prices
			if (_lastAsset1Price == 0 || _lastAsset2Price == 0)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate the spread
			_currentSpread = _lastAsset1Price - _lastAsset2Price;

			// Process the spread with our indicators
			var spreadValue = _spreadSma.Process(_currentSpread, candle.ServerTime, candle.State == CandleStates.Finished);
			var stdDevValue = _spreadStdDev.Process(_currentSpread, candle.ServerTime, candle.State == CandleStates.Finished);

			// Check if indicators are formed
			if (!_spreadSma.IsFormed || !_spreadStdDev.IsFormed)
				return;

			decimal spreadSma = spreadValue.ToDecimal();
			decimal spreadStdDev = stdDevValue.ToDecimal();

			// Calculate z-score
			decimal zScore = (spreadStdDev == 0) ? 0 : (_currentSpread - spreadSma) / spreadStdDev;

			LogInfo($"Current spread: {_currentSpread}, SMA: {spreadSma}, StdDev: {spreadStdDev}, Z-score: {zScore}");

			// Trading logic
			if (Math.Abs(Position) == 0) // No position, check for entry
			{
				// Spread is too low (Asset1 cheap relative to Asset2)
				if (zScore < -EntryThreshold)
				{
					EnterLongSpread();
					LogInfo($"Long spread entry: Asset1 price={_lastAsset1Price}, Asset2 price={_lastAsset2Price}, Spread={_currentSpread}");
				}
				// Spread is too high (Asset1 expensive relative to Asset2)
				else if (zScore > EntryThreshold)
				{
					EnterShortSpread();
					LogInfo($"Short spread entry: Asset1 price={_lastAsset1Price}, Asset2 price={_lastAsset2Price}, Spread={_currentSpread}");
				}
			}
			else // Have position, check for exit
			{
				if ((Position > 0 && _currentSpread >= spreadSma) || // Long spread and spread has reverted to mean
					(Position < 0 && _currentSpread <= spreadSma))   // Short spread and spread has reverted to mean
				{
					ClosePositions();
					LogInfo($"Spread exit: Asset1 price={_lastAsset1Price}, Asset2 price={_lastAsset2Price}, Spread={_currentSpread}");
				}
			}
		}

		private void EnterLongSpread()
		{
			// Buy Asset1
			var asset1Order = CreateOrder(Sides.Buy, _lastAsset1Price, _asset1Volume);
			asset1Order.Security = Security;
			asset1Order.Portfolio = Portfolio;
			RegisterOrder(asset1Order);

			// Sell Asset2
			var asset2Order = CreateOrder(Sides.Sell, _lastAsset2Price, _asset2Volume);
			asset2Order.Security = Asset2Security;
			asset2Order.Portfolio = Asset2Portfolio;
			RegisterOrder(asset2Order);
		}

		private void EnterShortSpread()
		{
			// Sell Asset1
			var asset1Order = CreateOrder(Sides.Sell, _lastAsset1Price, _asset1Volume);
			asset1Order.Security = Security;
			asset1Order.Portfolio = Portfolio;
			RegisterOrder(asset1Order);

			// Buy Asset2
			var asset2Order = CreateOrder(Sides.Buy, _lastAsset2Price, _asset2Volume);
			asset2Order.Security = Asset2Security;
			asset2Order.Portfolio = Asset2Portfolio;
			RegisterOrder(asset2Order);
		}

		private void ClosePositions()
		{
			// Close position in Asset1
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));

			// Note: In a real implementation, you would also close the position
			// in Asset2 by checking its position via separate portfolio tracking
			// For simplicity, this example assumes symmetrical positions

			// Close position in Asset2 (simplified example)
			var asset2Order = CreateOrder(
				Position > 0 ? Sides.Buy : Sides.Sell, 
				_lastAsset2Price, 
				_asset2Volume);

			asset2Order.Security = Asset2Security;
			asset2Order.Portfolio = Asset2Portfolio;

			RegisterOrder(asset2Order);
		}
	}
}
