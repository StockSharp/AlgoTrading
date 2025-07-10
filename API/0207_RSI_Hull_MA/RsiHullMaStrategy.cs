using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on RSI and Hull Moving Average indicators (#207)
	/// </summary>
	public class RsiHullMaStrategy : Strategy
	{
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<int> _hullPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _previousHullValue;

		/// <summary>
		/// RSI period
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
		}

		/// <summary>
		/// Hull MA period
		/// </summary>
		public int HullPeriod
		{
			get => _hullPeriod.Value;
			set => _hullPeriod.Value = value;
		}

		/// <summary>
		/// ATR period for stop-loss
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop-loss
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type for strategy
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public RsiHullMaStrategy()
		{
			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetRange(5, 30)
				.SetDisplay("RSI Period", "Period for RSI indicator", "Indicators")
				.SetCanOptimize(true);

			_hullPeriod = Param(nameof(HullPeriod), 9)
				.SetRange(5, 20)
				.SetDisplay("Hull MA Period", "Period for Hull Moving Average", "Indicators")
				.SetCanOptimize(true);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetRange(7, 28)
				.SetDisplay("ATR Period", "ATR period for stop-loss calculation", "Risk Management")
				.SetCanOptimize(true);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetRange(1m, 4m)
				.SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")
				.SetCanOptimize(true);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
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

			_previousHullValue = default;

			// Initialize indicators
			var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
			var hullMA = new HullMovingAverage { Length = HullPeriod };
			var atr = new AverageTrueRange { Length = AtrPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(rsi, hullMA, atr, ProcessIndicators)
				.Start();
			
			// Enable ATR-based stop protection
			StartProtection(default, new Unit(AtrMultiplier, UnitTypes.Absolute));

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, rsi);
				DrawIndicator(area, hullMA);
				DrawOwnTrades(area);
			}
		}

		private void ProcessIndicators(ICandleMessage candle, decimal rsiValue, decimal hullValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Store previous Hull value for slope detection
			var previousHullValue = _previousHullValue;
			_previousHullValue = hullValue;

			// Skip first candle until we have previous value
			if (previousHullValue == 0)
				return;

			// Trading logic:
			// Long: RSI < 30 && HMA(t) > HMA(t-1) (oversold with rising HMA)
			// Short: RSI > 70 && HMA(t) < HMA(t-1) (overbought with falling HMA)
			
			var hullSlope = hullValue > previousHullValue;

			if (rsiValue < 30 && hullSlope && Position <= 0)
			{
				// Buy signal - RSI oversold with rising HMA
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (rsiValue > 70 && !hullSlope && Position >= 0)
			{
				// Sell signal - RSI overbought with falling HMA
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Exit conditions
			else if (Position > 0 && rsiValue > 50)
			{
				// Exit long position when RSI returns to neutral zone
				SellMarket(Position);
			}
			else if (Position < 0 && rsiValue < 50)
			{
				// Exit short position when RSI returns to neutral zone
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
