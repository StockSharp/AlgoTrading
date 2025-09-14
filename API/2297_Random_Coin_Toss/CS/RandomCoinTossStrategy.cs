using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that tosses a virtual coin to decide trade direction.
/// A long position is opened on heads, a short position on tails.
/// Each trade is protected by take-profit and stop-loss distances.
/// </summary>
public class RandomCoinTossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTimeSeed;
	private readonly StrategyParam<DataType> _candleType;

	private Random _random;

	/// <summary>
	/// Take-profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Use current time as random seed.
	/// </summary>
	public bool UseTimeSeed
	{
		get => _useTimeSeed.Value;
		set => _useTimeSeed.Value = value;
	}

	/// <summary>
	/// Type of candles used for the coin toss evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RandomCoinTossStrategy"/> class.
	/// </summary>
	public RandomCoinTossStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take-profit distance in price units", "Risk Management");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop-loss distance in price units", "Risk Management");

		_useTimeSeed = Param(nameof(UseTimeSeed), true)
			.SetDisplay("Use Time Seed", "Seed random generator from current time", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
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
		_random = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var seed = UseTimeSeed ? Environment.TickCount : 1;
		_random = new Random(seed);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(new Unit(TakeProfit, UnitTypes.Absolute), new Unit(StopLoss, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		var coin = _random.Next(2);

		if (coin == 0)
			BuyMarket(Volume + Math.Abs(Position));
		else
			SellMarket(Volume + Math.Abs(Position));
	}
}
