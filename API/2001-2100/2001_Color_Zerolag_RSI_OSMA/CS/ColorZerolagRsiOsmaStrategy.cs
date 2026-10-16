using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>Five independently formed Wilder RSIs and a two-stage zero-lag OSMA.</summary>
public class ColorZerolagRsiOsmaStrategy : Strategy
{
	private sealed class RsiState
	{
		private readonly int _length;
		private int _samples;
		private decimal? _previous;
		private decimal _sumGain, _sumLoss, _avgGain, _avgLoss;

		public RsiState(int length) => _length = length;

		// A strategy clone compares its reset indicator configuration structurally.
		public override bool Equals(object other)
			=> other is RsiState rhs && _length == rhs._length
				&& _samples == rhs._samples && _previous == rhs._previous
				&& _sumGain == rhs._sumGain && _sumLoss == rhs._sumLoss
				&& _avgGain == rhs._avgGain && _avgLoss == rhs._avgLoss;

		public override int GetHashCode()
			=> HashCode.Combine(_length, _samples, _previous, _sumGain, _sumLoss, _avgGain, _avgLoss);

		public decimal? Add(decimal close)
		{
			if (_previous is not decimal previous)
			{
				_previous = close;
				_samples = 1;
				return null;
			}

			var delta = close - previous;
			_previous = close;
			var gain = Math.Max(delta, 0m);
			var loss = Math.Max(-delta, 0m);
			if (_samples <= _length)
			{
				_sumGain += gain;
				_sumLoss += loss;
				_samples++;
				if (_samples <= _length)
					return null;
				_avgGain = _sumGain / _length;
				_avgLoss = _sumLoss / _length;
			}
			else
			{
				_avgGain = (_avgGain * (_length - 1) + gain) / _length;
				_avgLoss = (_avgLoss * (_length - 1) + loss) / _length;
			}
			if (_avgLoss == 0m)
				return _avgGain == 0m ? 50m : 100m;
			return 100m - 100m / (1m + _avgGain / _avgLoss);
		}
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smoothing1, _smoothing2;
	private readonly StrategyParam<int>[] _periods = new StrategyParam<int>[5];
	private readonly StrategyParam<decimal>[] _factors = new StrategyParam<decimal>[5];
	private readonly StrategyParam<bool> _allowBuy, _allowSell, _closeLong, _closeShort;
	private readonly StrategyParam<decimal> _stopLoss, _takeProfit;

	private RsiState[] _rsi;
	private decimal? _slow, _osma, _previous, _beforePrevious;
	private Order _pending;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Smoothing1 { get => _smoothing1.Value; set => _smoothing1.Value = value; }
	public int Smoothing2 { get => _smoothing2.Value; set => _smoothing2.Value = value; }
	public int RsiPeriod1 { get => _periods[0].Value; set => _periods[0].Value = value; }
	public int RsiPeriod2 { get => _periods[1].Value; set => _periods[1].Value = value; }
	public int RsiPeriod3 { get => _periods[2].Value; set => _periods[2].Value = value; }
	public int RsiPeriod4 { get => _periods[3].Value; set => _periods[3].Value = value; }
	public int RsiPeriod5 { get => _periods[4].Value; set => _periods[4].Value = value; }
	public decimal Factor1 { get => _factors[0].Value; set => _factors[0].Value = value; }
	public decimal Factor2 { get => _factors[1].Value; set => _factors[1].Value = value; }
	public decimal Factor3 { get => _factors[2].Value; set => _factors[2].Value = value; }
	public decimal Factor4 { get => _factors[3].Value; set => _factors[3].Value = value; }
	public decimal Factor5 { get => _factors[4].Value; set => _factors[4].Value = value; }
	public bool AllowBuy { get => _allowBuy.Value; set => _allowBuy.Value = value; }
	public bool AllowSell { get => _allowSell.Value; set => _allowSell.Value = value; }
	public bool CloseLong { get => _closeLong.Value; set => _closeLong.Value = value; }
	public bool CloseShort { get => _closeShort.Value; set => _closeShort.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	public ColorZerolagRsiOsmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Signal candle timeframe", "General");
		_smoothing1 = Param(nameof(Smoothing1), 9).SetGreaterThanZero()
			.SetDisplay("Smoothing 1", "Slow-trend EMA period", "Indicator");
		_smoothing2 = Param(nameof(Smoothing2), 5).SetGreaterThanZero()
			.SetDisplay("Smoothing 2", "OSMA-line EMA period", "Indicator");
		var periods = new[] { 8, 21, 34, 55, 89 };
		for (var i = 0; i < 5; i++)
		{
			_periods[i] = Param("RsiPeriod" + (i + 1), periods[i]).SetGreaterThanZero()
				.SetDisplay("RSI period " + (i + 1), "Wilder RSI length", "Indicator");
			_factors[i] = Param("Factor" + (i + 1), 1m)
				.SetDisplay("RSI factor " + (i + 1), "Weighted component", "Indicator");
		}
		_allowBuy = Param(nameof(AllowBuy), true).SetDisplay("Allow Buy", "Allow long entries", "Trading");
		_allowSell = Param(nameof(AllowSell), true).SetDisplay("Allow Sell", "Allow short entries", "Trading");
		_closeLong = Param(nameof(CloseLong), true).SetDisplay("Close Long", "Allow long exits", "Trading");
		_closeShort = Param(nameof(CloseShort), true).SetDisplay("Close Short", "Allow short exits", "Trading");
		_stopLoss = Param(nameof(StopLoss), 0m).SetNotNegative()
			.SetDisplay("Stop Loss", "Optional price-unit stop", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 0m).SetNotNegative()
			.SetDisplay("Take Profit", "Optional price-unit target", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	private void ResetHistory()
	{
		_rsi = new RsiState[5];
		for (var i = 0; i < 5; i++)
			_rsi[i] = new RsiState(_periods[i].Value);
		_slow = _osma = _previous = _beforePrevious = null;
		_pending = null;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetHistory();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetHistory();
		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = 0m;
		var formed = true;
		for (var i = 0; i < 5; i++)
		{
			var value = _rsi[i].Add(candle.ClosePrice);
			if (value is decimal rsi)
				fast += rsi * _factors[i].Value;
			else
				formed = false;
		}
		if (!formed)
			return;

		var slow = _slow is decimal oldSlow
			? oldSlow + 2m / (Smoothing1 + 1m) * (fast - oldSlow)
			: fast;
		var rawOsma = fast - slow;
		var osma = _osma is decimal previousOsma
			? previousOsma + 2m / (Smoothing2 + 1m) * (rawOsma - previousOsma)
			: rawOsma;
		var turnUp = _beforePrevious is decimal older && _previous is decimal previous
			&& previous < older && osma > previous;
		var turnDown = _beforePrevious is decimal olderDown && _previous is decimal previousDown
			&& previousDown > olderDown && osma < previousDown;

		_beforePrevious = _previous;
		_previous = _osma = osma;
		_slow = slow;

		if (!IsFormedAndOnlineAndAllowTrading()
			|| (_pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		if (turnUp)
		{
			if (Position < 0m && CloseShort)
				_pending = BuyMarket(-Position + (AllowBuy ? Volume : 0m));
			else if (Position == 0m && AllowBuy)
				_pending = BuyMarket(Volume);
		}
		else if (turnDown)
		{
			if (Position > 0m && CloseLong)
				_pending = SellMarket(Position + (AllowSell ? Volume : 0m));
			else if (Position == 0m && AllowSell)
				_pending = SellMarket(Volume);
		}
	}
}
