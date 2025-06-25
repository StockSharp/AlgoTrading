using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// MACD with Volume Cluster strategy.
	/// Enters positions when MACD signal coincides with abnormal volume spike.
	/// </summary>
	public class MacdVolumeClusterStrategy : Strategy
	{
		private readonly StrategyParam<int> _fastMacdPeriod;
		private readonly StrategyParam<int> _slowMacdPeriod;
		private readonly StrategyParam<int> _macdSignalPeriod;
		private readonly StrategyParam<int> _volumePeriod;
		private readonly StrategyParam<decimal> _volumeDeviationFactor;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _avgVolume;
		private decimal _volumeStdDev;
		private int _processedCandles;

		/// <summary>
		/// Fast MACD EMA period.
		/// </summary>
		public int FastMacdPeriod
		{
			get => _fastMacdPeriod.Value;
			set => _fastMacdPeriod.Value = value;
		}

		/// <summary>
		/// Slow MACD EMA period.
		/// </summary>
		public int SlowMacdPeriod
		{
			get => _slowMacdPeriod.Value;
			set => _slowMacdPeriod.Value = value;
		}

		/// <summary>
		/// MACD signal line period.
		/// </summary>
		public int MacdSignalPeriod
		{
			get => _macdSignalPeriod.Value;
			set => _macdSignalPeriod.Value = value;
		}

		/// <summary>
		/// Period for volume average calculation.
		/// </summary>
		public int VolumePeriod
		{
			get => _volumePeriod.Value;
			set => _volumePeriod.Value = value;
		}

		/// <summary>
		/// Volume deviation factor for volume spike detection.
		/// </summary>
		public decimal VolumeDeviationFactor
		{
			get => _volumeDeviationFactor.Value;
			set => _volumeDeviationFactor.Value = value;
		}

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize strategy.
		/// </summary>
		public MacdVolumeClusterStrategy()
		{
			_fastMacdPeriod = Param(nameof(FastMacdPeriod), 12)
				.SetGreaterThanZero()
				.SetDisplay("Fast MACD Period", "Period for fast EMA in MACD calculation", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(8, 16, 2);

			_slowMacdPeriod = Param(nameof(SlowMacdPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Slow MACD Period", "Period for slow EMA in MACD calculation", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(20, 30, 2);

			_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("MACD Signal Period", "Period for signal line in MACD calculation", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(7, 12, 1);

			_volumePeriod = Param(nameof(VolumePeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume Period", "Period for volume moving average calculation", "Volume Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_volumeDeviationFactor = Param(nameof(VolumeDeviationFactor), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Volume Deviation Factor", "Factor multiplied by standard deviation to detect volume spikes", "Volume Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

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

			// Initialize values
			_avgVolume = 0;
			_volumeStdDev = 0;
			_processedCandles = 0;

			// Create MACD indicator
			var macd = new MovingAverageConvergenceDivergence
			{
				LongPeriod = SlowMacdPeriod,
				ShortPeriod = FastMacdPeriod,
				SignalPeriod = MacdSignalPeriod
			};

			// Create volume-based indicators
			var smaVolume = new SimpleMovingAverage
			{
				Length = VolumePeriod
			};

			var stdDevVolume = new StandardDeviation
			{
				Length = VolumePeriod
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			
			// Bind MACD and process volume separately
			subscription
				.BindEx(macd, ProcessMacdAndVolume)
				.Start();

			// Start position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, macd);
				DrawOwnTrades(area);
			}
		}

		private void ProcessMacdAndVolume(ICandleMessage candle, IIndicatorValue macdValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Calculate volume statistics
			_processedCandles++;
			
			// Using exponential moving average approach for volume statistics
			// to avoid keeping large arrays of historical volumes
			if (_processedCandles == 1)
			{
				_avgVolume = candle.TotalVolume;
				_volumeStdDev = 0;
			}
			else
			{
				// Update average volume with smoothing factor
				decimal alpha = 2.0m / (VolumePeriod + 1);
				decimal oldAvg = _avgVolume;
				_avgVolume = alpha * candle.TotalVolume + (1 - alpha) * _avgVolume;
				
				// Update standard deviation (simplified approach)
				decimal volumeDev = Math.Abs(candle.TotalVolume - oldAvg);
				_volumeStdDev = alpha * volumeDev + (1 - alpha) * _volumeStdDev;
			}

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get values from MACD
			var macdLine = macdValue[0].To<decimal>();	  // MACD Line
			var signalLine = macdValue[1].To<decimal>();	// Signal Line
			
			// Determine if we have a volume spike
			bool isVolumeSpike = candle.TotalVolume > (_avgVolume + VolumeDeviationFactor * _volumeStdDev);
			
			// Log the values
			LogInfo($"MACD: {macdLine}, Signal: {signalLine}, Volume: {candle.TotalVolume}, " +
							$"Avg Volume: {_avgVolume}, StdDev: {_volumeStdDev}, Volume Spike: {isVolumeSpike}");

			// Trading logic
			if (isVolumeSpike)
			{
				// Buy signal: MACD line crosses above signal line with volume spike
				if (macdLine > signalLine && Position <= 0)
				{
					// Close any existing short position
					if (Position < 0)
						BuyMarket(Math.Abs(Position));
					
					// Open long position
					BuyMarket(Volume);
					LogInfo($"Buy signal: MACD ({macdLine}) > Signal ({signalLine}) with volume spike ({candle.TotalVolume})");
				}
				// Sell signal: MACD line crosses below signal line with volume spike
				else if (macdLine < signalLine && Position >= 0)
				{
					// Close any existing long position
					if (Position > 0)
						SellMarket(Math.Abs(Position));
					
					// Open short position
					SellMarket(Volume);
					LogInfo($"Sell signal: MACD ({macdLine}) < Signal ({signalLine}) with volume spike ({candle.TotalVolume})");
				}
			}
			
			// Exit logic: MACD crosses back
			if ((Position > 0 && macdLine < signalLine) || 
				(Position < 0 && macdLine > signalLine))
			{
				ClosePosition();
				LogInfo($"Exit signal: MACD and Signal crossed. Position closed at {candle.ClosePrice}");
			}
		}
	}
}