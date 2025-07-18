using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Donchian Reversal Strategy.
	/// Enters long when price bounces from the lower Donchian Channel band.
	/// Enters short when price bounces from the upper Donchian Channel band.
	/// </summary>
	public class DonchianReversalStrategy : Strategy
	{
		private readonly StrategyParam<int> _period;
		private readonly StrategyParam<Unit> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _previousClose;
		private bool _isFirstCandle = true;

		/// <summary>
		/// Period for Donchian Channel calculation.
		/// </summary>
		public int Period
		{
			get => _period.Value;
			set => _period.Value = value;
		}

		/// <summary>
		/// Stop loss percentage from entry price.
		/// </summary>
		public Unit StopLoss
		{
			get => _stopLoss.Value;
			set => _stopLoss.Value = value;
		}

		/// <summary>
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DonchianReversalStrategy"/>.
		/// </summary>
		public DonchianReversalStrategy()
		{
			_period = Param(nameof(Period), 20)
				.SetDisplay("Period", "Period for Donchian Channel calculation", "Indicator Settings")
				.SetOptimize(10, 40, 5)
				.SetCanOptimize(true);
				
			_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
				.SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")
				.SetOptimize(1m, 3m, 0.5m)
				.SetCanOptimize(true);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

			// Enable position protection using stop-loss
			StartProtection(
				takeProfit: null,
				stopLoss: StopLoss,
				isStopTrailing: false,
				useMarketOrders: true
			);

			// Initialize state
			_previousClose = 0;
			_isFirstCandle = true;
			
			// Create Donchian Channel indicator
			var donchian = new DonchianChannels { Length = Period };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicator and process candles
			subscription
				.BindEx(donchian, ProcessCandle)
				.Start();
				
			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, donchian);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// If this is the first candle, just store the close price
			if (_isFirstCandle)
			{
				_previousClose = candle.ClosePrice;
				_isFirstCandle = false;
				return;
			}

			var donchianTyped = (DonchianChannelsValue)donchianValue;
			var middleBand = donchianTyped.Middle;
			var upperBand = donchianTyped.UpperBand;
			var lowerBand = donchianTyped.LowerBand;

			// Check for price bounce from Donchian bands
			bool bouncedFromLower = _previousClose < lowerBand && candle.ClosePrice > lowerBand;
			bool bouncedFromUpper = _previousClose > upperBand && candle.ClosePrice < upperBand;
			
			// Long entry: Price bounced from lower band
			if (bouncedFromLower && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry: Price bounced from lower Donchian band ({lowerBand})");
			}
			// Short entry: Price bounced from upper band
			else if (bouncedFromUpper && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry: Price bounced from upper Donchian band ({upperBand})");
			}
			
			// Store current close price for next candle comparison
			_previousClose = candle.ClosePrice;
		}
	}
}