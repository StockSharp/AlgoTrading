using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on Donchian Channel breakouts with Hurst Exponent filter.
	/// Enters position when price breaks Donchian Channel with Hurst Exponent indicating trend persistence.
	/// </summary>
	public class DonchianHurstStrategy : Strategy
	{
		private readonly StrategyParam<int> _donchianPeriod;
		private readonly StrategyParam<int> _hurstPeriod;
		private readonly StrategyParam<decimal> _hurstThreshold;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _hurstValue;
		private bool _donchianIsFormed;

		/// <summary>
		/// Strategy parameter: Donchian Channel period.
		/// </summary>
		public int DonchianPeriod
		{
			get => _donchianPeriod.Value;
			set => _donchianPeriod.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Hurst Exponent calculation period.
		/// </summary>
		public int HurstPeriod
		{
			get => _hurstPeriod.Value;
			set => _hurstPeriod.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Hurst Exponent threshold for trend persistence.
		/// </summary>
		public decimal HurstThreshold
		{
			get => _hurstThreshold.Value;
			set => _hurstThreshold.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Stop-loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public DonchianHurstStrategy()
		{
			_donchianPeriod = Param(nameof(DonchianPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Donchian Period", "Period for Donchian Channel indicator", "Indicator Settings");

			_hurstPeriod = Param(nameof(HurstPeriod), 100)
				.SetGreaterThanZero()
				.SetDisplay("Hurst Period", "Period for Hurst Exponent calculation", "Indicator Settings");

			_hurstThreshold = Param(nameof(HurstThreshold), 0.5m)
				.SetRange(0, 1)
				.SetDisplay("Hurst Threshold", "Minimum Hurst Exponent value for trend persistence (>0.5 is trending)", "Indicator Settings");

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop Loss percentage from entry price", "Risk Management");

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

			// Reset state variables
			_hurstValue = 0;
			_donchianIsFormed = false;

			// Create Donchian Channel indicator
			var donchian = new DonchianChannels
			{
				Length = DonchianPeriod
			};

			// Create FractalDimension indicator for Hurst calculation
			// We use 1 - FractalDimension to get Hurst Exponent (H = 2 - D)
			var fractalDimension = new FractalDimension
			{
				Length = HurstPeriod
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);

			// Bind indicators to subscription and start
			subscription
				.BindEx(donchian, fractalDimension, ProcessIndicators)
				.Start();

			// Add chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, donchian);
				DrawOwnTrades(area);
			}

			// Start position protection with percentage-based stop-loss
			StartProtection(
				takeProfit: new Unit(0), // No take profit, using Donchian Channel for exit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessIndicators(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue fractalDimensionValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// --- FractalDimension logic (was ProcessFractalDimension) ---
			decimal fractalDimension = fractalDimensionValue.ToDecimal();
			_hurstValue = 2m - fractalDimension;

			// Log Hurst Exponent value periodically
			if (candle.OpenTime.Second == 0 && candle.OpenTime.Minute % 15 == 0)
			{
				LogInfo($"Current Hurst Exponent: {_hurstValue} (>{HurstThreshold} indicates trend persistence)");
			}

			// --- Donchian logic (was ProcessDonchianChannel) ---
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check if Donchian Channel is formed
			if (!_donchianIsFormed)
			{
				_donchianIsFormed = true;
				return;
			}

			var donchianTyped = (DonchianChannelsValue)donchianValue;

			// Convert indicator values to decimal
			decimal upper = donchianTyped.UpperBand;
			decimal lower = donchianTyped.LowerBand;
			decimal middle = donchianTyped.Middle;

			// Check for Hurst Exponent indicating trend persistence
			if (_hurstValue > HurstThreshold)
			{
				// Check for breakout signals
				if (candle.ClosePrice > upper && Position <= 0)
				{
					// Breakout above upper band - Buy signal
					LogInfo($"Buy signal: Breakout above Donchian upper band ({upper}) with Hurst = {_hurstValue}");
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (candle.ClosePrice < lower && Position >= 0)
				{
					// Breakout below lower band - Sell signal
					LogInfo($"Sell signal: Breakout below Donchian lower band ({lower}) with Hurst = {_hurstValue}");
					SellMarket(Volume + Math.Abs(Position));
				}
			}

			// Exit rules based on middle band reversion
			if ((Position > 0 && candle.ClosePrice < middle) ||
				(Position < 0 && candle.ClosePrice > middle))
			{
				LogInfo($"Exit signal: Price reverted to middle band ({middle})");
				ClosePosition();
			}
		}
	}
}
