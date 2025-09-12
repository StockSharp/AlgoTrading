using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Accumulates positions using unmitigated historical lows.
/// Places limit buys at previous day/week/month/year lows during London session
/// if price has not touched the level recently. Closes all positions on new all-time highs.
/// </summary>
public class UnmitigatedLevelsAccumulationStrategy : Strategy
{
	private readonly StrategyParam<int> _maxLookback;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<decimal> _basePdl;
	private readonly StrategyParam<decimal> _basePwl;
	private readonly StrategyParam<decimal> _basePml;
	private readonly StrategyParam<decimal> _basePyl;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _pdl = new decimal[5];
	private readonly bool[] _pdlPlaced = new bool[5];
	private readonly decimal[] _pwl = new decimal[3];
	private readonly bool[] _pwlPlaced = new bool[3];
	private readonly decimal[] _pml = new decimal[2];
	private readonly bool[] _pmlPlaced = new bool[2];
	private decimal _pyl;
	private bool _pylPlaced;

	private readonly Queue<decimal> _recentLows = new();
	private decimal _allTimeHigh;
	private DateTime _currentDay;
	private decimal _initialCapital;

	/// <summary>
	/// Lookback bars to check mitigation.
	/// </summary>
	public int MaxLookback
	{
		get => _maxLookback.Value;
		set => _maxLookback.Value = value;
	}

	/// <summary>
	/// London session start time (UTC).
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// London session end time (UTC).
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Base order size for previous day lows.
	/// </summary>
	public decimal BasePdl
	{
		get => _basePdl.Value;
		set => _basePdl.Value = value;
	}

	/// <summary>
	/// Base order size for previous week lows.
	/// </summary>
	public decimal BasePwl
	{
		get => _basePwl.Value;
		set => _basePwl.Value = value;
	}

	/// <summary>
	/// Base order size for previous month lows.
	/// </summary>
	public decimal BasePml
	{
		get => _basePml.Value;
		set => _basePml.Value = value;
	}

	/// <summary>
	/// Base order size for previous year lows.
	/// </summary>
	public decimal BasePyl
	{
		get => _basePyl.Value;
		set => _basePyl.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="UnmitigatedLevelsAccumulationStrategy"/>.
	/// </summary>
	public UnmitigatedLevelsAccumulationStrategy()
	{
		_maxLookback = Param(nameof(MaxLookback), 50)
			.SetGreaterThanZero()
			.SetDisplay("Max Lookback", "Lookback bars for mitigation", "General");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(9, 0, 0))
			.SetDisplay("Session Start", "London session start", "General");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(17, 0, 0))
			.SetDisplay("Session End", "London session end", "General");

		_basePdl = Param(nameof(BasePdl), 0.1m)
			.SetDisplay("Base PDL", "Base size for previous day lows", "Size");

		_basePwl = Param(nameof(BasePwl), 0.2m)
			.SetDisplay("Base PWL", "Base size for previous week lows", "Size");

		_basePml = Param(nameof(BasePml), 0.4m)
			.SetDisplay("Base PML", "Base size for previous month lows", "Size");

		_basePyl = Param(nameof(BasePyl), 0.8m)
			.SetDisplay("Base PYL", "Base size for previous year low", "Size");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		Array.Clear(_pdl, 0, _pdl.Length);
		Array.Clear(_pdlPlaced, 0, _pdlPlaced.Length);
		Array.Clear(_pwl, 0, _pwl.Length);
		Array.Clear(_pwlPlaced, 0, _pwlPlaced.Length);
		Array.Clear(_pml, 0, _pml.Length);
		Array.Clear(_pmlPlaced, 0, _pmlPlaced.Length);
		_pyl = 0m;
		_pylPlaced = false;
		_recentLows.Clear();
		_allTimeHigh = 0m;
		_currentDay = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_initialCapital = Portfolio.CurrentValue;

		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame()).Bind(ProcessDaily).Start();
		SubscribeCandles(TimeSpan.FromDays(7).TimeFrame()).Bind(ProcessWeekly).Start();
		SubscribeCandles(TimeSpan.FromDays(30).TimeFrame()).Bind(ProcessMonthly).Start();
		SubscribeCandles(TimeSpan.FromDays(365).TimeFrame()).Bind(ProcessYearly).Start();
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		for (var i = _pdl.Length - 1; i > 0; i--)
			_pdl[i] = _pdl[i - 1];
		_pdl[0] = candle.LowPrice;
	}

	private void ProcessWeekly(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		for (var i = _pwl.Length - 1; i > 0; i--)
			_pwl[i] = _pwl[i - 1];
		_pwl[0] = candle.LowPrice;
	}

	private void ProcessMonthly(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		for (var i = _pml.Length - 1; i > 0; i--)
			_pml[i] = _pml[i - 1];
		_pml[0] = candle.LowPrice;
	}

	private void ProcessYearly(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_pyl = candle.LowPrice;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_recentLows.Enqueue(candle.LowPrice);
		while (_recentLows.Count > MaxLookback)
			_recentLows.Dequeue();

		if (candle.HighPrice > _allTimeHigh)
			_allTimeHigh = candle.HighPrice;

		var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
			_currentDay = day;
			Array.Clear(_pdlPlaced, 0, _pdlPlaced.Length);
			Array.Clear(_pwlPlaced, 0, _pwlPlaced.Length);
			Array.Clear(_pmlPlaced, 0, _pmlPlaced.Length);
			_pylPlaced = false;
		}

		var multiplier = 1m;
		if (_initialCapital > 0)
		{
			var capitalMult = Portfolio.CurrentValue / _initialCapital;
			if (capitalMult >= 2m)
			{
				var doublings = (int)Math.Floor(Math.Log((double)capitalMult, 2));
				multiplier = (decimal)Math.Pow(2, doublings);
			}
		}

		var currentPdl = BasePdl * multiplier;
		var currentPwl = BasePwl * multiplier;
		var currentPml = BasePml * multiplier;
		var currentPyl = BasePyl * multiplier;

		var tod = candle.OpenTime.TimeOfDay;
		var inLondon = tod >= SessionStart && tod <= SessionEnd;

		if (inLondon)
		{
			for (var i = 0; i < _pdl.Length; i++)
			{
				if (_pdl[i] > 0 && !_pdlPlaced[i] && !IsLevelMitigated(_pdl[i]))
				{
					BuyLimit(_pdl[i], currentPdl);
					_pdlPlaced[i] = true;
				}
			}

			for (var i = 0; i < _pwl.Length; i++)
			{
				if (_pwl[i] > 0 && !_pwlPlaced[i] && !IsLevelMitigated(_pwl[i]))
				{
					BuyLimit(_pwl[i], currentPwl);
					_pwlPlaced[i] = true;
				}
			}

			for (var i = 0; i < _pml.Length; i++)
			{
				if (_pml[i] > 0 && !_pmlPlaced[i] && !IsLevelMitigated(_pml[i]))
				{
					BuyLimit(_pml[i], currentPml);
					_pmlPlaced[i] = true;
				}
			}

			if (_pyl > 0 && !_pylPlaced && !IsLevelMitigated(_pyl))
			{
				BuyLimit(_pyl, currentPyl);
				_pylPlaced = true;
			}
		}

		if (Position > 0 && candle.HighPrice >= _allTimeHigh)
			SellMarket(Position);
	}

	private bool IsLevelMitigated(decimal level)
	{
		foreach (var low in _recentLows)
		{
			if (low <= level)
				return true;
		}
		return false;
	}
}
