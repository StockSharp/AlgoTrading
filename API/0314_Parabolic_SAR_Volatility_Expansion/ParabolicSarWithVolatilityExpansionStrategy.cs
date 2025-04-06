using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Parabolic SAR with Volatility Expansion detection.
	/// </summary>
	public class ParabolicSarWithVolatilityExpansionStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _sarAf;
		private readonly StrategyParam<decimal> _sarMaxAf;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _volatilityExpansionFactor;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// SAR acceleration factor parameter.
		/// </summary>
		public decimal SarAf
		{
			get => _sarAf.Value;
			set => _sarAf.Value = value;
		}

		/// <summary>
		/// SAR maximum acceleration factor parameter.
		/// </summary>
		public decimal SarMaxAf
		{
			get => _sarMaxAf.Value;
			set => _sarMaxAf.Value = value;
		}

		/// <summary>
		/// ATR period parameter.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Volatility expansion factor parameter.
		/// </summary>
		public decimal VolatilityExpansionFactor
		{
			get => _volatilityExpansionFactor.Value;
			set => _volatilityExpansionFactor.Value = value;
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
		/// Constructor.
		/// </summary>
		public ParabolicSarWithVolatilityExpansionStrategy()
		{
			_sarAf = Param(nameof(SarAf), 0.02m)
				.SetGreaterThanZero()
				.SetDisplay("SAR AF", "Parabolic SAR acceleration factor", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(0.01m, 0.05m, 0.01m);

			_sarMaxAf = Param(nameof(SarMaxAf), 0.2m)
				.SetGreaterThanZero()
				.SetDisplay("SAR Max AF", "Parabolic SAR maximum acceleration factor", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(0.1m, 0.3m, 0.05m);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 28, 7);

			_volatilityExpansionFactor = Param(nameof(VolatilityExpansionFactor), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Volatility Expansion Factor", "Factor for volatility expansion detection", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

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

			// Create indicators
			var parabolicSar = new ParabolicSar
			{
				AccelerationFactor = SarAf,
				AccelerationLimit = SarMaxAf
			};
			
			var atr = new AverageTrueRange { Length = AtrPeriod };
			var atrSma = new SimpleMovingAverage { Length = AtrPeriod };
			var atrStdDev = new StandardDeviation { Length = AtrPeriod };

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(parabolicSar, atr, (candle, sarValue, atrValue) =>
				{
					// Calculate ATR average and standard deviation
					var atrInput = new DecimalIndicatorValue(atrValue);
					var atrSmaValue = atrSma.Process(atrInput);
					var atrStdDevValue = atrStdDev.Process(atrInput);
					
					// Process the strategy logic
					ProcessStrategy(
						candle,
						sarValue,
						atrValue,
						atrSmaValue.GetValue<decimal>(),
						atrStdDevValue.GetValue<decimal>()
					);
				})
				.Start();

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, parabolicSar);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}

		private void ProcessStrategy(ICandleMessage candle, decimal sarValue, decimal atrValue, decimal atrAvg, decimal atrStdDev)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check if volatility is expanding
			var volatilityThreshold = atrAvg + (VolatilityExpansionFactor * atrStdDev);
			var isVolatilityExpanding = atrValue > volatilityThreshold;
			
			// Trading logic - only trade during volatility expansion
			if (isVolatilityExpanding)
			{
				// Check price relative to SAR
				var isAboveSar = candle.ClosePrice > sarValue;
				var isBelowSar = candle.ClosePrice < sarValue;
				
				if (isAboveSar && Position <= 0)
				{
					// Price above SAR with volatility expansion - Go long
					CancelActiveOrders();
					
					// Calculate position size
					var volume = Volume + Math.Abs(Position);
					
					// Enter long position
					BuyMarket(volume);
				}
				else if (isBelowSar && Position >= 0)
				{
					// Price below SAR with volatility expansion - Go short
					CancelActiveOrders();
					
					// Calculate position size
					var volume = Volume + Math.Abs(Position);
					
					// Enter short position
					SellMarket(volume);
				}
			}
			
			// Exit logic - when price crosses SAR
			if ((Position > 0 && candle.ClosePrice < sarValue) ||
				(Position < 0 && candle.ClosePrice > sarValue))
			{
				// Close position
				ClosePosition();
			}
		}
	}
}
