using Ecng.Common;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Parabolic SAR strategy with sentiment divergence.
	/// </summary>
	public class ParabolicSarSentimentDivergenceStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _startAf;
		private readonly StrategyParam<decimal> _maxAf;
		private readonly StrategyParam<DataType> _candleType;

		private ParabolicSar _parabolicSar;
		private decimal _prevSentiment;
		private decimal _prevPrice;
		private bool _isFirstCandle = true;

		/// <summary>
		/// SAR Starting acceleration factor.
		/// </summary>
		public decimal StartAf
		{
			get => _startAf.Value;
			set => _startAf.Value = value;
		}

		/// <summary>
		/// SAR Maximum acceleration factor.
		/// </summary>
		public decimal MaxAf
		{
			get => _maxAf.Value;
			set => _maxAf.Value = value;
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
		/// Initialize <see cref="ParabolicSarSentimentDivergenceStrategy"/>.
		/// </summary>
		public ParabolicSarSentimentDivergenceStrategy()
		{
			_startAf = Param(nameof(StartAf), 0.02m)
			.SetRange(0.01m, 0.1m)
			.SetCanOptimize(true)
			.SetDisplay("Starting AF", "Starting acceleration factor for Parabolic SAR", "SAR Parameters");

			_maxAf = Param(nameof(MaxAf), 0.2m)
			.SetRange(0.1m, 0.5m)
			.SetCanOptimize(true)
			.SetDisplay("Maximum AF", "Maximum acceleration factor for Parabolic SAR", "SAR Parameters");

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

			_parabolicSar = null;
			_prevSentiment = default;
			_prevPrice = default;
			_isFirstCandle = true;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicator
			_parabolicSar = new ParabolicSar
			{
				Acceleration = StartAf,
				AccelerationMax = MaxAf,
			};


			// Create subscription
			var subscription = SubscribeCandles(CandleType);

			// Bind indicator and processor
			subscription
			.BindEx(_parabolicSar, ProcessCandle)
			.Start();

			// Setup visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _parabolicSar);
				DrawOwnTrades(area);
			}

			// Start position protection
			StartProtection(
			new Unit(2, UnitTypes.Percent),   // Take profit 2%
			new Unit(2, UnitTypes.Percent),   // Stop loss 2%
			true							 // Use trailing stop
			);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
			return;

			// Get SAR value
			var sarPrice = sarValue.ToDecimal();

			// Get current price and sentiment
			var price = candle.ClosePrice;
			var sentiment = GetSentiment();  // In real implementation, this would come from external API

			// Skip first candle to initialize previous values
			if (_isFirstCandle)
			{
				_prevPrice = price;
				_prevSentiment = sentiment;
				_isFirstCandle = false;
				return;
			}

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
			return;

			// Bullish divergence: Price falling but sentiment rising
			bool bullishDivergence = price < _prevPrice && sentiment > _prevSentiment;

			// Bearish divergence: Price rising but sentiment falling
			bool bearishDivergence = price > _prevPrice && sentiment < _prevSentiment;

			// Entry logic
			if (price > sarPrice && bullishDivergence && Position <= 0)
			{
				// Bullish divergence and price above SAR - Long entry
				BuyMarket(Volume);
				LogInfo($"Buy Signal: SAR={sarPrice}, Price={price}, Sentiment={sentiment}");
			}
			else if (price < sarPrice && bearishDivergence && Position >= 0)
			{
				// Bearish divergence and price below SAR - Short entry
				SellMarket(Volume);
				LogInfo($"Sell Signal: SAR={sarPrice}, Price={price}, Sentiment={sentiment}");
			}

			// Exit logic - handled by Parabolic SAR itself
			if (Position > 0 && price < sarPrice)
			{
				// Long position and price below SAR - Exit
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit Long: SAR={sarPrice}, Price={price}");
			}
			else if (Position < 0 && price > sarPrice)
			{
				// Short position and price above SAR - Exit
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit Short: SAR={sarPrice}, Price={price}");
			}

			// Update previous values
			_prevPrice = price;
			_prevSentiment = sentiment;
		}

		private decimal GetSentiment()
		{
			// This is a placeholder for a real sentiment analysis
			// In a real implementation, this would connect to a sentiment data provider
			// Returning a random value between -1 and 1 for simulation
			return (decimal)(RandomGen.GetDouble() * 2 - 1);
		}
	}
}
