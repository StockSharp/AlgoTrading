using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Cumulative Delta Breakout strategy
	/// Long entry: Cumulative Delta breaks above its N-period highest
	/// Short entry: Cumulative Delta breaks below its N-period lowest
	/// Exit: Cumulative Delta changes sign (crosses zero)
	/// </summary>
	public class CumulativeDeltaBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _cumulativeDelta;
		private decimal? _highestDelta;
		private decimal? _lowestDelta;
		private int _barCount;
		private readonly Queue<decimal> _deltaWindow = [];

		/// <summary>
		/// Lookback Period for highest/lowest delta
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}

		/// <summary>
		/// Candle type for strategy calculation
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize <see cref="CumulativeDeltaBreakoutStrategy"/>.
		/// </summary>
		public CumulativeDeltaBreakoutStrategy()
		{
			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for calculating highest/lowest delta", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
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

			_cumulativeDelta = 0;
			_highestDelta = null;
			_lowestDelta = null;
			_barCount = 0;
			_deltaWindow.Clear();

			// Create subscription for both candles and ticks
			var candleSubscription = SubscribeCandles(CandleType);
			
			// Bind candle processing
			candleSubscription
				.Bind(ProcessCandle)
				.Start();
				
			// Subscribe to ticks to compute delta
			SubscribeTicks()
				.Bind(trade =>
				{
					// Calculate delta: positive for buy trades, negative for sell trades
					var delta = trade.OriginSide == Sides.Buy ? trade.Volume : -trade.Volume;
					
					// Add to cumulative delta
					_cumulativeDelta += delta;
				})
				.Start();
				
			// Configure protection
			StartProtection(
				takeProfit: new Unit(3, UnitTypes.Percent),
				stopLoss: new Unit(2, UnitTypes.Percent)
			);

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, candleSubscription);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Increment bar counter
			_barCount++;

			// Update rolling window
			_deltaWindow.Enqueue(_cumulativeDelta);
			if (_deltaWindow.Count > LookbackPeriod)
				_deltaWindow.Dequeue();

			if (_deltaWindow.Count < LookbackPeriod)
			{
				_highestDelta = _deltaWindow.Max();
				_lowestDelta = _deltaWindow.Min();
				return;
			}

			_highestDelta = _deltaWindow.Max();
			_lowestDelta = _deltaWindow.Min();

			// Log current values
			LogInfo($"Candle Close: {candle.ClosePrice}, Cumulative Delta: {_cumulativeDelta}");
			LogInfo($"Highest Delta: {_highestDelta}, Lowest Delta: {_lowestDelta}");

			// Trading logic:
			// Long: Cumulative Delta breaks above highest
			if (_highestDelta.HasValue && _cumulativeDelta > _highestDelta && Position <= 0)
			{
				LogInfo($"Buy Signal: Cumulative Delta ({_cumulativeDelta}) breaking above highest ({_highestDelta})");
				BuyMarket(Volume + Math.Abs(Position));
			}
			// Short: Cumulative Delta breaks below lowest
			else if (_lowestDelta.HasValue && _cumulativeDelta < _lowestDelta && Position >= 0)
			{
				LogInfo($"Sell Signal: Cumulative Delta ({_cumulativeDelta}) breaking below lowest ({_lowestDelta})");
				SellMarket(Volume + Math.Abs(Position));
			}
			
			// Exit logic: Cumulative Delta crosses zero
			if (Position > 0 && _cumulativeDelta < 0)
			{
				LogInfo($"Exit Long: Cumulative Delta ({_cumulativeDelta}) < 0");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && _cumulativeDelta > 0)
			{
				LogInfo($"Exit Short: Cumulative Delta ({_cumulativeDelta}) > 0");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}