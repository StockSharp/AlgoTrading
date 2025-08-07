using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Stochastic RSI Crossover Strategy with EMA trend filter
	/// </summary>
	public class StochRsiCrossoverStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _smoothK;
		private readonly StrategyParam<int> _smoothD;
		private readonly StrategyParam<int> _rsiLength;
		private readonly StrategyParam<int> _stochLength;
		private readonly StrategyParam<int> _ema1Length;
		private readonly StrategyParam<int> _ema2Length;
		private readonly StrategyParam<int> _ema3Length;
		private readonly StrategyParam<int> _atrLength;
		private readonly StrategyParam<decimal> _atrLossMultiplier;
		private readonly StrategyParam<decimal> _atrProfitMultiplier;

		private RelativeStrengthIndex _rsi;
		private Highest _stochRsiHigh;
		private Lowest _stochRsiLow;
		private SimpleMovingAverage _smoothKSma;
		private SimpleMovingAverage _smoothDSma;
		private ExponentialMovingAverage _ema1;
		private ExponentialMovingAverage _ema2;
		private ExponentialMovingAverage _ema3;
		private AverageTrueRange _atr;

		private decimal _previousK;
		private decimal _previousD;
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
		/// First EMA length.
		/// </summary>
		public int Ema1Length
		{
			get => _ema1Length.Value;
			set => _ema1Length.Value = value;
		}

		/// <summary>
		/// Second EMA length.
		/// </summary>
		public int Ema2Length
		{
			get => _ema2Length.Value;
			set => _ema2Length.Value = value;
		}

		/// <summary>
		/// Third EMA length.
		/// </summary>
		public int Ema3Length
		{
			get => _ema3Length.Value;
			set => _ema3Length.Value = value;
		}

		/// <summary>
		/// ATR length for stop loss calculation.
		/// </summary>
		public int AtrLength
		{
			get => _atrLength.Value;
			set => _atrLength.Value = value;
		}

		/// <summary>
		/// ATR loss multiplier.
		/// </summary>
		public decimal AtrLossMultiplier
		{
			get => _atrLossMultiplier.Value;
			set => _atrLossMultiplier.Value = value;
		}

		/// <summary>
		/// ATR profit multiplier.
		/// </summary>
		public decimal AtrProfitMultiplier
		{
			get => _atrProfitMultiplier.Value;
			set => _atrProfitMultiplier.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public StochRsiCrossoverStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

			_ema1Length = Param(nameof(Ema1Length), 8)
				.SetGreaterThanZero()
				.SetDisplay("EMA 1 Length", "First EMA length", "Moving Averages")
				.SetCanOptimize(true)
				.SetOptimize(5, 15, 2);

			_ema2Length = Param(nameof(Ema2Length), 14)
				.SetGreaterThanZero()
				.SetDisplay("EMA 2 Length", "Second EMA length", "Moving Averages")
				.SetCanOptimize(true)
				.SetOptimize(10, 25, 3);

			_ema3Length = Param(nameof(Ema3Length), 50)
				.SetGreaterThanZero()
				.SetDisplay("EMA 3 Length", "Third EMA length", "Moving Averages")
				.SetCanOptimize(true)
				.SetOptimize(30, 70, 10);

			_atrLength = Param(nameof(AtrLength), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Length", "ATR calculation length", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);

			_atrLossMultiplier = Param(nameof(AtrLossMultiplier), 3.0m)
				.SetRange(0.5m, 10.0m)
				.SetDisplay("ATR Loss Multiplier", "ATR multiplier for stop loss", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);

			_atrProfitMultiplier = Param(nameof(AtrProfitMultiplier), 1.0m)
				.SetRange(0.5m, 10.0m)
				.SetDisplay("ATR Profit Multiplier", "ATR multiplier for take profit", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 3.0m, 0.5m);
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
			_ema1 = new ExponentialMovingAverage { Length = Ema1Length };
			_ema2 = new ExponentialMovingAverage { Length = Ema2Length };
			_ema3 = new ExponentialMovingAverage { Length = Ema3Length };
			_atr = new AverageTrueRange { Length = AtrLength };

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_rsi, _ema1, _ema2, _ema3, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ema1);
				DrawIndicator(area, _ema2);
				DrawIndicator(area, _ema3);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue ema1Value, IIndicatorValue ema2Value, IIndicatorValue ema3Value, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Wait for indicators to form
			if (!_rsi.IsFormed || !_ema1.IsFormed || !_ema2.IsFormed || !_ema3.IsFormed || !_atr.IsFormed)
				return;

			// Calculate Stochastic RSI manually
			var rsiPrice = rsiValue.ToDecimal();
			var highestRsi = _stochRsiHigh.Process(rsiPrice, candle.ServerTime, candle.State == CandleStates.Finished);
			var lowestRsi = _stochRsiLow.Process(rsiPrice, candle.ServerTime, candle.State == CandleStates.Finished);

			if (!highestRsi.IsFormed || !lowestRsi.IsFormed)
				return;

			// Calculate %K
			var highVal = highestRsi.ToDecimal();
			var lowVal = lowestRsi.ToDecimal();
			var stochRsi = highVal != lowVal ? (rsiPrice - lowVal) / (highVal - lowVal) * 100 : 50;

			// Calculate smoothed K and D values
			var kValue = _smoothKSma.Process(stochRsi, candle.ServerTime, candle.State == CandleStates.Finished);
			var dValue = _smoothDSma.Process(kValue.ToDecimal(), candle.ServerTime, candle.State == CandleStates.Finished);

			if (!kValue.IsFormed || !dValue.IsFormed)
				return;

			var k = kValue.ToDecimal();
			var d = dValue.ToDecimal();

			// Detect crossovers
			if (_previousK != 0 && _previousD != 0)
			{
				_kCrossedOverD = _previousK <= _previousD && k > d;
				_kCrossedUnderD = _previousK >= _previousD && k < d;
			}

			CheckEntryConditions(candle, k, d, ema1Value.ToDecimal(), ema2Value.ToDecimal(), ema3Value.ToDecimal(), atrValue.ToDecimal());

			// Store previous values
			_previousK = k;
			_previousD = d;
		}

		private void CheckEntryConditions(ICandleMessage candle, decimal k, decimal d, decimal ema1Value, decimal ema2Value, decimal ema3Value, decimal atrValue)
		{
			var currentPrice = candle.ClosePrice;

			// Long entry conditions: K crosses over D, K between 10-60, EMA trend is bullish, close > EMA1
			if (_kCrossedOverD && 
				k >= 10 && k <= 60 && 
				ema1Value > ema2Value && ema2Value > ema3Value && 
				currentPrice > ema1Value && 
				Position == 0)
			{
				var stopLoss = currentPrice - (atrValue * AtrLossMultiplier);
				var takeProfit = currentPrice + (atrValue * AtrProfitMultiplier);

				RegisterOrder(this.CreateOrder(Sides.Buy, currentPrice, GetOrderVolume()));
			}

			// Short entry conditions: K crosses under D, K between 40-95, EMA trend is bearish, close < EMA1
			if (_kCrossedUnderD && 
				k >= 40 && k <= 95 && 
				ema3Value > ema2Value && ema2Value > ema1Value && 
				currentPrice < ema1Value && 
				Position == 0)
			{
				var stopLoss = currentPrice + (atrValue * AtrLossMultiplier);
				var takeProfit = currentPrice - (atrValue * AtrProfitMultiplier);

				RegisterOrder(this.CreateOrder(Sides.Sell, currentPrice, GetOrderVolume()));
			}
		}

		private decimal GetOrderVolume()
		{
			return 1;
		}
	}
}