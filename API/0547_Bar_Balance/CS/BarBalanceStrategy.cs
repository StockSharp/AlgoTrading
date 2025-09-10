namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Bar balance strategy.
/// Buys when intrabar price balance is positive and sells when negative.
/// </summary>
public class BarBalanceStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private SimpleMovingAverage _balanceMa;

	/// <summary>
	/// Smoothing period for bar balance.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BarBalanceStrategy"/>.
	/// </summary>
	public BarBalanceStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Balance MA Length", "Period for bar balance average", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_balanceMa = new SimpleMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _balanceMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range == 0)
			return;

		var up = candle.ClosePrice - candle.LowPrice;
		var down = candle.HighPrice - candle.ClosePrice;
		var balance = (up - down) / range;

		var maValue = _balanceMa.Process(balance, candle.ServerTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading() || !_balanceMa.IsFormed)
			return;

		if (balance > 0 && maValue > 0 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (balance < 0 && maValue < 0 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
