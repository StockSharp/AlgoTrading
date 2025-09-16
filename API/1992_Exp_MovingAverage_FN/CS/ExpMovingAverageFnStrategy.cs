using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exponential moving average slope reversal strategy.
/// Enters long when the EMA turns up after falling.
/// Enters short when the EMA turns down after rising.
/// Supports optional stop-loss and take-profit in price units.
/// </summary>
public class ExpMovingAverageFnStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevValue;
	private decimal _prevPrevValue;
	private decimal _entryPrice;
	private bool _isInitialized;

	/// <summary>
	/// EMA period length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Absolute stop-loss value.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Absolute take-profit value.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type for EMA calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ExpMovingAverageFnStrategy()
	{
		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Period of the exponential moving average", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Absolute stop-loss in price units", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(500m, 2000m, 500m);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Absolute take-profit in price units", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1000m, 4000m, 500m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for EMA calculation", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevValue = 0m;
		_prevPrevValue = 0m;
		_entryPrice = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new EMA { Length = Length };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_isInitialized)
		{
		_prevPrevValue = _prevValue;
		_prevValue = emaValue;
		if (_prevPrevValue != 0m)
		_isInitialized = true;
		return;
		}

		var wasFalling = _prevValue < _prevPrevValue;
		var isRising = emaValue > _prevValue;

		if (wasFalling && isRising)
		{
		BuyMarket(Volume + Math.Abs(Position));
		_entryPrice = candle.ClosePrice;
		}
		else if (!wasFalling && !isRising)
		{
		SellMarket(Volume + Math.Abs(Position));
		_entryPrice = candle.ClosePrice;
		}

		CheckRisk(candle);

		_prevPrevValue = _prevValue;
		_prevValue = emaValue;
	}

	private void CheckRisk(ICandleMessage candle)
	{
		if (_entryPrice == 0m)
		return;

		if (Position > 0)
		{
		if (StopLoss > 0m && candle.ClosePrice <= _entryPrice - StopLoss)
		{
		SellMarket(Math.Abs(Position));
		}
		else if (TakeProfit > 0m && candle.ClosePrice >= _entryPrice + TakeProfit)
		{
		SellMarket(Math.Abs(Position));
		}
		}
		else if (Position < 0)
		{
		if (StopLoss > 0m && candle.ClosePrice >= _entryPrice + StopLoss)
		{
		BuyMarket(Math.Abs(Position));
		}
		else if (TakeProfit > 0m && candle.ClosePrice <= _entryPrice - TakeProfit)
		{
		BuyMarket(Math.Abs(Position));
		}
		}
	}
}
