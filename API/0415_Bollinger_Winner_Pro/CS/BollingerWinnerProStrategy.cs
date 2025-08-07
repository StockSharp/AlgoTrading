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
	/// Bollinger Bands Winner PRO Strategy with RSI, Aroon and MA filters
	/// </summary>
	public class BollingerWinnerProStrategy : Strategy
	{
		private decimal _rsiValue;
		private decimal _aroonUpValue;
		private decimal _maValue;
		private bool _inPosition;

		public BollingerWinnerProStrategy()
		{
			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

			// Bollinger Bands
			_bbLength = Param(nameof(BBLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

			_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
				.SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

			// RSI Filter
			_useRSI = Param(nameof(UseRSI), true)
				.SetDisplay("Use RSI", "Enable RSI filter", "RSI Filter");

			_rsiLength = Param(nameof(RSILength), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Length", "RSI period", "RSI Filter");

			_rsiAbove = Param(nameof(RSIAbove), 45m)
				.SetDisplay("RSI Above", "RSI threshold for long", "RSI Filter");

			_rsiBelow = Param(nameof(RSIBelow), 55m)
				.SetDisplay("RSI Below", "RSI threshold for short", "RSI Filter");

			// Aroon Filter
			_useAroon = Param(nameof(UseAroon), false)
				.SetDisplay("Use Aroon", "Enable Aroon filter", "Aroon Filter");

			_aroonLength = Param(nameof(AroonLength), 288)
				.SetGreaterThanZero()
				.SetDisplay("Aroon Period", "Aroon indicator period", "Aroon Filter");

			_aroonConfirmation = Param(nameof(AroonConfirmation), 90m)
				.SetDisplay("Aroon Confirmation", "Aroon confirmation level", "Aroon Filter");

			_aroonStop = Param(nameof(AroonStop), 70m)
				.SetDisplay("Aroon Stop", "Aroon stop level", "Aroon Filter");

			// Moving Average
			_useMA = Param(nameof(UseMA), true)
				.SetDisplay("Use MA", "Enable moving average filter", "Moving Average");

			_maType = Param(nameof(MAType), "EMA")
				.SetDisplay("MA Type", "Moving average type (EMA/SMA)", "Moving Average");

			_maLength = Param(nameof(MALength), 200)
				.SetGreaterThanZero()
				.SetDisplay("MA Length", "Moving average period", "Moving Average");

			// Strategy
			_candlePercent = Param(nameof(CandlePercent), 30m)
				.SetDisplay("Candle %", "Candle percentage for entry zones", "Strategy");

			_showLong = Param(nameof(ShowLong), true)
				.SetDisplay("Long entries", "Enable long entries", "Strategy");

			_showShort = Param(nameof(ShowShort), false)
				.SetDisplay("Short entries", "Enable short entries", "Strategy");

			_closeEarly = Param(nameof(CloseEarly), false)
				.SetDisplay("Close early", "Close position early when in profit", "Strategy");

			// Stop Loss
			_useSL = Param(nameof(UseSL), true)
				.SetDisplay("Use Stop Loss", "Enable stop loss", "Stop Loss");

			_slPercent = Param(nameof(SLPercent), 6m)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Stop Loss");
		}

		#region Parameters

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

		private readonly StrategyParam<bool> _useRSI;
		public bool UseRSI
		{
			get => _useRSI.Value;
			set => _useRSI.Value = value;
		}

		private readonly StrategyParam<int> _rsiLength;
		public int RSILength
		{
			get => _rsiLength.Value;
			set => _rsiLength.Value = value;
		}

		private readonly StrategyParam<decimal> _rsiAbove;
		public decimal RSIAbove
		{
			get => _rsiAbove.Value;
			set => _rsiAbove.Value = value;
		}

		private readonly StrategyParam<decimal> _rsiBelow;
		public decimal RSIBelow
		{
			get => _rsiBelow.Value;
			set => _rsiBelow.Value = value;
		}

		private readonly StrategyParam<bool> _useAroon;
		public bool UseAroon
		{
			get => _useAroon.Value;
			set => _useAroon.Value = value;
		}

		private readonly StrategyParam<int> _aroonLength;
		public int AroonLength
		{
			get => _aroonLength.Value;
			set => _aroonLength.Value = value;
		}

		private readonly StrategyParam<decimal> _aroonConfirmation;
		public decimal AroonConfirmation
		{
			get => _aroonConfirmation.Value;
			set => _aroonConfirmation.Value = value;
		}

		private readonly StrategyParam<decimal> _aroonStop;
		public decimal AroonStop
		{
			get => _aroonStop.Value;
			set => _aroonStop.Value = value;
		}

		private readonly StrategyParam<bool> _useMA;
		public bool UseMA
		{
			get => _useMA.Value;
			set => _useMA.Value = value;
		}

		private readonly StrategyParam<string> _maType;
		public string MAType
		{
			get => _maType.Value;
			set => _maType.Value = value;
		}

		private readonly StrategyParam<int> _maLength;
		public int MALength
		{
			get => _maLength.Value;
			set => _maLength.Value = value;
		}

		private readonly StrategyParam<decimal> _candlePercent;
		public decimal CandlePercent
		{
			get => _candlePercent.Value;
			set => _candlePercent.Value = value;
		}

		private readonly StrategyParam<bool> _showLong;
		public bool ShowLong
		{
			get => _showLong.Value;
			set => _showLong.Value = value;
		}

		private readonly StrategyParam<bool> _showShort;
		public bool ShowShort
		{
			get => _showShort.Value;
			set => _showShort.Value = value;
		}

		private readonly StrategyParam<bool> _closeEarly;
		public bool CloseEarly
		{
			get => _closeEarly.Value;
			set => _closeEarly.Value = value;
		}

		private readonly StrategyParam<bool> _useSL;
		public bool UseSL
		{
			get => _useSL.Value;
			set => _useSL.Value = value;
		}

		private readonly StrategyParam<decimal> _slPercent;
		public decimal SLPercent
		{
			get => _slPercent.Value;
			set => _slPercent.Value = value;
		}

		#endregion

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
			=> new[] { (Security, CandleType) };

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();

			_rsiValue = 0;
			_aroonUpValue = 0;
			_maValue = 0;
			_inPosition = false;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			var bollinger = new BollingerBands
			{
				Length = BBLength,
				Width = BBMultiplier
			};

			var rsi = new RelativeStrengthIndex { Length = RSILength };
			var aroon = UseAroon ? new Aroon { Length = AroonLength } : null;
			
			IIndicator ma = MAType == "EMA" 
				? new ExponentialMovingAverage { Length = MALength }
				: new SimpleMovingAverage { Length = MALength };

			// Subscribe to candles
			var subscription = SubscribeCandles(CandleType);

			if (UseAroon)
			{
				subscription
					.BindEx(bollinger, rsi, aroon, ma, OnProcessWithAroon)
					.Start();
			}
			else
			{
				subscription
					.BindEx(bollinger, rsi, ma, OnProcessWithoutAroon)
					.Start();
			}

			// Configure chart
			var area = CreateChartArea();

			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, bollinger);
				if (UseMA)
					DrawIndicator(area, ma);
				DrawOwnTrades(area);
			}

			// Start protection if enabled
			if (UseSL)
			{
				var stopValue = new Unit(SLPercent, UnitTypes.Percent);
				StartProtection(new(), stopValue);
			}
		}

		private void OnProcessWithAroon(ICandleMessage candle, 
			IIndicatorValue bollingerValue, IIndicatorValue rsiValue, 
			IIndicatorValue aroonValue, IIndicatorValue maValue)
		{
			_rsiValue = rsiValue.ToDecimal();
			var aroonTyped = (AroonValue)aroonValue;
			_aroonUpValue = aroonTyped.Up.Value;
			_maValue = maValue.ToDecimal();
			
			ProcessCandle(candle, bollingerValue);
		}

		private void OnProcessWithoutAroon(ICandleMessage candle, 
			IIndicatorValue bollingerValue, IIndicatorValue rsiValue, IIndicatorValue maValue)
		{
			_rsiValue = rsiValue.ToDecimal();
			_maValue = maValue.ToDecimal();
			
			ProcessCandle(candle, bollingerValue);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
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

			// Check filters
			var buyRSIFilter = !UseRSI || _rsiValue < RSIAbove;
			var sellRSIFilter = !UseRSI || _rsiValue > RSIBelow;
			
			var buyAroonFilter = !UseAroon || _aroonUpValue > AroonConfirmation;
			var sellAroonFilter = !UseAroon || _aroonUpValue < AroonStop;
			
			var buyMAFilter = !UseMA || closePrice > _maValue;
			var sellMAFilter = !UseMA || closePrice < _maValue;

			// Buy and sell signals
			var buy = buyZone < lowerBand && closePrice < openPrice && 
					 buyRSIFilter && buyAroonFilter && buyMAFilter;
			
			var sell = sellZone > upperBand && closePrice > openPrice && 
					  sellRSIFilter && sellAroonFilter && sellMAFilter;

			// Execute trades
			if (ShowLong && buy && Position == 0)
			{
				RegisterOrder(this.BuyMarket(Volume));
				_inPosition = true;
			}
			else if (_inPosition)
			{
				// Close early if enabled and in profit
				if (CloseEarly && closePrice > upperBand && PnL > 0)
				{
					RegisterOrder(this.SellMarket(Position));
					_inPosition = false;
				}
				// Normal exit
				else if (sell || sellAroonFilter)
				{
					RegisterOrder(this.SellMarket(Position));
					_inPosition = false;
				}
			}

			if (ShowShort && sell && Position == 0)
			{
				RegisterOrder(this.SellMarket(Volume));
				_inPosition = true;
			}
			else if (_inPosition && Position < 0)
			{
				if (CloseEarly && closePrice < lowerBand && PnL > 0)
				{
					RegisterOrder(this.BuyMarket(Position.Abs()));
					_inPosition = false;
				}
			}
		}
	}
}