using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bruno multi-filter trend strategy.
/// </summary>
public class BrunoStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _signalMultiplier;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxPositiveThreshold;
	private readonly StrategyParam<decimal> _adxNegativeThreshold;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<int> _stochasticKsmoothing;
	private readonly StrategyParam<int> _stochasticDsmoothing;
	private readonly StrategyParam<decimal> _stochasticOverbought;
	private readonly StrategyParam<decimal> _stochasticOversold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<Bar> _bars = [];
	private readonly List<decimal> _rawK = [];
	private readonly List<decimal> _smoothK = [];
	private readonly List<decimal> _sarValues = [];

	private decimal? _fastEma;
	private decimal? _slowEma;
	private decimal? _macdFast;
	private decimal? _macdSlow;
	private decimal? _macdSignal;

	private readonly SarState _sar = new(0.055m, 0.21m);

	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _bestPrice;
	private DateTimeOffset? _entryCandleTime;

	public decimal BaseVolume { get => _baseVolume.Value; set => _baseVolume.Value = value; }
	public decimal SignalMultiplier { get => _signalMultiplier.Value; set => _signalMultiplier.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public int TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }
	public int TrailingStepPips { get => _trailingStepPips.Value; set => _trailingStepPips.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxPositiveThreshold { get => _adxPositiveThreshold.Value; set => _adxPositiveThreshold.Value = value; }
	public decimal AdxNegativeThreshold { get => _adxNegativeThreshold.Value; set => _adxNegativeThreshold.Value = value; }
	public int FastEmaPeriod { get => _fastEmaPeriod.Value; set => _fastEmaPeriod.Value = value; }
	public int SlowEmaPeriod { get => _slowEmaPeriod.Value; set => _slowEmaPeriod.Value = value; }
	public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }
	public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }
	public int MacdSignalPeriod { get => _macdSignalPeriod.Value; set => _macdSignalPeriod.Value = value; }
	public int StochasticPeriod { get => _stochasticPeriod.Value; set => _stochasticPeriod.Value = value; }
	public int StochasticKsmoothing { get => _stochasticKsmoothing.Value; set => _stochasticKsmoothing.Value = value; }
	public int StochasticDsmoothing { get => _stochasticDsmoothing.Value; set => _stochasticDsmoothing.Value = value; }
	public decimal StochasticOverbought { get => _stochasticOverbought.Value; set => _stochasticOverbought.Value = value; }
	public decimal StochasticOversold { get => _stochasticOversold.Value; set => _stochasticOversold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BrunoStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.1m).SetGreaterThanZero();
		_signalMultiplier = Param(nameof(SignalMultiplier), 1.6m).SetGreaterThanZero();
		_stopLossPips = Param(nameof(StopLossPips), 50).SetNotNegative();
		_takeProfitPips = Param(nameof(TakeProfitPips), 100).SetNotNegative();
		_trailingStopPips = Param(nameof(TrailingStopPips), 30).SetNotNegative();
		_trailingStepPips = Param(nameof(TrailingStepPips), 5).SetNotNegative();
		_adxPeriod = Param(nameof(AdxPeriod), 14).SetGreaterThanZero();
		_adxPositiveThreshold = Param(nameof(AdxPositiveThreshold), 20m);
		_adxNegativeThreshold = Param(nameof(AdxNegativeThreshold), 40m);
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 8).SetGreaterThanZero();
		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 21).SetGreaterThanZero();
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 13).SetGreaterThanZero();
		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 34).SetGreaterThanZero();
		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 8).SetGreaterThanZero();
		_stochasticPeriod = Param(nameof(StochasticPeriod), 21).SetGreaterThanZero();
		_stochasticKsmoothing = Param(nameof(StochasticKsmoothing), 3).SetGreaterThanZero();
		_stochasticDsmoothing = Param(nameof(StochasticDsmoothing), 3).SetGreaterThanZero();
		_stochasticOverbought = Param(nameof(StochasticOverbought), 80m);
		_stochasticOversold = Param(nameof(StochasticOversold), 20m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_bars.Clear();
		_rawK.Clear();
		_smoothK.Clear();
		_sarValues.Clear();
		_fastEma = _slowEma = _macdFast = _macdSlow = _macdSignal = null;
		_sar.Reset();
		ResetProtection();
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

		_bars.Add(new Bar(candle.HighPrice, candle.LowPrice, candle.ClosePrice));
		var keep = Math.Max(Math.Max(AdxPeriod + 2, MacdSlowPeriod + MacdSignalPeriod + 2), StochasticPeriod + 10);
		if (_bars.Count > keep)
			_bars.RemoveRange(0, _bars.Count - keep);

		_fastEma = Ema(_fastEma, candle.ClosePrice, FastEmaPeriod);
		_slowEma = Ema(_slowEma, candle.ClosePrice, SlowEmaPeriod);
		_macdFast = Ema(_macdFast, candle.ClosePrice, MacdFastPeriod);
		_macdSlow = Ema(_macdSlow, candle.ClosePrice, MacdSlowPeriod);
		var macdMain = _macdFast.Value - _macdSlow.Value;
		_macdSignal = Ema(_macdSignal, macdMain, MacdSignalPeriod);

		var sar = _sar.Process(candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		if (sar is decimal sarValue)
		{
			_sarValues.Add(sarValue);
			if (_sarValues.Count > 4)
				_sarValues.RemoveAt(0);
		}

		UpdateStochastic();

		if (Position != 0m && ApplyProtection(candle))
			return;

		if (_bars.Count < AdxPeriod + 1 || _smoothK.Count < StochasticDsmoothing || _sarValues.Count < 3)
			return;

		var (plusDi, minusDi) = CalculateDirectionalIndex();
		var k = _smoothK[^1];
		var d = _smoothK.Skip(_smoothK.Count - StochasticDsmoothing).Average();
		var histogram = macdMain - _macdSignal.Value;

		var longDirectional = plusDi > minusDi && plusDi > AdxPositiveThreshold;
		var shortDirectional = plusDi < minusDi && plusDi < AdxNegativeThreshold;

		var longMomentum = _fastEma > _slowEma && k > d && k < StochasticOverbought;
		var shortMomentum = _fastEma < _slowEma && k < d && k > StochasticOversold;

		var longMacd = histogram > 0m && macdMain > _macdSignal;
		var shortMacd = histogram < 0m && macdMain < _macdSignal;

		var longSar = _sarValues[^3] < _sarValues[^2] && _sarValues[^2] < _sarValues[^1] && _fastEma > _slowEma;
		var shortSar = _sarValues[^3] > _sarValues[^2] && _sarValues[^2] > _sarValues[^1] && _fastEma < _slowEma;

		var (longVolume, shortVolume) = CalculateSignalVolumes(
			BaseVolume, SignalMultiplier,
			longDirectional, shortDirectional,
			longMomentum, shortMomentum,
			longMacd, shortMacd,
			longSar, shortSar);

		var hasLong = longVolume > BaseVolume;
		var hasShort = shortVolume > BaseVolume;

		if (hasLong == hasShort)
			return;

		if (hasLong)
			Enter(Sides.Buy, longVolume, candle);
		else
			Enter(Sides.Sell, shortVolume, candle);
	}

	private void Enter(Sides side, decimal targetVolume, ICandleMessage candle)
	{
		var volume = NormalizeVolume(targetVolume + (Position * (side == Sides.Buy ? -1m : 1m)).Abs());
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_entryPrice = candle.ClosePrice;
		_entryCandleTime = candle.OpenTime;
		_bestPrice = candle.ClosePrice;

		var pip = GetPipSize();
		_stopPrice = StopLossPips > 0
			? side == Sides.Buy ? _entryPrice - StopLossPips * pip : _entryPrice + StopLossPips * pip
			: null;
		_takePrice = TakeProfitPips > 0
			? side == Sides.Buy ? _entryPrice + TakeProfitPips * pip : _entryPrice - TakeProfitPips * pip
			: null;
	}

	private bool ApplyProtection(ICandleMessage candle)
	{
		if (_entryCandleTime is DateTimeOffset entryTime && candle.OpenTime <= entryTime)
			return false;

		var pip = GetPipSize();

		if (Position > 0m)
		{
			_bestPrice = _bestPrice is decimal best ? Math.Max(best, candle.HighPrice) : candle.HighPrice;

			if (TrailingStopPips > 0 && TrailingStepPips >= 0 &&
				_bestPrice.Value - _entryPrice >= (TrailingStopPips + TrailingStepPips) * pip)
			{
				var candidate = _bestPrice.Value - TrailingStopPips * pip;
				if (_stopPrice is null || candidate >= _stopPrice.Value + TrailingStepPips * pip)
					_stopPrice = candidate;
			}

			if ((_stopPrice is decimal stop && candle.LowPrice <= stop) ||
				(_takePrice is decimal take && candle.HighPrice >= take))
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}
		}
		else if (Position < 0m)
		{
			_bestPrice = _bestPrice is decimal best ? Math.Min(best, candle.LowPrice) : candle.LowPrice;

			if (TrailingStopPips > 0 && TrailingStepPips >= 0 &&
				_entryPrice - _bestPrice.Value >= (TrailingStopPips + TrailingStepPips) * pip)
			{
				var candidate = _bestPrice.Value + TrailingStopPips * pip;
				if (_stopPrice is null || candidate <= _stopPrice.Value - TrailingStepPips * pip)
					_stopPrice = candidate;
			}

			if ((_stopPrice is decimal stop && candle.HighPrice >= stop) ||
				(_takePrice is decimal take && candle.LowPrice <= take))
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}
		}

		return false;
	}

	private void UpdateStochastic()
	{
		if (_bars.Count < StochasticPeriod)
			return;

		var window = _bars.Skip(_bars.Count - StochasticPeriod).ToArray();
		var high = window.Max(b => b.High);
		var low = window.Min(b => b.Low);
		var raw = high == low ? 50m : (_bars[^1].Close - low) / (high - low) * 100m;

		_rawK.Add(raw);
		if (_rawK.Count > StochasticKsmoothing + StochasticDsmoothing + 2)
			_rawK.RemoveAt(0);

		if (_rawK.Count < StochasticKsmoothing)
			return;

		_smoothK.Add(_rawK.Skip(_rawK.Count - StochasticKsmoothing).Average());
		if (_smoothK.Count > StochasticDsmoothing + 2)
			_smoothK.RemoveAt(0);
	}

	private (decimal plusDi, decimal minusDi) CalculateDirectionalIndex()
	{
		var tr = 0m;
		var plus = 0m;
		var minus = 0m;
		var start = _bars.Count - AdxPeriod;

		for (var i = start; i < _bars.Count; i++)
		{
			var current = _bars[i];
			var previous = _bars[i - 1];
			var up = current.High - previous.High;
			var down = previous.Low - current.Low;

			plus += up > down && up > 0m ? up : 0m;
			minus += down > up && down > 0m ? down : 0m;

			tr += Math.Max(current.High - current.Low,
				Math.Max(Math.Abs(current.High - previous.Close), Math.Abs(current.Low - previous.Close)));
		}

		return tr <= 0m ? (0m, 0m) : (plus / tr * 100m, minus / tr * 100m);
	}

	internal static (decimal longVolume, decimal shortVolume) CalculateSignalVolumes(
		decimal baseVolume,
		decimal multiplier,
		bool longDirectional,
		bool shortDirectional,
		bool longMomentum,
		bool shortMomentum,
		bool longMacd,
		bool shortMacd,
		bool longSar,
		bool shortSar)
	{
		var longVolume = baseVolume;
		var shortVolume = baseVolume;

		if (longDirectional) longVolume *= multiplier;
		if (shortDirectional) shortVolume *= multiplier;
		if (longMomentum) longVolume *= multiplier;
		if (shortMomentum) shortVolume *= multiplier;
		if (longMacd) longVolume *= multiplier;
		if (shortMacd) shortVolume *= multiplier;
		if (longSar) longVolume *= multiplier;
		if (shortSar) shortVolume *= multiplier;

		return (longVolume, shortVolume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security?.MaxVolume is decimal max && max > 0m) volume = Math.Min(volume, max);
		if (Security?.MinVolume is decimal min && min > 0m) volume = Math.Max(volume, min);
		if (Security?.VolumeStep is decimal step && step > 0m) volume = Math.Floor(volume / step) * step;
		return volume;
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

	private void ResetProtection()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_bestPrice = null;
		_entryCandleTime = null;
	}

	private readonly record struct Bar(decimal High, decimal Low, decimal Close);

	private sealed class SarState(decimal step, decimal maximum)
	{
		private bool _initialized;
		private bool _up;
		private decimal _sar;
		private decimal _ep;
		private decimal _af;
		private decimal _previousHigh;
		private decimal _previousLow;
		private decimal _previousClose;
		private decimal _olderHigh;
		private decimal _olderLow;
		private int _count;

		public void Reset()
		{
			_initialized = false;
			_up = false;
			_sar = _ep = _af = 0m;
			_previousHigh = _previousLow = _previousClose = 0m;
			_olderHigh = _olderLow = 0m;
			_count = 0;
		}

		public decimal? Process(decimal high, decimal low, decimal close)
		{
			_count++;

			if (_count == 1)
			{
				_previousHigh = _olderHigh = high;
				_previousLow = _olderLow = low;
				_previousClose = close;
				return null;
			}

			if (!_initialized)
			{
				_up = close >= _previousClose;
				_sar = _up ? Math.Min(_previousLow, low) : Math.Max(_previousHigh, high);
				_ep = _up ? Math.Max(_previousHigh, high) : Math.Min(_previousLow, low);
				_af = step;
				_initialized = true;
				Shift(high, low, close);
				return _sar;
			}

			var next = _sar + _af * (_ep - _sar);

			if (_up)
			{
				next = Math.Min(next, Math.Min(_previousLow, _olderLow));
				if (low < next)
				{
					_up = false;
					next = _ep;
					_ep = low;
					_af = step;
				}
				else if (high > _ep)
				{
					_ep = high;
					_af = Math.Min(maximum, _af + step);
				}
			}
			else
			{
				next = Math.Max(next, Math.Max(_previousHigh, _olderHigh));
				if (high > next)
				{
					_up = true;
					next = _ep;
					_ep = high;
					_af = step;
				}
				else if (low < _ep)
				{
					_ep = low;
					_af = Math.Min(maximum, _af + step);
				}
			}

			_sar = next;
			Shift(high, low, close);
			return _sar;
		}

		private void Shift(decimal high, decimal low, decimal close)
		{
			_olderHigh = _previousHigh;
			_olderLow = _previousLow;
			_previousHigh = high;
			_previousLow = low;
			_previousClose = close;
		}
	}
}
