using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Hull Moving Average and ADX.
	/// Enters long when HMA increases and ADX > 25 (strong trend).
	/// Enters short when HMA decreases and ADX > 25 (strong trend).
	/// Exits when ADX < 20 (weakening trend).
	/// </summary>
	public class HullMaAdxStrategy : Strategy
	{
		private readonly StrategyParam<int> _hmaPeriod;
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private HullMovingAverage _hma;
		private AverageDirectionalMovementIndex _adx;
		private AverageTrueRange _atr;
		
		private decimal _prevHmaValue;
		private decimal _prevAdxValue;

		/// <summary>
		/// Hull Moving Average period.
		/// </summary>
		public int HmaPeriod
		{
			get => _hmaPeriod.Value;
			set => _hmaPeriod.Value = value;
		}

		/// <summary>
		/// ADX indicator period.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
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
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HullMaAdxStrategy"/>.
		/// </summary>
		public HullMaAdxStrategy()
		{
			_hmaPeriod = Param(nameof(HmaPeriod), 9)
				.SetDisplayName("HMA Period")
				.SetDescription("Period for Hull Moving Average calculation")
				.SetCategories("Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 15, 2);

			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetDisplayName("ADX Period")
				.SetDescription("Period for Average Directional Movement Index")
				.SetCategories("Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetDisplayName("ATR Multiplier")
				.SetDescription("ATR multiplier for stop loss calculation")
				.SetCategories("Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplayName("Candle Type")
				.SetDescription("Timeframe of data for strategy")
				.SetCategories("General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_hma = new HullMovingAverage { Length = HmaPeriod };
			_adx = new AverageDirectionalMovementIndex { Length = AdxPeriod };
			_atr = new AverageTrueRange { Length = 14 };

			// Initialize variables
			_prevHmaValue = 0;
			_prevAdxValue = 0;

			// Create subscription
			var subscription = SubscribeCandles(CandleType);

			// Process candles with indicators
			subscription
				.Bind(_hma, _adx, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _hma);
				DrawOwnTrades(area);

				// ADX in separate area
				var adxArea = CreateChartArea();
				if (adxArea != null)
				{
					DrawIndicator(adxArea, _adx);
				}
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal hma, decimal adx, decimal atr)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Detect HMA direction
			bool hmaIncreasing = hma > _prevHmaValue;
			bool hmaDecreasing = hma < _prevHmaValue;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
			{
				// Store current values for next candle
				_prevHmaValue = hma;
				_prevAdxValue = adx;
				return;
			}

			// Trading logic
			if (adx > 25)
			{
				// Strong trend detected
				if (hmaIncreasing && Position <= 0)
				{
					// HMA rising with strong trend - go long
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (hmaDecreasing && Position >= 0)
				{
					// HMA falling with strong trend - go short
					SellMarket(Volume + Math.Abs(Position));
				}
			}
			else if (adx < 20 && _prevAdxValue >= 20)
			{
				// Trend weakening - close position
				ClosePosition();
			}

			// Set dynamic stop loss based on ATR
			if (Position != 0)
			{
				StartProtection(
					new Unit(0), // No take profit - use ADX for exit
					new Unit(AtrMultiplier * atr, UnitTypes.Absolute)
				);
			}

			// Store current values for next candle
			_prevHmaValue = hma;
			_prevAdxValue = adx;
		}
	}
}