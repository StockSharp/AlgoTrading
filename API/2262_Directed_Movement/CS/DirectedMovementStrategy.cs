using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Directed Movement Strategy - contrarian RSI cross system.
/// </summary>
public class DirectedMovementStrategy : Strategy
{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<MaType> _firstMaType;
		private readonly StrategyParam<int> _firstMaLength;
		private readonly StrategyParam<MaType> _secondMaType;
		private readonly StrategyParam<int> _secondMaLength;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<decimal> _takeProfitPercent;

		private RelativeStrengthIndex _rsi = null!;
		private MovingAverage _fastMa = null!;
		private MovingAverage _slowMa = null!;
		private decimal _prevFast;
		private decimal _prevSlow;

		public DirectedMovementStrategy()
		{
				_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
						.SetDisplay("Candle Type", "Type of candles", "General");

				_rsiPeriod = Param(nameof(RsiPeriod), 14)
						.SetGreaterThanZero()
						.SetDisplay("RSI Period", "RSI calculation period", "General")
						.SetCanOptimize(true);

				_firstMaType = Param(nameof(FirstMaType), MaType.SMA)
						.SetDisplay("Fast MA Type", "Moving average for fast line", "General");

				_firstMaLength = Param(nameof(FirstMaLength), 12)
						.SetGreaterThanZero()
						.SetDisplay("Fast MA Length", "Period of fast moving average", "General")
						.SetCanOptimize(true);

				_secondMaType = Param(nameof(SecondMaType), MaType.EMA)
						.SetDisplay("Slow MA Type", "Moving average for slow line", "General");

				_secondMaLength = Param(nameof(SecondMaLength), 5)
						.SetGreaterThanZero()
						.SetDisplay("Slow MA Length", "Period of slow moving average", "General")
						.SetCanOptimize(true);

				_stopLossPercent = Param(nameof(StopLossPercent), 0m)
						.SetDisplay("Stop Loss %", "Stop loss in percent", "Risk");

				_takeProfitPercent = Param(nameof(TakeProfitPercent), 0m)
						.SetDisplay("Take Profit %", "Take profit in percent", "Risk");
		}

		public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
		public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
		public MaType FirstMaType { get => _firstMaType.Value; set => _firstMaType.Value = value; }
		public int FirstMaLength { get => _firstMaLength.Value; set => _firstMaLength.Value = value; }
		public MaType SecondMaType { get => _secondMaType.Value; set => _secondMaType.Value = value; }
		public int SecondMaLength { get => _secondMaLength.Value; set => _secondMaLength.Value = value; }
		public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
		public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
				base.OnStarted(time);

				_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
				_fastMa = CreateMa(FirstMaType, FirstMaLength);
				_slowMa = CreateMa(SecondMaType, SecondMaLength);
				_prevFast = 0m;
				_prevSlow = 0m;

				var subscription = SubscribeCandles(CandleType);
				subscription.Bind(_rsi, ProcessCandle).Start();

				var area = CreateChartArea();
				if (area != null)
				{
						DrawCandles(area, subscription);
						DrawIndicator(area, _fastMa);
						DrawIndicator(area, _slowMa);
						DrawOwnTrades(area);
				}

				if (TakeProfitPercent > 0m || StopLossPercent > 0m)
						StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));
				else
						StartProtection();
		}

		private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
		{
				if (candle.State != CandleStates.Finished)
						return;

				var fast = _fastMa.Process(rsiValue, candle.OpenTime, true).ToDecimal();
				var slow = _slowMa.Process(fast, candle.OpenTime, true).ToDecimal();

				if (!_slowMa.IsFormed)
				{
						_prevFast = fast;
						_prevSlow = slow;
						return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
						_prevFast = fast;
						_prevSlow = slow;
						return;
				}

				if (_prevFast > _prevSlow && fast <= slow)
				{
						if (Position < 0)
								BuyMarket(-Position); // close short
						BuyMarket(); // open long
				}
				else if (_prevFast < _prevSlow && fast >= slow)
				{
						if (Position > 0)
								SellMarket(Position); // close long
						SellMarket(); // open short
				}

				_prevFast = fast;
				_prevSlow = slow;
		}

		private static MovingAverage CreateMa(MaType type, int length)
				=> type switch
				{
						MaType.EMA => new ExponentialMovingAverage { Length = length },
						MaType.SMMA => new SmoothedMovingAverage { Length = length },
						MaType.WMA => new WeightedMovingAverage { Length = length },
						MaType.HMA => new HullMovingAverage { Length = length },
						_ => new SimpleMovingAverage { Length = length },
				};
}

/// <summary>
/// Types of moving averages available for smoothing.
/// </summary>
public enum MaType
{
		SMA,
		EMA,
		SMMA,
		WMA,
		HMA,
}

