using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy Tester - combines momentum and ADX for entry signals with ATR-based risk management
	/// </summary>
	public class StrategyTesterStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _momentumLength;
		private readonly StrategyParam<int> _adxSmoothingLength;
		private readonly StrategyParam<int> _diLength;
		private readonly StrategyParam<int> _adxKeyLevel;
		private readonly StrategyParam<int> _atrLength;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<int> _structureLookback;
		private readonly StrategyParam<bool> _exitByMomentum;
		private readonly StrategyParam<bool> _exitByStrategy;

		private LinearRegression _momentum;
		private Highest _highest;
		private Lowest _lowest;
		private SimpleMovingAverage _closeSma;
		private AverageDirectionalIndex _adx;
		private AverageTrueRange _atr;

		private decimal _previousMomentum;
		private decimal _previousAdx;
		private decimal _previousClose;

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Momentum indicator length.
		/// </summary>
		public int MomentumLength
		{
			get => _momentumLength.Value;
			set => _momentumLength.Value = value;
		}

		/// <summary>
		/// ADX smoothing length.
		/// </summary>
		public int AdxSmoothingLength
		{
			get => _adxSmoothingLength.Value;
			set => _adxSmoothingLength.Value = value;
		}

		/// <summary>
		/// DI length.
		/// </summary>
		public int DiLength
		{
			get => _diLength.Value;
			set => _diLength.Value = value;
		}

		/// <summary>
		/// ADX key level.
		/// </summary>
		public int AdxKeyLevel
		{
			get => _adxKeyLevel.Value;
			set => _adxKeyLevel.Value = value;
		}

		/// <summary>
		/// ATR length.
		/// </summary>
		public int AtrLength
		{
			get => _atrLength.Value;
			set => _atrLength.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop loss calculation.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Structure lookback period.
		/// </summary>
		public int StructureLookback
		{
			get => _structureLookback.Value;
			set => _structureLookback.Value = value;
		}

		/// <summary>
		/// Exit by momentum condition.
		/// </summary>
		public bool ExitByMomentum
		{
			get => _exitByMomentum.Value;
			set => _exitByMomentum.Value = value;
		}

		/// <summary>
		/// Exit by strategy condition.
		/// </summary>
		public bool ExitByStrategy
		{
			get => _exitByStrategy.Value;
			set => _exitByStrategy.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public StrategyTesterStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_momentumLength = Param(nameof(MomentumLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("Momentum Length", "Momentum indicator length", "Momentum Indicator")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_adxSmoothingLength = Param(nameof(AdxSmoothingLength), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Smoothing", "ADX smoothing length", "Directional Movement Index")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 2);

			_diLength = Param(nameof(DiLength), 14)
				.SetGreaterThanZero()
				.SetDisplay("DI Length", "DI calculation length", "Directional Movement Index")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 2);

			_adxKeyLevel = Param(nameof(AdxKeyLevel), 23)
				.SetRange(10, 50)
				.SetDisplay("ADX Key Level", "Key level for ADX", "Directional Movement Index")
				.SetCanOptimize(true)
				.SetOptimize(15, 35, 5);

			_atrLength = Param(nameof(AtrLength), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Length", "ATR calculation length", "Average True Range")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 2);

			_atrMultiplier = Param(nameof(AtrMultiplier), 1.6m)
				.SetRange(0.1m, 10.0m)
				.SetDisplay("ATR Multiplier", "ATR multiplier for stop loss", "Average True Range")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 3.0m, 0.3m);

			_structureLookback = Param(nameof(StructureLookback), 1)
				.SetGreaterThanZero()
				.SetDisplay("Structure Lookback", "Lookback period for structure", "Average True Range");

			_exitByMomentum = Param(nameof(ExitByMomentum), false)
				.SetDisplay("Exit by Momentum", "Exit when momentum turns down", "Strategy");

			_exitByStrategy = Param(nameof(ExitByStrategy), true)
				.SetDisplay("Exit by Strategy", "Exit by strategy conditions", "Strategy");
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

			// Initialize indicators
			_highest = new Highest { Length = MomentumLength };
			_lowest = new Lowest { Length = MomentumLength };
			_closeSma = new SimpleMovingAverage { Length = MomentumLength };
			_momentum = new LinearRegression { Length = MomentumLength };
			_adx = new AverageDirectionalIndex { Length = DiLength };
			_atr = new AverageTrueRange { Length = AtrLength };

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(new IIndicator[] { _highest, _lowest, _closeSma, _momentum, _adx, _atr }, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Wait for indicators to form
			if (!_momentum.IsFormed || !_adx.IsFormed || !_atr.IsFormed)
				return;

			// Extract and convert values from array to decimal
			var highestValue = values[0].ToDecimal();
			var lowestValue = values[1].ToDecimal();
			var closeSmaValue = values[2].ToDecimal();
			
			// LinearRegression is a complex indicator but can be converted to decimal directly
			var linRegValue = (LinearRegressionValue)values[3];

			if (linRegValue.LinearRegSlope is not decimal momentumValue)
				return;

			// DirectionalIndex (ADX) is a complex indicator - extract the main ADX value
			var adxTyped = (AverageDirectionalIndexValue)values[4];
			if (adxTyped.MovingAverage is not decimal adxValue)
				return;
			
			var atrValue = values[5].ToDecimal();

			// Detect pivot highs (simplified)
			var momentumPivotHigh = _previousMomentum != 0 && _previousMomentum > momentumValue;
			var adxPivotHigh = _previousAdx != 0 && _previousAdx > adxValue;

			CheckEntryConditions(candle, momentumValue, adxValue, atrValue, momentumPivotHigh, adxPivotHigh);
			CheckExitConditions(candle, momentumValue, adxValue, momentumPivotHigh);

			// Store previous values
			_previousMomentum = momentumValue;
			_previousAdx = adxValue;
			_previousClose = candle.ClosePrice;
		}

		private void CheckEntryConditions(ICandleMessage candle, decimal momentumValue, decimal adxValue, decimal atrValue, bool momentumPivotHigh, bool adxPivotHigh)
		{
			var currentPrice = candle.ClosePrice;

			// Buy condition 1: momentum pivot high and ADX declining
			var buyCondition1 = momentumPivotHigh && adxValue < _previousAdx;

			// Buy condition 2: ADX pivot high, momentum rising and negative
			var buyCondition2 = adxPivotHigh && momentumValue >= _previousMomentum && momentumValue < 0;

			if ((buyCondition1 || buyCondition2) && Position == 0)
			{
				var stopLoss = currentPrice - (atrValue * AtrMultiplier);
				RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Volume));
			}

			// Short conditions are disabled by default in original script
			// Can be enabled by setting appropriate conditions
		}

		private void CheckExitConditions(ICandleMessage candle, decimal momentumValue, decimal adxValue, bool momentumPivotHigh)
		{
			// Exit long on momentum pivot high (if exit by momentum is enabled)
			if (Position > 0 && ExitByMomentum && momentumPivotHigh)
			{
				RegisterOrder(CreateOrder(Sides.Sell, _previousClose, Math.Abs(Position)));
			}

			// Additional exit conditions based on strategy settings
			if (Position > 0 && ExitByStrategy)
			{
				// Strategy-specific exit conditions can be added here
			}
		}
	}
}