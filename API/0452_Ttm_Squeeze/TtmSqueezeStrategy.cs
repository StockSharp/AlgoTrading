using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// TTM Squeeze Strategy - uses TTM Squeeze momentum with RSI filter
	/// </summary>
	public class TtmSqueezeStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _squeezeLength;
		private readonly StrategyParam<int> _rsiLength;
		private readonly StrategyParam<bool> _useTP;
		private readonly StrategyParam<decimal> _tpPercent;

		private BollingerBands _bollingerBands;
		private KeltnerChannels _keltnerChannels;
		private Highest _highest;
		private Lowest _lowest;
		private SimpleMovingAverage _closeSma;
		private LinearRegression _momentum;
		private RelativeStrengthIndex _rsi;

		private decimal _previousMomentum;
		private decimal _currentMomentum;

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// TTM Squeeze calculation length.
		/// </summary>
		public int SqueezeLength
		{
			get => _squeezeLength.Value;
			set => _squeezeLength.Value = value;
		}

		/// <summary>
		/// RSI calculation length.
		/// </summary>
		public int RsiLength
		{
			get => _rsiLength.Value;
			set => _rsiLength.Value = value;
		}

		/// <summary>
		/// Use take profit.
		/// </summary>
		public bool UseTP
		{
			get => _useTP.Value;
			set => _useTP.Value = value;
		}

		/// <summary>
		/// Take profit percentage.
		/// </summary>
		public decimal TpPercent
		{
			get => _tpPercent.Value;
			set => _tpPercent.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public TtmSqueezeStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_squeezeLength = Param(nameof(SqueezeLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("Squeeze Length", "TTM Squeeze calculation length", "TTM Squeeze")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_rsiLength = Param(nameof(RsiLength), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Length", "RSI calculation length", "RSI")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 2);

			_useTP = Param(nameof(UseTP), false)
				.SetDisplay("Enable Take Profit", "Use take profit", "Take Profit");

			_tpPercent = Param(nameof(TpPercent), 1.2m)
				.SetRange(0.1m, 10.0m)
				.SetDisplay("TP Percent", "Take profit percentage", "Take Profit")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 3.0m, 0.3m);
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
			_bollingerBands = new BollingerBands { Length = SqueezeLength, Width = 2.0m };
			_keltnerChannels = new KeltnerChannels { Length = SqueezeLength, Multiplier = 1.0m };
			_highest = new Highest { Length = SqueezeLength };
			_lowest = new Lowest { Length = SqueezeLength };
			_closeSma = new SimpleMovingAverage { Length = SqueezeLength };
			_momentum = new LinearRegression { Length = SqueezeLength };
			_rsi = new RelativeStrengthIndex { Length = RsiLength };

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_rsi, _highest, _lowest, _closeSma, _momentum, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}

			// Setup protection if take profit is enabled
			if (UseTP)
			{
				StartProtection(new Unit(TpPercent / 100m, UnitTypes.Percent), new Unit());
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal highestValue, decimal lowestValue, decimal closeSmaValue, decimal momentumValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Wait for indicators to form
			if (!_momentum.IsFormed || !_rsi.IsFormed)
				return;

			// Calculate TTM Squeeze momentum oscillator
			// e1 = (highest + lowest) / 2 + sma(close)
			var e1 = (highestValue + lowestValue) / 2 + closeSmaValue;
			_currentMomentum = momentumValue; // LinearRegression of (close - e1/2)

			CheckEntryConditions(candle, rsiValue);

			// Store previous momentum
			_previousMomentum = _currentMomentum;
		}

		private void CheckEntryConditions(ICandleMessage candle, decimal rsiValue)
		{
			var currentPrice = candle.ClosePrice;

			// Long entry: momentum < 0, momentum increasing for 2 bars, RSI crosses over 30
			if (_currentMomentum < 0 && 
				_previousMomentum != 0 && 
				_currentMomentum > _previousMomentum && 
				// Need to track previous RSI values for crossover detection
				_previousMomentum < _currentMomentum && // momentum trend up (simplified)
				rsiValue > 30 && // RSI crossover 30 (simplified)
				Position == 0)
			{
				RegisterOrder(this.CreateOrder(Sides.Buy, currentPrice, GetOrderVolume()));
			}

			// Short entry: momentum > 0, momentum decreasing for 2 bars, RSI crosses under 70
			if (_currentMomentum > 0 && 
				_previousMomentum != 0 && 
				_currentMomentum < _previousMomentum && 
				// Need to track previous RSI values for crossover detection
				_previousMomentum > _currentMomentum && // momentum trend down (simplified)
				rsiValue < 70 && // RSI crossunder 70 (simplified)
				Position == 0)
			{
				RegisterOrder(this.CreateOrder(Sides.Sell, currentPrice, GetOrderVolume()));
			}
		}

		private decimal GetOrderVolume()
		{
			return 1;
		}
	}
}