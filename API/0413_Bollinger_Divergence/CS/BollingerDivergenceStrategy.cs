using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Drawing;

using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Charting;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Bollinger Bands Divergence Strategy
	/// </summary>
	public class BollingerDivergenceStrategy : Strategy
	{
		private decimal _prevUpperBand;
		private decimal _prevLowerBand;

		public BollingerDivergenceStrategy()
		{
			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

			_bbLength = Param(nameof(BBLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

			_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
				.SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

			_candlePercent = Param(nameof(CandlePercent), 30m)
				.SetDisplay("Candle %", "Candle percentage below/above the BB", "Strategy");

			_takeProfit = Param(nameof(TakeProfit), 5m)
				.SetDisplay("Take Profit %", "Take profit percentage", "Strategy");
		}

		private readonly StrategyParam<DataType> _candleTypeParam;
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		private readonly StrategyParam<int> _bbLength;
		public int BBLength
		{
			get => _bbLength.Value;
			set => _bbLength.Value = value;
		}

		private readonly StrategyParam<decimal> _bbMultiplier;
		public decimal BBMultiplier
		{
			get => _bbMultiplier.Value;
			set => _bbMultiplier.Value = value;
		}

		private readonly StrategyParam<decimal> _candlePercent;
		public decimal CandlePercent
		{
			get => _candlePercent.Value;
			set => _candlePercent.Value = value;
		}

		private readonly StrategyParam<decimal> _takeProfit;
		public decimal TakeProfit
		{
			get => _takeProfit.Value;
			set => _takeProfit.Value = value;
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
			=> new[] { (Security, CandleType) };

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();

			_prevUpperBand = 0;
			_prevLowerBand = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create Bollinger Bands indicator
			var bollinger = new BollingerBands
			{
				Length = BBLength,
				Width = BBMultiplier
			};

			// Subscribe to candles
			var subscription = SubscribeCandles(CandleType);

			subscription
				.BindEx(bollinger, OnProcess)
				.Start();

			// Configure chart
			var area = CreateChartArea();

			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, bollinger);
				DrawOwnTrades(area);
			}
		}

		private void OnProcess(ICandleMessage candle, IIndicatorValue bollingerValue)
		{
			// Only process finished candles
			if (candle.State != CandleStates.Finished)
				return;

			var closePrice = candle.ClosePrice;
			var openPrice = candle.OpenPrice;
			var highPrice = candle.HighPrice;
			var lowPrice = candle.LowPrice;
			
			var bollingerTyped = (BollingerBandsValue)bollingerValue;
			var upperBand = bollingerTyped.UpBand;
			var lowerBand = bollingerTyped.LowBand;
			var middleBand = bollingerTyped.MovingAverage;

			// Check for divergence
			var buySignal = false;
			var sellSignal = false;

			if (_prevUpperBand > 0 && _prevLowerBand > 0)
			{
				// Buy signal: close > upper, upper expanding, lower contracting, bullish candle
				buySignal = closePrice > upperBand && 
						   upperBand > _prevUpperBand && 
						   lowerBand < _prevLowerBand && 
						   closePrice > openPrice;

				// Sell signal: close < lower, lower contracting, upper expanding, bearish candle
				sellSignal = closePrice < lowerBand && 
							lowerBand < _prevLowerBand && 
							upperBand > _prevUpperBand && 
							closePrice < openPrice;
			}

			// Calculate entry zones
			var candleSize = highPrice - lowPrice;
			var buyZone = highPrice - (candleSize * ((100 - CandlePercent) / 100));
			var sellZone = lowPrice + (candleSize * ((100 - CandlePercent) / 100));

			// Long entry
			if (buySignal && buyZone > upperBand && Position == 0)
			{
				RegisterOrder(this.BuyMarket(Volume));
			}
			// Long exit
			else if (Position > 0 && closePrice < middleBand)
			{
				RegisterOrder(this.SellMarket(Position));
			}

			// Short entry
			if (sellSignal && sellZone < lowerBand && Position == 0)
			{
				RegisterOrder(this.SellMarket(Volume));
			}
			// Short exit
			else if (Position < 0 && closePrice > middleBand)
			{
				RegisterOrder(this.BuyMarket(Position.Abs()));
			}

			// Update previous bands
			_prevUpperBand = upperBand.Value;
			_prevLowerBand = lowerBand.Value;
		}
	}
}