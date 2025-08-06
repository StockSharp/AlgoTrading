using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// ADX Weakening Strategy.
	/// Enters long when ADX weakens and price is above MA.
	/// Enters short when ADX weakens and price is below MA.
	/// </summary>
	public class AdxWeakeningStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<Unit> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _prevAdxValue;

		/// <summary>
		/// Period for ADX calculation.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// Period for moving average.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// Stop loss percentage from entry price.
		/// </summary>
		public Unit StopLoss
		{
			get => _stopLoss.Value;
			set => _stopLoss.Value = value;
		}

		/// <summary>
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AdxWeakeningStrategy"/>.
		/// </summary>
		public AdxWeakeningStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
				.SetRange(7, 28)
				.SetCanOptimize(true);
				
			_maPeriod = Param(nameof(MaPeriod), 20)
				.SetDisplay("MA Period", "Period for moving average", "Indicators")
				.SetRange(10, 50)
				.SetCanOptimize(true);
				
			_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
				.SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")
				.SetRange(1m, 3m)
				.SetCanOptimize(true);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
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
			_prevAdxValue = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Enable position protection using stop-loss
			StartProtection(
				takeProfit: null,
				stopLoss: StopLoss,
				isStopTrailing: false,
				useMarketOrders: true
			);


			// Create indicators
			var ma = new SimpleMovingAverage { Length = MaPeriod };
			var adx = new AverageDirectionalIndex { Length = AdxPeriod };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators and process candles
			subscription
				.BindEx(ma, adx, ProcessCandle)
				.Start();
				
			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ma);
				DrawIndicator(area, adx);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process candle with indicator values.
		/// </summary>
		/// <param name="candle">Candle.</param>
		/// <param name="ma">Moving average value.</param>
		/// <param name="adx">ADX value.</param>
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue adxValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var ma = maValue.ToDecimal();

			var adxTyped = (AverageDirectionalIndexValue)adxValue;

			if (adxTyped.MovingAverage is not decimal adx)
				return;

			var dx = adxTyped.Dx;

			if (dx.Plus is not decimal plusDi || dx.Minus is not decimal minusDi)
				return;

			// If this is the first calculation, just store the ADX value
			if (_prevAdxValue == 0)
			{
				_prevAdxValue = adx;
				return;
			}
			
			// Check if ADX is weakening (decreasing)
			bool isAdxWeakening = adx < _prevAdxValue;
			
			// Long entry: ADX weakening and price above MA
			if (isAdxWeakening && candle.ClosePrice > ma && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry: ADX weakening ({adx} < {_prevAdxValue}) and price above MA");
			}
			// Short entry: ADX weakening and price below MA
			else if (isAdxWeakening && candle.ClosePrice < ma && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry: ADX weakening ({adx} < {_prevAdxValue}) and price below MA");
			}
			
			// Update previous ADX value
			_prevAdxValue = adx;
		}
	}
}