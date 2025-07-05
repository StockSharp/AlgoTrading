using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that combines Parabolic SAR with volume confirmation.
	/// Enters trades when price crosses the Parabolic SAR with above-average volume.
	/// </summary>
	public class ParabolicSarVolumeStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _acceleration;
		private readonly StrategyParam<decimal> _maxAcceleration;
		private readonly StrategyParam<int> _volumePeriod;
		private readonly StrategyParam<DataType> _candleType;

		private ParabolicSar _parabolicSar;
		private VolumeIndicator _volumeIndicator;
		private SimpleMovingAverage _volumeAverage;
		
		private decimal _prevSar;
		private decimal _currentAvgVolume;
		private bool _prevPriceAboveSar;

		/// <summary>
		/// Parabolic SAR acceleration factor.
		/// </summary>
		public decimal Acceleration
		{
			get => _acceleration.Value;
			set => _acceleration.Value = value;
		}

		/// <summary>
		/// Parabolic SAR maximum acceleration factor.
		/// </summary>
		public decimal MaxAcceleration
		{
			get => _maxAcceleration.Value;
			set => _maxAcceleration.Value = value;
		}

		/// <summary>
		/// Period for volume moving average.
		/// </summary>
		public int VolumePeriod
		{
			get => _volumePeriod.Value;
			set => _volumePeriod.Value = value;
		}

		/// <summary>
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParabolicSarVolumeStrategy"/>.
		/// </summary>
		public ParabolicSarVolumeStrategy()
		{
			_acceleration = Param(nameof(Acceleration), 0.02m)
				.SetRange(0.01m, 0.1m)
				.SetCanOptimize(true)
				.SetDisplay("SAR Acceleration", "Starting acceleration factor", "Indicators");

			_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
				.SetRange(0.1m, 0.5m)
				.SetCanOptimize(true)
				.SetDisplay("SAR Max Acceleration", "Maximum acceleration factor", "Indicators");

			_volumePeriod = Param(nameof(VolumePeriod), 20)
				.SetRange(10, 50)
				.SetCanOptimize(true)
				.SetDisplay("Volume Period", "Period for volume moving average", "Indicators");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

			// Initialize indicators
			_parabolicSar = new ParabolicSar
			{
				Acceleration = Acceleration,
				AccelerationMax = MaxAcceleration
			};

			_volumeIndicator = new VolumeIndicator();
			
			_volumeAverage = new SimpleMovingAverage
			{
				Length = VolumePeriod
			};

			// Reset state variables
			_prevSar = 0;
			_currentAvgVolume = 0;
			_prevPriceAboveSar = false;

			// Create candle subscription
			var subscription = SubscribeCandles(CandleType);

			// Binding for Parabolic SAR indicator
			subscription
				.Bind(_parabolicSar, ProcessSarSignal)
				.Start();

			// Binding for Volume indicators
			subscription
				.BindEx(_volumeIndicator, (candle, volume) => 
				{
					var avgVolume = _volumeAverage.Process(volume).ToDecimal();
					_currentAvgVolume = avgVolume;
				})
				.Start();

			// Setup position protection with trailing stop
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute),
				stopLoss: new Unit(0, UnitTypes.Absolute),
				isStopTrailing: true
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _parabolicSar);
				
				var volumeArea = CreateChartArea();
				DrawIndicator(volumeArea, _volumeIndicator);
				DrawIndicator(volumeArea, _volumeAverage);
				
				DrawOwnTrades(area);
			}
		}

		private void ProcessSarSignal(ICandleMessage candle, decimal? sarValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Wait until strategy and indicators are ready
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get current price and volume
			var currentPrice = candle.ClosePrice;
			var currentVolume = candle.TotalVolume;
			var isPriceAboveSar = currentPrice > sarValue;
			
			// Determine if volume is above average
			var isHighVolume = currentVolume > _currentAvgVolume;

			// Check for SAR crossover with volume confirmation
			// Bullish crossover: Price crosses above SAR with high volume
			if (isPriceAboveSar && !_prevPriceAboveSar && isHighVolume && Position <= 0)
			{
				// Cancel existing orders before entering new position
				CancelActiveOrders();
				
				// Enter long position
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				
				LogInfo($"Long entry signal: Price {currentPrice} crossed above SAR {sarValue} with high volume {currentVolume} > avg {_currentAvgVolume}");
			}
			// Bearish crossover: Price crosses below SAR with high volume
			else if (!isPriceAboveSar && _prevPriceAboveSar && isHighVolume && Position >= 0)
			{
				// Cancel existing orders before entering new position
				CancelActiveOrders();
				
				// Enter short position
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				
				LogInfo($"Short entry signal: Price {currentPrice} crossed below SAR {sarValue} with high volume {currentVolume} > avg {_currentAvgVolume}");
			}
			// Exit signals based on SAR crossover (without volume confirmation)
			else if ((Position > 0 && !isPriceAboveSar) || (Position < 0 && isPriceAboveSar))
			{
				// Close position on SAR reversal
				ClosePosition();
				
				LogInfo($"Exit signal: SAR reversal. Price: {currentPrice}, SAR: {sarValue}");
			}

			// Update previous values for next candle
			_prevSar = sarValue;
			_prevPriceAboveSar = isPriceAboveSar;
		}
	}
}