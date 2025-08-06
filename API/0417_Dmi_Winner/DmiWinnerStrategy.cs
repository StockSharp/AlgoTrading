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
	/// Directional Movement Index Winner Strategy
	/// </summary>
	public class DmiWinnerStrategy : Strategy
	{
		private decimal _prevDiPlus;
		private decimal _prevPrevDiPlus;
		private decimal _prevDiMinus;
		private decimal _prevPrevDiMinus;

		public DmiWinnerStrategy()
		{
			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

			// DMI
			_diLength = Param(nameof(DILength), 14)
				.SetGreaterThanZero()
				.SetDisplay("DI Length", "Directional Indicator period", "DMI");

			_adxSmoothing = Param(nameof(ADXSmoothing), 13)
				.SetGreaterThanZero()
				.SetDisplay("ADX Smoothing", "ADX smoothing period", "DMI");

			_keyLevel = Param(nameof(KeyLevel), 23m)
				.SetDisplay("Key Level", "ADX key level threshold", "DMI");

			// Moving Average
			_useMA = Param(nameof(UseMA), true)
				.SetDisplay("Use MA", "Enable moving average filter", "Moving Average");

			_maType = Param(nameof(MAType), "EMA")
				.SetDisplay("MA Type", "Moving average type (EMA/SMA)", "Moving Average");

			_maLength = Param(nameof(MALength), 55)
				.SetGreaterThanZero()
				.SetDisplay("MA Length", "Moving average period", "Moving Average");

			// Strategy
			_showLong = Param(nameof(ShowLong), true)
				.SetDisplay("Long entries", "Enable long entries", "Strategy");

			_showShort = Param(nameof(ShowShort), false)
				.SetDisplay("Short entries", "Enable short entries", "Strategy");

			// Stop Loss
			_useSL = Param(nameof(UseSL), false)
				.SetDisplay("Use Stop Loss", "Enable stop loss", "Stop Loss");

			_slPercent = Param(nameof(SLPercent), 10m)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Stop Loss");
		}

		#region Parameters

		private readonly StrategyParam<DataType> _candleTypeParam;
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		private readonly StrategyParam<int> _diLength;
		public int DILength
		{
			get => _diLength.Value;
			set => _diLength.Value = value;
		}

		private readonly StrategyParam<int> _adxSmoothing;
		public int ADXSmoothing
		{
			get => _adxSmoothing.Value;
			set => _adxSmoothing.Value = value;
		}

		private readonly StrategyParam<decimal> _keyLevel;
		public decimal KeyLevel
		{
			get => _keyLevel.Value;
			set => _keyLevel.Value = value;
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

			_prevDiPlus = 0;
			_prevPrevDiPlus = 0;
			_prevDiMinus = 0;
			_prevPrevDiMinus = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			var dmi = new DirectionalIndex
			{
				Length = DILength
			};

			var adx = new AverageDirectionalIndex
			{
				Length = ADXSmoothing
			};

			IIndicator ma = null;
			if (UseMA)
			{
				ma = MAType == "EMA" 
					? new ExponentialMovingAverage { Length = MALength }
					: new SimpleMovingAverage { Length = MALength };
			}

			// Subscribe to candles
			var subscription = SubscribeCandles(CandleType);

			if (UseMA)
			{
				subscription
					.BindEx(dmi, adx, ma, OnProcessWithMA)
					.Start();
			}
			else
			{
				subscription
					.BindEx(dmi, adx, OnProcessWithoutMA)
					.Start();
			}

			// Configure chart
			var area = CreateChartArea();

			if (area != null)
			{
				DrawCandles(area, subscription);
				if (UseMA && ma != null)
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

		private void OnProcessWithMA(ICandleMessage candle, 
			IIndicatorValue dmiValue, IIndicatorValue adxValue, IIndicatorValue maValue)
		{
			ProcessCandle(candle, dmiValue, adxValue, maValue.ToDecimal());
		}

		private void OnProcessWithoutMA(ICandleMessage candle, 
			IIndicatorValue dmiValue, IIndicatorValue adxValue)
		{
			ProcessCandle(candle, dmiValue, adxValue, 0);
		}

		private void ProcessCandle(ICandleMessage candle, 
			IIndicatorValue dmiValue, IIndicatorValue adxValue, decimal maValue)
		{
			// Only process finished candles
			if (candle.State != CandleStates.Finished)
				return;

			var closePrice = candle.ClosePrice;
			var openPrice = candle.OpenPrice;
			
			var dmiTyped = (DirectionalIndexValue)dmiValue;
			var diPlus = dmiTyped.Plus ?? 0m;
			var diMinus = dmiTyped.Minus ?? 0m;
			var adxValueDecimal = adxValue.ToDecimal();

			// Check for 3 consecutive bars condition
			var longCond = false;
			var shortCond = false;

			if (_prevDiPlus > 0 && _prevPrevDiPlus > 0 && 
				_prevDiMinus > 0 && _prevPrevDiMinus > 0)
			{
				longCond = diPlus > diMinus && 
						  _prevDiPlus > _prevDiMinus && 
						  _prevPrevDiPlus > _prevPrevDiMinus;
				
				shortCond = diPlus < diMinus && 
						   _prevDiPlus < _prevDiMinus && 
						   _prevPrevDiPlus < _prevPrevDiMinus;
			}

			// MA filter
			var buyMAFilter = !UseMA || closePrice > maValue;
			var sellMAFilter = !UseMA || closePrice < maValue;

			// Entry conditions
			var longEntry = longCond && adxValueDecimal > KeyLevel && buyMAFilter && closePrice > openPrice;
			var shortEntry = shortCond && adxValueDecimal > KeyLevel && sellMAFilter && closePrice < openPrice;

			// Execute trades based on enabled directions
			if (ShowLong && !ShowShort)
			{
				if (longEntry && Position == 0)
				{
					RegisterOrder(this.BuyMarket(Volume));
				}
				else if (shortEntry && Position > 0)
				{
					RegisterOrder(this.SellMarket(Position));
				}
			}
			else if (!ShowLong && ShowShort)
			{
				if (shortEntry && Position == 0)
				{
					RegisterOrder(this.SellMarket(Volume));
				}
				else if (longEntry && Position < 0)
				{
					RegisterOrder(this.BuyMarket(Position.Abs()));
				}
			}
			else if (ShowLong && ShowShort)
			{
				if (longEntry)
				{
					if (Position < 0)
					{
						RegisterOrder(this.BuyMarket(Position.Abs() + Volume));
					}
					else if (Position == 0)
					{
						RegisterOrder(this.BuyMarket(Volume));
					}
				}
				else if (shortEntry)
				{
					if (Position > 0)
					{
						RegisterOrder(this.SellMarket(Position + Volume));
					}
					else if (Position == 0)
					{
						RegisterOrder(this.SellMarket(Volume));
					}
				}
			}

			// Update history
			_prevPrevDiPlus = _prevDiPlus;
			_prevPrevDiMinus = _prevDiMinus;
			_prevDiPlus = diPlus;
			_prevDiMinus = diMinus;
		}
	}
}