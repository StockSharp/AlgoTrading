using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// VWAP Mean Reversion Strategy.
	/// Enter when price deviates from VWAP by a certain ATR multiple.
	/// Exit when price returns to VWAP.
	/// </summary>
	public class VwapMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _kParam;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _atrPeriod;

		private AverageTrueRange _atr;
		private decimal _currentAtr;
		private decimal _currentVwap;

		/// <summary>
		/// ATR multiplier for entry.
		/// </summary>
		public decimal K
		{
			get => _kParam.Value;
			set => _kParam.Value = value;
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
		/// ATR period.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VwapMeanReversionStrategy"/>.
		/// </summary>
		public VwapMeanReversionStrategy()
		{
			_kParam = Param(nameof(K), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Multiplier", "ATR multiplier for entry distance from VWAP", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 4.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters");

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "ATR indicator period", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);
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
			_atr = new AverageTrueRange { Length = AtrPeriod };

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);

			// Create subscription for VWAP
			var vwapSubscription = new Subscription(DataType.VWAP, Security);

			// Bind indicators to candles
			subscription
				.Bind(_atr, (candle, atr) => ProcessATR(candle, atr))
				.Start();

			// Subscribe to VWAP data
			vwapSubscription.WhenTimeComeOut(this).Do(ProcessVwap).Apply(this);
			Subscribe(vwapSubscription);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _atr);
				DrawOwnTrades(area);
			}

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(5, UnitTypes.Percent),
				stopLoss: new Unit(2, UnitTypes.Percent)
			);
		}

		private void ProcessATR(ICandleMessage candle, decimal atr)
		{
			if (candle.State != CandleStates.Finished)
				return;

			_currentAtr = atr;
			ProcessStrategy();
		}

		private void ProcessVwap(Security security, DateTimeOffset time)
		{
			if (security != Security)
				return;

			_currentVwap = security.LastTrade?.Price ?? 0;
			ProcessStrategy();
		}

		private void ProcessStrategy()
		{
			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Skip if we don't have valid VWAP or ATR yet
			if (_currentVwap <= 0 || _currentAtr <= 0)
				return;

			var security = GetSecurity();
			
			// Make sure we have a valid current price
			var currentPrice = security.LastTrade?.Price ?? 0;
			if (currentPrice <= 0)
				return;

			// Calculate distance to VWAP
			var upperBand = _currentVwap + K * _currentAtr;
			var lowerBand = _currentVwap - K * _currentAtr;

			LogInfo($"Current Price: {currentPrice}, VWAP: {_currentVwap}, Upper: {upperBand}, Lower: {lowerBand}");

			// Entry logic
			if (Position == 0)
			{
				// Long Entry: Price is below lower band
				if (currentPrice < lowerBand)
				{
					// Buy when price is too low compared to VWAP
					LogInfo($"Buy Signal - Price ({currentPrice}) < Lower Band ({lowerBand})");
					BuyMarket(Volume);
				}
				// Short Entry: Price is above upper band
				else if (currentPrice > upperBand)
				{
					// Sell when price is too high compared to VWAP
					LogInfo($"Sell Signal - Price ({currentPrice}) > Upper Band ({upperBand})");
					SellMarket(Volume);
				}
			}
			// Exit logic
			else if (Position > 0 && currentPrice > _currentVwap)
			{
				// Exit Long: Price returned to VWAP
				LogInfo($"Exit Long - Price ({currentPrice}) > VWAP ({_currentVwap})");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && currentPrice < _currentVwap)
			{
				// Exit Short: Price returned to VWAP
				LogInfo($"Exit Short - Price ({currentPrice}) < VWAP ({_currentVwap})");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
