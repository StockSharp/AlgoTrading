using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// ADX strategy with Sentiment Momentum filter.
	/// </summary>
	public class AdxSentimentMomentumStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<decimal> _adxThreshold;
		private readonly StrategyParam<int> _sentimentPeriod;
		private readonly StrategyParam<decimal> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;

		private ADX _adx;
		private decimal _prevSentiment;
		private decimal _currentSentiment;
		private decimal _sentimentMomentum;

		/// <summary>
		/// ADX Period.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// ADX Threshold for strong trend.
		/// </summary>
		public decimal AdxThreshold
		{
			get => _adxThreshold.Value;
			set => _adxThreshold.Value = value;
		}

		/// <summary>
		/// Period for sentiment momentum calculation.
		/// </summary>
		public int SentimentPeriod
		{
			get => _sentimentPeriod.Value;
			set => _sentimentPeriod.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLoss
		{
			get => _stopLoss.Value;
			set => _stopLoss.Value = value;
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
		/// Initialize <see cref="AdxSentimentMomentumStrategy"/>.
		/// </summary>
		public AdxSentimentMomentumStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(5, 30)
			.SetCanOptimize(true)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators");

			_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetRange(15m, 35m)
			.SetCanOptimize(true)
			.SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators");

			_sentimentPeriod = Param(nameof(SentimentPeriod), 5)
			.SetRange(3, 10)
			.SetCanOptimize(true)
			.SetDisplay("Sentiment Period", "Period for sentiment momentum calculation", "Sentiment");

			_stopLoss = Param(nameof(StopLoss), 2m)
			.SetRange(1m, 5m)
			.SetCanOptimize(true)
			.SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

			_adx?.Reset();
			_prevSentiment = 0;
			_currentSentiment = 0;
			_sentimentMomentum = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create ADX Indicator
			_adx = new()
			{
				Length = AdxPeriod
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
			.BindEx(_adx, ProcessCandle)
			.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _adx);
				DrawOwnTrades(area);
			}

			// Start position protection
			StartProtection(
			new Unit(2, UnitTypes.Percent),   // Take profit 2%
			new Unit(StopLoss, UnitTypes.Percent)  // Stop loss based on parameter
			);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
			return;

			// Simulate sentiment data and calculate momentum
			UpdateSentiment(candle);

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
			return;

			var typedAdx = (AverageDirectionalIndexValue)adxValue;
			var adxMain = typedAdx.MovingAverage;
			var diPlus = typedAdx.Dx.Plus;
			var diMinus = typedAdx.Dx.Minus;

			// Entry logic based on ADX and sentiment momentum
			if (adxMain > AdxThreshold && diPlus > diMinus && _sentimentMomentum > 0 && Position <= 0)
			{
				// Strong uptrend with positive sentiment momentum - Long entry
				BuyMarket(Volume);
				LogInfo($"Buy Signal: ADX={adxMain}, +DI={diPlus}, -DI={diMinus}, Sentiment Momentum={_sentimentMomentum}");
			}
			else if (adxMain > AdxThreshold && diMinus > diPlus && _sentimentMomentum < 0 && Position >= 0)
			{
				// Strong downtrend with negative sentiment momentum - Short entry
				SellMarket(Volume);
				LogInfo($"Sell Signal: ADX={adxMain}, +DI={diPlus}, -DI={diMinus}, Sentiment Momentum={_sentimentMomentum}");
			}

			// Exit logic
			if (adxMain < 20 && Position != 0)
			{
				// Exit when trend weakens (ADX below 20)
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit Long: ADX={adxMain}");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit Short: ADX={adxMain}");
				}
			}
		}

		private void UpdateSentiment(ICandleMessage candle)
		{
			// This is a placeholder for real sentiment analysis data
			// In a real implementation, this would connect to a sentiment data provider

			// Update sentiment values
			_prevSentiment = _currentSentiment;

			// Simulate sentiment based on price action and some randomness
			_currentSentiment = SimulateSentiment(candle);

			// Calculate momentum as the change in sentiment
			_sentimentMomentum = _currentSentiment - _prevSentiment;
		}

		private decimal SimulateSentiment(ICandleMessage candle)
		{
			// Base sentiment on price movement (up = positive sentiment, down = negative sentiment)
			var priceUp = candle.OpenPrice < candle.ClosePrice;
			var priceChange = (candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice;

			// Calculate base sentiment from price change
			var baseSentiment = priceChange * 10; // Scale up for easier interpretation

			// Add noise to simulate real-world sentiment data
			var noise = (decimal)(RandomGen.GetDouble() * 0.2 - 0.1);

			// Sometimes sentiment can diverge from price action
			if (RandomGen.GetDouble() > 0.7)
			{
				noise *= 2; // Occasionally larger divergences
			}

			return baseSentiment + noise;
		}
	}
}
