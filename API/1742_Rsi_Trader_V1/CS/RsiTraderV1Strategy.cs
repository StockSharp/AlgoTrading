using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI-based trading strategy.
/// Buys when RSI crosses above <see cref="BuyPoint"/> after staying below for two bars.
/// Sells when RSI crosses below <see cref="SellPoint"/> after staying above for two bars.
/// Trades only within the specified time range and supports optional closing of opposite positions.
/// </summary>
public class RsiTraderV1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _buyPoint;
	private readonly StrategyParam<decimal> _sellPoint;
	private readonly StrategyParam<bool> _closeOnOpposite;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private RelativeStrengthIndex _rsi;
	private decimal _prevRsi;
	private decimal _prevPrevRsi;
	private bool _hasPrev;
	private bool _hasPrevPrev;

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold to open long position.
	/// </summary>
	public decimal BuyPoint
	{
		get => _buyPoint.Value;
		set => _buyPoint.Value = value;
	}

	/// <summary>
	/// RSI threshold to open short position.
	/// </summary>
	public decimal SellPoint
	{
		get => _sellPoint.Value;
		set => _sellPoint.Value = value;
	}

	/// <summary>
	/// Indicates whether to close opposite positions when a new signal appears.
	/// </summary>
	public bool CloseOnOpposite
	{
		get => _closeOnOpposite.Value;
		set => _closeOnOpposite.Value = value;
	}

	/// <summary>
	/// Start hour for trading (0-23).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// End hour for trading (0-23).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Take profit value in price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss value in price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public RsiTraderV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Calculation period", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_buyPoint = Param(nameof(BuyPoint), 30m)
			.SetDisplay("Buy Threshold", "RSI level for long entry", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_sellPoint = Param(nameof(SellPoint), 70m)
			.SetDisplay("Sell Threshold", "RSI level for short entry", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_closeOnOpposite = Param(nameof(CloseOnOpposite), true)
			.SetDisplay("Close On Opposite", "Close opposite position on signal", "General");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Trading start hour", "Time");

		_endHour = Param(nameof(EndHour), 23)
			.SetDisplay("End Hour", "Trading end hour", "Time");

		_takeProfit = Param(nameof(TakeProfit), 0.005m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.02m, 0.001m);

		_stopLoss = Param(nameof(StopLoss), 0.005m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.02m, 0.001m);
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

		_prevRsi = 0m;
		_prevPrevRsi = 0m;
		_hasPrev = false;
		_hasPrevPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(TakeProfit, UnitTypes.Price), new Unit(StopLoss, UnitTypes.Price));
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
			return;

		var hour = candle.OpenTime.Hour;
		var inTradingTime = hour >= StartHour && hour <= EndHour;
		if (!inTradingTime)
			return;

		if (!_hasPrev)
		{
			_prevRsi = rsiValue;
			_hasPrev = true;
			return;
		}

		if (!_hasPrevPrev)
		{
			_prevPrevRsi = _prevRsi;
			_prevRsi = rsiValue;
			_hasPrevPrev = true;
			return;
		}

		var longSignal = rsiValue > BuyPoint && _prevRsi < BuyPoint && _prevPrevRsi < BuyPoint;
		var shortSignal = rsiValue < SellPoint && _prevRsi > SellPoint && _prevPrevRsi > SellPoint;

		if (longSignal)
		{
			if (CloseOnOpposite && Position < 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Position <= 0)
				BuyMarket(Volume);
		}
		else if (shortSignal)
		{
			if (CloseOnOpposite && Position > 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (Position >= 0)
				SellMarket(Volume);
		}

		_prevPrevRsi = _prevRsi;
		_prevRsi = rsiValue;
	}
}

