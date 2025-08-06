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
	/// Bollinger Bands Winner LITE Strategy
	/// </summary>
	public class BollingerWinnerLiteStrategy : Strategy
	{
		public BollingerWinnerLiteStrategy()
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

			_showShort = Param(nameof(ShowShort), false)
				.SetDisplay("Short entries", "Enable short entries", "Strategy");
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

		private readonly StrategyParam<bool> _showShort;
		public bool ShowShort
		{
			get => _showShort.Value;
			set => _showShort.Value = value;
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
			=> new[] { (Security, CandleType) };

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

			// Calculate entry zones
			var candleSize = highPrice - lowPrice;
			var candlePercent = CandlePercent * 0.01m;
			var buyZone = (candleSize * candlePercent) + lowPrice;
			var sellZone = highPrice - (candleSize * candlePercent);

			// Body size check
			var bodySize = closePrice > openPrice ? closePrice - openPrice : openPrice - closePrice;
			var bs60 = bodySize * 0.6m;
			var bsBuy = lowPrice + bs60;

			// Buy signal
			var buy = buyZone < lowerBand && !(bsBuy < lowerBand) && closePrice < openPrice;

			// Sell signal  
			var sell = sellZone > upperBand && closePrice > openPrice;

			// Long entry
			if (buy && Position == 0)
			{
				RegisterOrder(this.BuyMarket(Volume));
			}
			// Exit or Short entry
			else if (sell)
			{
				if (Position > 0)
				{
					RegisterOrder(this.SellMarket(Position));
				}
				else if (ShowShort && Position == 0)
				{
					RegisterOrder(this.SellMarket(Volume));
				}
			}
		}
	}
}