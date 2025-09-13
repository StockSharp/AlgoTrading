using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RAVI based trend strategy.
/// Buys when daily RAVI rises while hourly RAVI is negative, sells on the opposite condition.
/// </summary>
public class MoveCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;

	private SimpleMovingAverage _fastH1;
	private SimpleMovingAverage _slowH1;
	private SimpleMovingAverage _fastD1;
	private SimpleMovingAverage _slowD1;

	private decimal _raviH1;
	private decimal _raviD1;
	private decimal _raviD1Prev1;
	private decimal _raviD1Prev2;
	private decimal _raviD1Prev3;

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MoveCrossStrategy"/> class.
	/// </summary>
	public MoveCrossStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 50)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit target in points", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_stopLoss = Param(nameof(StopLoss), 100)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Loss limit in points", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 400, 20);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[]
		{
			(Security, TimeSpan.FromHours(1).TimeFrame()),
			(Security, TimeSpan.FromDays(1).TimeFrame())
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_raviH1 = 0m;
		_raviD1 = 0m;
		_raviD1Prev1 = 0m;
		_raviD1Prev2 = 0m;
		_raviD1Prev3 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastH1 = new SimpleMovingAverage { Length = 2 };
		_slowH1 = new SimpleMovingAverage { Length = 24 };
		_fastD1 = new SimpleMovingAverage { Length = 2 };
		_slowD1 = new SimpleMovingAverage { Length = 24 };

		var h1 = SubscribeCandles(TimeSpan.FromHours(1).TimeFrame());
		h1.Bind(_fastH1, _slowH1, ProcessH1).Start();

		var d1 = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		d1.Bind(_fastD1, _slowD1, ProcessD1).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, h1);
			DrawOwnTrades(area);
		}
	}

	private void ProcessH1(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_fastH1.IsFormed || !_slowH1.IsFormed)
		return;

		_raviH1 = slow == 0m ? 0m : (fast - slow) / slow * 100m;

		TryTrade(candle);
	}

	private void ProcessD1(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_fastD1.IsFormed || !_slowD1.IsFormed)
		return;

		var newRavi = slow == 0m ? 0m : (fast - slow) / slow * 100m;

		_raviD1Prev3 = _raviD1Prev2;
		_raviD1Prev2 = _raviD1Prev1;
		_raviD1Prev1 = _raviD1;
		_raviD1 = newRavi;
	}

	private void TryTrade(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var price = candle.ClosePrice;

		if (_raviH1 < 0m &&
		_raviD1 > 1m &&
		_raviD1Prev1 < _raviD1 &&
		_raviD1Prev2 < _raviD1Prev1 &&
		_raviD1Prev3 < _raviD1Prev2 &&
		Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		if (TakeProfit > 0)
		SetTakeProfit(TakeProfit, price, Position + Volume);
		if (StopLoss > 0)
		SetStopLoss(StopLoss, price, Position + Volume);
		}
		else if (_raviH1 > 0m &&
		_raviD1 < -1m &&
		_raviD1Prev1 > _raviD1 &&
		_raviD1Prev2 > _raviD1Prev1 &&
		_raviD1Prev3 > _raviD1Prev2 &&
		Position >= 0)
		{
		SellMarket(Volume + Math.Max(Position, 0m));
		if (TakeProfit > 0)
		SetTakeProfit(TakeProfit, price, Position - Volume);
		if (StopLoss > 0)
		SetStopLoss(StopLoss, price, Position - Volume);
		}
	}
}
