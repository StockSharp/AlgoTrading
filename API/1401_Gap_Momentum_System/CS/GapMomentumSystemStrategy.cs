namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Algo.Indicators;

/// <summary>
/// Strategy using the Gap Momentum System by Perry Kaufman.
/// Buys when the gap momentum signal rises and sells or reverses when it falls.
/// </summary>
public class GapMomentumSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<bool> _longOnly;
	private readonly StrategyParam<DataType> _candleType;

	private GapMomentum _gapMomentum;
	private decimal _prevSignal;

	/// <summary>
	/// Period for gap sums.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Period for signal moving average.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Allow only long trades.
	/// </summary>
	public bool LongOnly
	{
		get => _longOnly.Value;
		set => _longOnly.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="GapMomentumSystemStrategy"/>.
	/// </summary>
	public GapMomentumSystemStrategy()
	{
		_period = Param(nameof(Period), 40)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Gap accumulation period", "Parameters");

		_signalPeriod = Param(nameof(SignalPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "SMA period", "Parameters");

		_longOnly = Param(nameof(LongOnly), true)
			.SetDisplay("Long Only", "Only long trades", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_gapMomentum?.Reset();
		_prevSignal = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_gapMomentum = new GapMomentum
		{
			Period = Period,
			SignalPeriod = SignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_gapMomentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal signal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_gapMomentum.IsFormed)
		{
			_prevSignal = signal;
			return;
		}

		if (signal > _prevSignal)
		{
			if (Position <= 0)
			{
				var vol = Position < 0 && !LongOnly ? Math.Abs(Position) + Volume : Volume;
				BuyMarket(vol);
			}
		}
		else if (signal < _prevSignal)
		{
			if (Position >= 0)
			{
				if (LongOnly)
				{
					if (Position > 0)
						SellMarket(Position);
				}
				else
				{
					var vol = Position > 0 ? Position + Volume : Volume;
					SellMarket(vol);
				}
			}
		}

		_prevSignal = signal;
	}

	private class GapMomentum : Indicator<ICandleMessage>
	{
		public int Period { get; set; } = 40;
		public int SignalPeriod { get; set; } = 20;

		private readonly Queue<decimal> _up = new();
		private readonly Queue<decimal> _dn = new();
		private readonly Queue<decimal> _ratio = new();
		private decimal _sumUp;
		private decimal _sumDn;
		private decimal _sumRatio;
		private decimal? _prevClose;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			var prevClose = _prevClose ?? candle.OpenPrice;
			var gap = candle.OpenPrice - prevClose;
			var up = gap > 0m ? gap : 0m;
			var dn = gap < 0m ? -gap : 0m;

			_sumUp += up;
			_sumDn += dn;
			_up.Enqueue(up);
			_dn.Enqueue(dn);
			if (_up.Count > Period)
				_sumUp -= _up.Dequeue();
			if (_dn.Count > Period)
				_sumDn -= _dn.Dequeue();

			var ratio = _sumDn == 0m ? 1m : 100m * _sumUp / _sumDn;
			_sumRatio += ratio;
			_ratio.Enqueue(ratio);
			if (_ratio.Count > SignalPeriod)
				_sumRatio -= _ratio.Dequeue();

			_prevClose = candle.ClosePrice;

			if (_ratio.Count < SignalPeriod)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			IsFormed = true;
			var signal = _sumRatio / SignalPeriod;
			return new DecimalIndicatorValue(this, signal, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_up.Clear();
			_dn.Clear();
			_ratio.Clear();
			_sumUp = 0m;
			_sumDn = 0m;
			_sumRatio = 0m;
			_prevClose = null;
		}
	}
}
