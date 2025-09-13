using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on BrakeExp indicator signals.
/// </summary>
public class BrakeExpChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _a;
	private readonly StrategyParam<decimal> _b;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public decimal A { get => _a.Value; set => _a.Value = value; }
	public decimal B { get => _b.Value; set => _b.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	public BrakeExpChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume", "General");
		_a = Param(nameof(A), 3m)
			.SetDisplay("A", "BrakeExp parameter A", "Indicator");
		_b = Param(nameof(B), 1m)
			.SetDisplay("B", "BrakeExp parameter B", "Indicator");
		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");
		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");
		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Buy Close", "Allow closing short positions", "Trading");
		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Sell Close", "Allow closing long positions", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var indicator = new BrakeExp { A = A, B = B };
		SubscribeCandles(CandleType)
			.BindEx(indicator, Process)
			.Start();
	}

	private void Process(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var v = (BrakeExpValue)value;

		if (v.BuySignal > 0m)
		{
			if (SellClose && Position < 0)
				BuyMarket(-Position);
			if (BuyOpen)
				BuyMarket(Volume);
		}
		else if (v.UpTrend > 0m && SellClose && Position < 0)
		{
			BuyMarket(-Position);
		}

		if (v.SellSignal > 0m)
		{
			if (BuyClose && Position > 0)
				SellMarket(Position);
			if (SellOpen)
				SellMarket(Volume);
		}
		else if (v.DownTrend > 0m && BuyClose && Position > 0)
		{
			SellMarket(Position);
		}
	}

	private class BrakeExp : BaseIndicator<decimal>
	{
		public decimal A { get; set; } = 3m;
		public decimal B { get; set; } = 1m;

		private bool _isLong = true;
		private bool _init;
		private decimal _max = decimal.MinValue;
		private decimal _min = decimal.MaxValue;
		private decimal _begin;
		private decimal _prevUp;
		private decimal _prevDn;
		private int _bar;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
				return new BrakeExpValue(this, input, default, default, default, default);

			if (!_init)
			{
				_begin = candle.LowPrice;
				_max = decimal.MinValue;
				_min = decimal.MaxValue;
				_isLong = true;
				_prevUp = 0m;
				_prevDn = 0m;
				_bar = 0;
				_init = true;
			}

			_max = Math.Max(_max, candle.HighPrice);
			_min = Math.Min(_min, candle.LowPrice);

			var exp = (decimal)Math.Exp((double)(_bar * (A * 0.1m))) - 1m;
			exp *= B;

			var value = _isLong ? _begin + exp : _begin - exp;

			if (_isLong && value > candle.LowPrice)
			{
				_isLong = false;
				_begin = _max;
				value = _begin;
				_bar = 0;
				_max = decimal.MinValue;
				_min = decimal.MaxValue;
			}
			else if (!_isLong && value < candle.HighPrice)
			{
				_isLong = true;
				_begin = _min;
				value = _begin;
				_bar = 0;
				_max = decimal.MinValue;
				_min = decimal.MaxValue;
			}

			decimal up = 0m, dn = 0m;

			if (_isLong)
				up = value;
			else
				dn = value;

			decimal buy = 0m, sell = 0m;

			if (_prevUp > 0m && dn > 0m)
				buy = dn;

			if (_prevDn > 0m && up > 0m)
				sell = up;

			_prevUp = up;
			_prevDn = dn;
			_bar++;

			IsFormed = true;
			return new BrakeExpValue(this, input, up, dn, buy, sell);
		}

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();
			_isLong = true;
			_init = false;
			_max = decimal.MinValue;
			_min = decimal.MaxValue;
			_begin = 0m;
			_prevUp = 0m;
			_prevDn = 0m;
			_bar = 0;
		}
	}

	private class BrakeExpValue : ComplexIndicatorValue
	{
		public BrakeExpValue(IIndicator indicator, IIndicatorValue input, decimal upTrend, decimal downTrend, decimal buySignal, decimal sellSignal)
			: base(indicator, input,
				(nameof(UpTrend), upTrend),
				(nameof(DownTrend), downTrend),
				(nameof(BuySignal), buySignal),
				(nameof(SellSignal), sellSignal))
		{
		}

		public decimal UpTrend => (decimal)GetValue(nameof(UpTrend));
		public decimal DownTrend => (decimal)GetValue(nameof(DownTrend));
		public decimal BuySignal => (decimal)GetValue(nameof(BuySignal));
		public decimal SellSignal => (decimal)GetValue(nameof(SellSignal));
	}
}