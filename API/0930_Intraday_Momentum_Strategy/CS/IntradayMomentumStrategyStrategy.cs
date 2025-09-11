using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Intraday momentum strategy using EMA crossover, RSI and VWAP.
/// </summary>
public class IntradayMomentumStrategyStrategy : Strategy
{
	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<int> _emaSlowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<decimal> _stopLossPerc;
	private readonly StrategyParam<decimal> _takeProfitPerc;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private RelativeStrengthIndex _rsi;
	private VWAP _vwap;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _prevSet;
	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// Fast EMA period length.
	/// </summary>
	public int EmaFastLength { get => _emaFastLength.Value; set => _emaFastLength.Value = value; }

	/// <summary>
	/// Slow EMA period length.
	/// </summary>
	public int EmaSlowLength { get => _emaSlowLength.Value; set => _emaSlowLength.Value = value; }

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPerc { get => _stopLossPerc.Value; set => _stopLossPerc.Value = value; }

	/// <summary>
	/// Take-profit percentage.
	/// </summary>
	public decimal TakeProfitPerc { get => _takeProfitPerc.Value; set => _takeProfitPerc.Value = value; }

	/// <summary>
	/// Session start hour.
	/// </summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }

	/// <summary>
	/// Session start minute.
	/// </summary>
	public int StartMinute { get => _startMinute.Value; set => _startMinute.Value = value; }

	/// <summary>
	/// Session end hour.
	/// </summary>
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }

	/// <summary>
	/// Session end minute.
	/// </summary>
	public int EndMinute { get => _endMinute.Value; set => _endMinute.Value = value; }

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public IntradayMomentumStrategyStrategy()
	{
		_emaFastLength = Param(nameof(EmaFastLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Period for fast EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_emaSlowLength = Param(nameof(EmaSlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Period for slow EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 1);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 1);

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
			.SetRange(0, 100)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60, 90, 5);

		_rsiOversold = Param(nameof(RsiOversold), 30)
			.SetRange(0, 100)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_stopLossPerc = Param(nameof(StopLossPerc), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_takeProfitPerc = Param(nameof(TakeProfitPerc), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 0.5m);

		_startHour = Param(nameof(StartHour), 9)
			.SetRange(0, 23)
			.SetDisplay("Session Start Hour", "Trading session start hour", "Session");

		_startMinute = Param(nameof(StartMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("Session Start Minute", "Trading session start minute", "Session");

		_endHour = Param(nameof(EndHour), 15)
			.SetRange(0, 23)
			.SetDisplay("Session End Hour", "Trading session end hour", "Session");

		_endMinute = Param(nameof(EndMinute), 45)
			.SetRange(0, 59)
			.SetDisplay("Session End Minute", "Trading session end minute", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_prevFast = 0m;
		_prevSlow = 0m;
		_prevSet = false;
		_entryPrice = 0m;
		_stopLoss = 0m;
		_takeProfit = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaFast = new ExponentialMovingAverage { Length = EmaFastLength };
		_emaSlow = new ExponentialMovingAverage { Length = EmaSlowLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_vwap = new VWAP();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaFast, _emaSlow, _rsi, _vwap, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal rsi, decimal vwap)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed || !_rsi.IsFormed || !_vwap.IsFormed)
			return;

		var time = candle.OpenTime.TimeOfDay;
		var start = new TimeSpan(StartHour, StartMinute, 0);
		var end = new TimeSpan(EndHour, EndMinute, 0);
		var inSession = time >= start && time <= end;

		if (!inSession)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
			return;
		}

		if (!_prevSet)
		{
			_prevFast = emaFast;
			_prevSlow = emaSlow;
			_prevSet = true;
			return;
		}

		var crossover = _prevFast <= _prevSlow && emaFast > emaSlow;
		var crossunder = _prevFast >= _prevSlow && emaFast < emaSlow;

		_prevFast = emaFast;
		_prevSlow = emaSlow;

		var longCondition = crossover && rsi < RsiOverbought && candle.ClosePrice > vwap;
		var shortCondition = crossunder && rsi > RsiOversold && candle.ClosePrice < vwap;

		if (longCondition && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopLoss = _entryPrice * (1 - StopLossPerc / 100m);
			_takeProfit = _entryPrice * (1 + TakeProfitPerc / 100m);
			BuyMarket();
		}
		else if (shortCondition && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopLoss = _entryPrice * (1 + StopLossPerc / 100m);
			_takeProfit = _entryPrice * (1 - TakeProfitPerc / 100m);
			SellMarket();
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
				BuyMarket();
		}
	}
}

