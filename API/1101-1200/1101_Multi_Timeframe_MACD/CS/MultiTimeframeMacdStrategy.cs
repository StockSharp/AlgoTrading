using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum MultiTimeframeMacdEntry
{
	Crossover,
	ZeroLine,
}

/// <summary>
/// MACD agreement strategy across working and higher timeframes.
/// </summary>
public class MultiTimeframeMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<bool> _showCurrentTimeframe;
	private readonly StrategyParam<bool> _showHigherTimeframe;
	private readonly StrategyParam<MultiTimeframeMacdEntry> _entry;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPercent;

	private MacdFrame _current;
	private MacdFrame _higher;
	private int _lastCombined;
	private decimal? _bestPrice;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DataType HigherCandleType { get => _higherCandleType.Value; set => _higherCandleType.Value = value; }
	public bool ShowCurrentTimeframe { get => _showCurrentTimeframe.Value; set => _showCurrentTimeframe.Value = value; }
	public bool ShowHigherTimeframe { get => _showHigherTimeframe.Value; set => _showHigherTimeframe.Value = value; }
	public MultiTimeframeMacdEntry Entry { get => _entry.Value; set => _entry.Value = value; }
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }
	public decimal TrailingStopPercent { get => _trailingStopPercent.Value; set => _trailingStopPercent.Value = value; }

	public MultiTimeframeMacdStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12).SetGreaterThanZero();
		_slowLength = Param(nameof(SlowLength), 26).SetGreaterThanZero();
		_signalLength = Param(nameof(SignalLength), 9).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromDays(1).TimeFrame());
		_showCurrentTimeframe = Param(nameof(ShowCurrentTimeframe), true);
		_showHigherTimeframe = Param(nameof(ShowHigherTimeframe), true);
		_entry = Param(nameof(Entry), MultiTimeframeMacdEntry.Crossover);
		_useTrailingStop = Param(nameof(UseTrailingStop), false);
		_trailingStopPercent = Param(nameof(TrailingStopPercent), 2m).SetGreaterThanZero();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, HigherCandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_current = null;
		_higher = null;
		_lastCombined = 0;
		_bestPrice = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_current = new MacdFrame(FastLength, SlowLength, SignalLength);
		_higher = new MacdFrame(FastLength, SlowLength, SignalLength);

		var currentSubscription = SubscribeCandles(CandleType);
		currentSubscription.Bind(candle =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			_current.Process(candle.ClosePrice);
			if (ApplyTrailing(candle))
				return;

			EvaluateAgreement();
		}).Start();

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription.Bind(candle =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			_higher.Process(candle.ClosePrice);
			EvaluateAgreement();
		}).Start();

		if (ShowCurrentTimeframe)
		{
			var area = CreateChartArea();
			if (area != null)
				DrawCandles(area, currentSubscription);
		}

		if (ShowHigherTimeframe)
		{
			var area = CreateChartArea();
			if (area != null)
				DrawCandles(area, higherSubscription);
		}
	}

	private void EvaluateAgreement()
	{
		if (!_current.IsReady || !_higher.IsReady)
			return;

		var current = _current.Direction(Entry);
		var higher = _higher.Direction(Entry);
		var combined = current != 0 && current == higher ? current : 0;

		if (combined == 0)
		{
			_lastCombined = 0;
			return;
		}

		if (combined == _lastCombined)
			return;

		if (combined > 0 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_bestPrice = null;
		}
		else if (combined < 0 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_bestPrice = null;
		}

		_lastCombined = combined;
	}

	private bool ApplyTrailing(ICandleMessage candle)
	{
		if (!UseTrailingStop || Position == 0)
			return false;

		if (Position > 0)
		{
			_bestPrice = _bestPrice is decimal best ? Math.Max(best, candle.HighPrice) : candle.HighPrice;
			var stop = _bestPrice.Value * (1m - TrailingStopPercent / 100m);
			if (candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				_bestPrice = null;
				_lastCombined = 0;
				return true;
			}
		}
		else
		{
			_bestPrice = _bestPrice is decimal best ? Math.Min(best, candle.LowPrice) : candle.LowPrice;
			var stop = _bestPrice.Value * (1m + TrailingStopPercent / 100m);
			if (candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				_bestPrice = null;
				_lastCombined = 0;
				return true;
			}
		}

		return false;
	}

	private sealed class MacdFrame(int fastLength, int slowLength, int signalLength)
	{
		private readonly int _fastLength = fastLength;
		private readonly int _slowLength = slowLength;
		private readonly int _signalLength = signalLength;

		private decimal? _fast;
		private decimal? _slow;
		private decimal? _signal;
		private int _count;

		public decimal Macd { get; private set; }
		public decimal Signal { get; private set; }
		public bool IsReady => _count >= _slowLength + _signalLength;

		public void Process(decimal price)
		{
			_count++;
			_fast = Ema(_fast, price, _fastLength);
			_slow = Ema(_slow, price, _slowLength);
			Macd = _fast.Value - _slow.Value;
			_signal = Ema(_signal, Macd, _signalLength);
			Signal = _signal.Value;
		}

		public int Direction(MultiTimeframeMacdEntry mode)
		{
			if (!IsReady)
				return 0;

			var value = mode == MultiTimeframeMacdEntry.Crossover ? Macd - Signal : Macd;
			return value > 0m ? 1 : value < 0m ? -1 : 0;
		}

		private static decimal Ema(decimal? previous, decimal value, int length)
		{
			if (previous is null)
				return value;

			var alpha = 2m / (length + 1m);
			return previous.Value + alpha * (value - previous.Value);
		}
	}
}
