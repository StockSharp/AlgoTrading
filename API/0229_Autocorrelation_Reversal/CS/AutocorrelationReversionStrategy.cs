using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on price autocorrelation.
	/// Buys when autocorrelation is negative and price is below average.
	/// Sells when autocorrelation is negative and price is above average.
	/// </summary>
	public class AutocorrelationReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _autoCorrPeriod;
		private readonly StrategyParam<decimal> _autoCorrThreshold;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private SimpleMovingAverage _sma;
		private decimal _currentPrice;
		private readonly Queue<decimal> _priceHistory = [];
		private decimal _latestAutocorrelation;

		/// <summary>
		/// Period for autocorrelation calculation.
		/// </summary>
		public int AutoCorrPeriod
		{
			get => _autoCorrPeriod.Value;
			set => _autoCorrPeriod.Value = value;
		}

		/// <summary>
		/// Autocorrelation threshold for signal generation.
		/// </summary>
		public decimal AutoCorrThreshold
		{
			get => _autoCorrThreshold.Value;
			set => _autoCorrThreshold.Value = value;
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
		public AutocorrelationReversionStrategy()
		{
			_autoCorrPeriod = Param(nameof(AutoCorrPeriod), 20)
				.SetDisplay("Autocorrelation period", "Period for autocorrelation calculation", "Strategy parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_autoCorrThreshold = Param(nameof(AutoCorrThreshold), -0.3m)
				.SetDisplay("Autocorr threshold", "Threshold for autocorrelation signals", "Strategy parameters")
				.SetCanOptimize(true)
				.SetOptimize(-0.5m, -0.1m, 0.1m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetDisplay("Stop-loss %", "Stop-loss as percentage from entry price", "Risk management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle type", "Type of candles to use", "General");
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

			_priceHistory.Clear();
			_latestAutocorrelation = default;
			_currentPrice = default;

			// Initialize the SMA indicator (using same period as autocorrelation for simplicity)
			_sma = new SimpleMovingAverage { Length = AutoCorrPeriod };

			// Create a subscription to candlesticks
			var subscription = SubscribeCandles(CandleType);

			// Subscribe to candle processing
			subscription
				.Bind(_sma, ProcessCandle)
				.Start();

			// Start position protection
			StartProtection(
				new Unit(StopLossPercent, UnitTypes.Percent),
				new Unit(StopLossPercent * 1.5m, UnitTypes.Percent));

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _sma);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal smaValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Update current price and price history
			_currentPrice = candle.ClosePrice;
			
			// Update price history queue
			_priceHistory.Enqueue(_currentPrice);
			if (_priceHistory.Count > AutoCorrPeriod)
				_priceHistory.Dequeue();

			// Wait until we have enough data
			if (_priceHistory.Count < AutoCorrPeriod)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate autocorrelation
			_latestAutocorrelation = CalculateAutocorrelation();

			// Log the autocorrelation value
			LogInfo($"Autocorrelation: {_latestAutocorrelation}, Current price: {_currentPrice}, SMA: {smaValue}");

			// Trading logic: Look for negative autocorrelation below threshold
			if (_latestAutocorrelation < AutoCorrThreshold)
			{
				// Price below average - buy signal
				if (_currentPrice < smaValue && Position <= 0)
				{
					BuyMarket(Volume);
					LogInfo($"Buy signal: Autocorr={_latestAutocorrelation}, Price={_currentPrice}, SMA={smaValue}");
				}
				// Price above average - sell signal
				else if (_currentPrice > smaValue && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Sell signal: Autocorr={_latestAutocorrelation}, Price={_currentPrice}, SMA={smaValue}");
				}
			}
		}

		private decimal CalculateAutocorrelation()
		{
			// Convert queue to array for easier calculation
			decimal[] prices = _priceHistory.ToArray();
			
			// Calculate price changes
			decimal[] priceChanges = new decimal[prices.Length - 1];
			for (int i = 0; i < prices.Length - 1; i++)
			{
				priceChanges[i] = prices[i + 1] - prices[i];
			}

			// Calculate autocorrelation of lag 1
			decimal meanChange = priceChanges.Average();
			
			decimal numerator = 0;
			decimal denominator = 0;
			
			for (int i = 0; i < priceChanges.Length - 1; i++)
			{
				decimal deviation1 = priceChanges[i] - meanChange;
				decimal deviation2 = priceChanges[i + 1] - meanChange;
				
				numerator += deviation1 * deviation2;
				denominator += deviation1 * deviation1;
			}
			
			// Guard against division by zero
			if (denominator == 0)
				return 0;
				
			return numerator / denominator;
		}
	}
}
