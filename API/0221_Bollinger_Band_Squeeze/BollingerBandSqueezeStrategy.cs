using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Bollinger Band Squeeze strategy. 
	/// Trades when volatility decreases (bands squeeze) followed by a breakout.
	/// </summary>
	public class BollingerBandSqueezeStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerPeriodParam;
		private readonly StrategyParam<decimal> _bollingerMultiplierParam;
		private readonly StrategyParam<int> _lookbackPeriodParam;
		private readonly StrategyParam<DataType> _candleTypeParam;

		private BollingerBands _bollinger;
		private AverageTrueRange _atr;
		private decimal _prevBollingerWidth;
		private decimal _avgBollingerWidth;
		private decimal _bollingerWidthSum;
		private readonly Queue<decimal> _bollingerWidths = [];

		/// <summary>
		/// Bollinger Bands period.
		/// </summary>
		public int BollingerPeriod
		{
			get => _bollingerPeriodParam.Value;
			set => _bollingerPeriodParam.Value = value;
		}

		/// <summary>
		/// Bollinger Bands multiplier.
		/// </summary>
		public decimal BollingerMultiplier
		{
			get => _bollingerMultiplierParam.Value;
			set => _bollingerMultiplierParam.Value = value;
		}

		/// <summary>
		/// Period for averaging Bollinger width.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriodParam.Value;
			set => _lookbackPeriodParam.Value = value;
		}

		/// <summary>
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public BollingerBandSqueezeStrategy()
		{
			_bollingerPeriodParam = Param(nameof(BollingerPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_bollingerMultiplierParam = Param(nameof(BollingerMultiplier), 2.0m)
				.SetRange(0.1m, decimal.MaxValue)
				.SetDisplay("Bollinger Multiplier", "Standard deviation multiplier for Bollinger Bands", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

			_lookbackPeriodParam = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for averaging Bollinger width", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "Common");
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

			// Initialize indicator
			_bollinger = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerMultiplier
			};
			
			_atr = new AverageTrueRange { Length = BollingerPeriod };
			
			// Reset state
			_prevBollingerWidth = 0;
			_avgBollingerWidth = 0;
			_bollingerWidthSum = 0;
			_bollingerWidths.Clear();

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_bollinger, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _bollinger);
				DrawOwnTrades(area);
			}
			
			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(2, UnitTypes.Absolute) // Stop loss at 2*ATR
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal atr)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Calculate Bollinger width (upper - lower)
			var bollingerWidth = upperBand - lowerBand;
			
			// Track average Bollinger width over lookback period
			_bollingerWidths.Enqueue(bollingerWidth);
			_bollingerWidthSum += bollingerWidth;
			
			if (_bollingerWidths.Count > LookbackPeriod)
			{
				var oldValue = _bollingerWidths.Dequeue();
				_bollingerWidthSum -= oldValue;
			}
			
			if (_bollingerWidths.Count == LookbackPeriod)
			{
				_avgBollingerWidth = _bollingerWidthSum / LookbackPeriod;
				
				// Detect Bollinger Band squeeze (narrowing bands)
				bool isSqueeze = bollingerWidth < _avgBollingerWidth;
				
				// Breakout after squeeze
				if (isSqueeze)
				{
					// Upside breakout
					if (candle.ClosePrice > upperBand && Position <= 0)
					{
						BuyMarket(Volume + Math.Abs(Position));
					}
					// Downside breakout
					else if (candle.ClosePrice < lowerBand && Position >= 0)
					{
						SellMarket(Volume + Math.Abs(Position));
					}
				}
			}
			
			_prevBollingerWidth = bollingerWidth;
		}
	}
}