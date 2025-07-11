using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Accumulation/Distribution (A/D) Strategy
	/// Long entry: A/D rising and price above MA
	/// Short entry: A/D falling and price below MA
	/// Exit: A/D changes direction
	/// </summary>
	public class ADStrategy : Strategy
	{
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _previousADValue;
		private bool _isFirstCandle;

		/// <summary>
		/// MA Period
		/// </summary>
		public int MAPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
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
		/// Initialize <see cref="ADStrategy"/>.
		/// </summary>
		public ADStrategy()
		{
			_maPeriod = Param(nameof(MAPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
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

			_previousADValue = 0;
			_isFirstCandle = true;

			// Create indicators
			var ma = new SimpleMovingAverage { Length = MAPeriod };
			var ad = new AccumulationDistributionLine();

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			// We need to bind both indicators but handle with one callback
			subscription
				.Bind(ma, ad, ProcessCandle)
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
				DrawIndicator(area, ma);
				DrawIndicator(area, ad);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal adValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Skip the first candle, just initialize values
			if (_isFirstCandle)
			{
				_previousADValue = adValue;
				_isFirstCandle = false;
				return;
			}
			
			// Check for A/D direction
			var adRising = adValue > _previousADValue;
			var adFalling = adValue < _previousADValue;
			
			// Log current values
			LogInfo($"Candle Close: {candle.ClosePrice}, MA: {maValue}, A/D: {adValue}");
			LogInfo($"Previous A/D: {_previousADValue}, A/D Rising: {adRising}, A/D Falling: {adFalling}");

			// Trading logic:
			// Long: A/D rising and price above MA
			if (adRising && candle.ClosePrice > maValue && Position <= 0)
			{
				LogInfo($"Buy Signal: A/D rising and Price ({candle.ClosePrice}) > MA ({maValue})");
				BuyMarket(Volume + Math.Abs(Position));
			}
			// Short: A/D falling and price below MA
			else if (adFalling && candle.ClosePrice < maValue && Position >= 0)
			{
				LogInfo($"Sell Signal: A/D falling and Price ({candle.ClosePrice}) < MA ({maValue})");
				SellMarket(Volume + Math.Abs(Position));
			}
			
			// Exit logic: A/D changes direction
			if (Position > 0 && adFalling)
			{
				LogInfo($"Exit Long: A/D changing direction (falling)");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && adRising)
			{
				LogInfo($"Exit Short: A/D changing direction (rising)");
				BuyMarket(Math.Abs(Position));
			}

			// Store current A/D value for next comparison
			_previousADValue = adValue;
		}
	}
}