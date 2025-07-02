using System;
using System.Collections.Generic;
using Ecng.ComponentModel;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Beta Adjusted Pairs Trading strategy uses beta-normalized prices 
	/// to identify trading opportunities when the spread deviates from historical means.
	/// </summary>
	public class BetaAdjustedPairsStrategy : Strategy
	{
		// Strategy parameters
		private readonly StrategyParam<Security> _asset1Param;
		private readonly StrategyParam<Security> _asset2Param;
		private readonly StrategyParam<Portfolio> _asset1PortfolioParam;
		private readonly StrategyParam<Portfolio> _asset2PortfolioParam;
		private readonly StrategyParam<decimal> _betaAsset1Param;
		private readonly StrategyParam<decimal> _betaAsset2Param;
		private readonly StrategyParam<int> _lookbackPeriodParam;
		private readonly StrategyParam<decimal> _entryThresholdParam;
		private readonly StrategyParam<decimal> _stopLossParam;

		// Internal state variables
		private decimal _asset1Price;
		private decimal _asset2Price;
		private decimal _currentSpread;
		private decimal _averageSpread;
		private decimal _spreadStdDev;
		private decimal _entrySpread;
		private bool _inPosition;
		private bool _isLong; // Long = long Asset1, short Asset2; Short = short Asset1, long Asset2
		
		// Historical data
		private readonly List<decimal> _spreadHistory = [];

		/// <summary>
		/// Asset 1 for pairs trading
		/// </summary>
		public Security Asset1
		{
			get => _asset1Param.Value;
			set => _asset1Param.Value = value;
		}

		/// <summary>
		/// Asset 2 for pairs trading
		/// </summary>
		public Security Asset2
		{
			get => _asset2Param.Value;
			set => _asset2Param.Value = value;
		}

		/// <summary>
		/// Portfolio for trading Asset1
		/// </summary>
		public Portfolio Asset1Portfolio
		{
			get => _asset1PortfolioParam.Value;
			set => _asset1PortfolioParam.Value = value;
		}

		/// <summary>
		/// Portfolio for trading Asset2
		/// </summary>
		public Portfolio Asset2Portfolio
		{
			get => _asset2PortfolioParam.Value;
			set => _asset2PortfolioParam.Value = value;
		}

		/// <summary>
		/// Beta coefficient for Asset1 relative to market
		/// </summary>
		public decimal BetaAsset1
		{
			get => _betaAsset1Param.Value;
			set => _betaAsset1Param.Value = value;
		}

		/// <summary>
		/// Beta coefficient for Asset2 relative to market
		/// </summary>
		public decimal BetaAsset2
		{
			get => _betaAsset2Param.Value;
			set => _betaAsset2Param.Value = value;
		}

		/// <summary>
		/// Lookback period for calculating spread statistics
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriodParam.Value;
			set => _lookbackPeriodParam.Value = value;
		}

		/// <summary>
		/// Standard deviation threshold for entries (in multiples of standard deviation)
		/// </summary>
		public decimal EntryThreshold
		{
			get => _entryThresholdParam.Value;
			set => _entryThresholdParam.Value = value;
		}

		/// <summary>
		/// Stop loss threshold (in percentage of entry spread)
		/// </summary>
		public decimal StopLoss
		{
			get => _stopLossParam.Value;
			set => _stopLossParam.Value = value;
		}

		/// <summary>
		/// Constructor with default parameters
		/// </summary>
		public BetaAdjustedPairsStrategy()
		{
			_asset1Param = Param(nameof(Asset1), (Security)null)
				.SetDisplay("Asset 1", "Primary asset for pairs trading", "Assets")
				.SetRequired();
				
			_asset2Param = Param(nameof(Asset2), (Security)null)
				.SetDisplay("Asset 2", "Secondary asset for pairs trading", "Assets")
				.SetRequired();
				
			_asset1PortfolioParam = Param(nameof(Asset1Portfolio), (Portfolio)null)
				.SetDisplay("Asset 1 Portfolio", "Portfolio for trading Asset 1", "Portfolios")
				.SetRequired();
				
			_asset2PortfolioParam = Param(nameof(Asset2Portfolio), (Portfolio)null)
				.SetDisplay("Asset 2 Portfolio", "Portfolio for trading Asset 2", "Portfolios")
				.SetRequired();
				
			_betaAsset1Param = Param(nameof(BetaAsset1), 1.0m)
				.SetDisplay("Beta Asset 1", "Beta coefficient for Asset 1 relative to market", "Parameters")
				.SetNotNegative();
				
			_betaAsset2Param = Param(nameof(BetaAsset2), 1.0m)
				.SetDisplay("Beta Asset 2", "Beta coefficient for Asset 2 relative to market", "Parameters")
				.SetNotNegative();
				
			_lookbackPeriodParam = Param(nameof(LookbackPeriod), 20)
				.SetDisplay("Lookback Period", "Period for calculating spread statistics", "Parameters")
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);
				
			_entryThresholdParam = Param(nameof(EntryThreshold), 2.0m)
				.SetDisplay("Entry Threshold", "Standard deviation threshold for entries", "Parameters")
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
				
			_stopLossParam = Param(nameof(StopLoss), 2.0m)
				.SetDisplay("Stop Loss", "Stop loss as percentage of entry spread", "Risk Management")
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return
			[
				(Asset1, DataType.Level1),
				(Asset2, DataType.Level1)
			];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Verify that both assets and portfolios are set
			if (Asset1 == null)
				throw new InvalidOperationException("Asset1 is not specified.");

			if (Asset2 == null)
				throw new InvalidOperationException("Asset2 is not specified.");

			if (Asset1Portfolio == null)
				throw new InvalidOperationException("Asset1Portfolio is not specified.");

			if (Asset2Portfolio == null)
				throw new InvalidOperationException("Asset2Portfolio is not specified.");

			// Reset internal state
			_spreadHistory.Clear();
			_inPosition = false;
			_currentSpread = 0;
			_averageSpread = 0;
			_spreadStdDev = 0;
			_entrySpread = 0;

			// Create subscriptions for both assets
			var asset1Subscription = new Subscription(DataType.Level1, Asset1);
			var asset2Subscription = new Subscription(DataType.Level1, Asset2);

			// Handle price updates for Asset1
			asset1Subscription
				.WhenLevel1FieldReceived(this, Level1Fields.LastTradePrice)
				.Do(msg =>
				{
					_asset1Price = msg.Message.Changes.TryGetValue(Level1Fields.LastTradePrice, out var price) 
						? price.To<decimal>() 
						: 0;
						
					UpdateSpread();
				})
				.Apply(this);

			// Handle price updates for Asset2
			asset2Subscription
				.WhenLevel1FieldReceived(this, Level1Fields.LastTradePrice)
				.Do(msg =>
				{
					_asset2Price = msg.Message.Changes.TryGetValue(Level1Fields.LastTradePrice, out var price) 
						? price.To<decimal>() 
						: 0;
						
					UpdateSpread();
				})
				.Apply(this);

			// Subscribe to market data
			Subscribe(asset1Subscription);
			Subscribe(asset2Subscription);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				// If chart features are needed, add them here
				// For example, you could track and draw the spread
			}
		}

		private void UpdateSpread()
		{
			// Skip if prices are not yet available
			if (_asset1Price <= 0 || _asset2Price <= 0)
				return;

			// Calculate beta-adjusted spread
			_currentSpread = (_asset1Price / BetaAsset1) - (_asset2Price / BetaAsset2);

			// Update historical spread data
			_spreadHistory.Add(_currentSpread);

			// Keep only lookback period data points
			while (_spreadHistory.Count > LookbackPeriod)
			{
				_spreadHistory.RemoveAt(0);
			}

			// We need at least lookback period data points to start trading
			if (_spreadHistory.Count < LookbackPeriod)
				return;

			// Calculate spread statistics
			CalculateSpreadStatistics();

			// Check if we're ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check position management
			if (_inPosition)
			{
				CheckExitConditions();
			}
			else
			{
				CheckEntryConditions();
			}
		}

		private void CalculateSpreadStatistics()
		{
			// Calculate mean
			decimal sum = 0;
			foreach (var spread in _spreadHistory)
			{
				sum += spread;
			}
			_averageSpread = sum / _spreadHistory.Count;

			// Calculate standard deviation
			decimal sumOfSquaredDifferences = 0;
			foreach (var spread in _spreadHistory)
			{
				decimal difference = spread - _averageSpread;
				sumOfSquaredDifferences += difference * difference;
			}
			_spreadStdDev = (decimal)Math.Sqrt((double)(sumOfSquaredDifferences / _spreadHistory.Count));
		}

		private void CheckEntryConditions()
		{
			// Make sure we have valid statistics
			if (_spreadStdDev == 0)
				return;

			// Normalized spread distance from mean (in standard deviations)
			decimal zScore = (_currentSpread - _averageSpread) / _spreadStdDev;

			// Check if spread is significantly above average (short signal)
			if (zScore > EntryThreshold)
			{
				EnterShortPosition();
			}
			// Check if spread is significantly below average (long signal)
			else if (zScore < -EntryThreshold)
			{
				EnterLongPosition();
			}
		}

		private void CheckExitConditions()
		{
			// Check for mean reversion (exit condition)
			if (_isLong && _currentSpread > _averageSpread)
			{
				ExitPosition();
			}
			else if (!_isLong && _currentSpread < _averageSpread)
			{
				ExitPosition();
			}
			// Check for stop loss
			else
			{
				decimal spreadDifference = Math.Abs(_currentSpread - _entrySpread);
				decimal stopLossThreshold = _entrySpread * StopLoss / 100;
				
				if (spreadDifference > stopLossThreshold)
				{
					ExitPosition();
					LogInfo($"Stop loss triggered. Entry spread: {_entrySpread}, Current spread: {_currentSpread}");
				}
			}
		}

		private void EnterLongPosition()
		{
			// Long = long Asset1, short Asset2
			_inPosition = true;
			_isLong = true;
			_entrySpread = _currentSpread;
			
			// Calculate trade volume based on strategy's Volume property
			var volume = Volume;

			// Create and register orders
			var longOrder = new Order
			{
				Portfolio = Asset1Portfolio,
				Security = Asset1,
				Side = Sides.Buy,
				Volume = volume,
				Type = OrderTypes.Market
			};

			var shortOrder = new Order
			{
				Portfolio = Asset2Portfolio,
				Security = Asset2,
				Side = Sides.Sell,
				Volume = volume,
				Type = OrderTypes.Market
			};

			RegisterOrder(longOrder);
			RegisterOrder(shortOrder);
			
			LogInfo($"Entered LONG position (long Asset1, short Asset2) at spread: {_currentSpread}, Mean: {_averageSpread}, StdDev: {_spreadStdDev}");
		}

		private void EnterShortPosition()
		{
			// Short = short Asset1, long Asset2
			_inPosition = true;
			_isLong = false;
			_entrySpread = _currentSpread;
			
			// Calculate trade volume based on strategy's Volume property
			var volume = Volume;

			// Create and register orders
			var shortOrder = new Order
			{
				Portfolio = Asset1Portfolio,
				Security = Asset1,
				Side = Sides.Sell,
				Volume = volume,
				Type = OrderTypes.Market
			};

			var longOrder = new Order
			{
				Portfolio = Asset2Portfolio,
				Security = Asset2,
				Side = Sides.Buy,
				Volume = volume,
				Type = OrderTypes.Market
			};

			RegisterOrder(shortOrder);
			RegisterOrder(longOrder);
			
			LogInfo($"Entered SHORT position (short Asset1, long Asset2) at spread: {_currentSpread}, Mean: {_averageSpread}, StdDev: {_spreadStdDev}");
		}

		private void ExitPosition()
		{
			if (!_inPosition)
				return;

			// Calculate trade volume
			var volume = Volume;

			// Close positions in opposite directions
			if (_isLong)
			{
				// Close long Asset1, short Asset2
				var closeAsset1 = new Order
				{
					Portfolio = Asset1Portfolio,
					Security = Asset1,
					Side = Sides.Sell,
					Volume = volume,
					Type = OrderTypes.Market
				};

				var closeAsset2 = new Order
				{
					Portfolio = Asset2Portfolio,
					Security = Asset2,
					Side = Sides.Buy,
					Volume = volume,
					Type = OrderTypes.Market
				};

				RegisterOrder(closeAsset1);
				RegisterOrder(closeAsset2);
			}
			else
			{
				// Close short Asset1, long Asset2
				var closeAsset1 = new Order
				{
					Portfolio = Asset1Portfolio,
					Security = Asset1,
					Side = Sides.Buy,
					Volume = volume,
					Type = OrderTypes.Market
				};

				var closeAsset2 = new Order
				{
					Portfolio = Asset2Portfolio,
					Security = Asset2,
					Side = Sides.Sell,
					Volume = volume,
					Type = OrderTypes.Market
				};

				RegisterOrder(closeAsset1);
				RegisterOrder(closeAsset2);
			}

			// Reset position state
			_inPosition = false;
			
			LogInfo($"Exited position at spread: {_currentSpread}, Entry spread: {_entrySpread}");
		}

		/// <inheritdoc />
		protected override void OnStopped()
		{
			// Close any open position when strategy stops
			if (_inPosition)
			{
				ExitPosition();
			}
			
			base.OnStopped();
		}
	}
}
