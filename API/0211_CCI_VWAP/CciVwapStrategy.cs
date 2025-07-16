using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that uses CCI and VWAP indicators to identify oversold and overbought conditions.
	/// Enters long when CCI is below -100 and price is below VWAP.
	/// Enters short when CCI is above 100 and price is above VWAP.
	/// </summary>
	public class CciVwapStrategy : Strategy
	{
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		
		private CommodityChannelIndex _cci;
		private decimal _currentVwap;
		
		/// <summary>
		/// CCI period parameter.
		/// </summary>
		public int CciPeriod
		{
			get => _cciPeriod.Value;
			set => _cciPeriod.Value = value;
		}
		
		/// <summary>
		/// Stop-loss percentage parameter.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
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
		/// Strategy constructor.
		/// </summary>
		public CciVwapStrategy()
		{
			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("CCI period", "CCI indicator period", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle type", "Type of candles to use", "General");
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
			
			// Initialize CCI indicator
			_cci = new CommodityChannelIndex
			{
				Length = CciPeriod
			};
			
			// Create subscription for Level1 to get VWAP
			SubscribeLevel1()
				.Bind(ProcessLevel1)
				.Start();

			// Bind CCI to candle subscription
			var candlesSubscription = SubscribeCandles(CandleType)
				.Bind(_cci, ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take-profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent) // Stop-loss as percentage
			);
			
			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, candlesSubscription);
				DrawIndicator(area, _cci);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessLevel1(Level1ChangeMessage level1)
		{
			if (level1.Changes.TryGetValue(Level1Fields.VWAP, out var vwap))
			{
				_currentVwap = (decimal)vwap;
			}
		}
		
		private void ProcessCandle(ICandleMessage candle, decimal cciValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
				
			// Skip if strategy is not ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Skip if we don't have VWAP yet
			if (_currentVwap == 0)
				return;
				
			// Long signal: CCI below -100 and price below VWAP
			if (cciValue < -100 && candle.ClosePrice < _currentVwap && Position <= 0)
			{
				BuyMarket(Volume);
				LogInfo($"Buy signal: CCI={cciValue:F2}, Price={candle.ClosePrice}, VWAP={_currentVwap}");
			}
			// Short signal: CCI above 100 and price above VWAP
			else if (cciValue > 100 && candle.ClosePrice > _currentVwap && Position >= 0)
			{
				SellMarket(Volume);
				LogInfo($"Sell signal: CCI={cciValue:F2}, Price={candle.ClosePrice}, VWAP={_currentVwap}");
			}
			// Exit long position: Price crosses above VWAP
			else if (Position > 0 && candle.ClosePrice > _currentVwap)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit long: Price={candle.ClosePrice}, VWAP={_currentVwap}");
			}
			// Exit short position: Price crosses below VWAP
			else if (Position < 0 && candle.ClosePrice < _currentVwap)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: Price={candle.ClosePrice}, VWAP={_currentVwap}");
			}
		}
	}
}
