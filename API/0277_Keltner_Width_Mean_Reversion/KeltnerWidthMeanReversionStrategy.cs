namespace StockSharp.Samples.Strategies
{
	using System;
	using System.Collections.Generic;
	using StockSharp.Algo;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Keltner Width Mean Reversion Strategy.
	/// Strategy trades based on mean reversion of Keltner Channel width.
	/// </summary>
	public class KeltnerWidthMeanReversionStrategy : Strategy
	{
		private ExponentialMovingAverage _ema;
		private AverageTrueRange _atr;
		private decimal _lastEma;
		private decimal _lastAtr;
		private decimal _lastChannelWidth;
		
		private SimpleMovingAverage _widthAvg;
		private StandardDeviation _widthStdDev;
		private decimal _lastWidthAvg;
		private decimal _lastWidthStdDev;

		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _keltnerMultiplier;
		private readonly StrategyParam<int> _widthLookbackPeriod;
		private readonly StrategyParam<decimal> _widthDeviationMultiplier;
		private readonly StrategyParam<decimal> _atrStopMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Period for EMA calculation.
		/// </summary>
		public int EmaPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
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
		/// Multiplier for Keltner Channel bands.
		/// </summary>
		public decimal KeltnerMultiplier
		{
			get => _keltnerMultiplier.Value;
			set => _keltnerMultiplier.Value = value;
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
		/// Multiplier for ATR to determine stop-loss distance.
		/// </summary>
		public decimal AtrStopMultiplier
		{
			get => _atrStopMultiplier.Value;
			set => _atrStopMultiplier.Value = value;
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
		/// Initializes a new instance of <see cref="KeltnerWidthMeanReversionStrategy"/>.
		/// </summary>
		public KeltnerWidthMeanReversionStrategy()
		{
			_emaPeriod = Param(nameof(EmaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Keltner Multiplier", "Multiplier for Keltner Channel bands", "Indicators")
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

			_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Stop Multiplier", "Multiplier for ATR to determine stop-loss distance", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use in the strategy", "General");
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
			_ema = new ExponentialMovingAverage { Length = EmaPeriod };
			_atr = new AverageTrueRange { Length = AtrPeriod };
			_widthAvg = new SimpleMovingAverage { Length = WidthLookbackPeriod };
			_widthStdDev = new StandardDeviation { Length = WidthLookbackPeriod };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Setup candle processing
			subscription
				.Bind(ProcessCandle)
				.Start();

			// Create chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Process EMA
			var emaValue = _ema.Process(candle);
			if (emaValue.IsFinal)
				_lastEma = emaValue.GetValue<decimal>();

			// Process ATR
			var atrValue = _atr.Process(candle);
			if (atrValue.IsFinal)
				_lastAtr = atrValue.GetValue<decimal>();

			// Calculate Keltner Channel
			if (_lastEma > 0 && _lastAtr > 0)
			{
				// Calculate upper and lower bands
				var upperBand = _lastEma + KeltnerMultiplier * _lastAtr;
				var lowerBand = _lastEma - KeltnerMultiplier * _lastAtr;
				
				// Calculate channel width
				var channelWidth = upperBand - lowerBand;
				_lastChannelWidth = channelWidth;
				
				// Process width's average and standard deviation
				var widthAvgValue = _widthAvg.Process(new DecimalIndicatorValue(channelWidth));
				var widthStdDevValue = _widthStdDev.Process(new DecimalIndicatorValue(channelWidth));
				
				if (widthAvgValue.IsFinal && widthStdDevValue.IsFinal)
				{
					_lastWidthAvg = widthAvgValue.GetValue<decimal>();
					_lastWidthStdDev = widthStdDevValue.GetValue<decimal>();
					
					// Check if strategy is ready to trade
					if (!IsFormedAndOnlineAndAllowTrading())
						return;
					
					// Calculate thresholds
					var lowerThreshold = _lastWidthAvg - WidthDeviationMultiplier * _lastWidthStdDev;
					var upperThreshold = _lastWidthAvg + WidthDeviationMultiplier * _lastWidthStdDev;
					
					// Trading logic
					if (_lastChannelWidth < lowerThreshold && Position <= 0)
					{
						// Channel width is compressed - Long signal (expecting expansion)
						BuyMarket(Volume + Math.Abs(Position));
						
						// Set ATR-based stop loss
						PlaceStopLoss(candle.ClosePrice - AtrStopMultiplier * _lastAtr);
					}
					else if (_lastChannelWidth > upperThreshold && Position >= 0)
					{
						// Channel width is expanded - Short signal (expecting contraction)
						SellMarket(Volume + Math.Abs(Position));
						
						// Set ATR-based stop loss
						PlaceStopLoss(candle.ClosePrice + AtrStopMultiplier * _lastAtr);
					}
					// Exit logic
					else if (_lastChannelWidth > _lastWidthAvg && Position > 0)
					{
						// Width returned to average - Exit long position
						SellMarket(Position);
					}
					else if (_lastChannelWidth < _lastWidthAvg && Position < 0)
					{
						// Width returned to average - Exit short position
						BuyMarket(Math.Abs(Position));
					}
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
			
			stopOrder.Type = OrderTypes.Stop;
			RegisterOrder(stopOrder);
		}
	}
}
