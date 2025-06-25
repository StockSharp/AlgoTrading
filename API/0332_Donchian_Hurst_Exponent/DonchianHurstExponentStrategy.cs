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
				.SetGreaterThan(0)
				.SetDisplay("Stop Loss %", "Stop Loss percentage from entry price", "Risk Management");

			_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security security, DataType dataType)> GetWorkingSecurities()
		{
			return new[] { (Security, CandleType) };
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Reset state variables
			_hurstValue = 0;
			_donchianIsFormed = false;

			// Create Donchian Channel indicator
			var donchian = new DonchianChannel
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
				.Bind(donchian, ProcessDonchianChannel)
				.BindEx(fractalDimension, ProcessFractalDimension)
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

		private void ProcessDonchianChannel(ICandleMessage candle, decimal upperBand, decimal lowerBand, decimal middleBand)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check if Donchian Channel is formed
			if (!_donchianIsFormed)
			{
				_donchianIsFormed = true;
				return;
			}

			// Check for Hurst Exponent indicating trend persistence
			if (_hurstValue > HurstThreshold)
			{
				// Check for breakout signals
				if (candle.ClosePrice > upperBand && Position <= 0)
				{
					// Breakout above upper band - Buy signal
					LogInfo($"Buy signal: Breakout above Donchian upper band ({upperBand}) with Hurst = {_hurstValue}");
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (candle.ClosePrice < lowerBand && Position >= 0)
				{
					// Breakout below lower band - Sell signal
					LogInfo($"Sell signal: Breakout below Donchian lower band ({lowerBand}) with Hurst = {_hurstValue}");
					SellMarket(Volume + Math.Abs(Position));
				}
			}

			// Exit rules based on middle band reversion
			if ((Position > 0 && candle.ClosePrice < middleBand) ||
				(Position < 0 && candle.ClosePrice > middleBand))
			{
				LogInfo($"Exit signal: Price reverted to middle band ({middleBand})");
				ClosePosition();
			}
		}

		private void ProcessFractalDimension(ICandleMessage candle, IIndicatorValue fractalDimensionValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Calculate Hurst Exponent from Fractal Dimension
			// Relationship: H = 2 - D, where D is fractal dimension
			decimal fractalDimension = fractalDimensionValue.GetValue<decimal>();
			_hurstValue = 2m - fractalDimension;

			// Log Hurst Exponent value periodically
			if (candle.OpenTime.Second == 0 && candle.OpenTime.Minute % 15 == 0)
			{
				LogInfo($"Current Hurst Exponent: {_hurstValue} (>{HurstThreshold} indicates trend persistence)");
			}
		}
	}
}
