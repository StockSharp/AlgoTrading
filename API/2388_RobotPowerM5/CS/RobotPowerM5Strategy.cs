using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bulls and Bears Power crossover strategy with trailing stop.
/// Buys when Bulls Power plus Bears Power is positive and sells when negative.
/// Applies fixed take profit and stop loss with trailing adjustment.
/// </summary>
public class RobotPowerM5Strategy : Strategy
{
	private readonly StrategyParam<int> _bullBearPeriod;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Period for Bulls Power and Bears Power.
	/// </summary>
	public int BullBearPeriod
	{
		get => _bullBearPeriod.Value;
		set => _bullBearPeriod.Value = value;
	}

	/// <summary>
	/// Trailing step distance.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
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
	/// Initializes a new instance of <see cref="RobotPowerM5Strategy"/>.
	/// </summary>
	public RobotPowerM5Strategy()
	{
		_bullBearPeriod = Param(nameof(BullBearPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Bull/Bear Period", "Bulls and Bears Power period", "Indicators");

		_trailingStep = Param(nameof(TrailingStep), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Trailing stop step size", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 150m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance", "Risk");

		_stopLoss = Param(nameof(StopLoss), 105m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var bulls = new BullsPower { Length = BullBearPeriod };
		var bears = new BearsPower { Length = BullBearPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bulls, bears, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bulls);
			DrawIndicator(area, bears);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal bullsValue, decimal bearsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sum = bullsValue + bearsValue;

		if (Position == 0)
		{
			if (sum > 0)
			{
				BuyMarket();
				_stopPrice = candle.ClosePrice - StopLoss;
				_takePrice = candle.ClosePrice + TakeProfit;
			}
			else if (sum < 0)
			{
				SellMarket();
				_stopPrice = candle.ClosePrice + StopLoss;
				_takePrice = candle.ClosePrice - TakeProfit;
			}
			return;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				_stopPrice = 0m;
				_takePrice = 0m;
				return;
			}

			if (candle.ClosePrice - _stopPrice > 2 * TrailingStep)
				_stopPrice = candle.ClosePrice - TrailingStep;
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
				_stopPrice = 0m;
				_takePrice = 0m;
				return;
			}

			if (_stopPrice - candle.ClosePrice > 2 * TrailingStep)
				_stopPrice = candle.ClosePrice + TrailingStep;
		}
	}
}
