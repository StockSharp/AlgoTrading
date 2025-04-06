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
	/// Strategy combining VWAP with ADX trend strength indicator.
	/// </summary>
	public class VwapAdxTrendStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<decimal> _adxThreshold;
		private readonly StrategyParam<decimal> _adxExitThreshold;
		private readonly StrategyParam<DataType> _candleType;
		
		private VolumeWeightedMovingAverage _vwap;
		private AverageDirectionalIndex _adx;
		private DirectionalIndex _di;
		
		private decimal _vwapValue;
		private decimal _adxValue;
		private decimal _plusDiValue;
		private decimal _minusDiValue;

		/// <summary>
		/// ADX period for trend strength calculation.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// ADX threshold for trend strength entry.
		/// </summary>
		public decimal AdxThreshold
		{
			get => _adxThreshold.Value;
			set => _adxThreshold.Value = value;
		}
		
		/// <summary>
		/// ADX threshold for trend strength exit.
		/// </summary>
		public decimal AdxExitThreshold
		{
			get => _adxExitThreshold.Value;
			set => _adxExitThreshold.Value = value;
		}

		/// <summary>
		/// Candle type to use for the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VwapAdxTrendStrategy"/>.
		/// </summary>
		public VwapAdxTrendStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetDisplayName("ADX Period")
				.SetDescription("Period for ADX and Directional Index calculations")
				.SetCategory("ADX")
				.SetCanOptimize(true)
				.SetOptimize(8, 20, 2);

			_adxThreshold = Param(nameof(AdxThreshold), 25m)
				.SetDisplayName("ADX Threshold")
				.SetDescription("ADX threshold for trend strength entry")
				.SetCategory("ADX")
				.SetCanOptimize(true)
				.SetOptimize(20m, 40m, 5m);
				
			_adxExitThreshold = Param(nameof(AdxExitThreshold), 20m)
				.SetDisplayName("ADX Exit Threshold")
				.SetDescription("ADX threshold for trend strength exit")
				.SetCategory("ADX")
				.SetCanOptimize(true)
				.SetOptimize(10m, 25m, 5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).ToTimeFrameDataType())
				.SetDisplayName("Candle Type")
				.SetDescription("Type of candles to use")
				.SetCategory("General");
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

			// Create indicators
			_vwap = new VolumeWeightedMovingAverage();
			
			_adx = new AverageDirectionalIndex
			{
				Length = AdxPeriod
			};
			
			_di = new DirectionalIndex
			{
				Length = AdxPeriod
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEach(
					_vwap,
					_adx,
					_di,
					ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _vwap);
				DrawIndicator(area, _adx);
				DrawOwnTrades(area);
			}
			
			// Setup position protection
			StartProtection(
				new Unit(2, UnitTypes.Percent), 
				new Unit(2, UnitTypes.Percent)
			);
		}
		
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue vwapValue, IIndicatorValue adxValue, IIndicatorValue diValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Extract values from indicators
			_vwapValue = vwapValue.GetValue<decimal>();
			_adxValue = adxValue.GetValue<decimal>();
			_plusDiValue = diValue[0].To<decimal>();  // +DI
			_minusDiValue = diValue[1].To<decimal>(); // -DI
			
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Log current values
			this.AddInfoLog($"VWAP: {_vwapValue:F2}, ADX: {_adxValue:F2}, +DI: {_plusDiValue:F2}, -DI: {_minusDiValue:F2}");
			
			// Trading logic
			// Buy when price is above VWAP, ADX > threshold, and +DI > -DI (strong uptrend)
			if (candle.ClosePrice > _vwapValue && _adxValue > AdxThreshold && _plusDiValue > _minusDiValue && Position <= 0)
			{
				BuyMarket(Volume);
				this.AddInfoLog($"Buy Signal: Price {candle.ClosePrice:F2} > VWAP {_vwapValue:F2}, ADX {_adxValue:F2} > {AdxThreshold}, +DI {_plusDiValue:F2} > -DI {_minusDiValue:F2}");
			}
			// Sell when price is below VWAP, ADX > threshold, and -DI > +DI (strong downtrend)
			else if (candle.ClosePrice < _vwapValue && _adxValue > AdxThreshold && _minusDiValue > _plusDiValue && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				this.AddInfoLog($"Sell Signal: Price {candle.ClosePrice:F2} < VWAP {_vwapValue:F2}, ADX {_adxValue:F2} > {AdxThreshold}, -DI {_minusDiValue:F2} > +DI {_plusDiValue:F2}");
			}
			// Exit long position when ADX weakens below exit threshold or -DI crosses above +DI
			else if (Position > 0 && (_adxValue < AdxExitThreshold || _minusDiValue > _plusDiValue))
			{
				SellMarket(Position);
				this.AddInfoLog($"Exit Long: ADX {_adxValue:F2} < {AdxExitThreshold} or -DI {_minusDiValue:F2} > +DI {_plusDiValue:F2}");
			}
			// Exit short position when ADX weakens below exit threshold or +DI crosses above -DI
			else if (Position < 0 && (_adxValue < AdxExitThreshold || _plusDiValue > _minusDiValue))
			{
				BuyMarket(Math.Abs(Position));
				this.AddInfoLog($"Exit Short: ADX {_adxValue:F2} < {AdxExitThreshold} or +DI {_plusDiValue:F2} > -DI {_minusDiValue:F2}");
			}
		}
	}
}