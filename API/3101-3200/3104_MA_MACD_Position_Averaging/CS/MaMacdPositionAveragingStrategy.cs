using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum MaMacdMovingAverageMethod
{
	Simple,
	Exponential,
	Smoothed,
	Weighted,
}

public enum MaMacdAppliedPrice
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted,
}

/// <summary>
/// MA + MACD position averaging strategy.
/// </summary>
public class MaMacdPositionAveragingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _stepLossingPips;
	private readonly StrategyParam<decimal> _lotCoefficient;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MaMacdMovingAverageMethod> _maMethod;
	private readonly StrategyParam<MaMacdAppliedPrice> _maAppliedPrice;
	private readonly StrategyParam<int> _indentPips;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<MaMacdAppliedPrice> _macdAppliedPrice;
	private readonly StrategyParam<decimal> _macdRatio;

	private readonly List<decimal> _maInputs = [];
	private readonly List<decimal> _maValues = [];
	private readonly List<(decimal Main, decimal Signal)> _macdValues = [];
	private readonly List<Leg> _legs = [];

	private decimal? _macdFast;
	private decimal? _macdSlow;
	private decimal? _macdSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public int TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }
	public int TrailingStepPips { get => _trailingStepPips.Value; set => _trailingStepPips.Value = value; }
	public int StepLossingPips { get => _stepLossingPips.Value; set => _stepLossingPips.Value = value; }
	public decimal LotCoefficient { get => _lotCoefficient.Value; set => _lotCoefficient.Value = value; }
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int MaShift { get => _maShift.Value; set => _maShift.Value = value; }
	public MaMacdMovingAverageMethod MaMethod { get => _maMethod.Value; set => _maMethod.Value = value; }
	public MaMacdAppliedPrice MaAppliedPrice { get => _maAppliedPrice.Value; set => _maAppliedPrice.Value = value; }
	public int IndentPips { get => _indentPips.Value; set => _indentPips.Value = value; }
	public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }
	public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }
	public int MacdSignalPeriod { get => _macdSignalPeriod.Value; set => _macdSignalPeriod.Value = value; }
	public MaMacdAppliedPrice MacdAppliedPrice { get => _macdAppliedPrice.Value; set => _macdAppliedPrice.Value = value; }
	public decimal MacdRatio { get => _macdRatio.Value; set => _macdRatio.Value = value; }

	public MaMacdPositionAveragingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame());
		_orderVolume = Param(nameof(OrderVolume), 0.1m).SetGreaterThanZero();
		_stopLossPips = Param(nameof(StopLossPips), 50).SetNotNegative();
		_takeProfitPips = Param(nameof(TakeProfitPips), 50).SetNotNegative();
		_trailingStopPips = Param(nameof(TrailingStopPips), 5).SetNotNegative();
		_trailingStepPips = Param(nameof(TrailingStepPips), 5).SetNotNegative();
		_stepLossingPips = Param(nameof(StepLossingPips), 30).SetNotNegative();
		_lotCoefficient = Param(nameof(LotCoefficient), 2m).SetGreaterThanZero();
		_signalBar = Param(nameof(SignalBar), 0).SetNotNegative();
		_maPeriod = Param(nameof(MaPeriod), 15).SetGreaterThanZero();
		_maShift = Param(nameof(MaShift), 0).SetNotNegative();
		_maMethod = Param(nameof(MaMethod), MaMacdMovingAverageMethod.Weighted);
		_maAppliedPrice = Param(nameof(MaAppliedPrice), MaMacdAppliedPrice.Weighted);
		_indentPips = Param(nameof(IndentPips), 4).SetNotNegative();
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12).SetGreaterThanZero();
		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26).SetGreaterThanZero();
		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9).SetGreaterThanZero();
		_macdAppliedPrice = Param(nameof(MacdAppliedPrice), MaMacdAppliedPrice.Weighted);
		_macdRatio = Param(nameof(MacdRatio), 0.9m).SetNotNegative();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_maInputs.Clear();
		_maValues.Clear();
		_macdValues.Clear();
		_legs.Clear();
		_macdFast = null;
		_macdSlow = null;
		_macdSignal = null;
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

		UpdateIndicators(candle);

		if (_legs.Select(l => l.Side).Distinct().Count() > 1)
		{
			FlattenAll();
			return;
		}

		ApplyProtection(candle);

		if (_legs.Count > 0)
		{
			TryAverage(candle.ClosePrice);
			return;
		}

		if (Position != 0)
			return;

		var maIndex = _maValues.Count - 1 - SignalBar - MaShift;
		var macdIndex = _macdValues.Count - 1 - SignalBar;

		if (maIndex < 0 || macdIndex < 0)
			return;

		var ma = _maValues[maIndex];
		var (main, signal) = _macdValues[macdIndex];

		if (signal == 0m)
			return;

		var ratio = main / signal;
		var pip = GetPipSize();
		var indent = IndentPips * pip;
		var close = candle.ClosePrice;

		if (main < 0m && signal < 0m && ratio >= MacdRatio && close > ma && close - ma >= indent)
			AddLeg(Sides.Buy, NormalizeVolume(OrderVolume), close);
		else if (main > 0m && signal > 0m && ratio >= MacdRatio && close < ma && ma - close >= indent)
			AddLeg(Sides.Sell, NormalizeVolume(OrderVolume), close);
	}

	private void UpdateIndicators(ICandleMessage candle)
	{
		var maPrice = GetAppliedPrice(candle, MaAppliedPrice);
		_maInputs.Add(maPrice);
		_maValues.Add(CalculateMa(_maInputs, MaPeriod, MaMethod));

		var macdPrice = GetAppliedPrice(candle, MacdAppliedPrice);
		_macdFast = Ema(_macdFast, macdPrice, MacdFastPeriod, 2m);
		_macdSlow = Ema(_macdSlow, macdPrice, MacdSlowPeriod, 2m);
		var main = _macdFast.Value - _macdSlow.Value;
		_macdSignal = Ema(_macdSignal, main, MacdSignalPeriod, 2m);
		_macdValues.Add((main, _macdSignal.Value));

		var keep = Math.Max(MaPeriod + MaShift + SignalBar + 10, MacdSlowPeriod + MacdSignalPeriod + SignalBar + 10);
		if (_maInputs.Count > keep)
		{
			var remove = _maInputs.Count - keep;
			_maInputs.RemoveRange(0, remove);
			_maValues.RemoveRange(0, remove);
			_macdValues.RemoveRange(0, remove);
		}
	}

	private void TryAverage(decimal close)
	{
		if (StepLossingPips <= 0 || _legs.Count == 0)
			return;

		var side = _legs[0].Side;
		if (_legs.Any(l => l.Side != side))
			return;

		var pip = GetPipSize();
		var distance = StepLossingPips * pip;
		var lastVolume = _legs[^1].Volume;
		var volume = NormalizeVolume(lastVolume * LotCoefficient);

		if (side == Sides.Buy)
		{
			var bestEntry = _legs.Min(l => l.EntryPrice);
			if (close <= bestEntry - distance)
				AddLeg(Sides.Buy, volume, close);
		}
		else
		{
			var bestEntry = _legs.Max(l => l.EntryPrice);
			if (close >= bestEntry + distance)
				AddLeg(Sides.Sell, volume, close);
		}
	}

	private void ApplyProtection(ICandleMessage candle)
	{
		if (_legs.Count == 0)
			return;

		var pip = GetPipSize();
		var exitBuy = 0m;
		var exitSell = 0m;

		for (var i = _legs.Count - 1; i >= 0; i--)
		{
			var leg = _legs[i];
			UpdateTrailing(leg, candle.ClosePrice, pip);

			var hit = leg.Side == Sides.Buy
				? (leg.StopPrice is decimal stop && candle.LowPrice <= stop) || (leg.TakePrice is decimal take && candle.HighPrice >= take)
				: (leg.StopPrice is decimal stop && candle.HighPrice >= stop) || (leg.TakePrice is decimal take && candle.LowPrice <= take);

			if (!hit)
				continue;

			if (leg.Side == Sides.Buy)
				exitSell += leg.Volume;
			else
				exitBuy += leg.Volume;

			_legs.RemoveAt(i);
		}

		if (exitSell > 0m)
			SellMarket(exitSell);
		if (exitBuy > 0m)
			BuyMarket(exitBuy);
	}

	private void UpdateTrailing(Leg leg, decimal close, decimal pip)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips < 0)
			return;

		var trail = TrailingStopPips * pip;
		var step = TrailingStepPips * pip;

		if (leg.Side == Sides.Buy)
		{
			if (close - leg.EntryPrice < trail + step)
				return;

			var candidate = close - trail;
			if (leg.StopPrice is null || candidate >= leg.StopPrice.Value + step)
				leg.StopPrice = candidate;
		}
		else
		{
			if (leg.EntryPrice - close < trail + step)
				return;

			var candidate = close + trail;
			if (leg.StopPrice is null || candidate <= leg.StopPrice.Value - step)
				leg.StopPrice = candidate;
		}
	}

	private void AddLeg(Sides side, decimal volume, decimal price)
	{
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		var pip = GetPipSize();
		var stopDistance = StopLossPips * pip;
		var takeDistance = TakeProfitPips * pip;

		_legs.Add(new Leg
		{
			Side = side,
			Volume = volume,
			EntryPrice = price,
			StopPrice = StopLossPips > 0 ? side == Sides.Buy ? price - stopDistance : price + stopDistance : null,
			TakePrice = TakeProfitPips > 0 ? side == Sides.Buy ? price + takeDistance : price - takeDistance : null,
		});
	}

	private void FlattenAll()
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));

		_legs.Clear();
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security?.MaxVolume is decimal max && max > 0m)
			volume = Math.Min(volume, max);
		if (Security?.MinVolume is decimal min && min > 0m)
			volume = Math.Max(volume, min);
		if (Security?.VolumeStep is decimal step && step > 0m)
			volume = Math.Floor(volume / step) * step;

		return volume;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;

		return step is 0.00001m or 0.001m ? step * 10m : step;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, MaMacdAppliedPrice type)
		=> type switch
		{
			MaMacdAppliedPrice.Open => candle.OpenPrice,
			MaMacdAppliedPrice.High => candle.HighPrice,
			MaMacdAppliedPrice.Low => candle.LowPrice,
			MaMacdAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			MaMacdAppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			MaMacdAppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};

	private static decimal CalculateMa(List<decimal> values, int period, MaMacdMovingAverageMethod method)
	{
		var count = Math.Min(period, values.Count);
		var start = values.Count - count;

		if (method == MaMacdMovingAverageMethod.Weighted)
		{
			var weighted = 0m;
			var weights = 0m;
			for (var i = 0; i < count; i++)
			{
				var weight = i + 1m;
				weighted += values[start + i] * weight;
				weights += weight;
			}
			return weighted / weights;
		}

		if (method == MaMacdMovingAverageMethod.Simple)
			return values.Skip(start).Average();

		var alpha = method == MaMacdMovingAverageMethod.Smoothed ? 1m / period : 2m / (period + 1m);
		var result = values[start];
		for (var i = start + 1; i < values.Count; i++)
			result += alpha * (values[i] - result);
		return result;
	}

	private static decimal Ema(decimal? previous, decimal value, int period, decimal numerator)
	{
		if (previous is null)
			return value;

		var alpha = numerator / (period + 1m);
		return previous.Value + alpha * (value - previous.Value);
	}

	private sealed class Leg
	{
		public Sides Side { get; init; }
		public decimal Volume { get; init; }
		public decimal EntryPrice { get; init; }
		public decimal? StopPrice { get; set; }
		public decimal? TakePrice { get; init; }
	}
}
