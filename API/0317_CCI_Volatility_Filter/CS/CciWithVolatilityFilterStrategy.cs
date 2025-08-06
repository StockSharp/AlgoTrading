using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on CCI with Volatility Filter.
	/// </summary>
	public class CciWithVolatilityFilterStrategy : Strategy
	{
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _cciOversold;
		private readonly StrategyParam<decimal> _cciOverbought;
		private readonly StrategyParam<DataType> _candleType;
		private CommodityChannelIndex _cci;
		private AverageTrueRange _atr;
		private SimpleMovingAverage _atrSma;

		/// <summary>
		/// CCI period parameter.
		/// </summary>
		public int CciPeriod
		{
			get => _cciPeriod.Value;
			set => _cciPeriod.Value = value;
		}

		/// <summary>
		/// ATR period parameter.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// CCI oversold level parameter.
		/// </summary>
		public decimal CciOversold
		{
			get => _cciOversold.Value;
			set => _cciOversold.Value = value;
		}

		/// <summary>
		/// CCI overbought level parameter.
		/// </summary>
		public decimal CciOverbought
		{
			get => _cciOverbought.Value;
			set => _cciOverbought.Value = value;
		}

		/// <summary>
		/// Candle type parameter.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public CciWithVolatilityFilterStrategy()
		{
			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("CCI Period", "Period for CCI calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 28, 7);

			_cciOversold = Param(nameof(CciOversold), -100m)
				.SetDisplay("CCI Oversold", "CCI oversold level", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(-150, -50, 25);

			_cciOverbought = Param(nameof(CciOverbought), 100m)
				.SetDisplay("CCI Overbought", "CCI overbought level", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(50, 150, 25);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		protected override void OnReseted()
		{
			base.OnReseted();

			_cci?.Reset();
			_atr?.Reset();
			_atrSma?.Reset();
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_cci = new CommodityChannelIndex { Length = CciPeriod };
			_atr = new AverageTrueRange { Length = AtrPeriod };
			_atrSma = new SimpleMovingAverage { Length = AtrPeriod };

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);

			subscription
				.Bind(_cci, _atr, (candle, cciValue, atrValue) =>
				{
					// Calculate ATR average
					var atrAvg = _atrSma.Process(atrValue, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();

					// Process the strategy logic
					ProcessStrategy(candle, cciValue, atrValue, atrAvg);
				})
				.Start();

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _cci);
				DrawIndicator(area, _atr);
				DrawOwnTrades(area);
			}

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
			);
		}

		private void ProcessStrategy(ICandleMessage candle, decimal cciValue, decimal atrValue, decimal atrAvg)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check volatility - only trade in low volatility environment
			var isLowVolatility = atrValue < atrAvg;
			
			// Trading logic - only enter during low volatility
			if (isLowVolatility)
			{
				if (cciValue < CciOversold && Position <= 0)
				{
					// CCI oversold in low volatility - Go long
					CancelActiveOrders();
					
					// Calculate position size
					var volume = Volume + Math.Abs(Position);
					
					// Enter long position
					BuyMarket(volume);
				}
				else if (cciValue > CciOverbought && Position >= 0)
				{
					// CCI overbought in low volatility - Go short
					CancelActiveOrders();
					
					// Calculate position size
					var volume = Volume + Math.Abs(Position);
					
					// Enter short position
					SellMarket(volume);
				}
			}
			
			// Exit logic - when CCI crosses over zero
			if ((Position > 0 && cciValue > 0) || (Position < 0 && cciValue < 0))
			{
				// Close position
				ClosePosition();
			}
		}
	}
}
