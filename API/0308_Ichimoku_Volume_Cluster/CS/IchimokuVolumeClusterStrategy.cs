using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Ichimoku Cloud with volume cluster confirmation.
	/// </summary>
	public class IchimokuVolumeClusterStrategy : Strategy
	{
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouSpanBPeriod;
		private readonly StrategyParam<int> _volumeAvgPeriod;
		private readonly StrategyParam<decimal> _volumeStdDevMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private SimpleMovingAverage _volumeAvg;
		private StandardDeviation _volumeStdDev;

		/// <summary>
		/// Tenkan-sen (Conversion Line) period.
		/// </summary>
		public int TenkanPeriod
		{
			get => _tenkanPeriod.Value;
			set => _tenkanPeriod.Value = value;
		}

		/// <summary>
		/// Kijun-sen (Base Line) period.
		/// </summary>
		public int KijunPeriod
		{
			get => _kijunPeriod.Value;
			set => _kijunPeriod.Value = value;
		}

		/// <summary>
		/// Senkou Span B (Leading Span B) period.
		/// </summary>
		public int SenkouSpanBPeriod
		{
			get => _senkouSpanBPeriod.Value;
			set => _senkouSpanBPeriod.Value = value;
		}

		/// <summary>
		/// Volume average period.
		/// </summary>
		public int VolumeAvgPeriod
		{
			get => _volumeAvgPeriod.Value;
			set => _volumeAvgPeriod.Value = value;
		}

		/// <summary>
		/// Volume standard deviation multiplier.
		/// </summary>
		public decimal VolumeStdDevMultiplier
		{
			get => _volumeStdDevMultiplier.Value;
			set => _volumeStdDevMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type parameter.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize a new instance of <see cref="IchimokuVolumeClusterStrategy"/>.
		/// </summary>
		public IchimokuVolumeClusterStrategy()
		{
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen (Conversion Line)", "Ichimoku Settings")
				.SetCanOptimize(true)
				.SetOptimize(7, 12, 1);

			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Kijun-sen Period", "Period for Kijun-sen (Base Line)", "Ichimoku Settings")
				.SetCanOptimize(true)
				.SetOptimize(20, 30, 2);

			_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
				.SetGreaterThanZero()
				.SetDisplay("Senkou Span B Period", "Period for Senkou Span B (Leading Span B)", "Ichimoku Settings")
				.SetCanOptimize(true)
				.SetOptimize(40, 60, 4);

			_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume Average Period", "Period for volume average and standard deviation", "Volume Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_volumeStdDevMultiplier = Param(nameof(VolumeStdDevMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Volume StdDev Multiplier", "Standard deviation multiplier for volume threshold", "Volume Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

\t\t/// <inheritdoc />
\t\tprotected override void OnReseted()
\t\t{
\t\t\tbase.OnReseted();

\t\t\t_volumeAvg = new SimpleMovingAverage { Length = VolumeAvgPeriod };
\t\t\t_volumeStdDev = new StandardDeviation { Length = VolumeAvgPeriod };
\t\t}

\t\t/// <inheritdoc />
\t\tprotected override void OnStarted(DateTimeOffset time)
\t\t{
\t\t\tbase.OnStarted(time);

\t\t\t// Create Ichimoku indicator
\t\t\tvar ichimoku = new Ichimoku
\t\t\t{
\t\t\t\tTenkan = { Length = TenkanPeriod },
\t\t\t\tKijun = { Length = KijunPeriod },
\t\t\t\tSenkouB = { Length = SenkouSpanBPeriod },
\t\t\t};

\t\t\t// Create subscription
\t\t\tvar subscription = SubscribeCandles(CandleType);

\t\t\t// Bind Ichimoku to subscription
\t\t\tsubscription
\t\t\t\t.BindEx(ichimoku, ProcessCandle)
\t\t\t\t.Start();

\t\t\t// Setup stop-loss at Kijun-sen level
\t\t\tStartProtection(
\t\t\t\ttakeProfit: new Unit(0), // We'll handle exits in the strategy logic
\t\t\t\tstopLoss: new Unit(0),   // Using Kijun-sen as dynamic stop-loss
\t\t\t\tuseMarketOrders: true
\t\t\t);

\t\t\t// Setup chart if available
\t\t\tvar area = CreateChartArea();
\t\t\tif (area != null)
\t\t\t{
\t\t\t\tDrawCandles(area, subscription);
\t\t\t\tDrawIndicator(area, ichimoku);
\t\t\t\tDrawOwnTrades(area);

\t\t\t\t// Create second area for volume
\t\t\t\tvar volumeArea = CreateChartArea();
\t\t\t\tif (volumeArea != null)
\t\t\t\t{
\t\t\t\t\tDrawIndicator(volumeArea, _volumeAvg);
\t\t\t\t}
\t\t\t}
\t\t}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue v)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			var volume = candle.TotalVolume;

			var volumeAvgValue = _volumeAvg.Process(volume, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			var volumeStdDevValue = _volumeStdDev.Process(volume, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var ichimokuTyped = (IchimokuValue)v;

			// Extract Ichimoku values
			if (ichimokuTyped.Tenkan is not decimal tenkan)
				return;

			if (ichimokuTyped.Kijun is not decimal kijun)
				return;

			if (ichimokuTyped.SenkouA is not decimal senkouA)
				return;

			if (ichimokuTyped.SenkouB is not decimal senkouB)
				return;

			// Determine cloud position
			var priceAboveCloud = candle.ClosePrice > Math.Max(senkouA, senkouB);
			var priceBelowCloud = candle.ClosePrice < Math.Min(senkouA, senkouB);
			
			// Check for volume spike
			var volumeThreshold = volumeAvgValue + VolumeStdDevMultiplier * volumeStdDevValue;
			var hasVolumeSpike = candle.TotalVolume > volumeThreshold;
			
			// Define entry conditions
			var longEntryCondition = priceAboveCloud && tenkan > kijun && hasVolumeSpike && Position <= 0;
			var shortEntryCondition = priceBelowCloud && tenkan < kijun && hasVolumeSpike && Position >= 0;
			
			// Define exit conditions
			var longExitCondition = priceBelowCloud && Position > 0;
			var shortExitCondition = priceAboveCloud && Position < 0;

			// Log current values
			LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, Volume: {candle.TotalVolume}, Threshold: {volumeThreshold}");
			LogInfo($"Tenkan: {tenkan}, Kijun: {kijun}, Senkou A: {senkouA}, Senkou B: {senkouB}");

			// Execute trading logic
			if (longEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Enter long position
				BuyMarket(positionSize);
				
				LogInfo($"Long entry: Price={candle.ClosePrice}, Volume={candle.TotalVolume}, Threshold={volumeThreshold}");
			}
			else if (shortEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Enter short position
				SellMarket(positionSize);
				
				LogInfo($"Short entry: Price={candle.ClosePrice}, Volume={candle.TotalVolume}, Threshold={volumeThreshold}");
			}
			else if (longExitCondition)
			{
				// Exit long position
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price={candle.ClosePrice}, Reason=Below Cloud");
			}
			else if (shortExitCondition)
			{
				// Exit short position
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price={candle.ClosePrice}, Reason=Above Cloud");
			}
		}
	}
}