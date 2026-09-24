using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

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

	private readonly List<Bar> _bars = [];
	private decimal? _macdFast;
	private decimal? _macdSlow;
	private decimal? _macdSignal;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _bestPrice;

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
		_bars.Clear();
		_macdFast = null;
		_macdSlow = null;
		_macdSignal = null;
		ResetTradeState();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_bars.Add(new(candle.HighPrice, candle.LowPrice, candle.ClosePrice));
		var keep = Math.Max(SlowMaPeriod + 5, 100);
		if (_bars.Count > keep)
			_bars.RemoveRange(0, _bars.Count - keep);

		var close = candle.ClosePrice;
		_macdFast = Ema(_macdFast, close, 12);
		_macdSlow = Ema(_macdSlow, close, 26);
		var macdMain = _macdFast.Value - _macdSlow.Value;
		_macdSignal = Ema(_macdSignal, macdMain, 9);

		if (ExitSwitch)
		{
			if (Position != 0)
				Flatten();
			return;
		}

		if (Position != 0 && ApplyRisk(candle))
			return;

		if (_bars.Count < Math.Max(SlowMaPeriod, 25))
			return;

		var fastK = StochasticK(5);
		var slowD = StochasticD(21, 10);
		var rsi = Rsi(14);
		var fastLwma = Lwma(FastMaPeriod);
		var slowLwma = Lwma(SlowMaPeriod);
		var momentumBurst = MomentumBurst(14, 3);
		var macdSignal = _macdSignal.Value;

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
			Enter(Sides.Buy, close);
		else if (shortSignal && Position <= 0m && Math.Abs(Position - TradeVolume) <= maxExposure)
			Enter(Sides.Sell, close);
	}

	private void Enter(Sides side, decimal close)
	{
		var previousAbs = Math.Abs(Position);
		var newAbs = previousAbs + TradeVolume;

		if (side == Sides.Buy)
			BuyMarket(TradeVolume);
		else
			SellMarket(TradeVolume);

		_entryPrice = previousAbs > 0m
			? (_entryPrice * previousAbs + close * TradeVolume) / newAbs
			: close;

		var pip = GetPipSize();
		var stopDistance = StopLossPips * pip;
		var targetDistance = stopDistance * RewardRatio;
		_stopPrice = side == Sides.Buy ? _entryPrice - stopDistance : _entryPrice + stopDistance;
		_takePrice = side == Sides.Buy ? _entryPrice + targetDistance : _entryPrice - targetDistance;
		_bestPrice = close;
	}

	private bool ApplyRisk(ICandleMessage candle)
	{
		var pip = GetPipSize();

		if (Position > 0m)
		{
			_bestPrice = _bestPrice is decimal best ? Math.Max(best, candle.HighPrice) : candle.HighPrice;

			if (EnableBreakEven && candle.HighPrice - _entryPrice >= BreakEvenTriggerPips * pip)
			{
				var candidate = _entryPrice + BreakEvenOffsetPips * pip;
				if (_stopPrice is null || candidate > _stopPrice)
					_stopPrice = candidate;
			}

			if (EnableTrailing && TrailingStopPips > 0)
			{
				var candidate = _bestPrice.Value - TrailingStopPips * pip;
				if (_stopPrice is null || candidate > _stopPrice)
					_stopPrice = candidate;
			}

			if ((_stopPrice is decimal stop && candle.LowPrice <= stop) ||
				(_takePrice is decimal take && candle.HighPrice >= take))
			{
				Flatten();
				return true;
			}
		}
		else
		{
			_bestPrice = _bestPrice is decimal best ? Math.Min(best, candle.LowPrice) : candle.LowPrice;

			if (EnableBreakEven && _entryPrice - candle.LowPrice >= BreakEvenTriggerPips * pip)
			{
				var candidate = _entryPrice - BreakEvenOffsetPips * pip;
				if (_stopPrice is null || candidate < _stopPrice)
					_stopPrice = candidate;
			}

			if (EnableTrailing && TrailingStopPips > 0)
			{
				var candidate = _bestPrice.Value + TrailingStopPips * pip;
				if (_stopPrice is null || candidate < _stopPrice)
					_stopPrice = candidate;
			}

			if ((_stopPrice is decimal stop && candle.HighPrice >= stop) ||
				(_takePrice is decimal take && candle.LowPrice <= take))
			{
				Flatten();
				return true;
			}
		}

		return false;
	}

	private void Flatten()
	{
		if (Position > 0m)
			SellMarket(Math.Abs(Position));
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));

		ResetTradeState();
	}

	private decimal StochasticK(int period)
	{
		var window = _bars.Skip(_bars.Count - period);
		var high = window.Max(b => b.High);
		var low = window.Min(b => b.Low);
		return high == low ? 50m : (_bars[^1].Close - low) / (high - low) * 100m;
	}

	private decimal StochasticD(int period, int smoothing)
	{
		var values = new List<decimal>();
		for (var shift = smoothing - 1; shift >= 0; shift--)
		{
			var end = _bars.Count - shift;
			if (end < period)
				continue;

			var window = _bars.Skip(end - period).Take(period);
			var high = window.Max(b => b.High);
			var low = window.Min(b => b.Low);
			var close = _bars[end - 1].Close;
			values.Add(high == low ? 50m : (close - low) / (high - low) * 100m);
		}

		return values.Count == 0 ? 50m : values.Average();
	}

	private decimal Rsi(int period)
	{
		var gains = 0m;
		var losses = 0m;
		for (var i = _bars.Count - period; i < _bars.Count; i++)
		{
			var diff = _bars[i].Close - _bars[i - 1].Close;
			if (diff > 0m)
				gains += diff;
			else
				losses -= diff;
		}

		if (losses == 0m)
			return 100m;
		var rs = gains / losses;
		return 100m - 100m / (1m + rs);
	}

	private decimal Lwma(int period)
	{
		var start = _bars.Count - period;
		var total = 0m;
		var weights = 0m;
		for (var i = 0; i < period; i++)
		{
			var weight = i + 1m;
			total += _bars[start + i].Close * weight;
			weights += weight;
		}
		return total / weights;
	}

	private decimal MomentumBurst(int period, int samples)
	{
		var max = 0m;
		for (var shift = 0; shift < samples; shift++)
		{
			var index = _bars.Count - 1 - shift;
			var baseIndex = index - period;
			if (baseIndex < 0 || _bars[baseIndex].Close == 0m)
				continue;

			var momentum = _bars[index].Close / _bars[baseIndex].Close * 100m;
			max = Math.Max(max, Math.Abs(momentum - 100m));
		}
		return max;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;
		return step is 0.00001m or 0.001m ? step * 10m : step;
	}

	private static decimal Ema(decimal? previous, decimal value, int period)
	{
		if (previous is null)
			return value;
		var alpha = 2m / (period + 1m);
		return previous.Value + alpha * (value - previous.Value);
	}

	private void ResetTradeState()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_bestPrice = null;
	}

	private readonly record struct Bar(decimal High, decimal Low, decimal Close);
}
