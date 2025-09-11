using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades when price crosses above the top Central Pivot Range level.
/// Places take profit at a fixed target and stop loss at the pivot level.
/// </summary>
public class PavanCprStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitTarget;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _todayPivot;
	private decimal _todayTop;
	private decimal _lastClose;
	private decimal _takeProfitPrice;
	private decimal _stopLossPrice;

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfitTarget
	{
		get => _takeProfitTarget.Value;
		set => _takeProfitTarget.Value = value;
	}

	/// <summary>
	/// Candle type used for trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public PavanCprStrategy()
	{
		_takeProfitTarget = Param(nameof(TakeProfitTarget), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in price points", "General")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for entry logic", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_todayPivot = 0m;
		_todayTop = 0m;
		_lastClose = 0m;
		_takeProfitPrice = 0m;
		_stopLossPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var daily = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		daily.Bind(ProcessDaily).Start();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessMain).Start();
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var pivot = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var top = (candle.HighPrice + candle.LowPrice) / 2m;

		_todayPivot = pivot;
		_todayTop = top;
	}

	private void ProcessMain(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_lastClose < _todayTop && candle.ClosePrice > _todayTop && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_takeProfitPrice = candle.ClosePrice + TakeProfitTarget;
			_stopLossPrice = _todayPivot;
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopLossPrice)
				SellMarket(Position);
			else if (candle.HighPrice >= _takeProfitPrice)
				SellMarket(Position);
		}

		_lastClose = candle.ClosePrice;
	}
}
