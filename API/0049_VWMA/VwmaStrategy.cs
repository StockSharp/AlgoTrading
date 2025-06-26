using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Volume Weighted Moving Average (VWMA) Strategy
	/// Long entry: Price crosses above VWMA
	/// Short entry: Price crosses below VWMA
	/// Exit: Price crosses back through VWMA
	/// </summary>
	public class VWMAStrategy : Strategy
	{
		private readonly StrategyParam<int> _vwmaPeriod;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _previousClosePrice;
		private decimal _previousVWMA;
		private bool _isFirstCandle;

		/// <summary>
		/// VWMA Period
		/// </summary>
		public int VWMAPeriod
		{
			get => _vwmaPeriod.Value;
			set => _vwmaPeriod.Value = value;
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
		/// Initialize <see cref="VWMAStrategy"/>.
		/// </summary>
		public VWMAStrategy()
		{
			_vwmaPeriod = Param(nameof(VWMAPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("VWMA Period", "Period for Volume Weighted Moving Average calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
				
			_previousClosePrice = 0;
			_previousVWMA = 0;
			_isFirstCandle = true;
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

			// Create VWMA indicator
			var vwma = new VolumeWeightedMovingAverage { Length = VWMAPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(vwma, ProcessCandle)
				.Start();

			// Configure protection
			StartProtection(
				takeProfit: new Unit(3, UnitTypes.Percent),
				stopLoss: new Unit(2, UnitTypes.Percent)
			);

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, vwma);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue vwmaValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract VWMA value from indicator result
			var vwmaPrice = vwmaValue.ToDecimal();
			
			// Skip the first candle, just initialize values
			if (_isFirstCandle)
			{
				_previousClosePrice = candle.ClosePrice;
				_previousVWMA = vwmaPrice;
				_isFirstCandle = false;
				return;
			}
			
			// Check for VWMA crossovers
			var crossoverUp = _previousClosePrice <= _previousVWMA && candle.ClosePrice > vwmaPrice;
			var crossoverDown = _previousClosePrice >= _previousVWMA && candle.ClosePrice < vwmaPrice;
			
			// Log current values
			LogInfo($"Candle Close: {candle.ClosePrice}, VWMA: {vwmaPrice}");
			LogInfo($"Previous Close: {_previousClosePrice}, Previous VWMA: {_previousVWMA}");
			LogInfo($"Crossover Up: {crossoverUp}, Crossover Down: {crossoverDown}");

			// Trading logic:
			// Long: Price crosses above VWMA
			if (crossoverUp && Position <= 0)
			{
				LogInfo($"Buy Signal: Price crossing above VWMA ({candle.ClosePrice} > {vwmaPrice})");
				BuyMarket(Volume + Math.Abs(Position));
			}
			// Short: Price crosses below VWMA
			else if (crossoverDown && Position >= 0)
			{
				LogInfo($"Sell Signal: Price crossing below VWMA ({candle.ClosePrice} < {vwmaPrice})");
				SellMarket(Volume + Math.Abs(Position));
			}
			
			// Exit logic: Price crosses back through VWMA
			if (Position > 0 && crossoverDown)
			{
				LogInfo($"Exit Long: Price crossing below VWMA ({candle.ClosePrice} < {vwmaPrice})");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && crossoverUp)
			{
				LogInfo($"Exit Short: Price crossing above VWMA ({candle.ClosePrice} > {vwmaPrice})");
				BuyMarket(Math.Abs(Position));
			}

			// Store current values for next comparison
			_previousClosePrice = candle.ClosePrice;
			_previousVWMA = vwmaPrice;
		}
	}
}