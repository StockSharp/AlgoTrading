using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>Five weighted TRIX components crossing a smoothed signal line.</summary>
public class ColorZerolagTrixStrategy : Strategy
{
	private sealed class TrixState
	{
		private readonly int _period;
		private readonly decimal _alpha;
		private decimal _first;
		private decimal _second;
		private decimal _third;
		private decimal _previousThird;
		private int _samples;

		public TrixState(int period)
		{
			_period = period;
			_alpha = 2m / (period + 1m);
		}

		public decimal? Add(decimal close)
		{
			if (_samples == 0)
			{
				_first = _second = _third = close;
				_previousThird = _third;
				_samples = 1;
				return null;
			}

			_first += _alpha * (close - _first);
			_second += _alpha * (_first - _second);
			_third += _alpha * (_second - _third);
			_samples++;
			var value = _previousThird == 0m ? 0m
				: 100m * (_third - _previousThird) / _previousThird;
			_previousThird = _third;
			// Each EMA stage needs history: do not trade during triple-EMA warmup.
			return _samples >= 3 * _period + 1 ? value : null;
		}
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smoothing;
	private readonly StrategyParam<int>[] _periods = new StrategyParam<int>[5];
	private readonly StrategyParam<decimal>[] _factors = new StrategyParam<decimal>[5];
	private readonly StrategyParam<bool> _buyOpen, _buyClose, _sellOpen, _sellClose;

	private TrixState[] _trix;
	private decimal? _slow, _previousFast, _previousSlow;
	private Order _pending;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Smoothing { get => _smoothing.Value; set => _smoothing.Value = value; }
	public int Period1 { get => _periods[0].Value; set => _periods[0].Value = value; }
	public int Period2 { get => _periods[1].Value; set => _periods[1].Value = value; }
	public int Period3 { get => _periods[2].Value; set => _periods[2].Value = value; }
	public int Period4 { get => _periods[3].Value; set => _periods[3].Value = value; }
	public int Period5 { get => _periods[4].Value; set => _periods[4].Value = value; }
	public decimal Factor1 { get => _factors[0].Value; set => _factors[0].Value = value; }
	public decimal Factor2 { get => _factors[1].Value; set => _factors[1].Value = value; }
	public decimal Factor3 { get => _factors[2].Value; set => _factors[2].Value = value; }
	public decimal Factor4 { get => _factors[3].Value; set => _factors[3].Value = value; }
	public decimal Factor5 { get => _factors[4].Value; set => _factors[4].Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	public ColorZerolagTrixStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Signal candle timeframe", "General");
		_smoothing = Param(nameof(Smoothing), 9).SetGreaterThanZero()
			.SetDisplay("Smoothing", "Slow-line EMA period", "Indicator");
		var periods = new[] { 5, 8, 13, 21, 34 };
		for (var i = 0; i < 5; i++)
		{
			_periods[i] = Param("Period" + (i + 1), periods[i]).SetGreaterThanZero()
				.SetDisplay("TRIX period " + (i + 1), "Triple EMA period", "Indicator");
			_factors[i] = Param("Factor" + (i + 1), 1m)
				.SetDisplay("TRIX factor " + (i + 1), "Component weight", "Indicator");
		}
		_buyOpen = Param(nameof(BuyOpen), true).SetDisplay("Buy Open", "Enable long entries", "Trading");
		_buyClose = Param(nameof(BuyClose), true).SetDisplay("Buy Close", "Enable long exits", "Trading");
		_sellOpen = Param(nameof(SellOpen), true).SetDisplay("Sell Open", "Enable short entries", "Trading");
		_sellClose = Param(nameof(SellClose), true).SetDisplay("Sell Close", "Enable short exits", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	private void ResetHistory()
	{
		_trix = new TrixState[5];
		for (var i = 0; i < 5; i++)
			_trix[i] = new TrixState(_periods[i].Value);
		_slow = _previousFast = _previousSlow = null;
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
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = 0m;
		var formed = true;
		for (var i = 0; i < _trix.Length; i++)
		{
			var value = _trix[i].Add(candle.ClosePrice);
			if (value is decimal trix)
				fast += trix * _factors[i].Value;
			else
				formed = false;
		}
		if (!formed)
			return;

		var slow = _slow is decimal previous
			? previous + 2m / (Smoothing + 1m) * (fast - previous)
			: fast;
		var buy = _previousFast is decimal oldFast && _previousSlow is decimal oldSlow
			&& oldFast > oldSlow && fast < slow;
		var sell = _previousFast is decimal oldFast2 && _previousSlow is decimal oldSlow2
			&& oldFast2 < oldSlow2 && fast > slow;

		_previousFast = fast;
		_previousSlow = _slow = slow;
		if (!IsFormedAndOnlineAndAllowTrading()
			|| (_pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		if (buy)
		{
			if (Position < 0m && SellClose)
				_pending = BuyMarket(-Position + (BuyOpen ? Volume : 0m));
			else if (Position == 0m && BuyOpen)
				_pending = BuyMarket(Volume);
		}
		else if (sell)
		{
			if (Position > 0m && BuyClose)
				_pending = SellMarket(Position + (SellOpen ? Volume : 0m));
			else if (Position == 0m && SellOpen)
				_pending = SellMarket(Volume);
		}
	}
}
