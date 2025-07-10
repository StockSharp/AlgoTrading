using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on ADX and CCI indicators.
	/// Enters long when ADX > 25 and CCI is oversold (< -100)
	/// Enters short when ADX > 25 and CCI is overbought (> 100)
	/// </summary>
	public class AdxCciStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _prevCciValue;
		private bool _isFirstValue = true;

		/// <summary>
		/// ADX period
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
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
		/// Stop-loss percentage
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Candle type for strategy calculation
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public AdxCciStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Period", "Period for ADX indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 1);

			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(14, 30, 1);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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

			// Create indicators
			var adx = new AverageDirectionalIndex { Length = AdxPeriod };
			var cci = new CommodityChannelIndex { Length = CciPeriod };

			// Reset state variables
			_prevCciValue = 0;
			_isFirstValue = true;
			
			// Enable position protection with stop-loss
			StartProtection(
				takeProfit: new Unit(0), // No take profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(adx, cci, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, adx);
				
				// Create a separate area for CCI
				var cciArea = CreateChartArea();
				if (cciArea != null)
				{
					DrawIndicator(cciArea, cci);
				}
				
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue cciValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// For the first value, just store and skip trading
			if (_isFirstValue)
			{
				_prevCciValue = cciValue.ToDecimal();
				_isFirstValue = false;
				return;
			}

			// Store for the next iteration
			_prevCciValue = cciValue.ToDecimal();

			var adxTyped = (AverageDirectionalIndexValue)adxValue;
			var adxMa = adxTyped.MovingAverage;

			// Trading logic
			if (adxMa > 25)
			{
				if (_prevCciValue < -100 && Position <= 0)
				{
					// Strong trend with oversold CCI - Buy
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (_prevCciValue > 100 && Position >= 0)
				{
					// Strong trend with overbought CCI - Sell
					SellMarket(Volume + Math.Abs(Position));
				}
			}
			else if (adxMa < 20)
			{
				// Trend is weakening - close any position
				if (Position > 0)
					SellMarket(Position);
				else if (Position < 0)
					BuyMarket(Math.Abs(Position));
			}
		}
	}
}