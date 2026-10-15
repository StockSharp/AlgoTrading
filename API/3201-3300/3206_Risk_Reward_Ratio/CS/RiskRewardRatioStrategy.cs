using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-filter risk/reward strategy.
/// </summary>
public class RiskRewardRatioStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _rewardRatio;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<int> _breakEvenTriggerPips;
	private readonly StrategyParam<int> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _exitSwitch;

	private readonly List<Leg> _legs = [];
	private readonly List<decimal> _momentumDistances = [];

	private WeightedMovingAverage _fastLwma;
	private WeightedMovingAverage _slowLwma;
	private RelativeStrengthIndex _rsi;
	private MovingAverageConvergenceDivergenceSignal _macd;
	private RateOfChange _momentum;
	private StochasticK _fastStochasticK;
	private StochasticK _slowStochasticK;
	private SimpleMovingAverage _fastStochasticMain;
	private SimpleMovingAverage _slowStochasticMain;
	private SimpleMovingAverage _slowStochasticSignal;

	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public decimal MomentumThreshold { get => _momentumThreshold.Value; set => _momentumThreshold.Value = value; }
	public decimal RewardRatio { get => _rewardRatio.Value; set => _rewardRatio.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int MaxPositions { get => _maxPositions.Value; set => _maxPositions.Value = value; }
	public bool EnableTrailing { get => _enableTrailing.Value; set => _enableTrailing.Value = value; }
	public int TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }
	public bool EnableBreakEven { get => _enableBreakEven.Value; set => _enableBreakEven.Value = value; }
	public int BreakEvenTriggerPips { get => _breakEvenTriggerPips.Value; set => _breakEvenTriggerPips.Value = value; }
	public int BreakEvenOffsetPips { get => _breakEvenOffsetPips.Value; set => _breakEvenOffsetPips.Value = value; }
	public bool ExitSwitch { get => _exitSwitch.Value; set => _exitSwitch.Value = value; }

	public RiskRewardRatioStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
		_fastMaPeriod = Param(nameof(FastMaPeriod), 6).SetGreaterThanZero();
		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85).SetGreaterThanZero();
		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m).SetNotNegative();
		_rewardRatio = Param(nameof(RewardRatio), 2m).SetGreaterThanZero();
		_stopLossPips = Param(nameof(StopLossPips), 20).SetGreaterThanZero();
		_maxPositions = Param(nameof(MaxPositions), 10).SetGreaterThanZero();
		_enableTrailing = Param(nameof(EnableTrailing), true);
		_trailingStopPips = Param(nameof(TrailingStopPips), 40).SetNotNegative();
		_enableBreakEven = Param(nameof(EnableBreakEven), true);
		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30).SetNotNegative();
		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30).SetNotNegative();
		_exitSwitch = Param(nameof(ExitSwitch), false);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_legs.Clear();
		_momentumDistances.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastLwma = new() { Length = FastMaPeriod };
		_slowLwma = new() { Length = SlowMaPeriod };
		_rsi = new() { Length = 14 };
		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 },
			},
			SignalMa = { Length = 9 },
		};
		_momentum = new() { Length = 14 };
		_fastStochasticK = new() { Length = 5 };
		_slowStochasticK = new() { Length = 21 };
		_fastStochasticMain = new() { Length = 2 };
		_slowStochasticMain = new() { Length = 4 };
		_slowStochasticSignal = new() { Length = 10 };

		SubscribeCandles(CandleType)
			.BindEx(_fastLwma, _slowLwma, _rsi, _macd, _momentum, _fastStochasticK, _slowStochasticK, ProcessCandle, true)
			.Start();
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order is null || trade.Trade is null)
			return;

		var leg = _legs.FirstOrDefault(l => l.Order == trade.Order);
		if (leg is not null)
			AddFill(leg, trade);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastLwmaValue, IIndicatorValue slowLwmaValue, IIndicatorValue rsiValue,
		IIndicatorValue macdValue, IIndicatorValue momentumValue, IIndicatorValue fastKValue, IIndicatorValue slowKValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastMain = UpdateFastStochastic(fastKValue);
		var slowSignal = UpdateSlowStochastic(slowKValue);
		UpdateMomentum(momentumValue);

		if (ExitSwitch)
		{
			Flatten();
			return;
		}

		if (ApplyRisk(candle))
			return;

		if (fastMain is not decimal fastK || slowSignal is not decimal slowD)
			return;

		if (!_fastLwma.IsFormed || !_slowLwma.IsFormed || !_rsi.IsFormed || !_macd.IsFormed || !_momentum.IsFormed)
			return;

		var macd = (IMovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdMain || macd.Signal is not decimal macdSignal)
			return;

		var rsi = rsiValue.ToDecimal();
		var fastLwma = fastLwmaValue.ToDecimal();
		var slowLwma = slowLwmaValue.ToDecimal();
		var momentumBurst = _momentumDistances.Count == 0 ? 0m : _momentumDistances.Max();

		var longSignal =
			fastK > slowD &&
			rsi > 50m &&
			fastLwma > slowLwma &&
			macdMain > macdSignal && macdMain > 0m &&
			momentumBurst >= MomentumThreshold;

		var shortSignal =
			fastK < slowD &&
			rsi < 50m &&
			fastLwma < slowLwma &&
			macdMain < macdSignal && macdMain < 0m &&
			momentumBurst >= MomentumThreshold;

		var maxExposure = MaxPositions * TradeVolume;

		if (longSignal && Position >= 0m && Position + TradeVolume <= maxExposure)
			Enter(Sides.Buy, candle.OpenTime);
		else if (shortSignal && Position <= 0m && Math.Abs(Position - TradeVolume) <= maxExposure)
			Enter(Sides.Sell, candle.OpenTime);
	}

	private decimal? UpdateFastStochastic(IIndicatorValue fastKValue)
	{
		if (!_fastStochasticK.IsFormed || fastKValue.IsEmpty)
			return null;

		var main = _fastStochasticMain.Process(fastKValue);
		return _fastStochasticMain.IsFormed && !main.IsEmpty ? main.ToDecimal() : null;
	}

	private decimal? UpdateSlowStochastic(IIndicatorValue slowKValue)
	{
		if (!_slowStochasticK.IsFormed || slowKValue.IsEmpty)
			return null;

		var main = _slowStochasticMain.Process(slowKValue);
		if (!_slowStochasticMain.IsFormed || main.IsEmpty)
			return null;

		var signal = _slowStochasticSignal.Process(main);
		return _slowStochasticSignal.IsFormed && !signal.IsEmpty ? signal.ToDecimal() : null;
	}

	private void UpdateMomentum(IIndicatorValue momentumValue)
	{
		if (!_momentum.IsFormed || momentumValue.IsEmpty)
			return;

		// |ROC| equals the momentum's distance from 100.
		_momentumDistances.Add(Math.Abs(momentumValue.ToDecimal()));

		if (_momentumDistances.Count > 3)
			_momentumDistances.RemoveAt(0);
	}

	private void Enter(Sides side, DateTime candleTime)
	{
		var order = side == Sides.Buy ? BuyMarket(TradeVolume) : SellMarket(TradeVolume);

		var leg = new Leg
		{
			Order = order,
			Side = side,
			EntryTime = candleTime,
		};

		_legs.Add(leg);

		// The emulator can fill a market order before BuyMarket or SellMarket returns it.
		foreach (var trade in MyTrades.Where(t => t.Order == order && t.Trade is not null).ToArray())
			AddFill(leg, trade);
	}

	private void AddFill(Leg leg, MyTrade trade)
	{
		leg.FilledVolume += trade.Trade.Volume;
		leg.FilledValue += trade.Trade.Price * trade.Trade.Volume;
		leg.FillPrice = leg.FilledValue / leg.FilledVolume;
		SetInitialLevels(leg, leg.FillPrice.Value);
	}

	private void SetInitialLevels(Leg leg, decimal fill)
	{
		leg.BestPrice = fill;

		if (GetPip() is not decimal pip)
		{
			leg.StopPrice = null;
			leg.TakePrice = null;
			return;
		}

		var stopDistance = StopLossPips * pip;
		var takeDistance = stopDistance * RewardRatio;

		if (leg.Side == Sides.Buy)
		{
			leg.StopPrice = fill - stopDistance;
			leg.TakePrice = fill + takeDistance;
		}
		else
		{
			leg.StopPrice = fill + stopDistance;
			leg.TakePrice = fill - takeDistance;
		}
	}

	private bool ApplyRisk(ICandleMessage candle)
	{
		var pip = GetPip();
		var exitSell = 0m;
		var exitBuy = 0m;

		for (var i = _legs.Count - 1; i >= 0; i--)
		{
			var leg = _legs[i];
			if (leg.FillPrice is not decimal fill || candle.OpenTime <= leg.EntryTime)
				continue;

			var isLong = leg.Side == Sides.Buy;
			leg.BestPrice = isLong ? Math.Max(leg.BestPrice, candle.HighPrice) : Math.Min(leg.BestPrice, candle.LowPrice);

			if (pip is decimal step)
				MoveStop(leg, fill, step);

			var hit = isLong
				? (leg.StopPrice is decimal stop && candle.LowPrice <= stop) || (leg.TakePrice is decimal take && candle.HighPrice >= take)
				: (leg.StopPrice is decimal shortStop && candle.HighPrice >= shortStop) || (leg.TakePrice is decimal shortTake && candle.LowPrice <= shortTake);

			if (!hit)
				continue;

			if (isLong)
				exitSell += leg.FilledVolume;
			else
				exitBuy += leg.FilledVolume;

			_legs.RemoveAt(i);
		}

		if (exitSell > 0m)
			SellMarket(exitSell);

		if (exitBuy > 0m)
			BuyMarket(exitBuy);

		return exitSell > 0m || exitBuy > 0m;
	}

	private void MoveStop(Leg leg, decimal fill, decimal pip)
	{
		var isLong = leg.Side == Sides.Buy;
		var advance = isLong ? leg.BestPrice - fill : fill - leg.BestPrice;

		if (EnableBreakEven && advance >= BreakEvenTriggerPips * pip)
		{
			var offset = BreakEvenOffsetPips * pip;
			TightenStop(leg, isLong ? fill + offset : fill - offset);
		}

		if (EnableTrailing && TrailingStopPips > 0)
		{
			var trail = TrailingStopPips * pip;

			if (advance >= trail)
				TightenStop(leg, isLong ? leg.BestPrice - trail : leg.BestPrice + trail);
		}
	}

	private static void TightenStop(Leg leg, decimal candidate)
	{
		if (leg.StopPrice is not decimal stop || (leg.Side == Sides.Buy ? candidate > stop : candidate < stop))
			leg.StopPrice = candidate;
	}

	private void Flatten()
	{
		if (Position > 0m)
			SellMarket(Math.Abs(Position));
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));

		_legs.Clear();
	}

	private decimal? GetPip()
	{
		var step = Security?.PriceStep;
		return step > 0m ? step : null;
	}

	private sealed class Leg
	{
		public Order Order { get; init; }
		public Sides Side { get; init; }
		public DateTime EntryTime { get; init; }
		public decimal FilledVolume { get; set; }
		public decimal FilledValue { get; set; }
		public decimal? FillPrice { get; set; }
		public decimal BestPrice { get; set; }
		public decimal? StopPrice { get; set; }
		public decimal? TakePrice { get; set; }
	}
}
