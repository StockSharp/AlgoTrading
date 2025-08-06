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
		private StochasticOscillator _stochRsi;
		private SimpleMovingAverage _smoothKSma;
		private SimpleMovingAverage _smoothDSma;
		private IIndicator _trendMa;
		private Supertrend _supertrend;

		private decimal _previousK;
		private decimal _previousD;
		private decimal _previousSupertrendDirection;
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
				.SetValidator(new DecimalRangeAttribute(0.5m, 10.0m))
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
			_stochRsi = new StochasticOscillator { Length = StochLength };
			_smoothKSma = new SimpleMovingAverage { Length = SmoothK };
			_smoothDSma = new SimpleMovingAverage { Length = SmoothD };
			_supertrend = new Supertrend { Length = AtrPeriod, Multiplier = AtrFactor };

			_trendMa = MaType == "SMA" 
				? (IIndicator)new SimpleMovingAverage { Length = MaLength }
				: new ExponentialMovingAverage { Length = MaLength };

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_rsi, _trendMa, _supertrend, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _trendMa);
				DrawIndicator(area, _supertrend);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal maValue, decimal supertrendValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Process Stochastic RSI manually
			var stochRsiValue = _stochRsi.Process(rsiValue);
			if (!stochRsiValue.IsFormed)
				return;

			var kValue = _smoothKSma.Process(stochRsiValue.GetValue<decimal>());
			var dValue = _smoothDSma.Process(kValue.GetValue<decimal>());

			if (!kValue.IsFormed || !dValue.IsFormed || !_supertrend.IsFormed)
				return;

			var k = kValue.GetValue<decimal>();
			var d = dValue.GetValue<decimal>();
			var currentPrice = candle.ClosePrice;

			// Get Supertrend direction (< 0 means uptrend, > 0 means downtrend)
			var supertrendDirection = currentPrice > supertrendValue ? -1 : 1;

			// Detect crossovers
			if (_previousK != 0 && _previousD != 0)
			{
				_kCrossedOverD = _previousK <= _previousD && k > d;
				_kCrossedUnderD = _previousK >= _previousD && k < d;
			}

			CheckEntryConditions(candle, k, d, maValue, supertrendDirection);
			CheckExitConditions(k, d, supertrendDirection);

			// Store previous values
			_previousK = k;
			_previousD = d;
			_previousSupertrendDirection = supertrendDirection;
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

		private void CheckExitConditions(decimal k, decimal d, decimal supertrendDirection)
		{
			// Exit long: K > 80 and K crosses under D
			if (Position > 0 && k > 80 && _kCrossedUnderD)
			{
				RegisterOrder(this.CreateOrder(Sides.Sell, _previousClose, Math.Abs(Position)));
			}

			// Exit short: K < 20 and K crosses over D
			if (Position < 0 && k < 20 && _kCrossedOverD)
			{
				RegisterOrder(this.CreateOrder(Sides.Buy, _previousClose, Math.Abs(Position)));
			}
		}

		private decimal GetOrderVolume()
		{
			return 1;
		}
	}
}