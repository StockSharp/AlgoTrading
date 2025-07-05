using System;
using System.Collections.Generic;
using Ecng.ComponentModel;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on VIX (Volatility Index) movements.
	/// It enters positions when VIX is rising (indicating increasing fear/volatility in the market)
	/// and price is moving in an expected direction relative to its moving average.
	/// </summary>
	public class VixTriggerStrategy : Strategy
	{
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<Security> _vixSecurity;

		private decimal _prevVix;

		/// <summary>
		/// Period for Moving Average calculation (default: 20)
		/// </summary>
		public int MAPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// Stop-loss as percentage from entry price (default: 2%)
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Type of candles used for strategy calculation
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// VIX Security (required)
		/// </summary>
		public Security VixSecurity
		{
			get => _vixSecurity.Value;
			set => _vixSecurity.Value = value;
		}

		/// <summary>
		/// Initialize the VIX Trigger strategy
		/// </summary>
		public VixTriggerStrategy()
		{
			_maPeriod = Param(nameof(MAPeriod), 20)
				.SetDisplay("MA Period", "Period for Moving Average calculation", "Technical Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "Data");

			_vixSecurity = Param<Security>(nameof(VixSecurity))
				.SetDisplay("VIX Security", "VIX Security to use for signals", "Data")
				.SetRequired();
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			// We need both the primary security and VIX
			return
			[
				(Security, CandleType),
				(VixSecurity, CandleType)
			];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Reset state variables
			_prevVix = 0;

			// Create indicator
			var sma = new SimpleMovingAverage { Length = MAPeriod };

			// Create subscriptions
			var mainSubscription = SubscribeCandles(CandleType);
			var vixSubscription = SubscribeCandles(new Subscription(CandleType, VixSecurity));

			// Bind indicator to main security candles
			mainSubscription
				.Bind(sma, ProcessMainCandle)
				.Start();

			// Process VIX candles separately
			vixSubscription
				.Bind(ProcessVixCandle)
				.Start();

			// Configure chart
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, mainSubscription);
				DrawIndicator(area, sma);
				DrawOwnTrades(area);
			}

			// Setup protection with stop-loss
			StartProtection(
				new Unit(0), // No take profit
				new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss as percentage of entry price
			);
		}

		// Latest VIX value and trend flags
		private decimal _latestVix;
		private bool _isVixRising;

		/// <summary>
		/// Process VIX candle to track VIX movements
		/// </summary>
		private void ProcessVixCandle(ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store latest VIX value
			_latestVix = candle.ClosePrice;

			// Initialize _prevVix on first VIX candle
			if (_prevVix == 0)
			{
				_prevVix = _latestVix;
				return;
			}

			// Check if VIX is rising
			_isVixRising = _latestVix > _prevVix;

			// Update previous VIX value
			_prevVix = _latestVix;
		}

		/// <summary>
		/// Process main security candle and check for trading signals
		/// </summary>
		private void ProcessMainCandle(ICandleMessage candle, decimal smaValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check if we have received VIX data
			if (_prevVix == 0)
				return;

			// Determine price position relative to MA
			bool isPriceBelowMA = candle.ClosePrice < smaValue;

			if (Position == 0)
			{
				// No position - check for entry signals
				if (_isVixRising && isPriceBelowMA)
				{
					// VIX is rising and price is below MA - buy (contrarian strategy)
					BuyMarket(Volume);
				}
				else if (_isVixRising && !isPriceBelowMA)
				{
					// VIX is rising and price is above MA - sell (contrarian strategy)
					SellMarket(Volume);
				}
			}
			else if (Position > 0)
			{
				// Long position - check for exit signal
				if (!_isVixRising)
				{
					// VIX is decreasing - exit long
					SellMarket(Position);
				}
			}
			else if (Position < 0)
			{
				// Short position - check for exit signal
				if (!_isVixRising)
				{
					// VIX is decreasing - exit short
					BuyMarket(Math.Abs(Position));
				}
			}
		}
	}
}
