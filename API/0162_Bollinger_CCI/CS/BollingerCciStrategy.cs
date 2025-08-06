using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Implementation of strategy - Bollinger Bands + CCI.
	/// Buy when price is below lower Bollinger Band and CCI is below -100 (oversold).
	/// Sell when price is above upper Bollinger Band and CCI is above 100 (overbought).
	/// </summary>
	public class BollingerCciStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerPeriod;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<decimal> _cciOversold;
		private readonly StrategyParam<decimal> _cciOverbought;
		private readonly StrategyParam<Unit> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Bollinger Bands period.
		/// </summary>
		public int BollingerPeriod
		{
			get => _bollingerPeriod.Value;
			set => _bollingerPeriod.Value = value;
		}

		/// <summary>
		/// Bollinger Bands deviation multiplier.
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
		}

		/// <summary>
		/// CCI period.
		/// </summary>
		public int CciPeriod
		{
			get => _cciPeriod.Value;
			set => _cciPeriod.Value = value;
		}

		/// <summary>
		/// CCI oversold level.
		/// </summary>
		public decimal CciOversold
		{
			get => _cciOversold.Value;
			set => _cciOversold.Value = value;
		}

		/// <summary>
		/// CCI overbought level.
		/// </summary>
		public decimal CciOverbought
		{
			get => _cciOverbought.Value;
			set => _cciOverbought.Value = value;
		}

		/// <summary>
		/// Stop-loss value.
		/// </summary>
		public Unit StopLoss
		{
			get => _stopLoss.Value;
			set => _stopLoss.Value = value;
		}

		/// <summary>
		/// Candle type used for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize <see cref="BollingerCciStrategy"/>.
		/// </summary>
		public BollingerCciStrategy()
		{
			_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Bollinger Parameters");

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Bollinger Parameters");

			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters");

			_cciOversold = Param(nameof(CciOversold), -100m)
				.SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters");

			_cciOverbought = Param(nameof(CciOverbought), 100m)
				.SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters");

			_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Absolute))
				.SetDisplay("Stop Loss", "Stop loss in ATR or value", "Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "General");
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

			Indicators.Clear();
		}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

			// Create indicators
			var bollinger = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};

			var cci = new CommodityChannelIndex { Length = CciPeriod };

			// Setup candle subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to candles
			subscription
				.BindEx(bollinger, cci, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, bollinger);
				
				// Create separate area for CCI
				var cciArea = CreateChartArea();
				if (cciArea != null)
				{
					DrawIndicator(cciArea, cci);
				}
				
				DrawOwnTrades(area);
			}

			// Start protective orders
			StartProtection(new(), StopLoss);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue cciValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// In this function we receive only the middle band value from the Bollinger Bands indicator
			// We need to calculate the upper and lower bands ourselves or get them directly from the indicator

			// Get Bollinger Bands values from the indicator
			var bb = (BollingerBandsValue)bollingerValue;
			var middleBand = bb.MovingAverage;
			var upperBand = bb.UpBand;
			var lowerBand = bb.LowBand;
			var cciTyped = cciValue.ToDecimal();

			// Current price
			var price = candle.ClosePrice;

			LogInfo($"Candle: {candle.OpenTime}, Close: {price}, " +
				$"Upper Band: {upperBand}, Middle Band: {middleBand}, Lower Band: {lowerBand}, " +
				$"CCI: {cciTyped}");

			// Trading rules
			if (price < lowerBand && cciTyped < CciOversold && Position <= 0)
			{
				// Buy signal - price below lower band and CCI oversold
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				
				LogInfo($"Buy signal: Price below lower Bollinger Band and CCI oversold ({cciTyped} < {CciOversold}). Volume: {volume}");
			}
			else if (price > upperBand && cciTyped > CciOverbought && Position >= 0)
			{
				// Sell signal - price above upper band and CCI overbought
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				
				LogInfo($"Sell signal: Price above upper Bollinger Band and CCI overbought ({cciTyped} > {CciOverbought}). Volume: {volume}");
			}
			// Exit conditions
			else if (price > middleBand && Position > 0)
			{
				// Exit long position when price returns to the middle band
				SellMarket(Position);
				LogInfo($"Exit long: Price returned to middle band. Position: {Position}");
			}
			else if (price < middleBand && Position < 0)
			{
				// Exit short position when price returns to the middle band
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: Price returned to middle band. Position: {Position}");
			}
		}
	}
}
