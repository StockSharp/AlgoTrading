using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple Stochastic strategy operating on three timeframes.
/// Opens positions when the fast stochastic crosses the signal line on the smallest timeframe while higher timeframes
/// confirm the trend.
/// </summary>
public class Exp3StoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType1;
	private readonly StrategyParam<DataType> _candleType2;
	private readonly StrategyParam<DataType> _candleType3;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose1;
	private readonly StrategyParam<bool> _sellPosClose1;
	private readonly StrategyParam<bool> _buyPosClose2;
	private readonly StrategyParam<bool> _sellPosClose2;
	private readonly StrategyParam<bool> _buyPosClose3;
	private readonly StrategyParam<bool> _sellPosClose3;

	private StochasticOscillator _stoch1 = null!;
	private StochasticOscillator _stoch2 = null!;
	private StochasticOscillator _stoch3 = null!;

	private int _trend1;
	private int _trend2;
	private int _trend3;

	private decimal? _prevK3;
	private decimal? _prevD3;

	private bool _buyOpenSignal;
	private bool _sellOpenSignal;
	private bool _buyCloseSignal;
	private bool _sellCloseSignal;

	/// <summary>
	/// First timeframe for stochastic indicator.
	/// </summary>
	public DataType CandleType1
	{
		get => _candleType1.Value;
		set => _candleType1.Value = value;
	}

	/// <summary>
	/// Second timeframe for stochastic indicator.
	/// </summary>
	public DataType CandleType2
	{
		get => _candleType2.Value;
		set => _candleType2.Value = value;
	}

	/// <summary>
	/// Third timeframe for stochastic indicator.
	/// </summary>
	public DataType CandleType3
	{
		get => _candleType3.Value;
		set => _candleType3.Value = value;
	}

	/// <summary>
	/// %K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// %D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing factor for %K line.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Close shorts when first timeframe indicates uptrend.
	/// </summary>
	public bool SellPosClose1
	{
		get => _sellPosClose1.Value;
		set => _sellPosClose1.Value = value;
	}

	/// <summary>
	/// Close longs when first timeframe indicates downtrend.
	/// </summary>
	public bool BuyPosClose1
	{
		get => _buyPosClose1.Value;
		set => _buyPosClose1.Value = value;
	}

	/// <summary>
	/// Close shorts when second timeframe indicates uptrend.
	/// </summary>
	public bool SellPosClose2
	{
		get => _sellPosClose2.Value;
		set => _sellPosClose2.Value = value;
	}

	/// <summary>
	/// Close longs when second timeframe indicates downtrend.
	/// </summary>
	public bool BuyPosClose2
	{
		get => _buyPosClose2.Value;
		set => _buyPosClose2.Value = value;
	}

	/// <summary>
	/// Close shorts when third timeframe indicates uptrend.
	/// </summary>
	public bool SellPosClose3
	{
		get => _sellPosClose3.Value;
		set => _sellPosClose3.Value = value;
	}

	/// <summary>
	/// Close longs when third timeframe indicates downtrend.
	/// </summary>
	public bool BuyPosClose3
	{
		get => _buyPosClose3.Value;
		set => _buyPosClose3.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public Exp3StoStrategy()
	{
		_candleType1 = Param(nameof(CandleType1), TimeSpan.FromMinutes(30).TimeFrame())
						   .SetDisplay("Timeframe 1", "Higher timeframe", "General");

		_candleType2 = Param(nameof(CandleType2), TimeSpan.FromMinutes(15).TimeFrame())
						   .SetDisplay("Timeframe 2", "Middle timeframe", "General");

		_candleType3 = Param(nameof(CandleType3), TimeSpan.FromMinutes(5).TimeFrame())
						   .SetDisplay("Timeframe 3", "Lower timeframe", "General");

		_kPeriod = Param(nameof(KPeriod), 5).SetGreaterThanZero().SetDisplay("%K Period", "Length of %K", "Stochastic");

		_dPeriod = Param(nameof(DPeriod), 3).SetGreaterThanZero().SetDisplay("%D Period", "Length of %D", "Stochastic");

		_slowing = Param(nameof(Slowing), 3).SetGreaterThanZero().SetDisplay("Slowing", "%K smoothing", "Stochastic");

		_buyPosOpen =
			Param(nameof(BuyPosOpen), true).SetDisplay("Enable Long", "Allow opening long positions", "Signals");

		_sellPosOpen =
			Param(nameof(SellPosOpen), true).SetDisplay("Enable Short", "Allow opening short positions", "Signals");

		_buyPosClose1 =
			Param(nameof(BuyPosClose1), true).SetDisplay("Close Long TF1", "Close longs if TF1 down", "Signals");
		_sellPosClose1 =
			Param(nameof(SellPosClose1), true).SetDisplay("Close Short TF1", "Close shorts if TF1 up", "Signals");

		_buyPosClose2 =
			Param(nameof(BuyPosClose2), true).SetDisplay("Close Long TF2", "Close longs if TF2 down", "Signals");
		_sellPosClose2 =
			Param(nameof(SellPosClose2), true).SetDisplay("Close Short TF2", "Close shorts if TF2 up", "Signals");

		_buyPosClose3 =
			Param(nameof(BuyPosClose3), true).SetDisplay("Close Long TF3", "Close longs if TF3 down", "Signals");
		_sellPosClose3 =
			Param(nameof(SellPosClose3), true).SetDisplay("Close Short TF3", "Close shorts if TF3 up", "Signals");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType1), (Security, CandleType2), (Security, CandleType3)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_stoch1 = null!;
		_stoch2 = null!;
		_stoch3 = null!;
		_trend1 = _trend2 = _trend3 = 0;
		_prevK3 = _prevD3 = null;
		_buyOpenSignal = _sellOpenSignal = false;
		_buyCloseSignal = _sellCloseSignal = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stoch1 = new StochasticOscillator { KPeriod = KPeriod, DPeriod = DPeriod, Smooth = Slowing };
		_stoch2 = new StochasticOscillator { KPeriod = KPeriod, DPeriod = DPeriod, Smooth = Slowing };
		_stoch3 = new StochasticOscillator { KPeriod = KPeriod, DPeriod = DPeriod, Smooth = Slowing };

		var sub1 = SubscribeCandles(CandleType1);
		sub1.BindEx(_stoch1, ProcessTf1).Start();

		var sub2 = SubscribeCandles(CandleType2);
		sub2.BindEx(_stoch2, ProcessTf2).Start();

		var sub3 = SubscribeCandles(CandleType3);
		sub3.BindEx(_stoch3, ProcessTf3).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub3);
			DrawIndicator(area, _stoch3);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTf1(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		_trend1 = 0;
		if (k > d && BuyPosOpen)
			_trend1 = 1;
		else if (k < d && SellPosOpen)
			_trend1 = -1;

		UpdateSignals();
	}

	private void ProcessTf2(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		_trend2 = 0;
		if (k > d && BuyPosOpen)
			_trend2 = 1;
		else if (k < d && SellPosOpen)
			_trend2 = -1;

		UpdateSignals();
	}

	private void ProcessTf3(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		if (_prevK3 is not decimal pk || _prevD3 is not decimal pd)
		{
			_prevK3 = k;
			_prevD3 = d;
			return;
		}

		_trend3 = 0;
		if (pk > pd)
		{
			_trend3 = 1;
			if (BuyPosOpen && k <= d && _trend1 > 0 && _trend2 > 0)
				_buyOpenSignal = true;
		}
		else if (pk < pd)
		{
			_trend3 = -1;
			if (SellPosOpen && k >= d && _trend1 < 0 && _trend2 < 0)
				_sellOpenSignal = true;
		}

		_prevK3 = k;
		_prevD3 = d;

		UpdateSignals();
	}

	private void UpdateSignals()
	{
		_buyCloseSignal = false;
		_sellCloseSignal = false;

		if (_trend1 > 0 && SellPosClose1)
			_sellCloseSignal = true;
		if (_trend1 < 0 && BuyPosClose1)
			_buyCloseSignal = true;
		if (_trend2 > 0 && SellPosClose2)
			_sellCloseSignal = true;
		if (_trend2 < 0 && BuyPosClose2)
			_buyCloseSignal = true;
		if (_trend3 > 0 && SellPosClose3)
			_sellCloseSignal = true;
		if (_trend3 < 0 && BuyPosClose3)
			_buyCloseSignal = true;

		ExecuteTrades();
	}

	private void ExecuteTrades()
	{
		if (_buyCloseSignal && Position > 0)
		{
			SellMarket(Position);
		}

		if (_sellCloseSignal && Position < 0)
		{
			BuyMarket(-Position);
		}

		_buyCloseSignal = false;
		_sellCloseSignal = false;

		if (_buyOpenSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_buyOpenSignal = false;
		}

		if (_sellOpenSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_sellOpenSignal = false;
		}
	}
}
