using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// VWAP Breakout Strategy (246).
	/// Enter when price breaks out from VWAP by a certain ATR multiple.
	/// Exit when price returns to VWAP.
	/// </summary>
	public class VwapBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _kParam;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _atrPeriod;

		private AverageTrueRange _atr;
		private VolumeWeightedMovingAverage _vwap;
		private decimal _currentAtr;
		private decimal _currentVwap;
		private decimal _currentPrice;

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
		/// Initializes a new instance of the <see cref="VwapBreakoutStrategy"/>.
		/// </summary>
		public VwapBreakoutStrategy()
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

			// Create ATR indicator
			_atr = new AverageTrueRange { Length = AtrPeriod };
			_vwap = new VolumeWeightedMovingAverage { Length = AtrPeriod };

			// Create candle subscription
			var subscription = SubscribeCandles(CandleType);

			// Bind ATR to candles
			subscription
				.Bind(_atr, ProcessCandle)
				.Start();

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

		private void ProcessCandle(ICandleMessage candle, decimal atr)
		{
			if (candle.State != CandleStates.Finished)
				return;

			_currentAtr = atr;
			_currentPrice = candle.ClosePrice;

			_currentVwap = _vwap.Process(candle).ToDecimal();

			UpdateStrategy();
		}

		private void UpdateStrategy()
		{
			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Ensure we have valid values
			if (_currentVwap <= 0 || _currentAtr <= 0 || _currentPrice <= 0)
				return;

			// Calculate entry bands
			var upperBand = _currentVwap + K * _currentAtr;
			var lowerBand = _currentVwap - K * _currentAtr;

			LogInfo($"Price: {_currentPrice}, VWAP: {_currentVwap}, Upper: {upperBand}, Lower: {lowerBand}");

			// Entry logic - BREAKOUT
			if (Position == 0)
			{
				// Long Entry: Price breaks above upper band
				if (_currentPrice > upperBand)
				{
					LogInfo($"Buy Signal - Price ({_currentPrice}) > Upper Band ({upperBand})");
					BuyMarket(Volume);
				}
				// Short Entry: Price breaks below lower band
				else if (_currentPrice < lowerBand)
				{
					LogInfo($"Sell Signal - Price ({_currentPrice}) < Lower Band ({lowerBand})");
					SellMarket(Volume);
				}
			}
			// Exit logic
			else if (Position > 0 && _currentPrice < _currentVwap)
			{
				// Exit Long: Price returns below VWAP
				LogInfo($"Exit Long - Price ({_currentPrice}) < VWAP ({_currentVwap})");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && _currentPrice > _currentVwap)
			{
				// Exit Short: Price returns above VWAP
				LogInfo($"Exit Short - Price ({_currentPrice}) > VWAP ({_currentVwap})");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
