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
	/// Exceeded Candle Strategy - trades on candle engulfing patterns
	/// </summary>
	public class ExceededCandleStrategy : Strategy
	{
		private ICandleMessage _prevCandle;
		private ICandleMessage _prevPrevCandle;
		private ICandleMessage _prevPrevPrevCandle;

		public ExceededCandleStrategy()
		{
			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

			_bbLength = Param(nameof(BBLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

			_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
				.SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");
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

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
			=> new[] { (Security, CandleType) };

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();
			_prevCandle = null;
			_prevPrevCandle = null;
			_prevPrevPrevCandle = null;
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
			
			var bollingerTyped = (BollingerBandsValue)bollingerValue;
			var upperBand = bollingerTyped.UpBand;
			var middleBand = bollingerTyped.MovingAverage;

			// Check for exceeded candle patterns
			var greenExceeded = false;
			var redExceeded = false;

			if (_prevCandle != null)
			{
				// Green candle surpasses the red one
				greenExceeded = _prevCandle.ClosePrice < _prevCandle.OpenPrice && 
							   closePrice > openPrice && 
							   closePrice > _prevCandle.OpenPrice;

				// Red candle surpasses the green one
				redExceeded = _prevCandle.ClosePrice > _prevCandle.OpenPrice && 
							 closePrice < openPrice && 
							 closePrice < _prevCandle.OpenPrice;
			}

			// Check for 3 consecutive red candles
			var last3Red = false;
			if (_prevCandle != null && _prevPrevCandle != null && _prevPrevPrevCandle != null)
			{
				last3Red = _prevCandle.ClosePrice < _prevCandle.OpenPrice &&
						  _prevPrevCandle.ClosePrice < _prevPrevCandle.OpenPrice &&
						  _prevPrevPrevCandle.ClosePrice < _prevPrevPrevCandle.OpenPrice;
			}

			// Entry and exit conditions
			var buyEntry = greenExceeded && closePrice < middleBand && !last3Red;
			var buyExit = closePrice > upperBand;

			// Execute trades
			if (buyEntry && Position == 0)
			{
				BuyMarket(Volume);
			}
			else if (buyExit && Position > 0)
			{
				SellMarket(Position);
			}

			// Update candle history
			_prevPrevPrevCandle = _prevPrevCandle;
			_prevPrevCandle = _prevCandle;
			_prevCandle = candle;
		}
	}
}