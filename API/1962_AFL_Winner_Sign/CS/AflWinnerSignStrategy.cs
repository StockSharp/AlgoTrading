using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a smoothed momentum crossover.
/// </summary>
public class AflWinnerSignStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly ExponentialMovingAverage _fast = new();
	private readonly ExponentialMovingAverage _slow = new();
	private decimal _prevK;
	private decimal _prevD;
	private bool _isInitialized;

	/// <summary>
	/// Base period for the oscillator.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Smoothing period for the fast line.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for the slow line.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AflWinnerSignStrategy"/>.
	/// </summary>
	public AflWinnerSignStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Period", "Base period for oscillator calculation", "AFL WinnerSign")
			.SetOptimize(5, 20, 1);

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Smoothing period for %K line", "AFL WinnerSign")
			.SetOptimize(3, 10, 1);

		_dPeriod = Param(nameof(DPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Smoothing period for %D line", "AFL WinnerSign")
			.SetOptimize(3, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_fast.Length = KPeriod;
		_slow.Length = DPeriod;
		_fast.Reset();
		_slow.Reset();
		_prevK = 0m;
		_prevD = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fast.Length = KPeriod;
		_slow.Length = DPeriod;

		var rsi = new RelativeStrengthIndex { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();

		StartProtection(new Unit(2, UnitTypes.Percent), new Unit(2, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var k = _fast.Process(new DecimalIndicatorValue(_fast, momentum, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var d = _slow.Process(new DecimalIndicatorValue(_slow, k, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_fast.IsFormed || !_slow.IsFormed)
			return;

		if (!_isInitialized)
		{
			_prevK = k;
			_prevD = d;
			_isInitialized = true;
			return;
		}

		if (_prevK <= _prevD && k > d && k < 35m && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (_prevK >= _prevD && k < d && k > 65m && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevK = k;
		_prevD = d;
	}
}
