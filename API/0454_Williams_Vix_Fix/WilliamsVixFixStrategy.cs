using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Williams VIX Fix Strategy - uses Williams VIX Fix with Bollinger Bands for volatility trading
	/// </summary>
	public class WilliamsVixFixStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _bbLength;
		private readonly StrategyParam<decimal> _bbMultiplier;
		private readonly StrategyParam<int> _wvfPeriod;
		private readonly StrategyParam<int> _wvfLookback;
		private readonly StrategyParam<decimal> _highestPercentile;
		private readonly StrategyParam<decimal> _lowestPercentile;

		private BollingerBands _bollingerBands;
		private Highest _highestClose;
		private Lowest _lowestClose;
		private SimpleMovingAverage _wvfSma;
		private StandardDeviation _wvfStdDev;
		private SimpleMovingAverage _wvfInvSma;
		private StandardDeviation _wvfInvStdDev;

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Bollinger Bands length.
		/// </summary>
		public int BbLength
		{
			get => _bbLength.Value;
			set => _bbLength.Value = value;
		}

		/// <summary>
		/// Bollinger Bands multiplier.
		/// </summary>
		public decimal BbMultiplier
		{
			get => _bbMultiplier.Value;
			set => _bbMultiplier.Value = value;
		}

		/// <summary>
		/// Williams VIX Fix lookback period for standard deviation.
		/// </summary>
		public int WvfPeriod
		{
			get => _wvfPeriod.Value;
			set => _wvfPeriod.Value = value;
		}

		/// <summary>
		/// Williams VIX Fix lookback period for percentile.
		/// </summary>
		public int WvfLookback
		{
			get => _wvfLookback.Value;
			set => _wvfLookback.Value = value;
		}

		/// <summary>
		/// Highest percentile threshold.
		/// </summary>
		public decimal HighestPercentile
		{
			get => _highestPercentile.Value;
			set => _highestPercentile.Value = value;
		}

		/// <summary>
		/// Lowest percentile threshold.
		/// </summary>
		public decimal LowestPercentile
		{
			get => _lowestPercentile.Value;
			set => _lowestPercentile.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public WilliamsVixFixStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_bbLength = Param(nameof(BbLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("BB Length", "Bollinger Bands length", "Bollinger Bands")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_bbMultiplier = Param(nameof(BbMultiplier), 2.0m)
				.SetDisplay("BB Multiplier", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

			_wvfPeriod = Param(nameof(WvfPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("WVF Period", "Williams VIX Fix lookback period for StdDev", "Williams VIX Fix")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_wvfLookback = Param(nameof(WvfLookback), 50)
				.SetGreaterThanZero()
				.SetDisplay("WVF Lookback", "Williams VIX Fix lookback period for percentile", "Williams VIX Fix")
				.SetCanOptimize(true)
				.SetOptimize(30, 70, 10);

			_highestPercentile = Param(nameof(HighestPercentile), 0.85m)
				.SetDisplay("Highest Percentile", "Highest percentile threshold", "Williams VIX Fix")
				.SetCanOptimize(true)
				.SetOptimize(0.75m, 0.95m, 0.05m);

			_lowestPercentile = Param(nameof(LowestPercentile), 0.99m)
				.SetDisplay("Lowest Percentile", "Lowest percentile threshold", "Williams VIX Fix")
				.SetCanOptimize(true)
				.SetOptimize(0.90m, 1.0m, 0.02m);
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
			_bollingerBands = new BollingerBands { Length = BbLength, Width = BbMultiplier };
			_highestClose = new Highest { Length = WvfPeriod };
			_lowestClose = new Lowest { Length = WvfPeriod };
			_wvfSma = new SimpleMovingAverage { Length = BbLength };
			_wvfStdDev = new StandardDeviation { Length = BbLength };
			_wvfInvSma = new SimpleMovingAverage { Length = BbLength };
			_wvfInvStdDev = new StandardDeviation { Length = BbLength };

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_bollingerBands, _highestClose, _lowestClose, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _bollingerBands);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue highestValue, IIndicatorValue lowestValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Wait for indicators to form
			if (!_bollingerBands.IsFormed || !_highestClose.IsFormed || !_lowestClose.IsFormed)
				return;

			var currentPrice = candle.ClosePrice;
			var lowPrice = candle.LowPrice;
			var highPrice = candle.HighPrice;

			// Calculate Williams VIX Fix
			var wvf = ((highestValue.ToDecimal() - lowPrice) / highestValue.ToDecimal()) * 100;
			
			// Process WVF through SMA and StdDev using manual calculation
			var wvfSmaValue = _wvfSma.Process(wvf, candle.ServerTime, candle.State == CandleStates.Finished);
			var wvfStdDevValue = _wvfStdDev.Process(wvf, candle.ServerTime, candle.State == CandleStates.Finished);

			if (!wvfSmaValue.IsFormed || !wvfStdDevValue.IsFormed)
				return;

			var wvfUpperBand = wvfSmaValue.ToDecimal() + (BbMultiplier * wvfStdDevValue.ToDecimal());

			// Calculate Williams VIX Fix Inverted
			var wvfInv = ((highPrice - lowestValue.ToDecimal()) / lowestValue.ToDecimal()) * 100;
			
			// Process WVF Inverted through SMA and StdDev using manual calculation
			var wvfInvSmaValue = _wvfInvSma.Process(wvfInv, candle.ServerTime, candle.State == CandleStates.Finished);
			var wvfInvStdDevValue = _wvfInvStdDev.Process(wvfInv, candle.ServerTime, candle.State == CandleStates.Finished);

			if (!wvfInvSmaValue.IsFormed || !wvfInvStdDevValue.IsFormed)
				return;

			var wvfInvUpperBand = wvfInvSmaValue.ToDecimal() + (BbMultiplier * wvfInvStdDevValue.ToDecimal());

			CheckEntryExitConditions(candle, wvf, wvfUpperBand, wvfInv, wvfInvUpperBand);
		}

		private void CheckEntryExitConditions(ICandleMessage candle, decimal wvf, decimal wvfUpperBand, decimal wvfInv, decimal wvfInvUpperBand)
		{
			var currentPrice = candle.ClosePrice;
			var bbLower = _bollingerBands.LowBand.GetValue(0);
			var bbUpper = _bollingerBands.UpBand.GetValue(0);

			// Simplified range calculations (original uses complex percentile logic)
			var rangeHigh = wvf * HighestPercentile; // Simplified
			var rangeHighInv = wvfInv * LowestPercentile; // Simplified

			// Buy condition: WVF signals and price below lower Bollinger Band
			var buyCondition = (wvf >= wvfUpperBand || wvf >= rangeHigh) && currentPrice < bbLower;

			// Sell condition: WVF Inverted signals and price above upper Bollinger Band
			var sellCondition = (wvfInv <= wvfInvUpperBand || wvfInv <= rangeHighInv) && currentPrice > bbUpper;

			if (buyCondition && Position == 0)
			{
				RegisterOrder(this.CreateOrder(Sides.Buy, currentPrice, GetOrderVolume()));
			}

			if (Position > 0 && sellCondition)
			{
				RegisterOrder(this.CreateOrder(Sides.Sell, currentPrice, Math.Abs(Position)));
			}
		}

		private decimal GetOrderVolume()
		{
			return 1;
		}
	}
}