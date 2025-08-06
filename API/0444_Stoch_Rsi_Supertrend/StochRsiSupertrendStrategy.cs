using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Stochastic RSI + Supertrend Strategy with trend moving average filter
	/// </summary>
	public class StochRsiSupertrendStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _smoothK;
		private readonly StrategyParam<int> _smoothD;
		private readonly StrategyParam<int> _rsiLength;
		private readonly StrategyParam<int> _stochLength;
		private readonly StrategyParam<string> _maType;
		private readonly StrategyParam<int> _maLength;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrFactor;
		private readonly StrategyParam<bool> _showShort;

		private RelativeStrengthIndex _rsi;
		private Highest _stochRsiHigh;
		private Lowest _stochRsiLow;
		private SimpleMovingAverage _smoothKSma;
		private SimpleMovingAverage _smoothDSma;
		private IIndicator _trendMa;
		private AverageTrueRange _atr;

		private decimal _previousK;
		private decimal _previousD;
		private decimal _previousSupertrendValue;
		private decimal _previousClose;
		private bool _kCrossedOverD;
		private bool _kCrossedUnderD;

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// %K smoothing periods.
		/// </summary>
		public int SmoothK
		{
			get => _smoothK.Value;
			set => _smoothK.Value = value;
		}

		/// <summary>
		/// %D smoothing periods.
		/// </summary>
		public int SmoothD
		{
			get => _smoothD.Value;
			set => _smoothD.Value = value;
		}

		/// <summary>
		/// RSI length for Stochastic RSI calculation.
		/// </summary>
		public int RsiLength
		{
			get => _rsiLength.Value;
			set => _rsiLength.Value = value;
		}

		/// <summary>
		/// Stochastic length for Stochastic RSI calculation.
		/// </summary>
		public int StochLength
		{
			get => _stochLength.Value;
			set => _stochLength.Value = value;
		}

		/// <summary>
		/// Trend moving average type.
		/// </summary>
		public string MaType
		{
			get => _maType.Value;
			set => _maType.Value = value;
		}

		/// <summary>
		/// Trend moving average length.
		/// </summary>
		public int MaLength
		{
			get => _maLength.Value;
			set => _maLength.Value = value;
		}

		/// <summary>
		/// ATR period for Supertrend calculation.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// ATR factor for Supertrend calculation.
		/// </summary>
		public decimal AtrFactor
		{
			get => _atrFactor.Value;
			set => _atrFactor.Value = value;
		}

		/// <summary>
		/// Enable short entries.
		/// </summary>
		public bool ShowShort
		{
			get => _showShort.Value;
			set => _showShort.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public StochRsiSupertrendStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_smoothK = Param(nameof(SmoothK), 3)
				.SetGreaterThanZero()
				.SetDisplay("Smooth K", "%K smoothing periods", "Stochastic RSI")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

			_smoothD = Param(nameof(SmoothD), 3)
				.SetGreaterThanZero()
				.SetDisplay("Smooth D", "%D smoothing periods", "Stochastic RSI")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

			_rsiLength = Param(nameof(RsiLength), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Length", "RSI length for Stochastic RSI", "Stochastic RSI")
				.SetCanOptimize(true)
				.SetOptimize(5, 25, 2);

			_stochLength = Param(nameof(StochLength), 14)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic Length", "Stochastic length for Stochastic RSI", "Stochastic RSI")
				.SetCanOptimize(true)
				.SetOptimize(5, 25, 2);

			_maType = Param(nameof(MaType), "EMA")
				.SetDisplay("MA Type", "Trend moving average type", "Moving Average");

			_maLength = Param(nameof(MaLength), 200)
				.SetGreaterThanZero()
				.SetDisplay("MA Length", "Trend moving average length", "Moving Average")
				.SetCanOptimize(true)
				.SetOptimize(150, 250, 25);

			_atrPeriod = Param(nameof(AtrPeriod), 11)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "ATR period for Supertrend", "Supertrend")
				.SetCanOptimize(true)
				.SetOptimize(7, 15, 2);

			_atrFactor = Param(nameof(AtrFactor), 2.0m)
				.SetRange(0.5m, 10.0m)
				.SetDisplay("ATR Factor", "ATR factor for Supertrend", "Supertrend")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);

			_showShort = Param(nameof(ShowShort), false)
				.SetDisplay("Show Short", "Enable short entries", "Strategy");
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
			_rsi = new RelativeStrengthIndex { Length = RsiLength };
			_stochRsiHigh = new Highest { Length = StochLength };
			_stochRsiLow = new Lowest { Length = StochLength };
			_smoothKSma = new SimpleMovingAverage { Length = SmoothK };
			_smoothDSma = new SimpleMovingAverage { Length = SmoothD };
			_atr = new AverageTrueRange { Length = AtrPeriod };

			_trendMa = MaType == "SMA" 
				? (IIndicator)new SimpleMovingAverage { Length = MaLength }
				: new ExponentialMovingAverage { Length = MaLength };

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_rsi, _trendMa, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _trendMa);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue maValue, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Wait for indicators to form
			if (!_rsi.IsFormed || !_trendMa.IsFormed || !_atr.IsFormed)
				return;

			// Calculate Stochastic RSI manually
			var rsiPrice = rsiValue.ToDecimal();
			var highestRsi = _stochRsiHigh.Process(new DecimalIndicatorValue(_stochRsiHigh, rsiPrice));
			var lowestRsi = _stochRsiLow.Process(new DecimalIndicatorValue(_stochRsiLow, rsiPrice));

			if (!highestRsi.IsFormed || !lowestRsi.IsFormed)
				return;

			// Calculate %K
			var highVal = highestRsi.ToDecimal();
			var lowVal = lowestRsi.ToDecimal();
			var stochRsi = highVal != lowVal ? (rsiPrice - lowVal) / (highVal - lowVal) * 100 : 50;

			var kValue = _smoothKSma.Process(new DecimalIndicatorValue(_smoothKSma, stochRsi));
			var dValue = _smoothDSma.Process(new DecimalIndicatorValue(_smoothDSma, kValue.ToDecimal()));

			if (!kValue.IsFormed || !dValue.IsFormed)
				return;

			var k = kValue.ToDecimal();
			var d = dValue.ToDecimal();
			var currentPrice = candle.ClosePrice;

			// Calculate simple SuperTrend-like signal using ATR
			var hl2 = (candle.HighPrice + candle.LowPrice) / 2;
			var atrVal = atrValue.ToDecimal();
			var upperBand = hl2 + (AtrFactor * atrVal);
			var lowerBand = hl2 - (AtrFactor * atrVal);

			// SuperTrend calculation - simplified version
			var supertrendValue = currentPrice > _previousSupertrendValue ? lowerBand : upperBand;
			var supertrendDirection = currentPrice > supertrendValue ? -1 : 1;

			// Detect crossovers
			if (_previousK != 0 && _previousD != 0)
			{
				_kCrossedOverD = _previousK <= _previousD && k > d;
				_kCrossedUnderD = _previousK >= _previousD && k < d;
			}

			CheckEntryConditions(candle, k, d, maValue.ToDecimal(), supertrendDirection);
			CheckExitConditions(candle, k, d, supertrendDirection);

			// Store previous values
			_previousK = k;
			_previousD = d;
			_previousSupertrendValue = supertrendValue;
			_previousClose = currentPrice;
		}

		private void CheckEntryConditions(ICandleMessage candle, decimal k, decimal d, decimal maValue, decimal supertrendDirection)
		{
			var currentPrice = candle.ClosePrice;

			// Long entry: close > trend MA, K < 20, K crosses over D, Supertrend is bullish
			if (currentPrice > maValue && 
				k < 20 && 
				_kCrossedOverD && 
				supertrendDirection < 0 && 
				Position == 0)
			{
				RegisterOrder(this.CreateOrder(Sides.Buy, currentPrice, GetOrderVolume()));
			}

			// Short entry: close < trend MA, K > 80, K crosses under D, Supertrend is bearish
			if (ShowShort && 
				currentPrice < maValue && 
				k > 80 && 
				_kCrossedUnderD && 
				supertrendDirection > 0 && 
				Position == 0)
			{
				RegisterOrder(this.CreateOrder(Sides.Sell, currentPrice, GetOrderVolume()));
			}
		}

		private void CheckExitConditions(ICandleMessage candle, decimal k, decimal d, decimal supertrendDirection)
		{
			var currentPrice = candle.ClosePrice;

			// Exit long: K > 80 and K crosses under D
			if (Position > 0 && k > 80 && _kCrossedUnderD)
			{
				RegisterOrder(this.CreateOrder(Sides.Sell, currentPrice, Math.Abs(Position)));
			}

			// Exit short: K < 20 and K crosses over D
			if (Position < 0 && k < 20 && _kCrossedOverD)
			{
				RegisterOrder(this.CreateOrder(Sides.Buy, currentPrice, Math.Abs(Position)));
			}
		}

		private decimal GetOrderVolume()
		{
			return 1;
		}
	}
}