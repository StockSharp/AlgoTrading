namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Color Step XCCX indicator.
/// </summary>
public class ColorStepXccxStrategy : Strategy
{
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _mPeriod;
	private readonly StrategyParam<int> _stepFast;
	private readonly StrategyParam<int> _stepSlow;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<DataType> _candleType;

	private readonly ColorStepXccxCalculator _calc = new();

	private decimal? _mPlusPrev1;
	private decimal? _mMinusPrev1;
	private decimal? _mPlusPrev2;
	private decimal? _mMinusPrev2;

	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	public int MPeriod
	{
		get => _mPeriod.Value;
		set => _mPeriod.Value = value;
	}

	public int StepSizeFast
	{
		get => _stepFast.Value;
		set => _stepFast.Value = value;
	}

	public int StepSizeSlow
	{
		get => _stepSlow.Value;
		set => _stepSlow.Value = value;
	}

	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ColorStepXccxStrategy()
	{
		_dPeriod = Param(nameof(DPeriod), 30)
			.SetDisplay("Price MA", "Period of price smoothing", "Indicator");

		_mPeriod = Param(nameof(MPeriod), 7)
			.SetDisplay("Deviation MA", "Period of deviation smoothing", "Indicator");

		_stepFast = Param(nameof(StepSizeFast), 5)
			.SetDisplay("Fast Step", "Fast step size", "Indicator");

		_stepSlow = Param(nameof(StepSizeSlow), 30)
			.SetDisplay("Slow Step", "Slow step size", "Indicator");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entry", string.Empty, "Trading");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entry", string.Empty, "Trading");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", string.Empty, "Trading");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", string.Empty, "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_mPlusPrev1 = _mMinusPrev1 = _mPlusPrev2 = _mMinusPrev2 = null;
		_calc.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_calc.Configure(DPeriod, MPeriod, StepSizeFast, StepSizeSlow);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		var (mPlus, mMinus) = _calc.Process(price);
		if (mPlus is null || mMinus is null)
			return;

		_mPlusPrev2 = _mPlusPrev1;
		_mMinusPrev2 = _mMinusPrev1;
		_mPlusPrev1 = mPlus;
		_mMinusPrev1 = mMinus;

		if (_mPlusPrev2 is null || _mMinusPrev2 is null || _mPlusPrev1 is null || _mMinusPrev1 is null)
			return;

		if (_mPlusPrev2 > _mMinusPrev2)
		{
			if (AllowShortExit && Position < 0)
				ClosePosition();

			if (AllowLongEntry && _mPlusPrev1 <= _mMinusPrev1 && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
				BuyMarket();
		}
		else if (_mPlusPrev2 < _mMinusPrev2)
		{
			if (AllowLongExit && Position > 0)
				ClosePosition();

			if (AllowShortEntry && _mPlusPrev1 >= _mMinusPrev1 && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
				SellMarket();
		}
	}

	private sealed class ColorStepXccxCalculator
	{
		private ExponentialMovingAverage _priceMa;
		private ExponentialMovingAverage _devUpMa;
		private ExponentialMovingAverage _devDnMa;

		private decimal _fmin1;
		private decimal _fmax1;
		private decimal _smin1;
		private decimal _smax1;
		private int _ftrend;
		private int _strend;

		public void Configure(int dPeriod, int mPeriod, int stepFast, int stepSlow)
		{
			_priceMa = new() { Length = dPeriod };
			_devUpMa = new() { Length = mPeriod };
			_devDnMa = new() { Length = mPeriod };
			StepFast = stepFast;
			StepSlow = stepSlow;

			Reset();
		}

		public int StepFast { get; private set; }
		public int StepSlow { get; private set; }

		public (decimal? mPlus, decimal? mMinus) Process(decimal price)
		{
			var maValue = _priceMa.Process(price);
			if (!_priceMa.IsFormed)
				return default;

			var up = price - maValue.Value;
			var dn = Math.Abs(up);

			var upMa = _devUpMa.Process(up).Value;
			var dnMa = _devDnMa.Process(dn).Value;

			if (!_devUpMa.IsFormed || !_devDnMa.IsFormed || dnMa == 0m)
				return default;

			var xccx = 100m * upMa / dnMa;

			var fmax0 = xccx + 2m * StepFast;
			var fmin0 = xccx - 2m * StepFast;

			if (xccx > _fmax1) _ftrend = 1;
			if (xccx < _fmin1) _ftrend = -1;

			if (_ftrend > 0 && fmin0 < _fmin1) fmin0 = _fmin1;
			if (_ftrend < 0 && fmax0 > _fmax1) fmax0 = _fmax1;

			var smax0 = xccx + 2m * StepSlow;
			var smin0 = xccx - 2m * StepSlow;

			if (xccx > _smax1) _strend = 1;
			if (xccx < _smin1) _strend = -1;

			if (_strend > 0 && smin0 < _smin1) smin0 = _smin1;
			if (_strend < 0 && smax0 > _smax1) smax0 = _smax1;

			decimal? mPlus = _ftrend > 0 ? fmin0 + StepFast : _ftrend < 0 ? fmax0 - StepFast : null;
			decimal? mMinus = _strend > 0 ? smin0 + StepSlow : _strend < 0 ? smax0 - StepSlow : null;

			_fmin1 = fmin0;
			_fmax1 = fmax0;
			_smin1 = smin0;
			_smax1 = smax0;

			return (mPlus, mMinus);
		}

		public void Reset()
		{
			_fmin1 = _smin1 = decimal.MaxValue;
			_fmax1 = _smax1 = decimal.MinValue;
			_ftrend = _strend = 0;

			_priceMa?.Reset();
			_devUpMa?.Reset();
			_devDnMa?.Reset();
		}
	}
}
