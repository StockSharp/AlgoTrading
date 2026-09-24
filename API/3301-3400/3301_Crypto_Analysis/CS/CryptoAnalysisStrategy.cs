using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crypto Analysis: Bollinger/LWMA/RSI entry with higher-timeframe Momentum and MACD filters.
/// </summary>
public class CryptoAnalysisStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _enableMoneyTrailing;
	private readonly StrategyParam<decimal> _moneyTrailTarget;
	private readonly StrategyParam<decimal> _moneyTrailStop;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<int> _breakEvenTriggerPips;
	private readonly StrategyParam<int> _breakEvenOffsetPips;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _equityRiskPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private readonly List<Bar> _primary = [];
	private readonly List<decimal> _momentumCloses = [];
	private readonly Queue<decimal> _momentumDeviations = new();

	private decimal? _macdFast;
	private decimal? _macdSlow;
	private decimal? _macdSignal;
	private bool _macdReady;
	private int _macdCount;

	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _bestPrice;
	private decimal _initialEquity;
	private decimal _peakEquity;
	private decimal? _moneyTrailPeak;

	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
	public bool UseMoneyTakeProfit { get => _useMoneyTakeProfit.Value; set => _useMoneyTakeProfit.Value = value; }
	public decimal MoneyTakeProfit { get => _moneyTakeProfit.Value; set => _moneyTakeProfit.Value = value; }
	public bool UsePercentTakeProfit { get => _usePercentTakeProfit.Value; set => _usePercentTakeProfit.Value = value; }
	public decimal PercentTakeProfit { get => _percentTakeProfit.Value; set => _percentTakeProfit.Value = value; }
	public bool EnableMoneyTrailing { get => _enableMoneyTrailing.Value; set => _enableMoneyTrailing.Value = value; }
	public decimal MoneyTrailTarget { get => _moneyTrailTarget.Value; set => _moneyTrailTarget.Value = value; }
	public decimal MoneyTrailStop { get => _moneyTrailStop.Value; set => _moneyTrailStop.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public int TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }
	public bool UseBreakEven { get => _useBreakEven.Value; set => _useBreakEven.Value = value; }
	public int BreakEvenTriggerPips { get => _breakEvenTriggerPips.Value; set => _breakEvenTriggerPips.Value = value; }
	public int BreakEvenOffsetPips { get => _breakEvenOffsetPips.Value; set => _breakEvenOffsetPips.Value = value; }
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public decimal MomentumBuyThreshold { get => _momentumBuyThreshold.Value; set => _momentumBuyThreshold.Value = value; }
	public decimal MomentumSellThreshold { get => _momentumSellThreshold.Value; set => _momentumSellThreshold.Value = value; }
	public int MacdFastLength { get => _macdFastLength.Value; set => _macdFastLength.Value = value; }
	public int MacdSlowLength { get => _macdSlowLength.Value; set => _macdSlowLength.Value = value; }
	public int MacdSignalLength { get => _macdSignalLength.Value; set => _macdSignalLength.Value = value; }
	public bool UseEquityStop { get => _useEquityStop.Value; set => _useEquityStop.Value = value; }
	public decimal EquityRiskPercent { get => _equityRiskPercent.Value; set => _equityRiskPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DataType MomentumCandleType { get => _momentumCandleType.Value; set => _momentumCandleType.Value = value; }
	public DataType MacdCandleType { get => _macdCandleType.Value; set => _macdCandleType.Value = value; }

	public CryptoAnalysisStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m).SetGreaterThanZero();
		_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false);
		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 100m).SetNotNegative();
		_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false);
		_percentTakeProfit = Param(nameof(PercentTakeProfit), 1m).SetNotNegative();
		_enableMoneyTrailing = Param(nameof(EnableMoneyTrailing), false);
		_moneyTrailTarget = Param(nameof(MoneyTrailTarget), 50m).SetNotNegative();
		_moneyTrailStop = Param(nameof(MoneyTrailStop), 20m).SetNotNegative();
		_stopLossPips = Param(nameof(StopLossPips), 50).SetNotNegative();
		_takeProfitPips = Param(nameof(TakeProfitPips), 100).SetNotNegative();
		_trailingStopPips = Param(nameof(TrailingStopPips), 30).SetNotNegative();
		_useBreakEven = Param(nameof(UseBreakEven), true);
		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30).SetNotNegative();
		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 2).SetNotNegative();
		_fastMaPeriod = Param(nameof(FastMaPeriod), 6).SetGreaterThanZero();
		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85).SetGreaterThanZero();
		_momentumPeriod = Param(nameof(MomentumPeriod), 14).SetGreaterThanZero();
		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m).SetNotNegative();
		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m).SetNotNegative();
		_macdFastLength = Param(nameof(MacdFastLength), 12).SetGreaterThanZero();
		_macdSlowLength = Param(nameof(MacdSlowLength), 26).SetGreaterThanZero();
		_macdSignalLength = Param(nameof(MacdSignalLength), 9).SetGreaterThanZero();
		_useEquityStop = Param(nameof(UseEquityStop), false);
		_equityRiskPercent = Param(nameof(EquityRiskPercent), 10m).SetNotNegative();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame());
		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, MomentumCandleType), (Security, MacdCandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_primary.Clear();
		_momentumCloses.Clear();
		_momentumDeviations.Clear();
		_macdFast = null;
		_macdSlow = null;
		_macdSignal = null;
		_macdReady = false;
		_macdCount = 0;
		ResetTradeState();
		_initialEquity = 0m;
		_peakEquity = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_initialEquity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		_peakEquity = _initialEquity;

		SubscribeCandles(MomentumCandleType).Bind(ProcessMomentum).Start();
		SubscribeCandles(MacdCandleType).Bind(ProcessMacd).Start();
		SubscribeCandles(CandleType).Bind(ProcessPrimary).Start();
	}

	private void ProcessMomentum(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_momentumCloses.Add(candle.ClosePrice);
		var keep = MomentumPeriod + 5;
		if (_momentumCloses.Count > keep)
			_momentumCloses.RemoveRange(0, _momentumCloses.Count - keep);

		if (_momentumCloses.Count <= MomentumPeriod)
			return;

		var previous = _momentumCloses[^1 - MomentumPeriod];
		if (previous == 0m)
			return;

		var momentum = candle.ClosePrice / previous * 100m;
		_momentumDeviations.Enqueue(Math.Abs(momentum - 100m));
		while (_momentumDeviations.Count > 3)
			_momentumDeviations.Dequeue();
	}

	private void ProcessMacd(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_macdCount++;
		_macdFast = Ema(_macdFast, candle.ClosePrice, MacdFastLength);
		_macdSlow = Ema(_macdSlow, candle.ClosePrice, MacdSlowLength);
		var main = _macdFast.Value - _macdSlow.Value;
		_macdSignal = Ema(_macdSignal, main, MacdSignalLength);
		_macdReady = _macdCount >= MacdSlowLength + MacdSignalLength;
	}

	private void ProcessPrimary(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_primary.Add(new(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));
		var keep = Math.Max(SlowMaPeriod + 5, 30);
		if (_primary.Count > keep)
			_primary.RemoveRange(0, _primary.Count - keep);

		if (Position != 0 && ApplyRisk(candle))
			return;

		if (_primary.Count < Math.Max(SlowMaPeriod, 21) ||
			_momentumDeviations.Count < 3 ||
			!_macdReady ||
			_macdSignal is null ||
			_macdFast is null ||
			_macdSlow is null)
			return;

		var previous = _primary[^2];
		var bollinger = BollingerAtPrevious(20, 2m);
		var fast = Lwma(FastMaPeriod);
		var slow = Lwma(SlowMaPeriod);
		var rsi = Rsi(14);
		var momentum = _momentumDeviations.Max();
		var macdMain = _macdFast.Value - _macdSlow.Value;
		var macdSignal = _macdSignal.Value;

		var longSignal =
			previous.Low <= bollinger.Lower &&
			fast < slow &&
			rsi > 50m &&
			momentum >= MomentumBuyThreshold &&
			macdMain > macdSignal;

		var shortSignal =
			previous.High >= bollinger.Upper &&
			fast < slow &&
			rsi < 50m &&
			momentum >= MomentumSellThreshold &&
			macdMain < macdSignal;

		if (longSignal && Position <= 0m)
			Enter(Sides.Buy, candle.ClosePrice);
		else if (shortSignal && Position >= 0m)
			Enter(Sides.Sell, candle.ClosePrice);
	}

	private void Enter(Sides side, decimal price)
	{
		var volume = OrderVolume + Math.Abs(Position);
		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_entryPrice = price;
		var pip = GetPipSize();
		_stopPrice = StopLossPips > 0
			? side == Sides.Buy ? price - StopLossPips * pip : price + StopLossPips * pip
			: null;
		_takePrice = TakeProfitPips > 0
			? side == Sides.Buy ? price + TakeProfitPips * pip : price - TakeProfitPips * pip
			: null;
		_bestPrice = price;
		_moneyTrailPeak = null;
	}

	private bool ApplyRisk(ICandleMessage candle)
	{
		var close = candle.ClosePrice;
		var floating = FloatingPnL(close);
		var equity = (Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? _initialEquity) + floating;
		_peakEquity = Math.Max(_peakEquity, equity);

		if (UseMoneyTakeProfit && floating >= MoneyTakeProfit)
			return Flatten();

		if (UsePercentTakeProfit && _initialEquity > 0m && floating >= _initialEquity * PercentTakeProfit / 100m)
			return Flatten();

		if (EnableMoneyTrailing && floating >= MoneyTrailTarget)
		{
			_moneyTrailPeak = _moneyTrailPeak is decimal peak ? Math.Max(peak, floating) : floating;
			if (_moneyTrailPeak.Value - floating >= MoneyTrailStop)
				return Flatten();
		}

		if (UseEquityStop && _peakEquity > 0m && (_peakEquity - equity) / _peakEquity * 100m >= EquityRiskPercent)
			return Flatten();

		var pip = GetPipSize();

		if (Position > 0m)
		{
			_bestPrice = _bestPrice is decimal best ? Math.Max(best, candle.HighPrice) : candle.HighPrice;

			if (UseBreakEven && candle.HighPrice - _entryPrice >= BreakEvenTriggerPips * pip)
			{
				var candidate = _entryPrice + BreakEvenOffsetPips * pip;
				if (_stopPrice is null || candidate > _stopPrice)
					_stopPrice = candidate;
			}

			if (TrailingStopPips > 0)
			{
				var candidate = _bestPrice.Value - TrailingStopPips * pip;
				if (_stopPrice is null || candidate > _stopPrice)
					_stopPrice = candidate;
			}

			if ((_stopPrice is decimal stop && candle.LowPrice <= stop) ||
				(_takePrice is decimal take && candle.HighPrice >= take))
				return Flatten();
		}
		else
		{
			_bestPrice = _bestPrice is decimal best ? Math.Min(best, candle.LowPrice) : candle.LowPrice;

			if (UseBreakEven && _entryPrice - candle.LowPrice >= BreakEvenTriggerPips * pip)
			{
				var candidate = _entryPrice - BreakEvenOffsetPips * pip;
				if (_stopPrice is null || candidate < _stopPrice)
					_stopPrice = candidate;
			}

			if (TrailingStopPips > 0)
			{
				var candidate = _bestPrice.Value + TrailingStopPips * pip;
				if (_stopPrice is null || candidate < _stopPrice)
					_stopPrice = candidate;
			}

			if ((_stopPrice is decimal stop && candle.HighPrice >= stop) ||
				(_takePrice is decimal take && candle.LowPrice <= take))
				return Flatten();
		}

		return false;
	}

	private bool Flatten()
	{
		if (Position > 0m)
			SellMarket(Math.Abs(Position));
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));
		else
			return false;

		ResetTradeState();
		return true;
	}

	private decimal FloatingPnL(decimal price)
	{
		if (Position == 0m || _entryPrice == 0m)
			return 0m;

		var direction = Position > 0m ? 1m : -1m;
		return (price - _entryPrice) * direction * Math.Abs(Position) * (Security?.Multiplier ?? 1m);
	}

	private (decimal Upper, decimal Lower) BollingerAtPrevious(int period, decimal deviations)
	{
		var end = _primary.Count - 1;
		var values = _primary.Skip(end - period).Take(period).Select(b => b.Close).ToArray();
		var mean = values.Average();
		var variance = values.Select(v => (v - mean) * (v - mean)).Average();
		var std = (decimal)Math.Sqrt((double)variance);
		return (mean + deviations * std, mean - deviations * std);
	}

	private decimal Lwma(int period)
	{
		var start = _primary.Count - period;
		var total = 0m;
		var weights = 0m;
		for (var i = 0; i < period; i++)
		{
			var weight = i + 1m;
			total += _primary[start + i].Typical * weight;
			weights += weight;
		}
		return total / weights;
	}

	private decimal Rsi(int period)
	{
		var gains = 0m;
		var losses = 0m;
		for (var i = _primary.Count - period; i < _primary.Count; i++)
		{
			var diff = _primary[i].Close - _primary[i - 1].Close;
			if (diff > 0m) gains += diff; else losses -= diff;
		}
		if (losses == 0m) return 100m;
		var rs = gains / losses;
		return 100m - 100m / (1m + rs);
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m) return 0.0001m;
		return step is 0.00001m or 0.001m ? step * 10m : step;
	}

	private static decimal Ema(decimal? previous, decimal value, int period)
	{
		if (previous is null) return value;
		var alpha = 2m / (period + 1m);
		return previous.Value + alpha * (value - previous.Value);
	}

	private void ResetTradeState()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_bestPrice = null;
		_moneyTrailPeak = null;
	}

	private readonly record struct Bar(decimal Open, decimal High, decimal Low, decimal Close)
	{
		public decimal Typical => (High + Low + Close) / 3m;
	}
}
