using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the ColorMETRO Williams %R indicator.
/// It trades when the fast step line crosses the slow step line.
/// </summary>
public class ColorMetroWprStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<int> _fastStep;
	private readonly StrategyParam<int> _slowStep;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	// Indicator instance
	private WilliamsR _wpr;

	// Previous state for step calculations
	private decimal _fMinPrev;
	private decimal _fMaxPrev;
	private decimal _sMinPrev;
	private decimal _sMaxPrev;
	private int _fTrend;
	private int _sTrend;

	// Previous and current step values
	private decimal _prevMPlus;
	private decimal _prevMMinus;
	private decimal _currMPlus;
	private decimal _currMMinus;
	private bool _isFirstValue;

	/// <summary>
	/// Period for Williams %R.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Step size for the fast line.
	/// </summary>
	public int FastStep
	{
		get => _fastStep.Value;
		set => _fastStep.Value = value;
	}

	/// <summary>
	/// Step size for the slow line.
	/// </summary>
	public int SlowStep
	{
		get => _slowStep.Value;
		set => _slowStep.Value = value;
	}

	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorMetroWprStrategy"/>.
	/// </summary>
	public ColorMetroWprStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 7)
			.SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_fastStep = Param(nameof(FastStep), 5)
			.SetDisplay("Fast Step", "Step size for fast line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_slowStep = Param(nameof(SlowStep), 15)
			.SetDisplay("Slow Step", "Step size for slow line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 1);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take Profit (%)", "Take profit as percentage", "Risk parameters")
			.SetCanOptimize(true)
			.SetOptimize(2m, 6m, 1m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss (%)", "Stop loss as percentage", "Risk parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
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

		_fMinPrev = decimal.MaxValue;
		_fMaxPrev = decimal.MinValue;
		_sMinPrev = decimal.MaxValue;
		_sMaxPrev = decimal.MinValue;
		_fTrend = 0;
		_sTrend = 0;
		_prevMPlus = 0m;
		_prevMMinus = 0m;
		_currMPlus = 0m;
		_currMMinus = 0m;
		_isFirstValue = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_wpr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _wpr);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		// Use only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var wpr = wprValue + 100m;

		var fmax0 = wpr + 2m * FastStep;
		var fmin0 = wpr - 2m * FastStep;

		if (wpr > _fMaxPrev)
			_fTrend = 1;
		if (wpr < _fMinPrev)
			_fTrend = -1;

		if (_fTrend > 0 && fmin0 < _fMinPrev)
			fmin0 = _fMinPrev;
		if (_fTrend < 0 && fmax0 > _fMaxPrev)
			fmax0 = _fMaxPrev;

		var smax0 = wpr + 2m * SlowStep;
		var smin0 = wpr - 2m * SlowStep;

		if (wpr > _sMaxPrev)
			_sTrend = 1;
		if (wpr < _sMinPrev)
			_sTrend = -1;

		if (_sTrend > 0 && smin0 < _sMinPrev)
			smin0 = _sMinPrev;
		if (_sTrend < 0 && smax0 > _sMaxPrev)
			smax0 = _sMaxPrev;

		var mPlus = _fTrend > 0 ? fmin0 + FastStep : fmax0 - FastStep;
		var mMinus = _sTrend > 0 ? smin0 + SlowStep : smax0 - SlowStep;

		_fMinPrev = fmin0;
		_fMaxPrev = fmax0;
		_sMinPrev = smin0;
		_sMaxPrev = smax0;

		if (_isFirstValue)
		{
			_prevMPlus = mPlus;
			_prevMMinus = mMinus;
			_currMPlus = mPlus;
			_currMMinus = mMinus;
			_isFirstValue = false;
			return;
		}

		_prevMPlus = _currMPlus;
		_prevMMinus = _currMMinus;
		_currMPlus = mPlus;
		_currMMinus = mMinus;

		var prevFastAboveSlow = _prevMPlus > _prevMMinus;
		var prevFastBelowSlow = _prevMPlus < _prevMMinus;

		if (prevFastAboveSlow)
		{
			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo("Exit short: fast line above slow line.");
			}

			if (_currMPlus <= _currMMinus && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Buy signal: fast line crossed below slow line. MPlus: {_prevMPlus:F5}->{_currMPlus:F5}, MMinus: {_prevMMinus:F5}->{_currMMinus:F5}");
			}
		}
		else if (prevFastBelowSlow)
		{
			if (Position > 0)
			{
				SellMarket(Position);
				LogInfo("Exit long: fast line below slow line.");
			}

			if (_currMPlus >= _currMMinus && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Sell signal: fast line crossed above slow line. MPlus: {_prevMPlus:F5}->{_currMPlus:F5}, MMinus: {_prevMMinus:F5}->{_currMMinus:F5}");
			}
		}
	}
}