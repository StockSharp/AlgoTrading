namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// TEMA crossover with Supertrend filter strategy.
/// </summary>
public class ThreeKilosBtc15mStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<int> _long2Period;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<Unit> _takeProfit;
	private readonly StrategyParam<Unit> _stopLoss;

	private ExponentialMovingAverage _ema1Short;
	private ExponentialMovingAverage _ema2Short;
	private ExponentialMovingAverage _ema3Short;

	private ExponentialMovingAverage _ema1Long;
	private ExponentialMovingAverage _ema2Long;
	private ExponentialMovingAverage _ema3Long;

	private ExponentialMovingAverage _ema1Long2;
	private ExponentialMovingAverage _ema2Long2;
	private ExponentialMovingAverage _ema3Long2;

	private AverageTrueRange _atr;

	private bool _isSupertrendInit;
	private decimal _up;
	private decimal _dn;
	private bool _uptrend;
	private decimal _prevClose;

	private decimal? _prevTema1;
	private decimal? _prevTema2;

	public ThreeKilosBtc15mStrategy()
	{
	_candleType =
		Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type",
				"Candle type for strategy calculation", "General");

	_shortPeriod = Param(nameof(ShortPeriod), 30)
			   .SetDisplay("Short TEMA Period",
					   "Period for short TEMA", "Indicators");

	_longPeriod = Param(nameof(LongPeriod), 50)
			  .SetDisplay("Middle TEMA Period",
					  "Period for middle TEMA", "Indicators");

	_long2Period = Param(nameof(Long2Period), 140)
			   .SetDisplay("Long TEMA Period",
					   "Period for long TEMA", "Indicators");

	_atrLength = Param(nameof(AtrLength), 10)
			 .SetDisplay("ATR Length", "ATR length for Supertrend",
					 "Supertrend");

	_multiplier =
		Param(nameof(Multiplier), 2m)
		.SetDisplay("Multiplier", "ATR multiplier for Supertrend",
				"Supertrend");

	_takeProfit = Param(nameof(TakeProfit), new Unit(1m, UnitTypes.Percent))
			  .SetDisplay("Take Profit (%)",
					  "Take profit in percent", "Protection");

	_stopLoss = Param(nameof(StopLoss), new Unit(1m, UnitTypes.Percent))
			.SetDisplay("Stop Loss (%)", "Stop loss in percent",
					"Protection");
	}

	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}
	public int ShortPeriod
	{
	get => _shortPeriod.Value;
	set => _shortPeriod.Value = value;
	}
	public int LongPeriod
	{
	get => _longPeriod.Value;
	set => _longPeriod.Value = value;
	}
	public int Long2Period
	{
	get => _long2Period.Value;
	set => _long2Period.Value = value;
	}
	public int AtrLength
	{
	get => _atrLength.Value;
	set => _atrLength.Value = value;
	}
	public decimal Multiplier
	{
	get => _multiplier.Value;
	set => _multiplier.Value = value;
	}
	public Unit TakeProfit
	{
	get => _takeProfit.Value;
	set => _takeProfit.Value = value;
	}
	public Unit StopLoss
	{
	get => _stopLoss.Value;
	set => _stopLoss.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_ema1Short = null;
	_ema2Short = null;
	_ema3Short = null;

	_ema1Long = null;
	_ema2Long = null;
	_ema3Long = null;

	_ema1Long2 = null;
	_ema2Long2 = null;
	_ema3Long2 = null;

	_atr = null;

	_isSupertrendInit = false;
	_up = _dn = 0m;
	_uptrend = true;
	_prevClose = 0m;

	_prevTema1 = null;
	_prevTema2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_ema1Short = new() { Length = ShortPeriod };
	_ema2Short = new() { Length = ShortPeriod };
	_ema3Short = new() { Length = ShortPeriod };

	_ema1Long = new() { Length = LongPeriod };
	_ema2Long = new() { Length = LongPeriod };
	_ema3Long = new() { Length = LongPeriod };

	_ema1Long2 = new() { Length = Long2Period };
	_ema2Long2 = new() { Length = Long2Period };
	_ema3Long2 = new() { Length = Long2Period };

	_atr = new AverageTrueRange { Length = AtrLength };

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(_atr, ProcessCandle).Start();

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawOwnTrades(area);
	}

	StartProtection(TakeProfit, StopLoss);
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
	if (candle.State != CandleStates.Finished)
		return;

	var time = candle.ServerTime;
	var isFinal = true;

	var tema1 = CalcTema(_ema1Short, _ema2Short, _ema3Short,
				 candle.HighPrice, time, isFinal);
	var tema2 = CalcTema(_ema1Long, _ema2Long, _ema3Long, candle.LowPrice,
				 time, isFinal);
	var tema3 = CalcTema(_ema1Long2, _ema2Long2, _ema3Long2,
				 candle.ClosePrice, time, isFinal);

	if (!_ema3Short.IsFormed || !_ema3Long.IsFormed || !_ema3Long2.IsFormed)
	{
		_prevTema1 = tema1;
		_prevTema2 = tema2;
		_prevClose = candle.ClosePrice;
		return;
	}

	var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;

	if (!_isSupertrendInit)
	{
		_up = hl2 - Multiplier * atr;
		_dn = hl2 + Multiplier * atr;
		_uptrend = true;
		_isSupertrendInit = true;
	}
	else
	{
		var prevUp = _up;
		var prevDn = _dn;
		var prevTrend = _uptrend;

		_up = prevTrend ? Math.Max(hl2 - Multiplier * atr, prevUp)
				: hl2 - Multiplier * atr;
		_dn = prevTrend ? hl2 + Multiplier * atr
				: Math.Min(hl2 + Multiplier * atr, prevDn);
		_uptrend = _prevClose > prevDn	 ? true
			   : _prevClose < prevUp ? false
						 : prevTrend;
	}

	var longCross = _prevTema2.HasValue && _prevTema1.HasValue &&
			_prevTema2 <= _prevTema1 && tema2 > tema1;
	var shortCross = _prevTema1.HasValue && _prevTema2.HasValue &&
			 _prevTema1 <= _prevTema2 && tema1 > tema2;

	if (longCross && tema2 > tema3 && _uptrend && Position <= 0)
	{
		BuyMarket(Volume + Math.Abs(Position));
	}
	else if (shortCross && tema2 < tema3 && !_uptrend && Position >= 0)
	{
		SellMarket(Volume + Math.Abs(Position));
	}

	_prevTema1 = tema1;
	_prevTema2 = tema2;
	_prevClose = candle.ClosePrice;
	}

	private static decimal CalcTema(ExponentialMovingAverage ema1,
					ExponentialMovingAverage ema2,
					ExponentialMovingAverage ema3,
					decimal price, DateTimeOffset time,
					bool isFinal)
	{
	var e1 = ema1.Process(price, time, isFinal).ToDecimal();
	var e2 = ema2.Process(e1, time, isFinal).ToDecimal();
	var e3 = ema3.Process(e2, time, isFinal).ToDecimal();
	return 3m * (e1 - e2) + e3;
	}
}
