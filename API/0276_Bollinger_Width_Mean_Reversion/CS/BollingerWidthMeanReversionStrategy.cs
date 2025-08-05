namespace StockSharp.Samples.Strategies
{
	using System;
	using System.Collections.Generic;
	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Bollinger Width Mean Reversion Strategy.
	/// Strategy trades based on mean reversion of Bollinger Bands width.
	/// </summary>
	public class BollingerWidthMeanReversionStrategy : Strategy
	{
		private BollingerBands _bollinger;
		private SimpleMovingAverage _widthAvg;
		private StandardDeviation _widthStdDev;

		private decimal _lastWidthAvg;
		private decimal _lastWidthStdDev;
		private AverageTrueRange _atr;

		private readonly StrategyParam<int> _bollingerLength;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<int> _widthLookbackPeriod;
		private readonly StrategyParam<decimal> _widthDeviationMultiplier;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Length of Bollinger Bands period.
		/// </summary>
		public int BollingerLength
		{
			get => _bollingerLength.Value;
			set => _bollingerLength.Value = value;
		}

		/// <summary>
		/// Deviation multiplier for Bollinger Bands.
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
		}

		/// <summary>
		/// Lookback period for width's mean and standard deviation.
		/// </summary>
		public int WidthLookbackPeriod
		{
			get => _widthLookbackPeriod.Value;
			set => _widthLookbackPeriod.Value = value;
		}

		/// <summary>
		/// Multiplier for width's standard deviation to determine entry threshold.
		/// </summary>
		public decimal WidthDeviationMultiplier
		{
			get => _widthDeviationMultiplier.Value;
			set => _widthDeviationMultiplier.Value = value;
		}

		/// <summary>
		/// Period for ATR calculation.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Multiplier for ATR to determine stop-loss distance.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Type of candles to use in the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="BollingerWidthMeanReversionStrategy"/>.
		/// </summary>
		public BollingerWidthMeanReversionStrategy()
		{
			_bollingerLength = Param(nameof(BollingerLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Length", "Period for Bollinger Bands calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_widthLookbackPeriod = Param(nameof(WidthLookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Width Lookback", "Lookback period for width's mean and standard deviation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_widthDeviationMultiplier = Param(nameof(WidthDeviationMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Width Deviation Multiplier", "Multiplier for width's standard deviation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR calculation", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Multiplier", "Multiplier for ATR to determine stop-loss distance", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use in the strategy", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();
			_lastWidthAvg = default;
			_lastWidthStdDev = default;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);


			// Initialize indicators
			_bollinger = new BollingerBands
			{
				Length = BollingerLength,
				Width = BollingerDeviation
			};

			_widthAvg = new SimpleMovingAverage { Length = WidthLookbackPeriod };
			_widthStdDev = new StandardDeviation { Length = WidthLookbackPeriod };
			_atr = new AverageTrueRange { Length = AtrPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_bollinger, _atr, ProcessBollinger)
				.Start();

			// Create chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _bollinger);
				DrawOwnTrades(area);
			}
		}

		private void ProcessBollinger(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Process ATR
			var lastAtr = atrValue.ToDecimal();

			var bollingerTyped = (BollingerBandsValue)bollingerValue;

			// Calculate Bollinger width
			var lastWidth = bollingerTyped.UpBand - bollingerTyped.LowBand;

			// Calculate width's average and standard deviation
			var widthAvg = _widthAvg.Process(lastWidth, candle.ServerTime, candle.State == CandleStates.Finished);
			var widthStdDev = _widthStdDev.Process(lastWidth, candle.ServerTime, candle.State == CandleStates.Finished);

			if (widthAvg.IsFinal && widthStdDev.IsFinal)
			{
				_lastWidthAvg = widthAvg.ToDecimal();
				_lastWidthStdDev = widthStdDev.ToDecimal();

				// Check if strategy is ready to trade
				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				// Calculate thresholds
				var lowerThreshold = _lastWidthAvg - WidthDeviationMultiplier * _lastWidthStdDev;
				var upperThreshold = _lastWidthAvg + WidthDeviationMultiplier * _lastWidthStdDev;

				// Trading logic
				if (lastWidth < lowerThreshold && Position <= 0)
				{
					// Width is compressed - Long signal (expecting expansion)
					BuyMarket(Volume + Math.Abs(Position));
					
					// Set ATR-based stop loss
					if (lastAtr > 0)
					{
						var stopPrice = candle.ClosePrice - AtrMultiplier * lastAtr;
						PlaceStopLoss(stopPrice);
					}
				}
				else if (lastWidth > upperThreshold && Position >= 0)
				{
					// Width is expanded - Short signal (expecting contraction)
					SellMarket(Volume + Math.Abs(Position));
					
					// Set ATR-based stop loss
					if (lastAtr > 0)
					{
						var stopPrice = candle.ClosePrice + AtrMultiplier * lastAtr;
						PlaceStopLoss(stopPrice);
					}
				}
				// Exit logic
				else if (lastWidth > _lastWidthAvg && Position > 0)
				{
					// Width returned to average - Exit long position
					SellMarket(Position);
				}
				else if (lastWidth < _lastWidthAvg && Position < 0)
				{
					// Width returned to average - Exit short position
					BuyMarket(Math.Abs(Position));
				}
			}
		}

		private void PlaceStopLoss(decimal price)
		{
			// Place a stop order as stop loss
			var stopOrder = CreateOrder(
				Position > 0 ? Sides.Sell : Sides.Buy,
				price,
				Math.Abs(Position)
			);
			
			RegisterOrder(stopOrder);
		}
	}
}
