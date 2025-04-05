using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// RSI Breakout Strategy (247).
	/// Enter when RSI breaks out above/below its average by a certain multiple of standard deviation.
	/// Exit when RSI returns to its average.
	/// </summary>
	public class RsiBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<int> _averagePeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;

		private RelativeStrengthIndex _rsi;
		private SimpleMovingAverage _rsiAverage;
		private StandardDeviation _rsiStdDev;
		
		private decimal _prevRsiValue;
		private decimal _currentRsiValue;
		private decimal _currentRsiAvg;
		private decimal _currentRsiStdDev;

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
		/// Initializes a new instance of the <see cref="RsiBreakoutStrategy"/>.
		/// </summary>
		public RsiBreakoutStrategy()
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
			return new[] { (Security, CandleType) };
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

			// Bind RSI to candles
			subscription
				.Bind(_rsi, ProcessRsi)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _rsi);
				DrawIndicator(area, _rsiAverage);
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

			// Store previous and current RSI value
			_prevRsiValue = _currentRsiValue;
			_currentRsiValue = rsiValue;

			// Process RSI through average and standard deviation indicators
			var avgValue = _rsiAverage.Process(new DecimalIndicatorValue(rsiValue));
			var stdDevValue = _rsiStdDev.Process(new DecimalIndicatorValue(rsiValue));
			
			_currentRsiAvg = avgValue.GetValue<decimal>();
			_currentRsiStdDev = stdDevValue.GetValue<decimal>();
			
			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading() || !_rsiAverage.IsFormed || !_rsiStdDev.IsFormed)
				return;

			// Calculate bands
			var upperBand = _currentRsiAvg + Multiplier * _currentRsiStdDev;
			var lowerBand = _currentRsiAvg - Multiplier * _currentRsiStdDev;

			LogInfo($"RSI: {_currentRsiValue}, RSI Avg: {_currentRsiAvg}, Upper: {upperBand}, Lower: {lowerBand}");

			// Entry logic - BREAKOUT
			if (Position == 0)
			{
				// Long Entry: RSI breaks above upper band
				if (_currentRsiValue > upperBand)
				{
					LogInfo($"Buy Signal - RSI ({_currentRsiValue}) > Upper Band ({upperBand})");
					BuyMarket(Volume);
				}
				// Short Entry: RSI breaks below lower band
				else if (_currentRsiValue < lowerBand)
				{
					LogInfo($"Sell Signal - RSI ({_currentRsiValue}) < Lower Band ({lowerBand})");
					SellMarket(Volume);
				}
			}
			// Exit logic
			else if (Position > 0 && _currentRsiValue < _currentRsiAvg)
			{
				// Exit Long: RSI returns below average
				LogInfo($"Exit Long - RSI ({_currentRsiValue}) < RSI Avg ({_currentRsiAvg})");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && _currentRsiValue > _currentRsiAvg)
			{
				// Exit Short: RSI returns above average
				LogInfo($"Exit Short - RSI ({_currentRsiValue}) > RSI Avg ({_currentRsiAvg})");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
