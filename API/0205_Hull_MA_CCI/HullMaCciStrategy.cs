using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Hull Moving Average and CCI indicators (#205)
	/// </summary>
	public class HullMaCciStrategy : Strategy
	{
		private readonly StrategyParam<int> _hullPeriod;
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _previousHullValue;

		/// <summary>
		/// Hull MA period
		/// </summary>
		public int HullPeriod
		{
			get => _hullPeriod.Value;
			set => _hullPeriod.Value = value;
		}

		/// <summary>
		/// CCI period
		/// </summary>
		public int CciPeriod
		{
			get => _cciPeriod.Value;
			set => _cciPeriod.Value = value;
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
		public HullMaCciStrategy()
		{
			_hullPeriod = Param(nameof(HullPeriod), 9)
				.SetRange(5, 20)
				.SetDisplay("Hull MA Period", "Period for Hull Moving Average", "Indicators")
				.SetCanOptimize(true);

			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetRange(10, 50)
				.SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")
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

			// Initialize indicators
			var hullMA = new HullMovingAverage { Length = HullPeriod };
			var cci = new CommodityChannelIndex { Length = CciPeriod };
			var atr = new AverageTrueRange { Length = AtrPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(hullMA, cci, atr, ProcessIndicators)
				.Start();
			
			// Enable ATR-based stop protection
			StartProtection(default, new Unit(AtrMultiplier, UnitTypes.Absolute));

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, hullMA);
				DrawIndicator(area, cci);
				DrawOwnTrades(area);
			}
		}

		private void ProcessIndicators(ICandleMessage candle, decimal hullValue, decimal cciValue, decimal atrValue)
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
			// Long: HMA(t) > HMA(t-1) && CCI < -100 (HMA rising with oversold conditions)
			// Short: HMA(t) < HMA(t-1) && CCI > 100 (HMA falling with overbought conditions)
			
			var hullSlope = hullValue > previousHullValue;

			if (hullSlope && cciValue < -100 && Position <= 0)
			{
				// Buy signal - HMA rising with oversold CCI
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (!hullSlope && cciValue > 100 && Position >= 0)
			{
				// Sell signal - HMA falling with overbought CCI
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Exit conditions based on HMA slope change
			else if (Position > 0 && !hullSlope)
			{
				// Exit long position when HMA starts falling
				SellMarket(Position);
			}
			else if (Position < 0 && hullSlope)
			{
				// Exit short position when HMA starts rising
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
