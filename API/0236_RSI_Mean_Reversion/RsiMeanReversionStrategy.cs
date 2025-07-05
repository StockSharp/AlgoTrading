using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// RSI Mean Reversion Strategy.
	/// Enter when RSI deviates from its average by a certain multiple of standard deviation.
	/// Exit when RSI returns to its average.
	/// </summary>
	public class RsiMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<int> _averagePeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;

		private RelativeStrengthIndex _rsi;
		private SimpleMovingAverage _rsiAverage;
		private StandardDeviation _rsiStdDev;
		
		private decimal _prevRsiValue;

		/// <summary>
		/// RSI period.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
		}

		/// <summary>
		/// Period for RSI average calculation.
		/// </summary>
		public int AveragePeriod
		{
			get => _averagePeriod.Value;
			set => _averagePeriod.Value = value;
		}

		/// <summary>
		/// Standard deviation multiplier for entry.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplier.Value;
			set => _multiplier.Value = value;
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
		/// Initializes a new instance of the <see cref="RsiMeanReversionStrategy"/>.
		/// </summary>
		public RsiMeanReversionStrategy()
		{
			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Period", "Period for RSI calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);

			_averagePeriod = Param(nameof(AveragePeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for RSI average calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_multiplier = Param(nameof(Multiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("StdDev Multiplier", "Standard deviation multiplier for entry", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters");
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
			_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
			_rsiAverage = new SimpleMovingAverage { Length = AveragePeriod };
			_rsiStdDev = new StandardDeviation { Length = AveragePeriod };

			// Create candle subscription
			var subscription = SubscribeCandles(CandleType);

			// Define custom indicator chain processing
			subscription
				.Bind(_rsi, ProcessRsi)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _rsi);
				DrawOwnTrades(area);
			}

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(5, UnitTypes.Percent),
				stopLoss: new Unit(2, UnitTypes.Percent)
			);
		}

		private void ProcessRsi(ICandleMessage candle, decimal rsiValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			// Process RSI through average and standard deviation indicators
			var rsiAvgValue = _rsiAverage.Process(rsiValue, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			var rsiStdDevValue = _rsiStdDev.Process(rsiValue, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			
			// Store previous RSI value for changes detection
			decimal currentRsiValue = rsiValue;
			
			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading() || !_rsiAverage.IsFormed || !_rsiStdDev.IsFormed)
			{
				_prevRsiValue = currentRsiValue;
				return;
			}

			// Calculate bands
			var upperBand = rsiAvgValue + Multiplier * rsiStdDevValue;
			var lowerBand = rsiAvgValue - Multiplier * rsiStdDevValue;

			LogInfo($"RSI: {currentRsiValue}, RSI Avg: {rsiAvgValue}, Upper: {upperBand}, Lower: {lowerBand}");

			// Entry logic
			if (Position == 0)
			{
				// Long Entry: RSI is below lower band
				if (currentRsiValue < lowerBand)
				{
					LogInfo($"Buy Signal - RSI ({currentRsiValue}) < Lower Band ({lowerBand})");
					BuyMarket(Volume);
				}
				// Short Entry: RSI is above upper band
				else if (currentRsiValue > upperBand)
				{
					LogInfo($"Sell Signal - RSI ({currentRsiValue}) > Upper Band ({upperBand})");
					SellMarket(Volume);
				}
			}
			// Exit logic
			else if (Position > 0 && currentRsiValue > rsiAvgValue)
			{
				// Exit Long: RSI returned to average
				LogInfo($"Exit Long - RSI ({currentRsiValue}) > RSI Avg ({rsiAvgValue})");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && currentRsiValue < rsiAvgValue)
			{
				// Exit Short: RSI returned to average
				LogInfo($"Exit Short - RSI ({currentRsiValue}) < RSI Avg ({rsiAvgValue})");
				BuyMarket(Math.Abs(Position));
			}
			
			_prevRsiValue = currentRsiValue;
		}
	}
}
