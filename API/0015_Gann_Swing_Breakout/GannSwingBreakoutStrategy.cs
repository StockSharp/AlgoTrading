using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Gann Swing Breakout technique.
	/// It detects swing highs and lows, then enters positions when price breaks out
	/// after a pullback to a moving average.
	/// </summary>
	public class GannSwingBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _swingLookback;
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<DataType> _candleType;

		// State tracking
		private decimal? _lastSwingHigh;
		private decimal? _lastSwingLow;
		private int _highBarIndex;
		private int _lowBarIndex;
		private int _currentBarIndex;
		private readonly List<decimal> _recentHighs = [];
		private readonly List<decimal> _recentLows = [];
		private readonly List<ICandleMessage> _recentCandles = [];
		private decimal _prevMaValue;

		/// <summary>
		/// Number of bars to identify swing points.
		/// </summary>
		public int SwingLookback
		{
			get => _swingLookback.Value;
			set => _swingLookback.Value = value;
		}

		/// <summary>
		/// Period for moving average calculation.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize the Gann Swing Breakout strategy.
		/// </summary>
		public GannSwingBreakoutStrategy()
		{
			_swingLookback = Param(nameof(SwingLookback), 5)
				.SetDisplay("Swing Lookback", "Number of bars to identify swing points", "Trading parameters")
				.SetCanOptimize(true)
				.SetOptimize(3, 10, 1);

			_maPeriod = Param(nameof(MaPeriod), 20)
				.SetDisplay("MA Period", "Period for moving average calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
				
			_lastSwingHigh = null;
			_lastSwingLow = null;
			_highBarIndex = 0;
			_lowBarIndex = 0;
			_currentBarIndex = 0;
			_prevMaValue = 0;
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

			// Create indicators
			var ma = new SimpleMovingAverage { Length = MaPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(ma, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ma);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal? maValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Update bar index
			_currentBarIndex++;
			
			// Store recent candles and prices for swing detection
			_recentCandles.Add(candle);
			_recentHighs.Add(candle.HighPrice);
			_recentLows.Add(candle.LowPrice);
			
			// Keep only necessary history for swing detection
			int maxHistory = Math.Max(SwingLookback * 2 + 1, MaPeriod);
			if (_recentCandles.Count > maxHistory)
			{
				_recentCandles.RemoveAt(0);
				_recentHighs.RemoveAt(0);
				_recentLows.RemoveAt(0);
			}
			
			// Skip processing until we have enough data
			if (_recentCandles.Count < SwingLookback * 2 + 1)
			{
				_prevMaValue = maValue;
				return;
			}

			// Detect swing high and low points
			DetectSwingPoints();
			
			// Check for price crossing MA
			var isPriceAboveMa = candle.ClosePrice > maValue;
			var isPriceBelowMa = candle.ClosePrice < maValue;
			var wasPriceAboveMa = _recentCandles[_recentCandles.Count - 2].ClosePrice > _prevMaValue;
			
			// Detect MA pullback and breakout conditions
			var isPullbackFromLow = !isPriceBelowMa && wasPriceAboveMa;
			var isPullbackFromHigh = !isPriceAboveMa && !wasPriceAboveMa;
			
			// Trading logic
			if (_lastSwingHigh.HasValue && _lastSwingLow.HasValue)
			{
				// Long setup: Price breaks above last swing high after pullback to MA
				if (candle.ClosePrice > _lastSwingHigh.Value && isPullbackFromLow && Position <= 0)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
					LogInfo($"Buy signal: Breakout above swing high {_lastSwingHigh} after MA pullback");
				}
				// Short setup: Price breaks below last swing low after pullback to MA
				else if (candle.ClosePrice < _lastSwingLow.Value && isPullbackFromHigh && Position >= 0)
				{
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
					LogInfo($"Sell signal: Breakout below swing low {_lastSwingLow} after MA pullback");
				}
				// Exit logic for long positions
				else if (Position > 0 && candle.ClosePrice < maValue)
				{
					SellMarket(Position);
					LogInfo($"Exit long: Price {candle.ClosePrice} dropped below MA {maValue}");
				}
				// Exit logic for short positions
				else if (Position < 0 && candle.ClosePrice > maValue)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit short: Price {candle.ClosePrice} rose above MA {maValue}");
				}
			}
			
			// Update previous MA value
			_prevMaValue = maValue;
		}

		private void DetectSwingPoints()
		{
			// Check for swing high
			int midPoint = _recentHighs.Count - SwingLookback - 1;
			bool isSwingHigh = true;
			decimal centerHigh = _recentHighs[midPoint];
			
			for (int i = midPoint - SwingLookback; i < midPoint; i++)
			{
				if (i < 0 || _recentHighs[i] > centerHigh)
				{
					isSwingHigh = false;
					break;
				}
			}
			
			for (int i = midPoint + 1; i <= midPoint + SwingLookback; i++)
			{
				if (i >= _recentHighs.Count || _recentHighs[i] > centerHigh)
				{
					isSwingHigh = false;
					break;
				}
			}
			
			// Check for swing low
			bool isSwingLow = true;
			decimal centerLow = _recentLows[midPoint];
			
			for (int i = midPoint - SwingLookback; i < midPoint; i++)
			{
				if (i < 0 || _recentLows[i] < centerLow)
				{
					isSwingLow = false;
					break;
				}
			}
			
			for (int i = midPoint + 1; i <= midPoint + SwingLookback; i++)
			{
				if (i >= _recentLows.Count || _recentLows[i] < centerLow)
				{
					isSwingLow = false;
					break;
				}
			}
			
			// Update swing points if detected
			if (isSwingHigh)
			{
				_lastSwingHigh = centerHigh;
				_highBarIndex = _currentBarIndex - SwingLookback - 1;
				LogInfo($"New swing high detected: {_lastSwingHigh}");
			}
			
			if (isSwingLow)
			{
				_lastSwingLow = centerLow;
				_lowBarIndex = _currentBarIndex - SwingLookback - 1;
				LogInfo($"New swing low detected: {_lastSwingLow}");
			}
		}
	}
}