using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// S7 Up Bot breakout strategy.
/// Opens long after double bottom and short after double top.
/// Includes optional trailing and early exit logic.
/// </summary>
public class S7UpBotStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _takeProfit;
		private readonly StrategyParam<decimal> _stopLoss;
		private readonly StrategyParam<decimal> _hlDivergence;
		private readonly StrategyParam<decimal> _spanPrice;
		private readonly StrategyParam<int> _maxTrades;
		private readonly StrategyParam<bool> _trailingStop;
		private readonly StrategyParam<decimal> _trailStopLoss;
		private readonly StrategyParam<bool> _zeroTrailingStop;
		private readonly StrategyParam<decimal> _stepTrailing;
		private readonly StrategyParam<bool> _outputAtLower;
		private readonly StrategyParam<bool> _outputAtRevers;
		private readonly StrategyParam<decimal> _spanToRevers;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _orderVolume;
		
		private decimal _prevLow;
		private decimal _prevHigh;
		private decimal _entryPrice;
		private decimal _stopPrice;
		private decimal _takeProfitPrice;
		private bool _isLong;
		private int _openTrades;
		/// <summary>Take profit in absolute price.</summary>
		public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
		
		/// <summary>Stop loss in absolute price. Set 0 for auto.</summary>
		public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
		
		/// <summary>Allowed divergence between consecutive highs or lows.</summary>
		public decimal HlDivergence { get => _hlDivergence.Value; set => _hlDivergence.Value = value; }
		
		/// <summary>Distance from extreme to price required for entry.</summary>
		public decimal SpanPrice { get => _spanPrice.Value; set => _spanPrice.Value = value; }
		
		/// <summary>Maximum simultaneous trades.</summary>
		public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }
		
		/// <summary>Use trailing stop.</summary>
		public bool TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
		
		/// <summary>Trailing stop distance.</summary>
		public decimal TrailStopLoss { get => _trailStopLoss.Value; set => _trailStopLoss.Value = value; }
		
		/// <summary>Enable zero trailing logic.</summary>
		public bool ZeroTrailingStop { get => _zeroTrailingStop.Value; set => _zeroTrailingStop.Value = value; }
		
		/// <summary>Minimal step to move zero trailing.</summary>
		public decimal StepTrailing { get => _stepTrailing.Value; set => _stepTrailing.Value = value; }
		
		/// <summary>Exit when price crosses previous extremum.</summary>
		public bool OutputAtLower { get => _outputAtLower.Value; set => _outputAtLower.Value = value; }
		
		/// <summary>Exit when reverse pattern detected.</summary>
		public bool OutputAtRevers { get => _outputAtRevers.Value; set => _outputAtRevers.Value = value; }
		
		/// <summary>Distance to reversal exit trigger.</summary>
		public decimal SpanToRevers { get => _spanToRevers.Value; set => _spanToRevers.Value = value; }
		
		/// <summary>Candle type.</summary>
		public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
		
		/// <summary>Order volume.</summary>
		public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
		/// <summary>
		/// Initializes a new instance of the <see cref="S7UpBotStrategy"/> class.
		/// </summary>
		public S7UpBotStrategy()
		{
			_takeProfit = Param(nameof(TakeProfit), 30m)
			.SetDisplay("Take Profit", "Absolute take profit", "Risk");
			
			_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Absolute stop loss, 0 for auto", "Risk");
			
			_hlDivergence = Param(nameof(HlDivergence), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("HL Divergence", "Max difference between highs or lows", "General");
			
			_spanPrice = Param(nameof(SpanPrice), 6m)
			.SetGreaterThanZero()
			.SetDisplay("Span Price", "Distance from extreme to price", "General");
			
			_maxTrades = Param(nameof(MaxTrades), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum simultaneous trades", "General");
			
			_trailingStop = Param(nameof(TrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk");
			
			_trailStopLoss = Param(nameof(TrailStopLoss), 20m)
			.SetDisplay("Trail Stop", "Trailing stop distance", "Risk");
			
			_zeroTrailingStop = Param(nameof(ZeroTrailingStop), false)
			.SetDisplay("Zero Trailing", "Move stop to better price after profit", "Risk");
			
			_stepTrailing = Param(nameof(StepTrailing), 0.5m)
			.SetDisplay("Step Trailing", "Minimal step to move zero trailing", "Risk");
			
			_outputAtLower = Param(nameof(OutputAtLower), false)
			.SetDisplay("Exit At Extremum", "Close when price crosses previous high/low", "Exit");
			
			_outputAtRevers = Param(nameof(OutputAtRevers), false)
			.SetDisplay("Exit At Reversal", "Close on reversal pattern", "Exit");
			
			_spanToRevers = Param(nameof(SpanToRevers), 3m)
			.SetDisplay("Span To Revers", "Distance for reversal exit", "Exit");
			
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");
			
			_orderVolume = Param(nameof(OrderVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume per trade", "General");
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
			
			Volume = OrderVolume;
			StartProtection();
			
			var subscription = SubscribeCandles(CandleType);
			subscription
			.Bind(ProcessCandle)
			.Start();
		}
		
		private void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
			return;
			
			var price = candle.ClosePrice;
			
			if (_openTrades > 0)
			ManagePosition(candle, price);
			
			if (_openTrades < MaxTrades && _prevLow != 0m && _prevHigh != 0m)
			CheckEntry(candle, price);
			
			_prevLow = candle.LowPrice;
			_prevHigh = candle.HighPrice;
		}
		
		private void CheckEntry(ICandleMessage candle, decimal price)
		{
			if (Math.Abs(candle.LowPrice - _prevLow) < HlDivergence &&
			price - candle.LowPrice > SpanPrice &&
			price - candle.LowPrice < SpanPrice * 1.5m)
			{
				BuyMarket();
				_openTrades++;
				_isLong = true;
				_entryPrice = price;
				_stopPrice = StopLoss > 0 ? price - StopLoss : candle.LowPrice - HlDivergence;
				_takeProfitPrice = TakeProfit > 0 ? price + TakeProfit : 0m;
			}
			else if (Math.Abs(candle.HighPrice - _prevHigh) < HlDivergence &&
			candle.HighPrice - price > SpanPrice &&
			candle.HighPrice - price < SpanPrice * 1.5m)
			{
				SellMarket();
				_openTrades++;
				_isLong = false;
				_entryPrice = price;
				_stopPrice = StopLoss > 0 ? price + StopLoss : candle.HighPrice + HlDivergence;
				_takeProfitPrice = TakeProfit > 0 ? price - TakeProfit : 0m;
			}
		}
		
		private void ManagePosition(ICandleMessage candle, decimal price)
		{
			if (_isLong)
			{
				if (OutputAtLower && price < _prevLow)
				{
					SellMarket();
					_openTrades = 0;
					return;
				}
				
				if (OutputAtRevers &&
				Math.Abs(candle.HighPrice - _prevHigh) < HlDivergence &&
				candle.HighPrice - price > SpanToRevers &&
				candle.HighPrice - price < SpanToRevers * 1.5m)
				{
					SellMarket();
					_openTrades = 0;
					return;
				}
				
				if (_takeProfitPrice > 0 && candle.HighPrice >= _takeProfitPrice)
				{
					SellMarket();
					_openTrades = 0;
					return;
				}
				
				if (_stopPrice > 0 && candle.LowPrice <= _stopPrice)
				{
					SellMarket();
					_openTrades = 0;
					return;
				}
				
				if (TrailingStop)
				{
					var trail = price - TrailStopLoss;
					if (trail > _stopPrice)
					_stopPrice = trail;
				}
				
				if (ZeroTrailingStop && _stopPrice < _entryPrice)
				{
					var newStop = StopLoss > 0 ? price - StopLoss : price - (SpanPrice + HlDivergence);
					if (newStop - _stopPrice > StepTrailing)
					_stopPrice = newStop;
				}
			}
			else
			{
				if (OutputAtLower && price > _prevHigh)
				{
					BuyMarket();
					_openTrades = 0;
					return;
				}
				
				if (OutputAtRevers &&
				Math.Abs(candle.LowPrice - _prevLow) < HlDivergence &&
				price - candle.LowPrice > SpanToRevers &&
				price - candle.LowPrice < SpanToRevers * 1.5m)
				{
					BuyMarket();
					_openTrades = 0;
					return;
				}
				
				if (_takeProfitPrice > 0 && candle.LowPrice <= _takeProfitPrice)
				{
					BuyMarket();
					_openTrades = 0;
					return;
				}
				
				if (_stopPrice > 0 && candle.HighPrice >= _stopPrice)
				{
					BuyMarket();
					_openTrades = 0;
					return;
				}
				
				if (TrailingStop)
				{
					var trail = price + TrailStopLoss;
					if (trail < _stopPrice)
					_stopPrice = trail;
				}
				
				if (ZeroTrailingStop && _stopPrice > _entryPrice)
				{
					var newStop = StopLoss > 0 ? price + StopLoss : price + (SpanPrice + HlDivergence);
					if (_stopPrice - newStop > StepTrailing)
					_stopPrice = newStop;
				}
			}
		}
}
