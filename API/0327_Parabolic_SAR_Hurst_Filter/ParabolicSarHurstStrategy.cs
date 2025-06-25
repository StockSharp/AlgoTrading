using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Parabolic SAR with Hurst Filter Strategy.
	/// Enters a position when price crosses SAR and Hurst exponent indicates a persistent trend.
	/// </summary>
	public class ParabolicSarHurstStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _sarAccelerationFactor;
		private readonly StrategyParam<decimal> _sarMaxAccelerationFactor;
		private readonly StrategyParam<int> _hurstPeriod;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _prevSarValue;
		private decimal _hurstValue;

		/// <summary>
		/// Parabolic SAR acceleration factor.
		/// </summary>
		public decimal SarAccelerationFactor
		{
			get => _sarAccelerationFactor.Value;
			set => _sarAccelerationFactor.Value = value;
		}

		/// <summary>
		/// Parabolic SAR maximum acceleration factor.
		/// </summary>
		public decimal SarMaxAccelerationFactor
		{
			get => _sarMaxAccelerationFactor.Value;
			set => _sarMaxAccelerationFactor.Value = value;
		}

		/// <summary>
		/// Hurst exponent calculation period.
		/// </summary>
		public int HurstPeriod
		{
			get => _hurstPeriod.Value;
			set => _hurstPeriod.Value = value;
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
		public ParabolicSarHurstStrategy()
		{
			_sarAccelerationFactor = Param(nameof(SarAccelerationFactor), 0.02m)
				.SetRange(0.01m, 0.2m)
				.SetDisplay("SAR Acceleration Factor", "Initial acceleration factor for Parabolic SAR", "SAR Settings")
				.SetCanOptimize(true)
				.SetOptimize(0.01m, 0.1m, 0.01m);

			_sarMaxAccelerationFactor = Param(nameof(SarMaxAccelerationFactor), 0.2m)
				.SetRange(0.05m, 0.5m)
				.SetDisplay("SAR Max Acceleration Factor", "Maximum acceleration factor for Parabolic SAR", "SAR Settings")
				.SetCanOptimize(true)
				.SetOptimize(0.1m, 0.3m, 0.05m);

			_hurstPeriod = Param(nameof(HurstPeriod), 100)
				.SetRange(20, 200)
				.SetDisplay("Hurst Period", "Period for Hurst exponent calculation", "Hurst Settings")
				.SetCanOptimize(true)
				.SetOptimize(50, 150, 25);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
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

			// Initialize values
			_prevSarValue = 0;
			_hurstValue = 0.5m; // Default value (random walk)

			// Create indicators
			var parabolicSar = new ParabolicSar
			{
				AccelerationFactor = SarAccelerationFactor,
				AccelerationLimit = SarMaxAccelerationFactor
			};

			var hurstIndicator = new HurstExponent
			{
				Length = HurstPeriod
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);

			// Bind indicators to the subscription
			subscription
				.BindEx(parabolicSar, hurstIndicator, ProcessCandle)
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
				DrawIndicator(area, parabolicSar);
				DrawIndicator(area, hurstIndicator);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue, IIndicatorValue hurstValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get SAR and Hurst values
			var sarPrice = sarValue.GetValue<decimal>();
			_hurstValue = hurstValue.GetValue<decimal>();

			// Store previous SAR for comparison
			var currentSarValue = sarPrice;
			
			// Log the values
			LogInfo($"SAR: {sarPrice}, Hurst: {_hurstValue}, Price: {candle.ClosePrice}");

			// Skip first candle (need previous SAR value for comparison)
			if (_prevSarValue == 0)
			{
				_prevSarValue = currentSarValue;
				return;
			}

			// Trading logic based on Parabolic SAR and Hurst exponent
			// Hurst > 0.5 indicates trending market (persistence)
			if (_hurstValue > 0.5m)
			{
				// Long signal: Price crossed above SAR
				if (candle.ClosePrice > sarPrice && Position <= 0)
				{
					// Close any existing short position
					if (Position < 0)
						BuyMarket(Math.Abs(Position));

					// Open long position
					BuyMarket(Volume);
					LogInfo($"Long signal: SAR={sarPrice}, Price={candle.ClosePrice}, Hurst={_hurstValue}");
				}
				// Short signal: Price crossed below SAR
				else if (candle.ClosePrice < sarPrice && Position >= 0)
				{
					// Close any existing long position
					if (Position > 0)
						SellMarket(Math.Abs(Position));

					// Open short position
					SellMarket(Volume);
					LogInfo($"Short signal: SAR={sarPrice}, Price={candle.ClosePrice}, Hurst={_hurstValue}");
				}
			}
			else
			{
				// If Hurst < 0.5, consider closing positions as market is not trending
				if (Position != 0)
				{
					LogInfo($"Closing position as Hurst < 0.5: Hurst={_hurstValue}");
					ClosePosition();
				}
			}

			// Update previous SAR value
			_prevSarValue = currentSarValue;
		}
	}
}